using MailKit.Net.Smtp;
using MimeKit;

namespace MonitoreoWeb.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task EnviarVerificacionAsync(string emailDestino, string token, string baseUrl)
        {
            var link = $"{baseUrl}/Clientes/Verificar?token={token}";

            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress("Taller Reparaciones", _config["MailSettings:From"]));
            mensaje.To.Add(new MailboxAddress("", emailDestino));
            mensaje.Subject = "Verifica tu correo electrónico";

            mensaje.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>¡Bienvenido al Taller!</h2>
                    <p>Gracias por registrarte. Haz clic en el botón para verificar tu correo:</p>
                    <a href='{link}'
                       style='background:#4F46E5;color:white;padding:12px 24px;
                              text-decoration:none;border-radius:6px;display:inline-block;'>
                        Verificar Email
                    </a>
                    <p>Este enlace expira en 24 horas.</p>
                    <p>Si no creaste esta cuenta, ignora este mensaje.</p>"
            };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                _config["MailSettings:Host"],
                int.Parse(_config["MailSettings:Port"]!),
                false
            );
            await smtp.AuthenticateAsync(
                _config["MailSettings:User"],
                _config["MailSettings:Pass"]
            );
            await smtp.SendAsync(mensaje);
            await smtp.DisconnectAsync(true);
        }
    }
}