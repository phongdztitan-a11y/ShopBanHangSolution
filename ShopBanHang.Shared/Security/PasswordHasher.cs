using System;
using System.Security.Cryptography;

namespace ShopBanHang.Shared.Security
{
    public static class PasswordHasher
    {
        private const string Prefix = "PBKDF2";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 100_000;

        public static string Hash(string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var key = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize);

            return string.Join(
                "$",
                Prefix,
                Iterations,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(key));
        }

        public static bool Verify(string password, string storedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(storedPassword))
                return false;

            if (!IsHashed(storedPassword))
                return storedPassword == password;

            var parts = storedPassword.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations))
                return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expectedKey = Convert.FromBase64String(parts[3]);
                var actualKey = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    expectedKey.Length);

                return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool IsHashed(string storedPassword) =>
            storedPassword.StartsWith(Prefix + "$", StringComparison.Ordinal);

        public static string HashIfNeeded(string passwordOrHash)
        {
            if (string.IsNullOrWhiteSpace(passwordOrHash))
                return string.Empty;

            return IsHashed(passwordOrHash) ? passwordOrHash : Hash(passwordOrHash);
        }
    }
}
