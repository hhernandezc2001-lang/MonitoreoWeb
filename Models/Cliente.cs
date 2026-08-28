using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("Cliente")]
    public class Cliente
    {
        [Key]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "Ingresa un correo electrónico válido")]
        public string Email { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; }

        public bool Activo { get; set; }

        // --- Campos para verificación de email ---
        public bool EmailVerificado { get; set; } = false;

        public string? TokenVerificacion { get; set; }

        public DateTime? TokenExpira { get; set; }
    }
}