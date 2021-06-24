namespace Swizzle.Services
{
    public sealed class CollectionNotRegisteredException : IngestionException
    {
        internal CollectionNotRegisteredException(string collectionKey)
            : base($"A collection with the key '{collectionKey}' is not registered.")
        {
        }
    }
}
