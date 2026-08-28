namespace MonitoreoWeb.Models.ViewModels
{
    public class DispositivoRegistradoViewModel
    {
        // =========================
        // DATOS DEL DISPOSITIVO
        // =========================

        public int IdDispositivo { get; set; }

        public string Marca { get; set; } = string.Empty;

        public string Modelo { get; set; } = string.Empty;

        public string? IMEI { get; set; }

        public string? Color { get; set; }


        // =========================
        // DATOS DEL CLIENTE
        // =========================

        public int IdCliente { get; set; }

        public string NombreCliente { get; set; } = string.Empty;

        public string ApellidoCliente { get; set; } = string.Empty;

        public string TelefonoCliente { get; set; } = string.Empty;

        public string EmailCliente { get; set; } = string.Empty;
    }
}