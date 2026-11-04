using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    public sealed record FishingPool(
        FishingLocationType Location,
        FishingRodType Rod,
        FishingBaitType Bait,
        FishTimeOfDayType TimeOfDay,
        IReadOnlyList<FishingPoolEntry> Entries);
}
