using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

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

        // ==========================================
        // CREAR REPARACIÓN (CON ANTICIPO Y MÉTODO DE PAGO)
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearReparacion(Reparacion reparacion, decimal? anticipo, string? metodoPagoAnticipo)
        {
            // 1. Capturamos de forma segura el ID del usuario desde la sesión actual
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            int idAdminLogueado = 1; // Valor por defecto de respaldo si el claim no viniera

            if (!string.IsNullOrEmpty(idUsuarioClaim) && int.TryParse(idUsuarioClaim, out int idParsed))
            {
                idAdminLogueado = idParsed;
            }

            if (ModelState.IsValid)
            {
                reparacion.FechaIngreso = DateTime.Now;
                reparacion.Activo = true;

                // ⚡ ASIGNACIÓN DIRECTA ANTES DE AGREGAR AL CONTEXTO
                reparacion.IdUsuario = idAdminLogueado;

                // 2. Guardar la reparación
                _context.Reparacion.Add(reparacion);
                _context.SaveChanges(); // Aquí se genera el IdReparacion

                // 3. Generar el token de consulta
                var token = new TokenConsulta
                {
                    IdReparacion = reparacion.IdReparacion,
                    CodigoUnico = "REP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    FechaCreacion = DateTime.Now,
                    Activo = true
                };
                _context.TokenConsulta.Add(token);

                // 4. Registrar anticipo si existe
                if (anticipo.HasValue && anticipo.Value > 0)
                {
                    var primerPago = new Pago
                    {
                        IdReparacion = reparacion.IdReparacion,
                        Monto = anticipo.Value,
                        FechaPago = DateTime.Now,
                        MetodoPago = string.IsNullOrWhiteSpace(metodoPagoAnticipo) ? "Efectivo" : metodoPagoAnticipo.Trim()
                    };
                    _context.Pago.Add(primerPago);
                }

                _context.SaveChanges();

                TempData["Success"] = $"Reparación #{reparacion.IdReparacion} registrada con éxito.";
                return RedirectToAction("Reparaciones");
            }

            ViewBag.Clientes = _context.Cliente.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            ViewBag.Tecnicos = _context.Usuario.Where(u => u.Rol == "Tecnico" && u.Activo).OrderBy(u => u.Nombre).ToList();
            return View(reparacion);
        }

        // ==========================================
        // LISTADO Y BÚSQUEDA DE REPARACIONES
        // ==========================================

        public IActionResult Reparaciones(string buscar, int? estado)
        {
            var query = _context.Reparacion
                .Include(r => r.Tokens)                   // 👈 Carga el Token activo
                .Include(r => r.Dispositivo)                // 👈 Carga Marca y Modelo
                    .ThenInclude(d => d.Cliente)            // 👈 Carga Nombre y Apellido del Cliente
                .Include(r => r.Tecnico)                    // 👈 Carga el Técnico asignado
                .AsQueryable();

            // ==========================================
            // BÚSQUEDA (INCLUYE CONSULTA EN TokenConsulta)
            // ==========================================

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim();

                query = query.Where(r =>
                    _context.TokenConsulta.Any(t => t.IdReparacion == r.IdReparacion && t.CodigoUnico.Contains(buscar)) ||
                    (r.Dispositivo != null && r.Dispositivo.Cliente != null &&
                        (r.Dispositivo.Cliente.Nombre.Contains(buscar) || r.Dispositivo.Cliente.Apellido.Contains(buscar))) ||
                    (r.Dispositivo != null && (r.Dispositivo.Marca.Contains(buscar) || r.Dispositivo.Modelo.Contains(buscar)))
                );
            }

            // ==========================================
            // FILTRO POR ESTADO
            // ==========================================

            if (estado.HasValue)
            {
                query = query.Where(r => r.IdEstado == estado.Value);
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
                .FirstOrDefault(r => r.IdReparacion == id);

            if (reparacion == null)
            {
                return NotFound();
            }

            // Obtener el token activo correspondiente desde TokenConsulta
            var tokenActivo = _context.TokenConsulta
                .FirstOrDefault(t => t.IdReparacion == id && t.Activo)?.CodigoUnico ?? "SIN-TOKEN";

            ViewBag.TokenConsulta = tokenActivo;

            var avances = _context.HistorialAvance
                .Include(a => a.Fotos)
                .Include(a => a.Usuario)
                .Where(a => a.IdReparacion == id)
                .OrderByDescending(a => a.Fecha)
                .ToList();

            bool puedeAgregar = reparacion.Activo && reparacion.IdEstado != 7 && reparacion.IdEstado != 8;

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
        // CAMBIAR ESTADO DE LA REPARACIÓN (7: Terminada, 8: Entregada)
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CambiarEstado(int idReparacion, int nuevoEstado)
        {
            var reparacion = _context.Reparacion.FirstOrDefault(r => r.IdReparacion == idReparacion);

            if (reparacion == null)
            {
                return NotFound();
            }

            reparacion.IdEstado = nuevoEstado;

            // ESTADO 7: Terminada (El técnico terminó el trabajo en taller)
            if (nuevoEstado == 7 && !reparacion.FechaTerminacion.HasValue)
            {
                reparacion.FechaTerminacion = DateTime.Now;
            }

            // ESTADO 8: Entregada (El cliente recoge físicamente su equipo)
            if (nuevoEstado == 8 && !reparacion.FechaEntregaCliente.HasValue)
            {
                reparacion.FechaEntregaCliente = DateTime.Now;

                // Si por alguna razón pasó directo a entregada sin marcar terminada:
                if (!reparacion.FechaTerminacion.HasValue)
                {
                    reparacion.FechaTerminacion = DateTime.Now;
                }
            }

            _context.SaveChanges();

            TempData["Success"] = "El estado de la reparación fue actualizado correctamente.";
            return RedirectToAction("DetalleReparacion", new { id = idReparacion });
        }

        // ==========================================================
        // APLICAR GARANTÍA
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AplicarGarantia(int idReparacionOriginal, string motivoGarantia)
        {
            if (string.IsNullOrWhiteSpace(motivoGarantia))
            {
                TempData["Error"] = "Debes especificar el motivo o falla por el cual aplica la garantía.";
                return RedirectToAction("DetalleReparacion", new { id = idReparacionOriginal });
            }

            var reparacionOriginal = _context.Reparacion
                .Include(r => r.Dispositivo)
                .FirstOrDefault(r => r.IdReparacion == idReparacionOriginal);

            if (reparacionOriginal == null)
            {
                return NotFound();
            }

            // 1. VALIDACIÓN DE FECHA DE GARANTÍA
            if (reparacionOriginal.FechaEntregaCliente.HasValue)
            {
                var fechaLimite = reparacionOriginal.FechaEntregaCliente.Value.AddDays(reparacionOriginal.DiasGarantia);
                if (DateTime.Now > fechaLimite)
                {
                    TempData["Error"] = $"La garantía expiró el {fechaLimite:dd/MM/yyyy}.";
                    return RedirectToAction("DetalleReparacion", new { id = idReparacionOriginal });
                }
            }

            // 2. OBTENER EL ID DEL USUARIO QUE AUTORIZA LA GARANTÍA
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            int idAdminGarantia = 1;

            if (!string.IsNullOrEmpty(idUsuarioClaim) && int.TryParse(idUsuarioClaim, out int idParsedGarantia))
            {
                idAdminGarantia = idParsedGarantia;
            }

            // 3. CREAR LA NUEVA ORDEN DE GARANTÍA ENLAZADA
            var nuevaGarantia = new Reparacion
            {
                IdDispositivo = reparacionOriginal.IdDispositivo,
                IdEstado = 1, // Pendiente / Ingresado
                FechaIngreso = DateTime.Now,
                ProblemaReportado = $"[RECLAMO DE GARANTÍA - ORDEN #{idReparacionOriginal}]: {motivoGarantia.Trim()}",
                CostoEstimado = 0,
                CostoFinal = 0,
                Activo = true,
                EsGarantia = true,
                IdReparacionOriginal = idReparacionOriginal,
                DiasGarantia = 0,
                IdUsuario = idAdminGarantia
            };

            _context.Reparacion.Add(nuevaGarantia);
            _context.SaveChanges();

            // 4. REGISTRAR TOKEN EN LA TABLA TokenConsulta
            var tokenGarantia = new TokenConsulta
            {
                IdReparacion = nuevaGarantia.IdReparacion,
                CodigoUnico = "GAR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                FechaCreacion = DateTime.Now,
                Activo = true
            };

            _context.TokenConsulta.Add(tokenGarantia);
            _context.SaveChanges();

            TempData["Success"] = $"Se generó la orden de garantía #{nuevaGarantia.IdReparacion} vinculada a la orden original #{idReparacionOriginal}.";

            return RedirectToAction("DetalleReparacion", new { id = nuevaGarantia.IdReparacion });
        }

        // ==========================================================
        // REGISTRAR PAGO
        // ==========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarPago(int id, decimal monto, string metodoPago)
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
                TempData["Error"] = $"El pago (${monto:N2}) no puede ser mayor al saldo pendiente (${saldoPendiente:N2}).";
                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            // REGISTRAR PAGO GUARDANDO EL MÉTODO SELECCIONADO
            _context.Pago.Add(new Pago
            {
                IdReparacion = id,
                Monto = monto,
                FechaPago = DateTime.Now,
                MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago.Trim()
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
                .FirstOrDefault(r => r.IdReparacion == id && r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado == 7 || reparacion.IdEstado == 8)
            {
                TempData["Error"] = "No se puede agregar una actualización a una reparación ya finalizada o entregada.";
                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            if (string.IsNullOrWhiteSpace(descripcionAvance))
            {
                TempData["Error"] = "Debes escribir una descripción para la actualización.";
                return RedirectToAction("DetalleReparacion", new { id = id });
            }

            // 1. REGISTRAR EL AVANCE
            var avance = new HistorialAvance
            {
                IdReparacion = id,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Descripcion = descripcionAvance.Trim()
            };

            _context.HistorialAvance.Add(avance);
            _context.SaveChanges();

            // 2. GUARDAR LAS FOTOS (SI LAS HAY)
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

            TempData["Success"] = "La actualización se agregó correctamente a la bitácora.";

            return RedirectToAction("DetalleReparacion", new { id = id });
        }

        [HttpGet]
        public IActionResult RegistrarUsuario()
        {
            return View(new RegistroUsuarioViewModel
            {
                Activo = false
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarUsuario(RegistroUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
            {
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

        [HttpGet]
        public IActionResult Ticket(int id)
        {
            var reparacion = _context.Reparacion.FirstOrDefault(r => r.IdReparacion == id);
            if (reparacion == null) return NotFound();

            // Si la reparación ya fue entregada, redirige obligatoriamente al Ticket de Entrega y Garantía
            if (reparacion.IdEstado == 8) // Cambia '8' por el ID numérico de tu estado "Entregado"
            {
                return RedirectToAction("TicketEntrega", "Admin", new { id = id });
            }
            else
            {
                return RedirectToAction("TicketRecepcion", "Servicio", new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LiquidarYEntregar(int idReparacion, decimal montoLiquidar, string metodoPago, string firmaAdminBase64)
        {
            var reparacion = _context.Reparacion
                .Include(r => r.Pagos)
                .FirstOrDefault(r => r.IdReparacion == idReparacion);

            if (reparacion == null) return NotFound();

            // VALIDAR QUE LA REPARACIÓN YA ESTÉ TERMINADA ANTES DE ENTREGARSE
            if (reparacion.IdEstado != 7)
            {
                TempData["Error"] =
                    "La reparación debe estar en estado 'Terminada' antes de poder entregarse al cliente.";

                return RedirectToAction("DetalleReparacion", new { id = idReparacion });
            }

            if (montoLiquidar > 0)
            {
                // VALIDAR QUE EL MONTO NO EXCEDA EL SALDO PENDIENTE
                decimal costoBase = reparacion.CostoFinal ?? reparacion.CostoEstimado ?? 0;
                decimal totalPagadoActual = reparacion.Pagos?.Sum(p => p.Monto) ?? 0;
                decimal saldoPendiente = costoBase - totalPagadoActual;

                if (montoLiquidar > saldoPendiente)
                {
                    TempData["Error"] =
                        $"El monto a liquidar (${montoLiquidar:N2}) no puede ser mayor al saldo pendiente (${saldoPendiente:N2}).";

                    return RedirectToAction("DetalleReparacion", new { id = idReparacion });
                }

                _context.Pago.Add(new Pago
                {
                    IdReparacion = idReparacion,
                    Monto = montoLiquidar,
                    MetodoPago = string.IsNullOrWhiteSpace(metodoPago) ? "Efectivo" : metodoPago,
                    FechaPago = DateTime.Now
                });
            }

            reparacion.IdEstado = 8; // Estado Entregado
            reparacion.FechaEntregaCliente = DateTime.Now;
            reparacion.FirmaAdminEntregaBase64 = firmaAdminBase64;

            if (!reparacion.CostoFinal.HasValue)
            {
                reparacion.CostoFinal = reparacion.CostoEstimado;
            }

            _context.SaveChanges();

            return RedirectToAction("TicketEntrega", "Admin", new { id = idReparacion });
        }

        [HttpGet]
        public IActionResult TicketEntrega(int id)
        {
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Tecnico)
                .Include(r => r.Pagos)
                .FirstOrDefault(r => r.IdReparacion == id);

            if (reparacion == null) return NotFound();

            return View(reparacion);
        }
    }
}