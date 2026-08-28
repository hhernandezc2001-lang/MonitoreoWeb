using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
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
        // POR AHORA SOLO DEJAMOS LA ACCIÓN PREPARADA.
        // TODAVÍA NO ASIGNA LA REPARACIÓN.
        // ==========================================================
        // ACEPTAR REPARACIÓN
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Aceptar(int id)
        {
            // ==========================================
            // 1. OBTENER EL ID DEL TÉCNICO AUTENTICADO
            // ==========================================

            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            int idTecnico = int.Parse(idUsuarioClaim.Value);


            // ==========================================
            // 2. BUSCAR LA REPARACIÓN
            // ==========================================

            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.Activo);


            if (reparacion == null)
            {
                return NotFound();
            }


            // ==========================================
            // 3. VERIFICAR QUE SIGA DISPONIBLE
            // ==========================================

            if (reparacion.IdTecnico != null ||
                reparacion.IdEstado != 1)
            {
                TempData["Error"] =
                    "Esta reparación ya no está disponible.";

                return RedirectToAction("Solicitudes");
            }


            // ==========================================
            // 4. ASIGNAR EL TÉCNICO
            // ==========================================

            reparacion.IdTecnico = idTecnico;


            // ==========================================
            // 5. CAMBIAR ESTADO
            // ==========================================

            reparacion.IdEstado = 2;


            // ==========================================
            // 6. GUARDAR CAMBIOS
            // ==========================================

            _context.SaveChanges();


            // ==========================================
            // 7. MENSAJE DE CONFIRMACIÓN
            // ==========================================

            TempData["Success"] =
                "La reparación fue aceptada correctamente.";


            // ==========================================
            // 8. REGRESAR A SOLICITUDES
            // ==========================================

            return RedirectToAction("Solicitudes");
        }

        // ==========================================================
        // MIS REPARACIONES
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        public IActionResult MisReparaciones()
        {
            // Obtener el IdUsuario del técnico autenticado
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }


            // Buscar solamente las reparaciones
            // asignadas al técnico actual
            var reparaciones = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Where(r =>
                    r.Activo &&
                    r.IdTecnico == idTecnico
                )
                .OrderByDescending(r => r.FechaIngreso)
                .ToList();


            return View(reparaciones);
        }

        // ==========================================================
        // DETALLE DE REPARACIÓN
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        public IActionResult Detalle(int id)
        {
            // Obtener el técnico que está conectado
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }


            // Buscar la reparación
            // pero solamente si pertenece
            // al técnico actual
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


            return View(reparacion);
        }

        // ==========================================================
        // INICIAR DIAGNÓSTICO
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult IniciarDiagnostico(int id)
        {
            // Obtener el técnico autenticado
            var idUsuarioClaim = User.FindFirst("IdUsuario");

            if (idUsuarioClaim == null)
            {
                return Unauthorized();
            }

            if (!int.TryParse(idUsuarioClaim.Value, out int idTecnico))
            {
                return Unauthorized();
            }


            // Buscar la reparación
            // solamente si pertenece al técnico actual
            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);


            if (reparacion == null)
            {
                return NotFound();
            }


            // Verificar que esté en estado "Aceptada"
            if (reparacion.IdEstado != 2)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en estado Aceptada.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }


            // Cambiar:
            // Aceptada (2) → Diagnóstico (3)
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
            // Obtener el técnico autenticado
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


            // Buscar la reparación
            // solamente si pertenece al técnico actual
            var reparacion = _context.Reparacion
                .FirstOrDefault(r =>
                    r.IdReparacion == id &&
                    r.IdTecnico == idTecnico &&
                    r.Activo);


            if (reparacion == null)
            {
                return NotFound();
            }


            // Verificar que esté en diagnóstico
            if (reparacion.IdEstado != 3)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en etapa de diagnóstico.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }


            // Validar que exista información
            if (string.IsNullOrWhiteSpace(diagnostico))
            {
                TempData["Error"] =
                    "Debes ingresar un diagnóstico.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }


            // Guardar diagnóstico
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


            // Solo se puede pasar a este estado
            // desde Diagnóstico
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


            // Solo se puede iniciar la reparación
            // desde Diagnóstico
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

            // Solo puede avanzar desde Esperando Refacción
            if (reparacion.IdEstado != 4)
            {
                TempData["Error"] =
                    "La reparación no se encuentra esperando una refacción.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            // 4 → 5
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
        public async Task<IActionResult> EnviarAPruebas(
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

            // Solo puede avanzar desde En Reparación
            if (reparacion.IdEstado != 5)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en proceso de reparación.";

                return RedirectToAction("Detalle", new { id = id });
            }

            if (string.IsNullOrWhiteSpace(descripcionAvance))
            {
                TempData["Error"] =
                    "Debes describir el trabajo realizado antes de enviar a pruebas.";

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
            _context.SaveChanges(); // Necesario para obtener el IdAvance generado


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
                    if (foto.Length == 0)
                    {
                        continue;
                    }

                    var extension = Path.GetExtension(foto.FileName).ToLower();

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        continue; // Ignora archivos que no sean imágenes
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


            // ==========================================
            // 3. CAMBIAR ESTADO: En Reparación → Pruebas
            // ==========================================

            reparacion.IdEstado = 6;

            _context.SaveChanges();

            TempData["Success"] =
                "La reparación fue enviada a pruebas junto con la evidencia adjunta.";

            return RedirectToAction("Detalle", new { id = id });
        }

        // ==========================================================
        // PRUEBAS SATISFACTORIAS
        // ==========================================================

        [Authorize(Roles = "Tecnico")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PruebasSatisfactorias(int id)
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

            // Solo puede pasar a Terminada
            // si actualmente está En Pruebas
            if (reparacion.IdEstado != 6)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en etapa de pruebas.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            // 6 → 7
            // 6 → 7
            reparacion.IdEstado = 7;

            // Registrar fecha real de terminación
            reparacion.FechaEntregaReal = DateTime.Now;

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

            // Solo puede regresar a reparación
            // si actualmente está En Pruebas
            if (reparacion.IdEstado != 6)
            {
                TempData["Error"] =
                    "La reparación no se encuentra en etapa de pruebas.";

                return RedirectToAction(
                    "Detalle",
                    new { id = id }
                );
            }

            // 6 → 5
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