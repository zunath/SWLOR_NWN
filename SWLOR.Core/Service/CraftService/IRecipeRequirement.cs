namespace SWLOR.Core.Service.CraftService
{
    public interface IRecipeRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }
}
