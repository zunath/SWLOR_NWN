using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame.Prospecting;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;

/// <summary>
/// A gatherer's build for the simulator: which lines they bought and which style they play. It names the loadouts the gates run, with
/// every purchase the Gathering rank allows.
/// </summary>
/// <param name="Name">The loadout's name in the reports.</param>
/// <param name="Prospector">Whether the gatherer bought the Prospector line.</param>
/// <param name="TreasureSeeker">Whether the gatherer bought the Treasure Seeker line.</param>
/// <param name="Style">The style they play.</param>
public sealed record GatheringLoadout(string Name, bool Prospector, bool TreasureSeeker, GatheringStyle Style)
{
    /// <summary>No perks and no style: the unperked gatherer every gate already judges.</summary>
    public static readonly GatheringLoadout None = new("No perks", false, false, GatheringStyle.None);

    /// <summary>The loadouts the gates run besides no perks.</summary>
    public static IReadOnlyList<GatheringLoadout> Perked { get; } = new[]
    {
        new GatheringLoadout("Prospector, no style", true, false, GatheringStyle.None),
        new GatheringLoadout("Prospector, Strip Mining", true, false, GatheringStyle.StripMining),
        new GatheringLoadout("Treasure Seeker, no style", false, true, GatheringStyle.None),
        new GatheringLoadout("Treasure Seeker, Careful Hands", false, true, GatheringStyle.CarefulHands),
        new GatheringLoadout("Both lines, no style", true, true, GatheringStyle.None),
        new GatheringLoadout("Both lines, Strip Mining", true, true, GatheringStyle.StripMining),
        new GatheringLoadout("Both lines, Careful Hands", true, true, GatheringStyle.CarefulHands)
    };

    /// <summary>The loadout's name as a command line writes it: lower case with dashes, such as <c>both-lines-strip-mining</c>.</summary>
    public string Slug => Name.ToLowerInvariant().Replace(", ", "-").Replace(' ', '-');

    /// <summary>Every loadout the gates run, the unperked gatherer first.</summary>
    public static IReadOnlyList<GatheringLoadout> All { get; } = new[] { None }.Concat(Perked).ToList();

    /// <summary>The loadout a command line names, or null.</summary>
    public static GatheringLoadout Find(string slug) =>
        All.FirstOrDefault(loadout => string.Equals(loadout.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>The perks the loadout owns.</summary>
    public IEnumerable<PerkType> Owned =>
        (Prospector ? GatheringPerkLines.Prospector : Array.Empty<PerkType>())
        .Concat(TreasureSeeker ? GatheringPerkLines.TreasureSeeker : Array.Empty<PerkType>());

    // The stat pipeline's attribute cache fills itself on first use and is not thread safe; the simulator asks from many threads.
    private static readonly object StatLock = new();

    /// <summary>The stat totals at a Gathering rank.</summary>
    public IReadOnlyDictionary<StatType, int> StatsAt(IReadOnlyDictionary<PerkType, PerkDetail> perks, int rank)
    {
        lock (StatLock)
            return GatheringPerkLines.StatsAt(perks, Owned.Where(perk => OwnsAtRank(perks, perk, rank)), rank, Style);
    }

    public ProspectingEffects ProspectingAt(IReadOnlyDictionary<PerkType, PerkDetail> perks, int rank)
    {
        var stats = StatsAt(perks, rank);
        return ProspectingEffects.FromStats(stat => stats.GetValueOrDefault(stat));
    }

    public SearchEffects SearchAt(IReadOnlyDictionary<PerkType, PerkDetail> perks, int rank)
    {
        var stats = StatsAt(perks, rank);
        return SearchEffects.FromStats(stat => stats.GetValueOrDefault(stat));
    }

    /// <summary>The items each power core refines beyond the Gathering rank's batch.</summary>
    public int RefineBatchAt(IReadOnlyDictionary<PerkType, PerkDetail> perks, int rank) =>
        StatsAt(perks, rank).GetValueOrDefault(StatType.ProspectingRefineBatch);

    private static bool OwnsAtRank(IReadOnlyDictionary<PerkType, PerkDetail> perks, PerkType perk, int rank) =>
        perks[perk].PerkLevels.Values.Any(level => GatheringPerkLines.RequiredRank(level) <= rank);
}
