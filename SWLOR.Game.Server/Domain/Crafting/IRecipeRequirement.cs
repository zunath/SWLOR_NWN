namespace SWLOR.Game.Server.Domain.Crafting
{
    public interface IRecipeRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }
}
