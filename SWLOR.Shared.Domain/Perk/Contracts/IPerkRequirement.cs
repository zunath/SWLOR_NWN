namespace SWLOR.Shared.Domain.Perk.Contracts
{
    public interface IPerkRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }

}
