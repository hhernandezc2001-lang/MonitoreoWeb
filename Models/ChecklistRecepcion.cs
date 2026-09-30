using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("ChecklistRecepcion")]
    public class ChecklistRecepcion
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdChecklist { get; set; }

        [Required]
        public int IdReparacion { get; set; }

        // --- Inspección Física Externa ---

        [Display(Name = "¿Enciende el equipo?")]
        public bool Enciende { get; set; } = false;

        [Display(Name = "¿Pantalla estrellada/quebrada?")]
        public bool PantallaEstrellada { get; set; } = false;

        [Display(Name = "¿Tapa trasera dañada?")]
        public bool TapaTraseraDañada { get; set; } = false;

        [Display(Name = "¿Rayones visibles?")]
        public bool RayonesVisibles { get; set; } = false;

        [Display(Name = "¿Signos de humedad o agua?")]
        public bool HumedadOAgua { get; set; } = false;

        // --- Funcionalidades de Hardware ---

        [Display(Name = "¿Carga correctamente?")]
        public bool CargaCorrectamente { get; set; } = false;

        [Display(Name = "¿Botones físicos funcionan?")]
        public bool BotonesFuncionan { get; set; } = false;

        [Display(Name = "Cámara frontal OK")]
        public bool CamaraFrontalOk { get; set; } = false;

        [Display(Name = "Cámara trasera OK")]
        public bool CamaraTraseraOk { get; set; } = false;

        [Display(Name = "Altavoz y Micrófono OK")]
        public bool AltavozMicOk { get; set; } = false;

        [Display(Name = "Reconoce tarjeta SIM")]
        public bool LeeSIM { get; set; } = false;

        // --- Observaciones y Firma ---

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder los 500 caracteres.")]
        [Display(Name = "Observaciones Adicionales")]
        public string? Observaciones { get; set; }

        /// <summary>
        /// Guarda la imagen en formato DataURL (data:image/png;base64,...) capturada desde el Canvas.
        /// </summary>
        [Display(Name = "Firma Digital del Cliente")]
        public string? FirmaClienteBase64 { get; set; }

        // --- Propiedad de Navegación ---

        [ForeignKey("IdReparacion")]
        public virtual Reparacion Reparacion { get; set; } = null!;
    }
}