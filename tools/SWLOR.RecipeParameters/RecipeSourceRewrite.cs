namespace SWLOR.RecipeParameters;

/// <summary>
/// The result of rewriting one recipe definition file.
/// </summary>
public sealed class RecipeSourceRewrite
{
    public string Text { get; }
    public bool Changed { get; }

    /// <summary>Every <c>RecipeType</c> member the file creates, whether or not it was edited.</summary>
    public IReadOnlyCollection<string> RecipesVisited { get; }

    /// <summary>Edits to builder chains, to helper and record data rows, and to helper signatures.</summary>
    public int ChainEdits { get; }
    public int RowEdits { get; }
    public int PlumbingEdits { get; }

    public RecipeSourceRewrite(
        string text,
        bool changed,
        IReadOnlyCollection<string> recipesVisited,
        int chainEdits,
        int rowEdits,
        int plumbingEdits)
    {
        Text = text;
        Changed = changed;
        RecipesVisited = recipesVisited;
        ChainEdits = chainEdits;
        RowEdits = rowEdits;
        PlumbingEdits = plumbingEdits;
    }
}
