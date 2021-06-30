using Swizzle.Client;

var session = new ClientSession
{
    DefaultHost = "swzl.me"
};

if (args.Length == 0)
    args = new[] { "help" };

return new SwizzleCommandLineTool(session).Run(args);
