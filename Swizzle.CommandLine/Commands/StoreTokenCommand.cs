using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Mono.Options;

namespace Swizzle.Client.Commands
{
    sealed class StoreTokenCommand : SwizzleCommand
    {
        bool _showHelp;

        public StoreTokenCommand() : base(
            "store-token",
            "Install or update the API token in the user's keychain; " +
            "if no global --host option is provided, the token will be " +
            "stored as a default fallback for all hosts.")
        {
            Options = new OptionSet
            {        
                "usage: swizzle [options]+ store-token TOKEN",
                "",
                "Command Options:",
                {
                    "h|?|help",
                    "Show this help",
                    (bool v) => _showHelp = v
                }
            };
        }

        public override Task<int> InvokeAsync(
            IEnumerable<string> arguments,
            CancellationToken cancellationToken)
        {
            var positionalArguments = Options.Parse(arguments);

            if (_showHelp)
            {
                Options.WriteOptionDescriptions(Console.Out);
                return Task.FromResult(1);
            }

            if (positionalArguments.Count != 1)
                return ErrorAsync("TOKEN was not provided");

            Session.StoreToken(positionalArguments[0]);

            return Task.FromResult(0);
        }
    }
}
