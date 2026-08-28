using MonitoreoWeb.Models;

namespace MonitoreoWeb.Models.ViewModels
{
    public class NuevoServicioViewModel
    {
        public Cliente Cliente { get; set; } = new Cliente();

        public Dispositivo Dispositivo { get; set; } = new Dispositivo();

        public Reparacion Reparacion { get; set; } = new Reparacion();
    }
}