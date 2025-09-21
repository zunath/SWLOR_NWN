using SWLOR.Shared.Core.Data.Entity;

namespace SWLOR.Game.Server.Service.BeastMasteryService
{
    public interface IMutationRequirement
    {
        string CheckRequirements(IncubationJob job);
    }
}
