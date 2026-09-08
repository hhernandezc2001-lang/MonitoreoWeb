using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("Pago")]
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }

        [Required]
        public int IdReparacion { get; set; }

        [Required]
        public decimal Monto { get; set; }

        public string? MetodoPago { get; set; }

        [Required]
        public DateTime FechaPago { get; set; }

        // Relación
        [ForeignKey("IdReparacion")]
        public Reparacion? Reparacion { get; set; }
    }
}