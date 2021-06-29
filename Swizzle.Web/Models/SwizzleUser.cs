using System.Collections.Generic;
using System.Security.Claims;

namespace Swizzle.Models
{
    public sealed record SwizzleUser(
        string Username,
        string FullName,
        string Email,
        string AuthToken)
    {
        public bool IsValid =>
            !string.IsNullOrEmpty(Username) &&
            !string.IsNullOrEmpty(FullName) &&
            !string.IsNullOrEmpty(Email) &&
            !string.IsNullOrEmpty(AuthToken) && AuthToken.Length >= 32;
        
        public IEnumerable<Claim> GetClaims()
        {
            yield return new Claim(ClaimTypes.NameIdentifier, Username);
            yield return new Claim(ClaimTypes.Name, FullName);
            yield return new Claim(ClaimTypes.Email, Email);
        }
    }
}
