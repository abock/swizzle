namespace Swizzle.Services
{
    public sealed class UnsupportedContentTypeException : IngestionException
    {
        internal UnsupportedContentTypeException(string filePath)
            : base($"unsupported content type: {filePath}")
        {
        }
    }
}
