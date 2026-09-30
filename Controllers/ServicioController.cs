using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;
using Microsoft.AspNetCore.SignalR;
using MonitoreoWeb.Hubs;
using System.Security.Claims;

namespace MonitoreoWeb.Controllers
{
    public class ServicioController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificacionHub> _hubContext; // ✅ 1. CAMPO DECLARADO

        // ✅ 2. INYECTADO EN EL CONSTRUCTOR
        public ServicioController(ApplicationDbContext context, IHubContext<NotificacionHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // ==========================================
        // GET: Servicio/Create (MUESTRA EL FORMULARIO)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            CargarClientes();

            var model = new NuevoServicioViewModel
            {
                Reparacion = new Reparacion
                {
                    Prioridad = "Normal",
                    FechaEntregaEstimada = DateTime.Now.AddDays(1)
                }
            };

            return View(model);
        }

        // ==========================================
        // POST: Servicio/Create (PROCESA EL GUARDADO)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            NuevoServicioViewModel model,
            string tipoCliente,
            string tipoDispositivo,
            string? descripcionRecepcion)
        {
            // ==========================================
            // VALIDAR PAGO INICIAL
            // ==========================================

            if (model.PagoInicial.HasValue && model.PagoInicial.Value > 0)
            {
                if (!model.Reparacion.CostoEstimado.HasValue)
                {
                    ModelState.AddModelError("",
                        "Debes definir un costo estimado antes de registrar un pago inicial.");

                    CargarClientes();
                    return View(model);
                }

                if (model.PagoInicial.Value > model.Reparacion.CostoEstimado.Value)
                {
                    ModelState.AddModelError("",
                        $"El pago inicial (${model.PagoInicial.Value:N2}) no puede ser mayor al costo estimado (${model.Reparacion.CostoEstimado.Value:N2}).");

                    CargarClientes();
                    return View(model);
                }
            }

            // Iniciar Transacción
            Cliente cliente = null;
            Dispositivo dispositivo = null;
            Reparacion reparacion = null;

            using var transaction = _context.Database.BeginTransaction();

            try
            {
                // ==========================================
                // OBTENER O CREAR CLIENTE
                // ==========================================

                if (tipoCliente == "existente")
                {
                    cliente = _context.Cliente
                        .FirstOrDefault(c =>
                            c.IdCliente == model.Cliente.IdCliente);

                    if (cliente == null)
                    {
                        ModelState.AddModelError(
                            "Cliente.IdCliente",
                            "El cliente seleccionado no existe."
                        );

                        CargarClientes();
                        return View(model);
                    }
                }
                else
                {
                    // VALIDAR DUPLICADOS
                    var email = model.Cliente.Email?.Trim();
                    var telefono = model.Cliente.Telefono?.Trim();
                    var nombre = model.Cliente.Nombre?.Trim();
                    var apellido = model.Cliente.Apellido?.Trim();

                    var duplicado = _context.Cliente.FirstOrDefault(c =>
                        c.Activo &&
                        (
                            (email != null && c.Email.ToLower() == email.ToLower()) ||
                            (telefono != null && c.Telefono == telefono) ||
                            (nombre != null && apellido != null && c.Nombre.ToLower() == nombre.ToLower() && c.Apellido.ToLower() == apellido.ToLower())
                        ));

                    if (duplicado != null)
                    {
                        string motivo;

                        if (email != null && duplicado.Email.ToLower() == email.ToLower())
                        {
                            motivo = "Ya existe un cliente registrado con ese correo electrónico. Búscalo como cliente existente.";
                        }
                        else if (telefono != null && duplicado.Telefono == telefono)
                        {
                            motivo = "Ya existe un cliente registrado con ese teléfono. Búscalo como cliente existente.";
                        }
                        else
                        {
                            motivo = "Ya existe un cliente registrado con ese nombre y apellido. Búscalo como cliente existente.";
                        }

                        ModelState.AddModelError("", motivo);

                        CargarClientes();
                        return View(model);
                    }

                    cliente = model.Cliente;
                    cliente.FechaRegistro = DateTime.Now;
                    cliente.Activo = true;

                    _context.Cliente.Add(cliente);
                    _context.SaveChanges();
                }

                // ==========================================
                // OBTENER O CREAR DISPOSITIVO
                // ==========================================

                if (tipoDispositivo == "existente")
                {
                    dispositivo = _context.Dispositivo
                        .FirstOrDefault(d =>
                            d.IdDispositivo == model.Dispositivo.IdDispositivo &&
                            d.IdCliente == cliente.IdCliente &&
                            d.Activo);

                    if (dispositivo == null)
                    {
                        ModelState.AddModelError(
                            "Dispositivo.IdDispositivo",
                            "El dispositivo seleccionado no es válido."
                        );

                        CargarClientes();
                        return View(model);
                    }
                }
                else
                {
                    dispositivo = model.Dispositivo;
                    dispositivo.IdCliente = cliente.IdCliente;
                    dispositivo.FechaRegistro = DateTime.Now;
                    dispositivo.Activo = true;

                    _context.Dispositivo.Add(dispositivo);
                    _context.SaveChanges();
                }

                // ==========================================
                // CREAR REPARACIÓN
                // ==========================================

                reparacion = model.Reparacion;
                reparacion.IdDispositivo = dispositivo.IdDispositivo;
                reparacion.FechaIngreso = DateTime.Now;
                reparacion.Activo = true;
                reparacion.IdEstado = 1; // Estado Pendiente / Ingresado

                // ASIGNAR EL USUARIO QUE REGISTRA LA REPARACIÓN
                var claimIdUsuarioReparacion = User.FindFirst("IdUsuario")?.Value;
                if (!string.IsNullOrEmpty(claimIdUsuarioReparacion) &&
                    int.TryParse(claimIdUsuarioReparacion, out int idUsuarioReparacion))
                {
                    reparacion.IdUsuario = idUsuarioReparacion;
                }

                // ASIGNAR PRIORIDAD (Mantiene la elegida o asigna Normal por defecto)
                reparacion.Prioridad = string.IsNullOrWhiteSpace(model.Reparacion?.Prioridad)
                    ? "Normal"
                    : model.Reparacion.Prioridad.Trim();

                // Protecciones de Nulos
                if (string.IsNullOrWhiteSpace(reparacion.ProblemaReportado))
                {
                    reparacion.ProblemaReportado = "Sin problema especificado";
                }

                if (reparacion.DiasGarantia <= 0)
                {
                    reparacion.DiasGarantia = 30; // Garantía por defecto
                }

                // Guardar la Reparación primero para obtener IdReparacion
                _context.Reparacion.Add(reparacion);
                _context.SaveChanges();

                // ==========================================
                // CREAR Y GUARDAR EN LA TABLA TokenConsulta
                // ==========================================

                var tokenConsulta = new TokenConsulta
                {
                    IdReparacion = reparacion.IdReparacion,
                    CodigoUnico = "REP-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                    FechaCreacion = DateTime.Now,
                    FechaExpiracion = DateTime.Now.AddDays(90),
                    Activo = true
                };

                _context.TokenConsulta.Add(tokenConsulta);
                _context.SaveChanges();

                // ==========================================
                // REGISTRAR CHECKLIST DE RECEPCIÓN Y FIRMA
                // ==========================================

                if (model.Checklist != null)
                {
                    var checklist = model.Checklist;
                    checklist.IdReparacion = reparacion.IdReparacion;
                    checklist.FirmaClienteBase64 = model.FirmaClienteBase64 ?? string.Empty;

                    _context.ChecklistRecepcion.Add(checklist);
                    _context.SaveChanges();
                }

                // ==========================================
                // REGISTRAR BITÁCORA Y FOTOS DE RECEPCIÓN (SI LAS HAY)
                // ==========================================
                bool tieneFotos = model.FotosRecepcion != null && model.FotosRecepcion.Any();
                bool tieneDescripcion = !string.IsNullOrWhiteSpace(descripcionRecepcion);

                if (tieneFotos || tieneDescripcion)
                {
                    // Reutilizamos el mismo usuario que registró la reparación
                    int idUsuarioActual = reparacion.IdUsuario ?? 1;

                    string textoAvance = tieneDescripcion
                        ? descripcionRecepcion.Trim()
                        : "Recepción inicial del equipo - Estado físico y estético registrado.";

                    // 1. Creamos un registro inicial en HistorialAvance con el ID del usuario real
                    var historialIngreso = new HistorialAvance
                    {
                        IdReparacion = reparacion.IdReparacion,
                        IdUsuario = idUsuarioActual,
                        Descripcion = textoAvance,
                        Fecha = DateTime.Now
                    };

                    _context.HistorialAvance.Add(historialIngreso);
                    _context.SaveChanges(); // Genera el IdAvance

                    // 2. Si hay fotos adjuntas o capturadas, las guardamos vinculadas al HistorialAvance
                    if (tieneFotos)
                    {
                        string carpetaUploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reparaciones");
                        if (!Directory.Exists(carpetaUploads))
                        {
                            Directory.CreateDirectory(carpetaUploads);
                        }

                        for (int i = 0; i < model.FotosRecepcion.Count; i++)
                        {
                            var archivo = model.FotosRecepcion[i];
                            if (archivo.Length > 0)
                            {
                                string nombreUnico = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
                                string rutaCompleta = Path.Combine(carpetaUploads, nombreUnico);

                                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                                {
                                    await archivo.CopyToAsync(stream);
                                }

                                string descFoto = (model.DescripcionesFotos != null && model.DescripcionesFotos.Count > i && !string.IsNullOrWhiteSpace(model.DescripcionesFotos[i]))
                                    ? model.DescripcionesFotos[i].Trim()
                                    : "Foto de recepción";

                                var fotoAvance = new FotoAvance
                                {
                                    IdReparacion = reparacion.IdReparacion,
                                    IdAvance = historialIngreso.IdAvance,
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
                }

                // ==========================================
                // REGISTRAR PAGO INICIAL CON MÉTODO ELEGIDO
                // ==========================================

                if (model.PagoInicial.HasValue && model.PagoInicial.Value > 0)
                {
                    _context.Pago.Add(new Pago
                    {
                        IdReparacion = reparacion.IdReparacion,
                        Monto = model.PagoInicial.Value,
                        MetodoPago = string.IsNullOrWhiteSpace(model.MetodoPago) ? "Efectivo" : model.MetodoPago.Trim(),
                        FechaPago = DateTime.Now
                    });

                    _context.SaveChanges();
                }

                // Confirmar transacción
                transaction.Commit();

                // =========================================================================
                // ✅ 3. DISPARAR LA NOTIFICACIÓN EN TIEMPO REAL CON SIGNALR
                // =========================================================================
                await _hubContext.Clients.All.SendAsync("RecibirNotificacionNuevaReparacion",
                    reparacion.IdReparacion,
                    $"{dispositivo.Marca} {dispositivo.Modelo}");

                // =========================================================================
                // REDIRIGIR AL TICKET DE RECEPCIÓN
                // =========================================================================
                return RedirectToAction("TicketRecepcion", "Servicio", new { id = reparacion.IdReparacion });
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                var mensajeDetalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                ModelState.AddModelError("", "Error al guardar en BD: " + mensajeDetalle);

                CargarClientes();
                return View(model);
            }
        }

        // ==========================================
        // VISTA DE IMPRESIÓN DEL TICKET DE RECEPCIÓN
        // ==========================================

        [HttpGet]
        public IActionResult TicketRecepcion(int id)
        {
            var reparacion = _context.Reparacion
                .Include(r => r.Dispositivo)
                    .ThenInclude(d => d.Cliente)
                .Include(r => r.Pagos)
                .FirstOrDefault(r => r.IdReparacion == id);

            if (reparacion == null) return NotFound();

            // Pasamos el checklist y el token mediante ViewBag
            ViewBag.Checklist = _context.ChecklistRecepcion.FirstOrDefault(c => c.IdReparacion == id);
            ViewBag.Token = _context.TokenConsulta.FirstOrDefault(t => t.IdReparacion == id)?.CodigoUnico;

            return View(reparacion);
        }

        // ==========================================
        // OBTENER DISPOSITIVOS DEL CLIENTE
        // ==========================================

        [HttpGet]
        public IActionResult ObtenerDispositivos(int idCliente)
        {
            var dispositivos = _context.Dispositivo
                .Where(d =>
                    d.IdCliente == idCliente &&
                    d.Activo)
                .Select(d => new
                {
                    id = d.IdDispositivo,
                    nombre = d.Marca + " " + d.Modelo
                })
                .ToList();

            return Json(dispositivos);
        }

        // ==========================================
        // BUSCAR CLIENTES
        // ==========================================

        [HttpGet]
        public IActionResult BuscarClientes(string termino)
        {
            if (string.IsNullOrWhiteSpace(termino))
            {
                return Json(new List<object>());
            }

            termino = termino.Trim();

            var clientes = _context.Cliente
                .Where(c =>
                    c.Activo &&
                    (
                        c.Nombre.Contains(termino) ||
                        c.Apellido.Contains(termino) ||
                        c.Telefono.Contains(termino) ||
                        c.Email.Contains(termino)
                    )
                )
                .Select(c => new
                {
                    id = c.IdCliente,
                    nombre = c.Nombre + " " + c.Apellido,
                    telefono = c.Telefono,
                    email = c.Email
                })
                .Take(10)
                .ToList();

            return Json(clientes);
        }

        // ==========================================
        // BUSCAR DISPOSITIVOS
        // ==========================================

        [HttpGet]
        public IActionResult BuscarDispositivos(int idCliente, string termino)
        {
            if (idCliente <= 0 || string.IsNullOrWhiteSpace(termino))
            {
                return Json(new List<object>());
            }

            termino = termino.Trim();

            var dispositivos = _context.Dispositivo
                .Where(d =>
                    d.IdCliente == idCliente &&
                    d.Activo &&
                    (
                        d.Marca.Contains(termino) ||
                        d.Modelo.Contains(termino) ||
                        d.IMEI.Contains(termino)
                    )
                )
                .Select(d => new
                {
                    id = d.IdDispositivo,
                    marca = d.Marca,
                    modelo = d.Modelo,
                    imei = d.IMEI
                })
                .Take(10)
                .ToList();

            return Json(dispositivos);
        }

        // ==========================================
        // CARGAR CLIENTES
        // ==========================================

        private void CargarClientes()
        {
            ViewBag.Clientes = new SelectList(
                _context.Cliente,
                "IdCliente",
                "Nombre"
            );
        }
    }
}