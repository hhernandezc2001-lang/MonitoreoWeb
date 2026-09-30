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
            // 1. OBTENER EL ID DEL USUARIO DESDE LA SESIÓN (CLAIMS)
            var idUsuarioClaim = User.FindFirst("IdUsuario")?.Value;
            if (!string.IsNullOrEmpty(idUsuarioClaim) && int.TryParse(idUsuarioClaim, out int idUser))
            {
                reparacion.IdUsuario = idUser; // 👈 Aquí se asigna directamente
            }

            if (ModelState.IsValid)
            {
                reparacion.FechaIngreso = DateTime.Now;
                reparacion.Activo = true;
                reparacion.IdEstado = 1; // Estado Pendiente

                // 2. Guardar la reparación
                _context.Reparacion.Add(reparacion);
                _context.SaveChanges(); // 👈 Al guardarse aquí, la base de datos ya recibe el IdUsuario

                // 3. Guardar el token en la tabla TokenConsulta
                var tokenConsulta = new TokenConsulta
                {
                    IdReparacion = reparacion.IdReparacion,
                    CodigoUnico = GenerarToken(),
                    FechaCreacion = DateTime.Now,
                    Activo = true
                };

                _context.TokenConsulta.Add(tokenConsulta);
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

        public IActionResult Index()
        {
            var solicitudes = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Where(r =>
                    r.Activo &&
                    r.IdEstado == 1 &&
                    r.IdTecnico == null
                )
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
                    NombreTecnico = null
                })
                .OrderByDescending(r => r.FechaIngreso)
                .ToList();

            return View(solicitudes);
        }

        // ==========================================
        // GENERAR TOKEN HELPER
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