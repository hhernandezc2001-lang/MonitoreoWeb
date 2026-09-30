using System.ComponentModel.DataAnnotations;

namespace MonitoreoWeb.Models.ViewModels
{
    public class CambiarPasswordViewModel
    {
        [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
        public string PasswordActual { get; set; }

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string PasswordNueva { get; set; }

        [Required(ErrorMessage = "Confirma tu nueva contraseña.")]
        [Compare("PasswordNueva", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmarPassword { get; set; }
    }
}