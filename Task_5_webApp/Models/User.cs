namespace Task_5_webApp.Models
{
    public class User
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string? Designation { get; set; }
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public bool IsEmailVerified { get; set; } = false;
        public string Status { get; set; } = "Unverified"; // unverified/active/blocked

        public DateTime? LastLoginTime { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string VerificationToken { get; set; }

    }
}
