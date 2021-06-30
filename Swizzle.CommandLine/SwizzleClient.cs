using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Swizzle.Dto;

namespace Swizzle
{
    public sealed class SwizzleClient
    {
        readonly HttpClient _httpClient;

        public SwizzleClient(
            Uri baseEndpointUri,
            string? authToken = null,
            string? collectionKeyOverride = null)
        {
            if (baseEndpointUri is null)
                throw new ArgumentNullException(nameof(baseEndpointUri));

            _httpClient = new HttpClient
            {
                BaseAddress = baseEndpointUri,
                DefaultRequestHeaders =
                {
                    { "Accept", "application/json" }
                }
            };

            if (authToken is not null)
                _httpClient.DefaultRequestHeaders.Add(
                    "Authorization",
                    "Bearer " + authToken);

            if (collectionKeyOverride is not null)
                _httpClient.DefaultRequestHeaders.Add(
                    "X-Swizzle-Collection-Key",
                    collectionKeyOverride);
        }

        static class Endpoint
        {
            public const string Redirect = "api/items/redirect";
        }

        public Task<ItemDto> CreateRedirectAsync(
            Uri targetUri,
            string? slug = null,
            CancellationToken cancellationToken = default)
            => InvokeEndpointAsync<CreateRedirectionItemDto, ItemDto>(
                HttpMethod.Post,
                Endpoint.Redirect,
                new CreateRedirectionItemDto(
                    targetUri,
                    slug),
                cancellationToken: cancellationToken);

        public Task<ItemDto> UpdateRedirectAsync(
            string slug,
            Uri targetUri,
            CancellationToken cancellationToken = default)
            => InvokeEndpointAsync<CreateRedirectionItemDto, ItemDto>(
                HttpMethod.Put,
                Endpoint.Redirect,
                new CreateRedirectionItemDto(
                    targetUri,
                    slug),
                cancellationToken: cancellationToken);

        Task<TResponse> InvokeEndpointAsync<TResponse>(
            HttpMethod method,
            string endpoint,
            CancellationToken cancellationToken = default)
            => InvokeEndpointAsync<object?, TResponse>(
                method,
                endpoint,
                writeRequest: false,
                request: null,
                cancellationToken: cancellationToken);

        Task<TResponse> InvokeEndpointAsync<TRequest, TResponse>(
            HttpMethod method,
            string endpoint,
            TRequest request,
            CancellationToken cancellationToken = default)
            => InvokeEndpointAsync<TRequest, TResponse>(
                method,
                endpoint,
                writeRequest: true,
                request: request,
                cancellationToken: cancellationToken);

        async Task<TResponse> InvokeEndpointAsync<TRequest, TResponse>(
            HttpMethod method,
            string endpoint,
            bool writeRequest,
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            if (endpoint is null)
                throw new ArgumentNullException(nameof(endpoint));

            var requestMessage = new HttpRequestMessage(method, endpoint);

            if (writeRequest)
                requestMessage.Content = JsonContent.Create(
                    request,
                    options: SwizzleJsonSerializerOptions.Default);

            var responseMessage = await _httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            responseMessage.EnsureSuccessStatusCode();

            var responseDto = await responseMessage.Content
                .ReadFromJsonAsync<TResponse>(
                    SwizzleJsonSerializerOptions.Default,
                    cancellationToken);

            return responseDto ?? throw new JsonException();
        }
    }
}
