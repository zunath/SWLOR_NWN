namespace SWLOR.RecipeParameters;

/// <summary>
/// The writer met source it cannot edit safely. It never guesses: the message names the file and line.
/// </summary>
public sealed class RecipeSourceWriterException : Exception
{
    public RecipeSourceWriterException(string message)
        : base(message)
    {
    }
}
