using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace MonitoreoWeb.Services
{
    public class TelegramService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public TelegramService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<bool> EnviarNotificacionAsync(string chatId, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(chatId))
            {
                return false; // Si el cliente no tiene Chat ID registrado, no hace nada
            }

            var token = _configuration["TelegramSettings:BotToken"];
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            // URL oficial de la API de Telegram para enviar mensajes
            var url = $"https://api.telegram.org/bot{token}/sendMessage?chat_id={chatId}&text={System.Net.WebUtility.UrlEncode(mensaje)}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                // Manejo básico de errores de red en caso de que falle la conexión
                return false;
            }
        }
    }
}