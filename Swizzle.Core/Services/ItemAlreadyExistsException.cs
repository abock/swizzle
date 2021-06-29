namespace Swizzle.Services
{
    public sealed class ItemAlreadyExistsException : IngestionException
    {
        internal ItemAlreadyExistsException(string collectionKey, string slug)
            : base($"item already exists in collection '{collectionKey}': {slug}")
        {
        }
    }
}
