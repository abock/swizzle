using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Mono.Options;

using Swizzle.Dto;

namespace Swizzle.Client.Commands
{
    sealed class RedirectCommand : SwizzleCommand
    {
        bool _showHelp;
        bool _updateRedirect;
        string? _slug;

        public RedirectCommand() : base(
            "redirect",
            "Create a redirection URL (short link) on the host.")
        {
            Options = new OptionSet
            {        
                "usage: swizzle [options]+ redirect [command-options]+ TARGET_URL",
                "",
                "Command Options:",
                {
                    "h|?|help",
                    "Show this help",
                    v => _showHelp = v is not null
                },
                {
                    "s|slug=",
                    "Attempt to use {SLUG} instead of forming a new one",
                    v => _slug = v
                },
                {
                    "update",
                    "Update an existing redirection if it exists",
                    v => _updateRedirect = v is not null
                },
            };
        }

        public async override Task<int> InvokeAsync(
            IEnumerable<string> arguments,
            CancellationToken cancellationToken)
        {
            var positionalArguments = Options.Parse(arguments);

            if (_showHelp)
            {
                Options.WriteOptionDescriptions(Console.Out);
                return 1;
            }

            if (positionalArguments.Count != 1)
                return Error("TARGET_URL was not provided");

            var client = Session.CreateClient();
            var targetUrl = new Uri(positionalArguments.Single());

            ItemDto result;

            if (_updateRedirect)
            {
                if (_slug is null)
                    return Error("The --slug option is required for updates");

                result = await client.UpdateRedirectAsync(
                    _slug,
                    targetUrl,
                    cancellationToken);
            }
            else
            {
                result = await client.CreateRedirectAsync(
                    targetUrl,
                    _slug,
                    cancellationToken);
            }

            if (Session.Json)
                RenderResult(result);
            else
                Console.WriteLine(result.Uri);

            return 0;
        }
    }
}
