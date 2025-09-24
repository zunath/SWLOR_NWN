namespace SWLOR.Shared.Domain.Crafting.Contracts
{
    public interface IRecipeRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }
}
