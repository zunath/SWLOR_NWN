using SWLOR.Game.Server.Domain.World;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    public sealed record FishingPool(
        FishingLocationType Location,
        FishingRodType Rod,
        FishingBaitType Bait,
        FishTimeOfDayType TimeOfDay,
        IReadOnlyList<FishingPoolEntry> Entries);
}
