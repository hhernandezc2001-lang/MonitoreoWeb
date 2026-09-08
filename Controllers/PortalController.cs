using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using Microsoft.EntityFrameworkCore;

namespace TuProyecto.Controllers
{
    public class PortalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PortalController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================================
        // PORTAL PRINCIPAL
        // ==========================================================

        public IActionResult Index()
        {
            return View();
        }


        // ==========================================================
        // CONSULTAR REPARACIÓN - GET
        // ==========================================================

        [HttpGet]
        public IActionResult Consultar()
        {
            return View();
        }


        // ==========================================================
        // CONSULTAR REPARACIÓN - POST
        // ==========================================================

        [HttpPost]
        public IActionResult Consultar(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError(
                    "",
                    "Ingresa el código de consulta."
                );
                return View();
            }

            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                .Include(r => r.Tecnico)
                .Include(r => r.Pagos)
                .FirstOrDefault(r =>
                    r.TokenConsulta == token.Trim() &&
                    r.Activo);

            // No encontrada
            if (reparacion == null)
            {
                ModelState.AddModelError(
                    "",
                    "No se encontró una reparación con ese código."
                );
                return View();
            }

            // ==========================================
            // BITÁCORA VISIBLE PARA EL CLIENTE
            // ==========================================

            var avances = _context.HistorialAvance
                .Include(a => a.Fotos)
                .Where(a => a.IdReparacion == reparacion.IdReparacion)
                .OrderByDescending(a => a.Fecha)
                .ToList();

            ViewBag.Bitacora = new MonitoreoWeb.Models.ViewModels.BitacoraViewModel
            {
                IdReparacion = reparacion.IdReparacion,
                Avances = avances,
                EsVistaCliente = true,
                PuedeAgregar = false
            };

            // Reparación encontrada
            return View("Resultado", reparacion);
        }
    }
}