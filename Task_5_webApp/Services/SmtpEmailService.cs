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

            //configure SMTP for testing (e.g., Gmail SMTP or local)
            using var client = new SmtpClient();
            await client.ConnectAsync("sandbox.smtp.mailtrap.io", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("e49597f4f06e58", "90c5dfae8192aa");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

    }
}
