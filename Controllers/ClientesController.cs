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
        [HttpPost]
        public async Task<IActionResult> Registrar(Cliente model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ==========================================
            // NORMALIZAR DATOS (quitar espacios)
            // ==========================================

            var email = model.Email?.Trim() ?? "";
            var telefono = model.Telefono?.Trim() ?? "";
            var nombre = model.Nombre?.Trim() ?? "";
            var apellido = model.Apellido?.Trim() ?? "";

            // Guardamos los valores ya normalizados en el modelo
            // para que lo que se guarde en la BD también quede limpio
            model.Email = email;
            model.Telefono = telefono;
            model.Nombre = nombre;
            model.Apellido = apellido;

            // ==========================================
            // VALIDAR DUPLICADOS
            // (usamos Trim() también sobre lo ya guardado,
            // por si hay registros viejos con espacios)
            // ==========================================

            var duplicado = _context.Cliente.FirstOrDefault(c =>
                c.Activo &&
                (
                    c.Email.Trim().ToLower() == email.ToLower() ||
                    c.Telefono.Trim() == telefono ||
                    (c.Nombre.Trim().ToLower() == nombre.ToLower() &&
                     c.Apellido.Trim().ToLower() == apellido.ToLower())
                ));

            if (duplicado != null)
            {
                string motivo;

                if (duplicado.Email.Trim().ToLower() == email.ToLower())
                {
                    motivo = "Ya existe un cliente registrado con ese correo electrónico.";
                }
                else if (duplicado.Telefono.Trim() == telefono)
                {
                    motivo = "Ya existe un cliente registrado con ese teléfono.";
                }
                else
                {
                    motivo = "Ya existe un cliente registrado con ese nombre y apellido.";
                }

                ModelState.AddModelError("", motivo);

                return View(model);
            }

            // Generar token único
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            model.FechaRegistro = DateTime.Now;

            _context.Cliente.Add(model);
            await _context.SaveChangesAsync();

            // Enviar correo de verificación
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            await _emailService.EnviarVerificacionAsync(model.Email, token, baseUrl);

            TempData["Success"] = "Cliente registrado. Se envió un correo para verificar el email.";
            return RedirectToAction("Registrar");
        }
    }
}