namespace MonitoreoWeb.Models.ViewModels
{
    public class SolicitudReparacionViewModel
    {
        // ==============================
        // DATOS DE LA REPARACIÓN
        // ==============================

        public int IdReparacion { get; set; }

        public string ProblemaReportado { get; set; } = string.Empty;

        public decimal? CostoEstimado { get; set; }

        public DateTime FechaIngreso { get; set; }

        public DateTime? FechaEntregaEstimada { get; set; }

        public int IdEstado { get; set; }


        // ==============================
        // DATOS DEL DISPOSITIVO
        // ==============================

        public int IdDispositivo { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public string? IMEI { get; set; }

        public string? Color { get; set; }


        // ==============================
        // DATOS DEL CLIENTE
        // ==============================

        public int IdCliente { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public string ApellidoCliente { get; set; } = string.Empty;

        public string TelefonoCliente { get; set; } = string.Empty;

        public string EmailCliente { get; set; } = string.Empty;


        // ==============================
        // DATOS DEL TÉCNICO
        // ==============================

        public int? IdTecnico { get; set; }

        public string? NombreTecnico { get; set; }
    }
}