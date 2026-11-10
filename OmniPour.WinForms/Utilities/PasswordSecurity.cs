using System;
using System.Security.Cryptography;

namespace OmniPour.WinForms.Utilities
{
    public static class PasswordSecurity
    {
        private const int SaltByteSize = 16;
        private const int HashByteSize = 32;
        private const int Iterations = 100000;

        public static (string hash, string salt) HashPassword(string plainTextPassword)
        {
            if (string.IsNullOrEmpty(plainTextPassword))
                throw new ArgumentException("Password cannot be empty.", nameof(plainTextPassword));

            byte[] saltBytes = new byte[SaltByteSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }

            using var pbkdf2 = new Rfc2898DeriveBytes(plainTextPassword, saltBytes, Iterations, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(HashByteSize);

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool VerifyPassword(string plainTextPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(plainTextPassword) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            try
            {
                byte[] saltBytes = Convert.FromBase64String(storedSalt);
                byte[] expectedHashBytes = Convert.FromBase64String(storedHash);

                using var pbkdf2 = new Rfc2898DeriveBytes(plainTextPassword, saltBytes, Iterations, HashAlgorithmName.SHA256);
                byte[] actualHashBytes = pbkdf2.GetBytes(HashByteSize);

                return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
            }
            catch
            {
                return false;
            }
        }
    }
}
