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


            // Buscar la reparación mediante el token
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
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


            // Reparación encontrada
            return View("Resultado", reparacion);
        }
    }
}