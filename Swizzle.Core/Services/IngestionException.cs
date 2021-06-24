using System;

namespace Swizzle.Services
{
    public abstract class IngestionException : Exception
    {
        private protected IngestionException(string message) : base(message)
        {
        }
    }
}
