using System.Collections.Concurrent;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CraftSim.Scenario.Forge
{
    /// <summary>
    /// The perks a simulated Smithery crafter has bought and the style they play: every purchase of the chosen lines that the
    /// crafter's rank allows, read from the real perk definitions, and the style's status effect from rank 20. It is a
    /// <see cref="CraftStyle"/>'s configuration; a crafter without one plays without perks.
    /// </summary>
    /// <param name="Style">The active style, or <see cref="ForgeStyle.None"/>.</param>
    /// <param name="Forgemaster">True when the crafter has bought the Forgemaster line.</param>
    /// <param name="Artisan">True when the crafter has bought the Artisan line.</param>
    public sealed record ForgeLoadout(ForgeStyle Style, bool Forgemaster, bool Artisan)
    {
        private static readonly Lazy<IReadOnlyDictionary<PerkType, PerkDetail>> Perks =
            new(() => new SmitheryPerkDefinition().BuildPerks());

        // The game's stat code fills its caches without locking, so each loadout and rank is worked out once, under one lock.
        private static readonly object StatLock = new();
        private static readonly ConcurrentDictionary<(ForgeLoadout Loadout, int Rank), ForgePerkStats> Computed = new();

        /// <summary>The perk stats at the given Smithery rank.</summary>
        public ForgePerkStats StatsAt(int rank)
        {
            if (Computed.TryGetValue((this, rank), out var known))
                return known;

            lock (StatLock)
            {
                return Computed.GetOrAdd((this, rank), key =>
                {
                    var owned = (key.Loadout.Forgemaster ? ForgePerkLines.Forgemaster : Array.Empty<PerkType>())
                        .Concat(key.Loadout.Artisan ? ForgePerkLines.Artisan : Array.Empty<PerkType>());
                    return ForgePerkLines.StatsAt(Perks.Value, owned, key.Rank, key.Loadout.Style);
                });
            }
        }

        /// <summary>The loadout in words for a report, such as "both perk lines in full (every purchase their Smithery rank allows) and plays Hot Forging".</summary>
        public string Describe()
        {
            var lines = Forgemaster && Artisan ? "both perk lines" : Forgemaster ? "the Forgemaster line" : Artisan ? "the Artisan line" : "no perk line";
            var style = Style == ForgeStyle.None ? "plays no style" : $"plays {ForgeStyles.Name(Style)}";
            return $"{lines}{(Forgemaster || Artisan ? " in full (every purchase their Smithery rank allows)" : string.Empty)} and {style}";
        }

        /// <summary>A style named for the gates, such as "Hot Forging (both lines)".</summary>
        public CraftStyle ToStyle(string name) => new(name, this);
    }
}
