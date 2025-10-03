namespace Task_5_webApp.Services
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(string toEmail, string confirmUrl);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
    }
}
