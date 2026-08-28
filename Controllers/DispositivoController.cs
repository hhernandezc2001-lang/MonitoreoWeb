using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;

namespace MonitoreoWeb.Controllers
{
    public class DispositivoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DispositivoController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // DISPOSITIVOS REGISTRADOS
        // ==========================================

        public IActionResult Index()
        {
            var dispositivos = _context.Dispositivo
                .Include(d => d.Cliente)
                .Where(d => d.Activo)
                .Select(d => new DispositivoRegistradoViewModel
                {
                    // =========================
                    // DISPOSITIVO
                    // =========================

                    IdDispositivo = d.IdDispositivo,

                    Marca = d.Marca,

                    Modelo = d.Modelo,

                    IMEI = d.IMEI,

                    Color = d.Color,


                    // =========================
                    // CLIENTE
                    // =========================

                    IdCliente = d.IdCliente,

                    NombreCliente = d.Cliente != null
                        ? d.Cliente.Nombre
                        : "Sin cliente",

                    ApellidoCliente = d.Cliente != null
                        ? d.Cliente.Apellido
                        : "",

                    TelefonoCliente = d.Cliente != null
                        ? d.Cliente.Telefono
                        : "Sin teléfono",

                    EmailCliente = d.Cliente != null
                        ? d.Cliente.Email
                        : "Sin correo"
                })
                .ToList();

            return View(dispositivos);
        }


        // ==========================================
        // CREAR DISPOSITIVO - GET
        // ==========================================

        public IActionResult Create()
        {
            ViewBag.Clientes = new SelectList(
                _context.Cliente.ToList(),
                "IdCliente",
                "Nombre"
            );

            return View();
        }


        // ==========================================
        // CREAR DISPOSITIVO - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Dispositivo dispositivo)
        {
            if (ModelState.IsValid)
            {
                dispositivo.FechaRegistro = DateTime.Now;

                dispositivo.Activo = true;

                _context.Dispositivo.Add(dispositivo);

                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Clientes = new SelectList(
                _context.Cliente.ToList(),
                "IdCliente",
                "Nombre"
            );

            return View(dispositivo);
        }

        // FUNCION PARA DESACTIVAR EL REGISTRO DE LA MARCA

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Desactivar(int id)
        {
            var dispositivo = _context.Dispositivo
                .FirstOrDefault(d => d.IdDispositivo == id);

            if (dispositivo == null)
            {
                return NotFound();
            }

            dispositivo.Activo = false;

            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}