using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Shared.Domain.Associate.Contracts
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
