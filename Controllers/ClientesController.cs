using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;
using MonitoreoWeb.Models.ViewModels;

namespace MonitoreoWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registrar(RegistroClienteViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existe = _context.Clientes.FirstOrDefault(c => c.Email == model.Email);

            if (existe != null)
            {
                ModelState.AddModelError("", "ATENCIÓN, Ya existe un cliente con ese correo");
                return View(model);
            }

            var cliente = new Cliente
            {
                Nombre = model.Nombre,
                Apellido = model.Apellido,
                Telefono = model.Telefono,
                Email = model.Email,
                FechaRegistro = DateTime.Now,
                Activo = model.Activo
            };

            _context.Clientes.Add(cliente);
            _context.SaveChanges();

            TempData["Success"] = "Cliente registrado correctamente";
            return RedirectToAction("Registrar");
        }
    }
}