using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("HistorialAvance")]
    public class HistorialAvance
    {
        [Key]
        public int IdAvance { get; set; }

        [Required]
        public int IdReparacion { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public DateTime Fecha { get; set; }

        [Required]
        public string Descripcion { get; set; } = string.Empty;

        // Relaciones
        [ForeignKey("IdReparacion")]
        public Reparacion? Reparacion { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        public ICollection<FotoAvance> Fotos { get; set; } = new List<FotoAvance>();
    }
}