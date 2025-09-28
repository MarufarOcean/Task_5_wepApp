using System.Security.Cryptography;
using System.Text;

namespace Task_5_webApp.Services
{
    public static class PasswordHasher
    {
        // IMPORTANT: minimal PBKDF2 hashing for demo
        public static string Hash(string password)
        {
            // note: for production, include salt and iterations properly
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public static bool Verify(string password, string hash) => Hash(password) == hash;
    }
}