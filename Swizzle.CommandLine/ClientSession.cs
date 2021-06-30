using System;

using Xamarin.Security;

namespace Swizzle.Client
{
    sealed class ClientSession
    {
        const string KeychainServiceName = "me.swzl.api-tokens";
        const string DefaultAuthTokenKey = "$Default";

        public string? DefaultHost { get; set; }
        public string? Host { get; set; }
        public string? OverrideCollectionKey { get; set; }
        public bool Json { get; set; }

        public string? AuthToken
        {
            get
            {
                var host = Host?.ToLowerInvariant();
                if (host is not null && Keychain.TryGetSecret(
                    KeychainServiceName,
                    host,
                    out var secret))
                    return secret;

                if (Keychain.TryGetSecret(
                    KeychainServiceName,
                    DefaultAuthTokenKey,
                    out secret))
                    return secret;

                return null;
            }
        }

        public bool StoreToken(string token)
        {
            Keychain.StoreSecret(
                KeychainServiceName,
                Host ?? DefaultAuthTokenKey,
                token);
            return true;
        }

        public SwizzleClient CreateClient()
        {
            var host = Host ?? DefaultHost;
            if (!Uri.TryCreate(
                    host,
                    UriKind.Absolute,
                    out var baseEndpointUri) ||
                baseEndpointUri.Scheme?.ToLowerInvariant() is not
                    ("https" or "http"))
                baseEndpointUri = new($"https://{host}");

            var authToken = AuthToken;
            if (authToken is null)
                throw new Exception(
                    $"no auth token is stored for the host '{host}'; " +
                    "use the store-token command first");

            return new(
                baseEndpointUri,
                authToken,
                OverrideCollectionKey);
        }
    }
}
