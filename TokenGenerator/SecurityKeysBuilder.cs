using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TokenGenerator
{
    public static class SecurityKeysBuilder
    {
        public static IAuthCreds Build(string salt, string signingKeySource, string encryptionKeySource)
        {
            if (string.IsNullOrEmpty(salt))
            {
                throw new ArgumentException($"'{nameof(salt)}' cannot be null or empty.", nameof(salt));
            }

            if (string.IsNullOrEmpty(signingKeySource))
            {
                throw new ArgumentException($"'{nameof(signingKeySource)}' cannot be null or empty.", nameof(signingKeySource));
            }

            if (string.IsNullOrEmpty(encryptionKeySource))
            {
                throw new ArgumentException($"'{nameof(encryptionKeySource)}' cannot be null or empty.", nameof(encryptionKeySource));
            }

            SymmetricSecurityKey signingKey;
            SigningCredentials signingCredentials;
            SymmetricSecurityKey encryptionKey;
            EncryptingCredentials encryptingCredentials;

            var saltValueBytes = Encoding.ASCII.GetBytes(salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(signingKeySource, saltValueBytes, 1, HashAlgorithmName.SHA512))
            {
                signingKey = new SymmetricSecurityKey(pbkdf2.GetBytes(256 / 8));
                signingCredentials = new SigningCredentials(
                    signingKey,
                    SecurityAlgorithms.HmacSha256);
            }
            using (var pbkdf2 = new Rfc2898DeriveBytes(encryptionKeySource, saltValueBytes, 1, HashAlgorithmName.SHA512))
            {
                encryptionKey = new SymmetricSecurityKey(pbkdf2.GetBytes(256 / 8));
                encryptingCredentials = new EncryptingCredentials(
                    encryptionKey,
                    SecurityAlgorithms.Aes256KW,
                    SecurityAlgorithms.Aes256CbcHmacSha512);
            }

            return new AuthCreds
            {
                EncryptingCredentials = encryptingCredentials,
                EncryptionKey = encryptionKey,
                SigningCredentials = signingCredentials,
                SigningKey = signingKey,
            };
        }
    }

    public class AuthCreds : IAuthCreds
    {
        public required SymmetricSecurityKey SigningKey { get; init; }
        public required SymmetricSecurityKey EncryptionKey { get; init; }
        public required EncryptingCredentials EncryptingCredentials { get; init; }
        public required SigningCredentials SigningCredentials { get; init; }
    }

    public interface IAuthCreds
    {
        SymmetricSecurityKey SigningKey { get; }
        SymmetricSecurityKey EncryptionKey { get; }
        EncryptingCredentials EncryptingCredentials { get; }
        SigningCredentials SigningCredentials { get; }
    }
}
