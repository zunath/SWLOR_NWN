namespace SWLOR.Shared.Domain.Contracts
{
    public interface IRecipeRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }
}
