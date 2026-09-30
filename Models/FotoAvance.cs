using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("FotoAvance")]
    public class FotoAvance
    {
        [Key]
        public int IdFoto { get; set; }

        [Required]
        public int IdReparacion { get; set; }

        [Required]
        public int IdAvance { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        [Required]
        public string RutaArchivo { get; set; } = string.Empty;

        [Required]
        public string NombreArchivo { get; set; } = string.Empty;

        [Required]
        public DateTime FechaSubida { get; set; }

        public bool VisibleCliente { get; set; }

        public bool Activo { get; set; }

        // Relaciones
        [ForeignKey("IdReparacion")]
        public Reparacion? Reparacion { get; set; }

        [ForeignKey("IdAvance")]
        public HistorialAvance? Avance { get; set; }

        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }
    }
}