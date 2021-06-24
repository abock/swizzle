namespace Swizzle.Services
{
    public sealed class IllegalSlugException : IngestionException
    {
        internal IllegalSlugException(
            string slug,
            string legalAlphabet)
            : base(
                $"Invalid slug '{slug}'; slug must conform to the " +
                $"alphabet '{legalAlphabet}'")
        {
        }
    }
}
