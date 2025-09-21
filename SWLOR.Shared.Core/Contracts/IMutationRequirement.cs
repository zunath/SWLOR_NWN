using SWLOR.Shared.Core.Data.Entity;

namespace SWLOR.Shared.Core.Contracts
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
