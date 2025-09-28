namespace Task_5_webApp.Services
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(string toEmail, string confirmUrl);
    }
}
