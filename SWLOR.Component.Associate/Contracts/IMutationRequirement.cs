using SWLOR.Component.Associate.Entity;
using SWLOR.Shared.Domain.Entity;

namespace SWLOR.Component.Associate.Contracts
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
