using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MonitoreoWeb.Models
{
    [Table("Reparacion")]
    public class Reparacion
    {
        [Key]
        public int IdReparacion { get; set; }

        public int IdDispositivo { get; set; }

        public int? IdTecnico { get; set; }

        public int IdEstado { get; set; } = 1;

        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        public DateTime? FechaEntregaEstimada { get; set; }
        public DateTime? FechaTerminacion { get; set; }
        public DateTime? FechaEntregaCliente { get; set; }

        public string? ProblemaReportado { get; set; }
        public string? Diagnostico { get; set; }

        public decimal? CostoEstimado { get; set; }
        public decimal? CostoFinal { get; set; }

        public int DiasGarantia { get; set; } = 30;
        public bool EsGarantia { get; set; }
        public int? IdReparacionOriginal { get; set; }
        public bool Activo { get; set; } = true;

        // CAMPO: PRIORIDAD (Urgente, Alta, Normal)
        public string Prioridad { get; set; } = "Normal";

        // PROPIEDAD AUXILIAR DE COMPATIBILIDAD
        [NotMapped]
        public DateTime? FechaEntregaReal
        {
            get => FechaEntregaCliente ?? FechaTerminacion;
            set => FechaTerminacion = value;
        }

        // PROPIEDAD AUXILIAR: IMEI CENSURADO (Ej. 2334••••••••)
        [NotMapped]
        public string ImeiCensurado
        {
            get
            {
                var imei = Dispositivo?.IMEI;
                if (string.IsNullOrWhiteSpace(imei)) return "No registrado";
                imei = imei.Trim();
                if (imei.Length <= 4) return imei;
                return imei.Substring(0, 4) + new string('•', imei.Length - 4);
            }
        }

        // ==========================================
        // RELACIONES DE NAVEGACIÓN
        // ==========================================

        [ForeignKey("IdDispositivo")]
        public virtual Dispositivo? Dispositivo { get; set; }

        [ForeignKey("IdTecnico")]
        public virtual Usuario? Tecnico { get; set; }

        public virtual ICollection<TokenConsulta> Tokens { get; set; } = new List<TokenConsulta>();

        public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

        public virtual ChecklistRecepcion? Checklist { get; set; }

        [NotMapped]
        public string TokenConsulta => Tokens?.FirstOrDefault(t => t.Activo)?.CodigoUnico ?? "SIN-TOKEN";

        public string? FirmaAdminEntregaBase64 { get; set; }

        public int? IdUsuario { get; set; }

        // Propiedad de navegación opcional para saber quién la registró
        [ForeignKey("IdUsuario")]
        public virtual Usuario? UsuarioCrea { get; set; }
    }
}