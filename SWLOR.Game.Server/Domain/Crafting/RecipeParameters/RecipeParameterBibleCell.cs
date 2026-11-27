namespace SWLOR.Game.Server.Domain.Crafting.RecipeParameters
{
    /// <summary>
    /// One Bible cell: the column header, the text shown, and whether the text is a number.
    /// </summary>
    public readonly record struct RecipeParameterBibleCell(string Header, string Text, bool IsNumber);
}
