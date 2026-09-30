using MonitoreoWeb.Models;

namespace MonitoreoWeb.Models.ViewModels
{
    public class BitacoraViewModel
    {
        public int IdReparacion { get; set; }
        public List<HistorialAvance> Avances { get; set; } = new();
        public bool EsVistaCliente { get; set; }
        public bool PuedeAgregar { get; set; }
        public string ControladorDestino { get; set; } = "Tecnico";

        // NUEVAS PENDIENTES.... 
        public List<IFormFile>? FotosAvance { get; set; }
        public List<string>? DescripcionesFotos { get; set; }
    }
}