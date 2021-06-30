using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Mono.Options;

namespace Swizzle.Client.Commands
{
    abstract class SwizzleCommand : Command
    {
        public SwizzleCommand(string name, string? help = null)
            : base(name, help)
        {
        }

        public ClientSession Session
            => ((SwizzleCommandLineTool)CommandSet).Session;

        public sealed override int Invoke(IEnumerable<string> arguments)
        {
            try
            {
                return InvokeAsync(arguments, default)
                    .GetAwaiter()
                    .GetResult();
            }
            catch (HttpRequestException e) when (
                e.StatusCode is HttpStatusCode.Unauthorized)
            {
                return Error(
                    "token is not authorized; provide a valid token " +
                    " through the 'store-token' command");
            }
            catch (Exception e)
            {
                return Error($"an unexpected error occurred: {e}");
            }
        }

        public virtual Task<int> InvokeAsync(
            IEnumerable<string> arguments,
            CancellationToken cancellationToken)
            => Task.FromResult(0);

        protected static int Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Error.WriteLine($"error: {message}");
            Console.ResetColor();
            return 1;
        }

        protected static void RenderResult<TResult>(TResult result)
            => Console.WriteLine(JsonSerializer.Serialize(
                result,
                SwizzleJsonSerializerOptions.Default));

        protected static Task<int> ErrorAsync(string message)
            => Task.FromResult(Error(message));
    }
}
