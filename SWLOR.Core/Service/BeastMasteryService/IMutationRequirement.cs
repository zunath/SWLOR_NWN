using SWLOR.Core.Entity;

namespace SWLOR.Core.Service.BeastMasteryService
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
