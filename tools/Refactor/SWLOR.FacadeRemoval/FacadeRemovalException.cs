namespace SWLOR.FacadeRemoval;

/// <summary>The input could not be processed (a malformed data file, a handler of a removed facade with no rename); nothing is written.</summary>
internal sealed class FacadeRemovalException : Exception
{
    public FacadeRemovalException(string message)
        : base(message)
    {
    }
}
