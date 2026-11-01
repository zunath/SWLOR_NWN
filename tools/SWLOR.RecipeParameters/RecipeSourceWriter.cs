namespace SWLOR.RecipeParameters;

/// <summary>
/// Writes generated minigame parameters into recipe definition source with Roslyn. It only inserts, replaces
/// or deletes the parameter arguments, so every other byte of a file, including its line endings, is kept.
/// </summary>
public static class RecipeSourceWriter
{
    /// <summary>
    /// Rewrites one file. <paramref name="planFor"/> maps a <c>RecipeType</c> member name to what its source needs.
    /// </summary>
    /// <remarks>
    /// Handles three shapes of recipe creation:
    /// <list type="bullet">
    /// <item><c>_builder.Create(RecipeType.X, ...)</c> chains get the builder call inserted or replaced at the chain's end.</item>
    /// <item>A helper method whose chain starts <c>_builder.Create(parameter, ...)</c> gains an optional
    /// <c>RecipeMinigameParameters parameters</c> argument that it passes to <c>.MinigameParameters(parameters)</c>,
    /// and each call of the helper gets <c>parameters: new X(...)</c>.</item>
    /// <item>A foreach over a record array whose chain starts <c>_builder.Create(recipe.Type, ...)</c> gains an
    /// optional <c>Parameters</c> record member that the chain passes on, and each <c>new Record(...)</c> row
    /// gets <c>Parameters: new X(...)</c>.</item>
    /// </list>
    /// Any other shape throws <see cref="RecipeSourceWriterException"/>.
    /// </remarks>
    public static RecipeSourceRewrite Rewrite(string path, string text, Func<string, RecipeParameterPlan> planFor)
    {
        return new RecipeSourceRewriter(path, text, planFor).Run();
    }
}
