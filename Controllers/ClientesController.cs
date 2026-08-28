using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Services;
using System.Security.Cryptography;

namespace MonitoreoWeb.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public ClientesController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // LISTADO
        public IActionResult Index()
        {
            var clientes = _context.Cliente.ToList();
            return View(clientes);
        }

        // GET REGISTRAR
        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        // POST REGISTRAR
        [HttpPost]
        public async Task<IActionResult> Registrar(Cliente model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Generar token único
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            model.FechaRegistro = DateTime.Now;
            model.EmailVerificado = false;
            model.TokenVerificacion = token;
            model.TokenExpira = DateTime.Now.AddHours(24);

            _context.Cliente.Add(model);
            await _context.SaveChangesAsync();

            // Enviar correo de verificación
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            await _emailService.EnviarVerificacionAsync(model.Email, token, baseUrl);

            TempData["Success"] = "Cliente registrado. Se envió un correo para verificar el email.";
            return RedirectToAction("Registrar");
        }

        // GET VERIFICAR (el link del correo llega aquí)
        [HttpGet]
        public async Task<IActionResult> Verificar(string token)
        {
            var cliente = await _context.Cliente
                .FirstOrDefaultAsync(c => c.TokenVerificacion == token);

            if (cliente == null)
            {
                TempData["Error"] = "El enlace de verificación no es válido.";
                return RedirectToAction("Index");
            }

            if (cliente.TokenExpira < DateTime.Now)
            {
                TempData["Error"] = "El enlace ha expirado. Solicita uno nuevo.";
                return RedirectToAction("Index");
            }

            cliente.EmailVerificado = true;
            cliente.TokenVerificacion = null;
            cliente.TokenExpira = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "¡Correo verificado correctamente!";
            return RedirectToAction("Index");
        }
    }
}