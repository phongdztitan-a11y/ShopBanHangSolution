using System.Security.Cryptography;
using System.Text;

namespace WebApplication3.Security
{
    public static class ApiTokenService
    {
        private const int TokenDays = 7;

        public static string GetTokenSecret() =>
            Environment.GetEnvironmentVariable("SHOPBANHANG_TOKEN_SECRET")
            ?? "dev-only-change-this-secret-on-render";

        public static string CreateToken(string userId)
        {
            var expiresUtc = DateTimeOffset.UtcNow.AddDays(TokenDays).ToUnixTimeSeconds();
            var payload = $"{userId}|{expiresUtc}";
            var signature = SignTokenPayload(payload);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{payload}|{signature}"));
        }

        public static bool TryGetUserId(HttpRequest request, out string userId)
        {
            userId = string.Empty;
            var header = request.Headers.Authorization.ToString();
            if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                var raw = Encoding.UTF8.GetString(Convert.FromBase64String(header["Bearer ".Length..].Trim()));
                var parts = raw.Split('|');
                if (parts.Length != 3 || !long.TryParse(parts[1], out var expiresUtc))
                    return false;

                if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiresUtc)
                    return false;

                var payload = $"{parts[0]}|{parts[1]}";
                var expected = Encoding.UTF8.GetBytes(SignTokenPayload(payload));
                var actual = Encoding.UTF8.GetBytes(parts[2]);
                if (actual.Length != expected.Length ||
                    !CryptographicOperations.FixedTimeEquals(actual, expected))
                {
                    return false;
                }

                userId = parts[0];
                return !string.IsNullOrWhiteSpace(userId);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string SignTokenPayload(string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(GetTokenSecret()));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
