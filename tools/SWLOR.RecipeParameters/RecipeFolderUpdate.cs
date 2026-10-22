namespace SWLOR.RecipeParameters;

/// <summary>
/// What an update of the recipe definition folder did, or would do in check mode.
/// </summary>
public sealed class RecipeFolderUpdate
{
    public int FilesScanned { get; init; }

    /// <summary>Paths relative to the folder, in order.</summary>
    public IReadOnlyList<string> ChangedFiles { get; init; } = Array.Empty<string>();

    public int ChainEdits { get; init; }
    public int RowEdits { get; init; }
    public int PlumbingEdits { get; init; }

    /// <summary>Recipes the profile covers that the source folder never creates, and source the build does not know.</summary>
    public IReadOnlyList<string> Problems { get; init; } = Array.Empty<string>();
}
