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

        public IActionResult Index()
        {
            // Asegúrate de que accedes al DbSet de Clientes correctamente
            var clientes = _context.Cliente.ToList();  // Esto traerá todos los clientes
            return View(clientes);
        }
    }
}