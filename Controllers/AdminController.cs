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

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
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
                .FirstOrDefault(r =>
                    r.IdReparacion == id);

            if (reparacion == null)
            {
                return NotFound();
            }

            return View(reparacion);
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
    }
}