using SWLOR.Shared.Domain.Character.ValueObjects;

namespace SWLOR.Shared.Domain.Character.Contracts
{
    public interface IStatGroupService
    {
        StatGroup LoadStats(uint creature);
    }
}
