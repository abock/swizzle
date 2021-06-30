using Mono.Options;

using Swizzle.Client.Commands;

namespace Swizzle.Client
{
    sealed class SwizzleCommandLineTool : CommandSet
    {
        public ClientSession Session { get; }

        public SwizzleCommandLineTool(ClientSession session)
            : base("swizzle")
        {
            Session = session;

            Add("usage: swizzle [options]+ COMMAND [command_options]+");
            Add("");
            Add("Global Options:");
            Add("h|?|help",
                "Show this help",
                v => { });
            Add("json",
                "Always output JSON results",
                v => Session.Json = v is not null);
            Add("host=",
                $"Use {{HOST}} (default is '{Session.DefaultHost}')",
                v => Session.Host = v);
            Add("c|collection-key=",
                "Override the collection {KEY} to use against the host.",
                v => Session.OverrideCollectionKey = v);
            Add("");
            Add("Commands:");
            Add(new StoreTokenCommand());
            Add("");
            Add(new RedirectCommand());
        }
    }
}
