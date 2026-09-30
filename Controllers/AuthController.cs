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

        // =========================
        // LOGIN GET
        // =========================
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Inicio", "Admin");
                }
                else if (User.IsInRole("Tecnico"))
                {
                    return RedirectToAction("Index", "Tecnico");
                }
            }

            return View();
        }

        // =========================
        // LOGIN POST
        // =========================
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var usuario = _context.Usuario
                .FirstOrDefault(u =>
                    u.Email == model.Email &&
                    u.Activo);

            if (usuario == null)
            {
                ModelState.AddModelError("",
                    "Usuario o contraseña incorrectos");

                return View(model);
            }

            var passwordHasher =
                new PasswordHasher<Usuario>();

            var resultado =
                passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.PasswordHash,
                    model.Password
                );

            if (resultado == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("",
                    "Usuario o contraseña incorrectos");

                return View(model);
            }

            // ✅ AVISO Y VALIDACIÓN DE CAMBIO OBLIGATORIO DE CONTRASEÑA
            if (usuario.DebeCambiarPassword)
            {
                // Creamos una sesión temporal o autenticación parcial para que pueda cambiar su clave
                var tempClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol),
                    new Claim("IdUsuario", usuario.IdUsuario.ToString())
                };

                var tempIdentity = new ClaimsIdentity(tempClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(tempIdentity));

                TempData["Warning"] = "⚠️ Por seguridad de los datos del taller, debes cambiar tu contraseña predeterminada antes de continuar.";
                return RedirectToAction("CambiarPassword", "Auth");
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("IdUsuario",
                    usuario.IdUsuario.ToString())
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal =
                new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = model.Recordarme
                });
            // REDIRECCIÓN SEGÚN EL ROL
            if (usuario.Rol == "Admin")
            {
                return RedirectToAction("Inicio", "Admin");
            }
            else if (usuario.Rol == "Tecnico")
            {
                return RedirectToAction("Solicitudes", "Tecnico");
            }
            return RedirectToAction("Login");
        }

        // =========================
        // LOGOUT
        // =========================
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            return RedirectToAction("Login", "Auth");
        }

        // =========================
        // ACCESS DENIED
        // =========================
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================
        // CAMBIAR PASSWORD GET
        // =========================
        [Authorize]
        [HttpGet]
        public IActionResult CambiarPassword()
        {
            return View();
        }

        // =========================
        // CAMBIAR PASSWORD POST
        // =========================
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var claimId = User.FindFirst("IdUsuario")?.Value;
            if (string.IsNullOrEmpty(claimId))
                return RedirectToAction("Login", "Auth");

            int idUsuario = int.Parse(claimId);
            var usuario = _context.Usuario.FirstOrDefault(u => u.IdUsuario == idUsuario);

            if (usuario == null)
                return NotFound();

            var passwordHasher = new PasswordHasher<Usuario>();
            var resultado = passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.PasswordActual);

            if (resultado == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("", "La contraseña actual es incorrecta.");
                return View(model);
            }

            // Actualizar contraseña y desactivar la bandera de cambio obligatorio
            usuario.PasswordHash = passwordHasher.HashPassword(usuario, model.PasswordNueva);
            usuario.DebeCambiarPassword = false;

            _context.Usuario.Update(usuario);
            await _context.SaveChangesAsync();

            TempData["Success"] = "¡Contraseña actualizada correctamente!";

            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Inicio", "Admin");
            }
            else if (User.IsInRole("Tecnico"))
            {
                return RedirectToAction("Index", "Tecnico");
            }

            return RedirectToAction("Login", "Auth");
        }

        // =========================
        // REGISTRAR GET
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        // =========================
        // REGISTRAR POST
        // =========================
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Registrar(
            RegistroUsuarioViewModel model)
        {
            // VALIDACIÓN MVC
            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    var campo = item.Key;

                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine(
                            $"CAMPO: {campo}"
                        );

                        Console.WriteLine(
                            $"ERROR: {error.ErrorMessage}"
                        );
                    }
                }

                return View(model);
            }

            // VALIDAR EMAIL EXISTENTE
            var existe = _context.Usuario
                .FirstOrDefault(u =>
                    u.Email == model.Email);

            if (existe != null)
            {
                ModelState.AddModelError("",
                    "Ya existe un usuario con ese correo");

                return View(model);
            }

            try
            {
                // CREAR USUARIO
                var usuario = new Usuario
                {
                    Nombre = model.Nombre,
                    Email = model.Email,
                    PasswordHash = "",
                    Rol = string.IsNullOrEmpty(model.Rol) ? "Usuario" : model.Rol,
                    Activo = model.Activo,
                    DebeCambiarPassword = true, // 👈 Obliga al nuevo usuario a cambiar su contraseña en su primer inicio de sesión
                    FechaCreacion = DateTime.Now
                };

                // HASH PASSWORD
                var passwordHasher =
                    new PasswordHasher<Usuario>();

                usuario.PasswordHash =
                    passwordHasher.HashPassword(
                        usuario,
                        model.Password
                    );

                // GUARDAR
                _context.Usuario.Add(usuario);

                _context.SaveChanges();

                Console.WriteLine(
                    "USUARIO GUARDADO CORRECTAMENTE"
                );

                TempData["Success"] =
                    "Usuario registrado correctamente";

                // QUEDARSE EN REGISTRAR
                return RedirectToAction("Registrar");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "ERROR AL GUARDAR:"
                );

                Console.WriteLine(ex.ToString());

                ModelState.AddModelError("",
                    "Ocurrió un error al registrar el usuario");

                return View(model);
            }
        }
    }
}