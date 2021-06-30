using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Logging;

using Swizzle.Models;

namespace Swizzle.Services
{
    public sealed class SwizzleAuthenticationService
    {
        readonly IngestionService _ingestionService;
        readonly ILogger<SwizzleAuthenticationService> _logger;

        public SwizzleAuthenticationService(
            IngestionService ingestionService,
            ILogger<SwizzleAuthenticationService> logger)
        {
            _ingestionService = ingestionService;
            _logger = logger;
        }

        public Task<ClaimsPrincipal?> AuthenticateUserAsync(
            HttpRequest request,
            string authenticationType,
            string token,
            int failureDelay = 3000)
        {
            try
            {
                var collection = _ingestionService.GetCollection(request);

                foreach (var usersPath in new[]
                {
                    Path.Combine(
                        _ingestionService.ContentRootPath,
                        collection.Key + ".users.json"),
                    Path.Combine(
                        _ingestionService.ContentRootPath,
                        "_all.users.json")
                })
                {
                    var user = FindUser(usersPath);
                    if (user is not null && user.IsValid)
                        return Task.FromResult<ClaimsPrincipal?>(
                            new ClaimsPrincipal(
                                new ClaimsIdentity(
                                    user.GetClaims(),
                                    authenticationType)));
                }

                return SlowDeny();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error deserializing user store");
            }

            return SlowDeny();

            async Task<ClaimsPrincipal?> SlowDeny()
            {
                await Task.Delay(failureDelay);
                return null;
            }

            SwizzleUser? FindUser(string usersPath)
            {
                if (!File.Exists(usersPath))
                    return null;

                return JsonSerializer
                    .Deserialize<SwizzleUser[]>(
                        File.ReadAllText(usersPath),
                        SwizzleJsonSerializerOptions.Default)
                    ?.FirstOrDefault(user => user.AuthToken == token);
            }
        }
    }
}
