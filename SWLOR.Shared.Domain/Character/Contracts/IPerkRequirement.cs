namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface IPerkRequirement
    {
        string CheckRequirements(uint player);
        string RequirementText { get; }
    }

}
