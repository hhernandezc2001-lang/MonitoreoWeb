using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;
using MonitoreoWeb.Services;
using System.IO;
using System.Security.Claims;

namespace MonitoreoWeb.Controllers
{
    public class TecnicoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly TelegramService _telegramService;

        public TecnicoController(ApplicationDbContext context, IWebHostEnvironment env, TelegramService telegramService)
        {
            _context = context;
            _env = env;
            _telegramService = telegramService;
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
                .Include(r => r.UsuarioCrea) // 👈 INCLUYE AL USUARIO QUE REGISTRÓ LA ORDEN
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
            var finalizadas = todas.Where(r => r.IdEstado == 7 || r.IdEstado == 8).ToList();

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
                .Include(r => r.UsuarioCrea)
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
        public async Task<IActionResult> AgregarAvance(int id, string? descripcion, List<IFormFile>? FotosAvance, List<string>? DescripcionesFotos)
        {
            int idUsuarioActual = 1;
            var claimIdUsuario = User.FindFirst("IdUsuario")?.Value;
            if (!string.IsNullOrEmpty(claimIdUsuario))
            {
                int.TryParse(claimIdUsuario, out idUsuarioActual);
            }

            string textoBitacora = "Actualización de evidencias fotográficas.";

            if (!string.IsNullOrWhiteSpace(descripcion))
            {
                textoBitacora = descripcion.Trim();
            }
            else if (DescripcionesFotos != null && DescripcionesFotos.Any() && !string.IsNullOrWhiteSpace(DescripcionesFotos[0]))
            {
                textoBitacora = DescripcionesFotos[0].Trim();
            }

            var nuevoAvance = new HistorialAvance
            {
                IdReparacion = id,
                IdUsuario = idUsuarioActual,
                Descripcion = textoBitacora,
                Fecha = DateTime.Now
            };

            _context.HistorialAvance.Add(nuevoAvance);
            _context.SaveChanges();

            if (FotosAvance != null && FotosAvance.Any())
            {
                string carpetaUploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reparaciones");
                if (!Directory.Exists(carpetaUploads))
                {
                    Directory.CreateDirectory(carpetaUploads);
                }

                for (int i = 0; i < FotosAvance.Count; i++)
                {
                    var archivo = FotosAvance[i];
                    if (archivo.Length > 0)
                    {
                        string nombreUnico = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
                        string rutaCompleta = Path.Combine(carpetaUploads, nombreUnico);

                        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                        {
                            await archivo.CopyToAsync(stream);
                        }

                        var fotoAvance = new FotoAvance
                        {
                            IdReparacion = id,
                            IdAvance = nuevoAvance.IdAvance,
                            IdUsuario = idUsuarioActual,
                            RutaArchivo = "/uploads/reparaciones/" + nombreUnico,
                            NombreArchivo = archivo.FileName,
                            FechaSubida = DateTime.Now,
                            VisibleCliente = true,
                            Activo = true
                        };

                        _context.FotoAvance.Add(fotoAvance);
                    }
                }

                _context.SaveChanges();
            }

            TempData["Success"] = "Avance publicado correctamente.";
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
        // PRUEBAS SATISFACTORIAS (ESTADO 7: TERMINADA)
        // ==========================================================
        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PruebasSatisfactorias(int id, decimal? costoFinal)
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

            // ⚡ IMPORTANTE: Incluimos al Dispositivo y al Cliente con su TelegramChatId
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
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
                TempData["Error"] = "La reparación no se encuentra en etapa de pruebas.";
                return RedirectToAction("Detalle", new { id = id });
            }

            reparacion.IdEstado = 7; // Estado 7: Terminada
            reparacion.FechaTerminacion = DateTime.Now;

            if (costoFinal.HasValue)
            {
                reparacion.CostoFinal = costoFinal.Value;
            }

            _context.SaveChanges(); // Guardamos el cambio de estado en la BD

            // ==========================================================
            // 🚀 ENVIAR NOTIFICACIÓN AUTOMÁTICA A TELEGRAM
            // ==========================================================
            try
            {
                var cliente = reparacion.Dispositivo?.Cliente;
                if (cliente != null && !string.IsNullOrWhiteSpace(cliente.TelegramChatId))
                {
                    string modeloDispositivo = $"{reparacion.Dispositivo?.Marca} {reparacion.Dispositivo?.Modelo}";
                    string mensaje = $"¡Hola, {cliente.Nombre}! 🛠️\n\nTe informamos que tu dispositivo *{modeloDispositivo}* (Orden #{reparacion.IdReparacion}) ha sido **reparado con éxito** y superó las pruebas de calidad.\n\nYa puedes pasar a recogerlo a nuestro taller. ¡Te esperamos!";

                    await _telegramService.EnviarNotificacionAsync(cliente.TelegramChatId, mensaje);
                }
            }
            catch (Exception ex)
            {
                // Si por alguna razón falla Telegram, no detenemos el sistema principal, solo lo registramos
                System.Diagnostics.Debug.WriteLine($"Error al enviar notificación de Telegram: {ex.Message}");
            }
            // ==========================================================

            TempData["Success"] = "Las pruebas fueron satisfactorias. La reparación está terminada y se envió la notificación al cliente.";

            return RedirectToAction("Detalle", new { id = id });
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