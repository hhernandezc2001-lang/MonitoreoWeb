using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("Pago")]
    public class Pago
    {
        [Key]
        public int IdPago { get; set; }

        public int IdReparacion { get; set; }

        public decimal Monto { get; set; }

        public DateTime FechaPago { get; set; } = DateTime.Now;

        public string? MetodoPago { get; set; }

        [ForeignKey("IdReparacion")]
        public virtual Reparacion? Reparacion { get; set; }
    }
}