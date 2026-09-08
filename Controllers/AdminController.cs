using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;
using Microsoft.EntityFrameworkCore; // Uso del entity


namespace MonitoreoWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IActionResult Inicio()
        {
            return View();
        }
        public IActionResult Reparaciones(string buscar, int? estado)
        {
            var query = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Tecnico)
                .AsQueryable();

            // ==========================================
            // BÚSQUEDA
            // ==========================================

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                query = query.Where(r =>
                    r.TokenConsulta!.Contains(buscar) ||

                    r.Dispositivo!.Cliente!.Nombre.Contains(buscar) ||

                    r.Dispositivo.Cliente.Apellido.Contains(buscar) ||

                    r.Dispositivo.Marca.Contains(buscar) ||

                    r.Dispositivo.Modelo.Contains(buscar)
                );
            }

            // ==========================================
            // FILTRO POR ESTADO
            // ==========================================

            if (estado.HasValue)
            {
                query = query.Where(r =>
                    r.IdEstado == estado.Value);
            }

            var reparaciones = query
                .OrderByDescending(r => r.FechaIngreso)
                .ToList();

            ViewBag.Buscar = buscar;
            ViewBag.Estado = estado;

            return View(reparaciones);
        }

        // ==========================================================
        // DETALLE DE REPARACIÓN
        // ==========================================================

        public IActionResult DetalleReparacion(int id)
        {
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Tecnico)
                .Include(r => r.Pagos)
                .FirstOrDefault(r =>
                    r.IdReparacion == id);

            if (reparacion == null)
            {
                return NotFound();
            }

            var avances = _context.HistorialAvance
                .Include(a => a.Fotos)
                .Include(a => a.Usuario)
                .Where(a => a.IdReparacion == id)
                .OrderByDescending(a => a.Fecha)
                .ToList();

            bool puedeAgregar =
                reparacion.Activo &&
                reparacion.IdEstado != 7;

            ViewBag.Bitacora = new MonitoreoWeb.Models.ViewModels.BitacoraViewModel
            {
                IdReparacion = id,
                Avances = avances,
                EsVistaCliente = false,
                PuedeAgregar = puedeAgregar,
                ControladorDestino = "Admin"
            };

            return View(reparacion);
        }

        // ==========================================================
        // REGISTRAR PAGO
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarPago(int id, decimal monto)
        {
            var reparacion = _context.Reparacion
                .Include(r => r.Pagos)
                .FirstOrDefault(r => r.IdReparacion == id);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (monto <= 0)
            {
                TempData["Error"] = "El monto del pago debe ser mayor a cero.";
                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            decimal costoBase = reparacion.CostoFinal ?? reparacion.CostoEstimado ?? 0;
            decimal totalPagado = reparacion.Pagos?.Sum(p => p.Monto) ?? 0;
            decimal saldoPendiente = costoBase - totalPagado;

            if (monto > saldoPendiente)
            {
                TempData["Error"] =
                    $"El pago (${monto:N2}) no puede ser mayor al saldo pendiente (${saldoPendiente:N2}).";

                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            _context.Pago.Add(new Pago
            {
                IdReparacion = id,
                Monto = monto,
                FechaPago = DateTime.Now
            });

            _context.SaveChanges();

            TempData["Success"] = "El pago se registró correctamente.";

            return RedirectToAction("DetalleReparacion", new { id = id });
        }

        // ==========================================================
        // AGREGAR AVANCE (BITÁCORA) - ADMIN
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAvance(
            int id,
            string descripcionAvance,
            List<IFormFile> fotos)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idUsuario))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado == 7)
            {
                TempData["Error"] =
                    "No se puede agregar una actualización a una reparación ya terminada.";

                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            if (string.IsNullOrWhiteSpace(descripcionAvance))
            {
                TempData["Error"] =
                    "Debes escribir una descripción para la actualización.";

                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            // ==========================================
            // 1. REGISTRAR EL AVANCE
            // ==========================================

            var avance = new HistorialAvance
            {
                IdReparacion = id,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Descripcion = descripcionAvance.Trim()
            };

            _context.HistorialAvance.Add(avance);
            _context.SaveChanges();

            // ==========================================
            // 2. GUARDAR LAS FOTOS (SI LAS HAY)
            // ==========================================

            if (fotos != null && fotos.Count > 0)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                var carpeta = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "reparaciones",
                    id.ToString());

                Directory.CreateDirectory(carpeta);

                foreach (var foto in fotos)
                {
                    if (foto == null || foto.Length == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(foto.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        continue;
                    }

                    var nombreUnico = $"{Guid.NewGuid()}{extension}";
                    var rutaFisica = Path.Combine(carpeta, nombreUnico);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await foto.CopyToAsync(stream);
                    }

                    _context.FotoAvance.Add(new FotoAvance
                    {
                        IdReparacion = id,
                        IdAvance = avance.IdAvance,
                        IdUsuario = idUsuario,
                        RutaArchivo = $"/uploads/reparaciones/{id}/{nombreUnico}",
                        NombreArchivo = foto.FileName,
                        FechaSubida = DateTime.Now,
                        VisibleCliente = true,
                        Activo = true
                    });
                }

                _context.SaveChanges();
            }

            TempData["Success"] =
                "La actualización se agregó correctamente a la bitácora.";

            return RedirectToAction("DetalleReparacion", new { id = id });
        }

        [HttpGet]
        public IActionResult RegistrarUsuario()
        {
            return View(new RegistroUsuarioViewModel
            {
                Activo = false // ← cambiado a false para que el checkbox inicie desmarcado
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarUsuario(RegistroUsuarioViewModel model)
        {
            Console.WriteLine($">>> Activo recibido: {model.Activo}"); // ← debug temporal

            if (!ModelState.IsValid)
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors);
                foreach (var error in errores)
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(model);
            }

            if (model.Password != model.ConfirmarPassword)
            {
                ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");
                return View(model);
            }

            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Rol = model.Rol,
                Activo = model.Activo,
                FechaCreacion = DateTime.Now
            };

            var passwordHasher = new PasswordHasher<Usuario>();
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, model.Password);

            _context.Usuario.Add(usuario);
            _context.SaveChanges();

            TempData["Success"] = "Usuario registrado correctamente.";
            return RedirectToAction("RegistrarUsuario", "Admin");
        }

        public IActionResult Tecnicos()
        {
            var tecnicos = _context.Usuario
                .Where(u => u.Rol == "Tecnico")
                .OrderBy(u => u.Nombre)
                .ToList();
            return View(tecnicos);
        }

        public IActionResult DispositivosReparacion()
        {
            return View();
        }

        public IActionResult GenerarToken()
        {
            return View();
        }

        public IActionResult Activaciones()
        {
            return View();
        }

        public IActionResult Administradores()
        {
            var admins = _context.Usuario
                .Where(u => u.Rol == "Admin")
                .OrderBy(u => u.Nombre)
                .ToList();
            return View(admins);
        }

        // ==========================================================
        // TICKET IMPRIMIBLE
        // ==========================================================

        public IActionResult Ticket(int id)
        {
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Tecnico)
                .Include(r => r.Pagos)
                .FirstOrDefault(r => r.IdReparacion == id);

            if (reparacion == null)
            {
                return NotFound();
            }

            return View(reparacion);
        }
    }
}