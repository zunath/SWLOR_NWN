namespace SWLOR.ContentInjection;

/// <summary>The input could not be processed; nothing was written.</summary>
internal sealed class ContentInjectionException : Exception
{
    public ContentInjectionException(string message)
        : base(message)
    {
    }
}
