using MailKit.Net.Smtp;
using MimeKit;

namespace Task_5_webApp.Services
{
    public class SmtpEmailService : IEmailService
    {
        private const string SmtpHost = "sandbox.smtp.mailtrap.io";
        private const int SmtpPort = 587;
        private const string SmtpUsername = "e49597f4f06e58";
        private const string SmtpPassword = "90c5dfae8192aa";
        private const string FromName = "User Admin ";
        private const string FromEmail = "no-reply@example.com";

        public Task SendConfirmationAsync(string toEmail, string confirmUrl)
        {
            var subject = "Confirm your account";
            var text = $"Click to confirm: {confirmUrl}";

            return SendEmailAsync(toEmail, subject, text);
        }

        public Task SendPasswordResetAsync(string toEmail, string resetLink)
        {
            var subject = "Password Reset Request";
            var text = $"Click here to reset your password: {resetLink}\n\nThis link will expire in 24 hours.";

            return SendEmailAsync(toEmail, subject, text);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string text)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromEmail));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = text };

            using var client = new SmtpClient();
            await client.ConnectAsync(SmtpHost, SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(SmtpUsername, SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}