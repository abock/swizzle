using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Swizzle.Services
{
    public sealed class SwizzleAuthenticationOptions
        : AuthenticationSchemeOptions
    {
    }

    public sealed class SwizzleAuthenticationHandler
        : AuthenticationHandler<SwizzleAuthenticationOptions>
    {
        readonly SwizzleAuthenticationService _authenticationService;

        public SwizzleAuthenticationHandler(
            SwizzleAuthenticationService authenticationService,
            IOptionsMonitor<SwizzleAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock)
            : base(
                options,
                logger,
                encoder,
                clock)
        {
            _authenticationService = authenticationService;
        }

        protected override Task HandleChallengeAsync(
            AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = $"Bearer, charset=\"UTF-8\"";
            return base.HandleChallengeAsync(properties);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization") ||
                !AuthenticationHeaderValue.TryParse(
                    Request.Headers["Authorization"],
                    out var authHeader) ||
                authHeader is null ||
                authHeader.Scheme != "Bearer" ||
                authHeader.Parameter is null)
                return AuthenticateResult.NoResult();

            var user = await _authenticationService.AuthenticateUserAsync(
                Request,
                Scheme.Name,
                authHeader.Parameter);

            if (user is null)
                return AuthenticateResult.Fail("token is not authorized");
            
            return AuthenticateResult.Success(
                new AuthenticationTicket(user, Scheme.Name));
        }
    }
}
