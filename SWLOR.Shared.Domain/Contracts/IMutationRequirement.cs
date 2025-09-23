using SWLOR.Shared.Domain.Model;

namespace SWLOR.Shared.Domain.Contracts
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
