using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;

namespace MonitoreoWeb.Controllers
{
    public class ServicioController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServicioController(ApplicationDbContext context)
        {
            _context = context;
        }


        // ==========================================
        // GET: Servicio/Create
        // ==========================================

        public IActionResult Create()
        {
            var viewModel = new NuevoServicioViewModel();

            CargarClientes();

            return View(viewModel);
        }


        // ==========================================
        // POST: Servicio/Create
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            NuevoServicioViewModel model,
            string tipoCliente,
            string tipoDispositivo)
        {
            // OBTENER O CREAR CLIENTE

            Cliente cliente;

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
                cliente = model.Cliente;

                cliente.FechaRegistro = DateTime.Now;
                cliente.Activo = true;
                cliente.EmailVerificado = false;

                _context.Cliente.Add(cliente);

                _context.SaveChanges();
            }


            // OBTENER O CREAR DISPOSITIVO

            Dispositivo dispositivo;

            if (tipoDispositivo == "existente")
            {
                dispositivo = _context.Dispositivo
                    .FirstOrDefault(d =>
                        d.IdDispositivo ==
                        model.Dispositivo.IdDispositivo &&
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


            // CREAR REPARACIÓN

            Reparacion reparacion = model.Reparacion;

            reparacion.IdDispositivo =
                dispositivo.IdDispositivo;

            reparacion.FechaIngreso =
                DateTime.Now;

            reparacion.Activo = true;

            // Estado Pendiente
            reparacion.IdEstado = 1;


            // GENERAR TOKEN

            reparacion.TokenConsulta =
                "REP-" +
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 8)
                    .ToUpper();


            // GUARDAR REPARACIÓN

            _context.Reparacion.Add(reparacion);

            _context.SaveChanges();


            // FINALIZAR

            return RedirectToAction(
                "Index",
                "Reparacion"
            );
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