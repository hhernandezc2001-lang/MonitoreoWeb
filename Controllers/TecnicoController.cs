using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;
using System.IO;
using System.Security.Claims;

namespace MonitoreoWeb.Controllers
{
    public class TecnicoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TecnicoController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }


        // ==========================================================
        // PANEL PRINCIPAL DEL TÉCNICO
        // ==========================================================

        public IActionResult Index()
        {
            return View();
        }


        // ==========================================================
        // SOLICITUDES DISPONIBLES
        // ==========================================================

        public IActionResult Solicitudes()
        {
            var solicitudes = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Where(r =>
                    r.Activo &&
                    r.IdTecnico == null &&
                    r.IdEstado == 1
                )
                .OrderByDescending(r => r.FechaIngreso)
                .ToList();

            return View(solicitudes);
        }


        // ==========================================================
        // ACEPTAR REPARACIÓN
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Aceptar(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            int idTecnico = int.Parse(idUsuarioClaim.Value);

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdTecnico != null ||
                reparacion.IdEstado != 1)
            {
                TempData["Error"] =
                    "Esta reparación ya no está disponible.";

                return RedirectToAction("Solicitudes");
            }

            reparacion.IdTecnico = idTecnico;
            reparacion.IdEstado = 2;

            _context.SaveChanges();

            TempData["Success"] =
                "La reparación fue aceptada. Ya puedes comenzar a trabajar en ella.";

            return RedirectToAction("Detalle", new { id = id });
        }

        // ==========================================================
        // MIS REPARACIONES
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        public IActionResult MisReparaciones(string vista = "curso")
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }

            var todas = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Where(r =>
                    r.Activo &&
                    r.IdTecnico == idTecnico
                )
                .OrderByDescending(r => r.FechaIngreso)
                .ToList();

            var enCurso = todas.Where(r => r.IdEstado >= 2 && r.IdEstado <= 6).ToList();
            var finalizadas = todas.Where(r => r.IdEstado == 7).ToList();

            ViewBag.Vista = vista;
            ViewBag.EnCursoCount = enCurso.Count;
            ViewBag.FinalizadasCount = finalizadas.Count;

            var reparaciones = vista == "finalizadas" ? finalizadas : enCurso;

            return View(reparaciones);
        }

        // ==========================================================
        // DETALLE DE REPARACIÓN
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        public IActionResult Detalle(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Pagos)
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            var avances = _context.HistorialAvance
                .Include(a => a.Fotos)
                .Include(a => a.Usuario)
                .Where(a => a.IdReparacion == id)
                .OrderByDescending(a => a.Fecha)
                .ToList();

            bool puedeAgregar =
                reparacion.IdEstado >= 2 &&
                reparacion.IdEstado <= 6;

            ViewBag.Bitacora = new BitacoraViewModel
            {
                IdReparacion = id,
                Avances = avances,
                EsVistaCliente = false,
                PuedeAgregar = puedeAgregar,
                ControladorDestino = "Tecnico"
            };

            return View(reparacion);
        }

        // ==========================================================
        // AGREGAR AVANCE (BITÁCORA)
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAvance(
            int id,
            string descripcionAvance,
            List<IFormFile> fotos)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            // Solo se puede documentar mientras está activa
            // (desde que fue aceptada hasta que entra a pruebas)
            if (reparacion.IdEstado < 2 || reparacion.IdEstado > 6)
            {
                TempData["Error"] =
                    "No se puede agregar una actualización en el estado actual de la reparación.";

                return RedirectToAction("Detalle", new { id = id });
            }

            if (string.IsNullOrWhiteSpace(descripcionAvance))
            {
                TempData["Error"] =
                    "Debes escribir una descripción para la actualización.";

                return RedirectToAction("Detalle", new { id = id });
            }

            // ==========================================
            // 1. REGISTRAR EL AVANCE
            // ==========================================

            var avance = new HistorialAvance
            {
                IdReparacion = id,
                IdUsuario = idTecnico,
                Fecha = DateTime.Now,
                Descripcion = descripcionAvance.Trim()
            };

            _context.HistorialAvance.Add(avance);
            _context.SaveChanges();

            // ==========================================
            // 2. GUARDAR LAS FOTOS (SI LAS HAY)
            // ==========================================

            if (fotos != null && fotos.Count > 0)
            {
                var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };

                var carpeta = Path.Combine(
                    _env.WebRootPath,
                    "uploads",
                    "reparaciones",
                    id.ToString());

                Directory.CreateDirectory(carpeta);

                foreach (var foto in fotos)
                {
                    if (foto == null || foto.Length == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(foto.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        continue;
                    }

                    var nombreUnico = $"{Guid.NewGuid()}{extension}";
                    var rutaFisica = Path.Combine(carpeta, nombreUnico);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await foto.CopyToAsync(stream);
                    }

                    _context.FotoAvance.Add(new FotoAvance
                    {
                        IdReparacion = id,
                        IdAvance = avance.IdAvance,
                        IdUsuario = idTecnico,
                        RutaArchivo = $"/uploads/reparaciones/{id}/{nombreUnico}",
                        NombreArchivo = foto.FileName,
                        FechaSubida = DateTime.Now,
                        VisibleCliente = true,
                        Activo = true
                    });
                }

                _context.SaveChanges();
            }

            TempData["Success"] =
                "La actualización se agregó correctamente a la bitácora.";

            return RedirectToAction("Detalle", new { id = id });
        }

        // ==========================================================
        // INICIAR DIAGNÓSTICO
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IniciarDiagnostico(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 2)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en estado Aceptada.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.IdEstado = 3;

            _context.SaveChanges();

            TempData["Success"] =
                "La reparación pasó a la etapa de diagnóstico.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

        // ==========================================================
        // GUARDAR DIAGNÓSTICO
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GuardarDiagnostico(
            int id,
            string diagnostico)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(
                    idUsuarioClaim.Value,
                    out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 3)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en etapa de diagnóstico.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                TempData["Error"] =
                    "Debes ingresar un diagnóstico.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.Diagnostico = diagnostico.Trim();

            _context.SaveChanges();

            TempData["Success"] =
                "El diagnóstico se guardó correctamente.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

        // ==========================================================
        // ESPERAR REFACCIÓN
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EsperarRefaccion(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(
                    idUsuarioClaim.Value,
                    out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 3)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en diagnóstico.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.IdEstado = 4;

            _context.SaveChanges();

            TempData["Success"] =
                "La reparación pasó a Esperando Refacción.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

        // ==========================================================
        // INICIAR REPARACIÓN
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IniciarReparacion(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(
                    idUsuarioClaim.Value,
                    out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 3)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en diagnóstico.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.IdEstado = 5;

            _context.SaveChanges();

            TempData["Success"] =
                "La reparación pasó a En Reparación.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

        // ==========================================================
        // REFACCIÓN RECIBIDA
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RefaccionRecibida(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(
                    idUsuarioClaim.Value,
                    out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 4)
            {
                TempData["Error"] =
                    "La reparación no se encuentra esperando una refacción.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.IdEstado = 5;

            _context.SaveChanges();

            TempData["Success"] =
                "La refacción fue recibida. La reparación pasó a En Reparación.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

        // ==========================================================
        // ENVIAR A PRUEBAS
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EnviarAPruebas(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 5)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en proceso de reparación.";

                return RedirectToAction("Detalle", new { id = id });
            }

            reparacion.IdEstado = 6;
            _context.SaveChanges();

            TempData["Success"] =
                "La reparación fue enviada a pruebas.";

            return RedirectToAction("Detalle", new { id = id });
        }

        // ==========================================================
        // PRUEBAS SATISFACTORIAS
        // ==========================================================
        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PruebasSatisfactorias(int id, decimal? costoFinal)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(
                    idUsuarioClaim.Value,
                    out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 6)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en etapa de pruebas.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.IdEstado = 7;
            reparacion.FechaEntregaReal = DateTime.Now;

            if (costoFinal.HasValue)
            {
                reparacion.CostoFinal = costoFinal.Value;
            }

            _context.SaveChanges();

            TempData["Success"] =
                "Las pruebas fueron satisfactorias. La reparación está terminada.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

        // ==========================================================
        // PRUEBAS NO SATISFACTORIAS
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PruebasNoSatisfactorias(int id)
        {
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(
                    idUsuarioClaim.Value,
                    out int idTecnico))
            {
                return Unauthorized();
            }

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);

            if (reparacion == null)
            {
                return NotFound();
            }

            if (reparacion.IdEstado != 6)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en etapa de pruebas.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            reparacion.IdEstado = 5;

            _context.SaveChanges();

            TempData["Error"] =
                "Las pruebas no fueron satisfactorias. La reparación regresó a En Reparación.";

            return RedirectToAction(
                "Detalle",
                new { id = id }
            );
        }

    }
}