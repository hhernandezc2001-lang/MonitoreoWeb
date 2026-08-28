using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("Reparacion")]
    public class Reparacion
    {
        [Key]
        public int IdReparacion { get; set; }

        [Required]
        public int IdDispositivo { get; set; }

        [Required]
        public int IdEstado { get; set; }

        public int? IdTecnico { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; }

        [Required]
        public string ProblemaReportado { get; set; } = string.Empty;

        public string? Diagnostico { get; set; }

        public decimal? CostoEstimado { get; set; }

        public decimal? CostoFinal { get; set; }

        public DateTime? FechaEntregaEstimada { get; set; }

        public DateTime? FechaEntregaReal { get; set; }

        public bool Activo { get; set; }
         // Nueva propíedad 
        public string? TokenConsulta { get; set; }

        // Relaciones

        [ForeignKey("IdDispositivo")]
        public Dispositivo? Dispositivo { get; set; }

        [ForeignKey("IdTecnico")]
        public Usuario? Tecnico { get; set; }
    }
}

