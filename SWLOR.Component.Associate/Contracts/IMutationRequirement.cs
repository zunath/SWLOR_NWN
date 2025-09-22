using SWLOR.Component.Associate.Entity;

namespace SWLOR.Component.Associate.Contracts
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
