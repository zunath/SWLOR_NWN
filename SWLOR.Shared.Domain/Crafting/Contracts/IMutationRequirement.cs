using SWLOR.Shared.Domain.Crafting.ValueObjects;

namespace SWLOR.Shared.Domain.Crafting.Contracts
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
