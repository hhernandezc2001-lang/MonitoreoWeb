using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("TokenConsulta")]
    public class TokenConsulta
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdToken { get; set; }

        public int IdReparacion { get; set; }

        [Required]
        [StringLength(100)]
        public string CodigoUnico { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        public DateTime? FechaExpiracion { get; set; }

        public bool Activo { get; set; } = true;

        [ForeignKey("IdReparacion")]
        public virtual Reparacion? Reparacion { get; set; }
    }
}