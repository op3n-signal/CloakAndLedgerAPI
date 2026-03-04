using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace TokenGenerator
{
    public static class Security
    {

        public const string Role = "Basic";
        public const string Policy = "BasicPolicy";
        public const string Issuer = "TemplateMiddleware";
        public const string Audience = "TemplateUsers";

        private static TokenContainer GetTokenForClaims(IEnumerable<Claim> claims, DateTime now, DateTime expires, IAuthCreds authCreds)
        {
            var handler = new JwtSecurityTokenHandler();
            string tokenText;

            var signedAndEncryptedToken = handler.CreateJwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                notBefore: now,
                expires: expires,
                issuedAt: now,
                signingCredentials: authCreds.SigningCredentials,
                subject: new ClaimsIdentity(claims),
                encryptingCredentials: authCreds.EncryptingCredentials
            );

            tokenText = handler.WriteToken(signedAndEncryptedToken);

            return new TokenContainer
            {
                Expires = expires,
                Token = tokenText
            };
        }

        public static TokenContainer GenerateUserToken(string username, TimeSpan timeToLive, IAuthCreds authCreds)
        {
            var now = DateTime.UtcNow;
            var expires = now.Add(timeToLive);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, Role),
            };

            return GetTokenForClaims(claims, now, expires, authCreds);
        }
    }

    public class TokenContainer
    {
        public string Token;
        public DateTime Expires;
    }
}
