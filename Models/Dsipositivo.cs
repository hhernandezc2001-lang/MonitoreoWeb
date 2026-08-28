using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("Dispositivo")]
    public class Dispositivo
    {
        [Key]
        public int IdDispositivo { get; set; }

        [Required]
        public int IdCliente { get; set; }

        [Required]
        public string Marca { get; set; } = string.Empty;

        [Required]
        public string Modelo { get; set; } = string.Empty;

        public string? IMEI { get; set; }

        public string? Color { get; set; }

        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }

        [ForeignKey("IdCliente")]
        public Cliente? Cliente { get; set; }
    }
}