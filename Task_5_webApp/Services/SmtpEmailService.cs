using MailKit.Net.Smtp;
using MimeKit;

namespace Task_5_webApp.Services
{
    public class SmtpEmailService : IEmailService
    {
        public async Task SendConfirmationAsync(string toEmail, string confirmUrl)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("User Admin", "no-reply@example.com"));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = "Confirm your account";

            var body = new TextPart("plain")
            {
                Text = $"Click to confirm: {confirmUrl}"
            };
            message.Body = body;

            // IMPORTANT: configure SMTP for testing (e.g., Gmail SMTP or local)
            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("yourgmail@gmail.com", "app_password");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

    }
}
