using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;

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

        [HttpGet]
        public IActionResult RegistrarUsuario()
        {
            return View(new RegistroUsuarioViewModel
            {
                Activo = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RegistrarUsuario(RegistroUsuarioViewModel model)
        {
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

            // Verificar que las contraseñas coincidan
            if (model.Password != model.ConfirmarPassword)
            {
                ModelState.AddModelError("ConfirmarPassword", "Las contraseñas no coinciden.");
                return View(model);
            }

            // Crear nuevo objeto de usuario
            var usuario = new Usuario
            {
                Nombre = model.Nombre,
                Email = model.Email,
                Rol = model.Rol,
                Activo = model.Activo,
                FechaCreacion = DateTime.Now
            };

            // Hashear la contraseña
            var passwordHasher = new PasswordHasher<Usuario>();
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, model.Password);

            // Agregar el usuario a la base de datos
            _context.Usuario.Add(usuario);
            _context.SaveChanges();

            // Agregar mensaje de éxito
            TempData["Success"] = "Usuario registrado correctamente.";

            // Redirigir a la vista de registro
            return RedirectToAction("RegistrarUsuario", "Admin");
        }

        public IActionResult Tecnicos()
        {
            var tecnicos = _context.Usuario
                .Where(u => u.Rol == "Tecnico" && u.Activo == true)
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
    }
}