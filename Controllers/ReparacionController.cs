using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;

namespace MonitoreoWeb.Controllers
{
    public class ReparacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReparacionController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // GET: Reparacion/Create
        // ==========================================

        public IActionResult Create()
        {
            ViewBag.Dispositivos = new SelectList(
                _context.Dispositivo,
                "IdDispositivo",
                "Modelo"
            );

            return View();
        }


        // ==========================================
        // POST: Reparacion/Create
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Reparacion reparacion)
        {
            if (ModelState.IsValid)
            {
                reparacion.FechaIngreso = DateTime.Now;

                reparacion.Activo = true;

                // Estado Pendiente
                reparacion.IdEstado = 1;

                reparacion.TokenConsulta = GenerarToken();

                _context.Reparacion.Add(reparacion);

                _context.SaveChanges();

                return View("Confirmacion", reparacion);
            }

            ViewBag.Dispositivos = new SelectList(
                _context.Dispositivo,
                "IdDispositivo",
                "Modelo"
            );

            return View(reparacion);
        }


        // ==========================================
        // GET: Reparacion/Index
        // ==========================================

        public IActionResult Index(string vista = "pendientes")
        {
            var todas = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Tecnico)
                .Where(r => r.Activo && (r.IdEstado == 1 || r.IdEstado == 2))
                .Select(r => new SolicitudReparacionViewModel
                {
                    IdReparacion = r.IdReparacion,
                    ProblemaReportado = r.ProblemaReportado,
                    CostoEstimado = r.CostoEstimado,
                    FechaIngreso = r.FechaIngreso,
                    FechaEntregaEstimada = r.FechaEntregaEstimada,
                    IdEstado = r.IdEstado,
                    IdDispositivo = r.Dispositivo!.IdDispositivo,
                    Marca = r.Dispositivo.Marca,
                    Modelo = r.Dispositivo.Modelo,
                    IMEI = r.Dispositivo.IMEI,
                    Color = r.Dispositivo.Color,
                    IdCliente = r.Dispositivo.Cliente!.IdCliente,
                    NombreCliente = r.Dispositivo.Cliente.Nombre,
                    ApellidoCliente = r.Dispositivo.Cliente.Apellido,
                    TelefonoCliente = r.Dispositivo.Cliente.Telefono,
                    EmailCliente = r.Dispositivo.Cliente.Email,
                    IdTecnico = r.IdTecnico,
                    NombreTecnico = r.Tecnico != null ? r.Tecnico.Nombre : null
                })
                .OrderByDescending(r => r.FechaIngreso)
                .ToList();

            var pendientes = todas.Where(s => s.IdEstado == 1).ToList();
            var aceptadas = todas.Where(s => s.IdEstado == 2).ToList();

            ViewBag.Vista = vista;
            ViewBag.PendientesCount = pendientes.Count;
            ViewBag.AceptadasCount = aceptadas.Count;

            var solicitudes = vista == "aceptadas" ? aceptadas : pendientes;

            return View(solicitudes);
        }


        // ==========================================
        // GENERAR TOKEN
        // ==========================================

        private string GenerarToken()
        {
            return "REP-" +
                   Guid.NewGuid()
                       .ToString("N")
                       .Substring(0, 8)
                       .ToUpper();
        }
    }
}