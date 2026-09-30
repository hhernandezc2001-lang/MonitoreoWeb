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
        [ValidateAntiForgeryToken]
        public IActionResult Consultar(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                ModelState.AddModelError("", "Ingresa el código de consulta.");
                return View();
            }

            token = token.Trim();

            // 1. Buscar el token activo en la tabla TokenConsulta
            var tokenObj = _context.TokenConsulta
                .FirstOrDefault(t => t.CodigoUnico == token && t.Activo);

            if (tokenObj == null)
            {
                ModelState.AddModelError("", "No se encontró ninguna reparación asociada a ese código.");
                return View();
            }

            // 2. Obtener la reparación relacionada
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Tecnico)
                .Include(r => r.Pagos)
                .FirstOrDefault(r => r.IdReparacion == tokenObj.IdReparacion && r.Activo);

            if (reparacion == null)
            {
                ModelState.AddModelError("", "No se encontró ninguna reparación con el código ingresado.");
                return View();
            }

            // 3. Obtener el historial de avances/bitácora
            var avances = _context.HistorialAvance
                .Include(a => a.Fotos)
                .Include(a => a.Usuario)
                .Where(a => a.IdReparacion == reparacion.IdReparacion)
                .OrderByDescending(a => a.Fecha)
                .ToList();

            // 4. ASIGNAR VIEWBAG.BITACORA (ESTO EVITA EL NULLREFERENCEEXCEPTION)
            ViewBag.Bitacora = new MonitoreoWeb.Models.ViewModels.BitacoraViewModel
            {
                IdReparacion = reparacion.IdReparacion,
                Avances = avances,
                EsVistaCliente = true,   // Es vista de cliente
                PuedeAgregar = false,    // El cliente NO puede agregar avances
                ControladorDestino = "Portal"
            };

            ViewBag.TokenConsulta = tokenObj.CodigoUnico;

            return View("Resultado", reparacion);
        }
    }
}