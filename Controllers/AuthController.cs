using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;

namespace MonitoreoWeb.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Modificación: Login POST
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuario
                .FirstOrDefault(u => u.Email == model.Email && u.Activo);

            if (usuario == null)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos");
                return View(model);
            }

            var passwordHasher = new PasswordHasher<Usuario>();
            var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Password);

            if (resultado == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "Usuario o contraseña incorrectos");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("IdUsuario", usuario.IdUsuario.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.Recordarme
                });

            // **Aquí redirigimos al "Inicio" del AdminController**
            return RedirectToAction("Inicio", "Admin");  // Redirige a "Inicio" en lugar de "Dashboard"
        }
        // Fin de la modificación 

        // Cerrar sesión
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        // Página de acceso denegado
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Vista para registrar un nuevo usuario
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        // Acción para registrar un nuevo usuario
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Registrar(RegistroUsuarioViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existe = _context.Usuario.FirstOrDefault(u => u.Email == model.Email);

            if (existe != null)
            {
                ModelState.AddModelError("", "Ya existe un usuario con ese correo");
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

            TempData["Success"] = "Usuario registrado correctamente";
            return RedirectToAction("Registrar");
        }
    }
}