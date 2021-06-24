namespace Swizzle.Services
{
    public sealed class IllegalPathException : IngestionException
    {
        internal IllegalPathException(string message) : base(message)
        {
        }
    }
}
