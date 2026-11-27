namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// One named value of a recipe's minigame parameters, in the units of its parameter definition.
    /// </summary>
    public readonly record struct RecipeParameterValue(string Name, double Value);
}
