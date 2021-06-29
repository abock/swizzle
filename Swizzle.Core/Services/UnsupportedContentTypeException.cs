namespace Swizzle.Services
{
    public sealed class UnsupportedContentTypeException : IngestionException
    {
        internal UnsupportedContentTypeException(string contentType)
            : base($"unsupported content type: {contentType}")
        {
        }
    }
}
