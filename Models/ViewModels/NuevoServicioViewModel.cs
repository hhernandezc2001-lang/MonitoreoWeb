using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace MonitoreoWeb.Models.ViewModels
{
    public class NuevoServicioViewModel
    {
        public Cliente Cliente { get; set; } = new();
        public Dispositivo Dispositivo { get; set; } = new();
        public Reparacion Reparacion { get; set; } = new();
        public ChecklistRecepcion Checklist { get; set; } = new();

        // Pagos iniciales
        public decimal? PagoInicial { get; set; }
        public string? MetodoPago { get; set; }

        // Firma del cliente
        public string? FirmaClienteBase64 { get; set; }

        //PROPIEDAD PARA LAS FOTOS DE RECEPCIÓN
        [Display(Name = "Fotografías del equipo (Evidencia de recepción)")]
        public List<IFormFile>? FotosRecepcion { get; set; }

        // Propiedad auxiliar para recibir las descripciones de cada foto desde el formulario
        public List<string>? DescripcionesFotos { get; set; }
    }
}