namespace SWLOR.Shared.Domain.Contracts
{
    public interface IPerkRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }

}
