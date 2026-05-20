using Microsoft.AspNetCore.Mvc;
using MonitoreoWeb.Data;
using MonitoreoWeb.Models;

namespace MonitoreoWeb.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTADO
        public IActionResult Index()
        {
            var clientes = _context.Cliente.ToList();

            return View(clientes);
        }

        // GET REGISTRAR
        [HttpGet]
        public IActionResult Registrar()
        {
            return View();
        }

        // POST REGISTRAR
        [HttpPost]
        public IActionResult Registrar(Cliente model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.FechaRegistro = DateTime.Now;

            _context.Cliente.Add(model);

            _context.SaveChanges();

            TempData["Success"] =
                "Cliente registrado correctamente";

            return RedirectToAction("Registrar");
        }
    }
}