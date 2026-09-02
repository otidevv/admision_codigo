using ADMISION.Services.Interfaces;
using ADMISION.ENTITIES.Models.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace ADMISION.Services.Implementations
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<Users> _identityHasher = new();
        private readonly ILogger<PasswordHasher>? _logger;

        public PasswordHasher(ILogger<PasswordHasher>? logger = null)
        {
            _logger = logger;
        }

        public string HashPassword(string password)
        {
            return _identityHasher.HashPassword(default!, password);
        }

        public bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(inputPassword))
                return false;

            try
            {
                var result = _identityHasher.VerifyHashedPassword(default!, storedHash, inputPassword);
                if (result == PasswordVerificationResult.Success ||
                    result == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    return true;
                }
            }
            catch
            {
                // no es un hash Identity válido — intentar formato legado salt.hash
            }

            if (storedHash.Contains('.'))
            {
                try
                {
                    var parts = storedHash.Split('.');
                    if (parts.Length == 2)
                    {
                        var salt = Convert.FromBase64String(parts[0]);
                        var storedSubkey = parts[1];

                        var hashedInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                            password: inputPassword,
                            salt: salt,
                            prf: KeyDerivationPrf.HMACSHA256,
                            iterationCount: 100000,
                            numBytesRequested: 256 / 8));

                        return CryptographicOperations.FixedTimeEquals(
                            System.Text.Encoding.ASCII.GetBytes(hashedInput),
                            System.Text.Encoding.ASCII.GetBytes(storedSubkey));
                    }
                }
                catch
                {
                    _logger?.LogWarning("Formato de hash legado inválido; rechazando autenticación.");
                }
            }

            return false;
        }
    }
}
