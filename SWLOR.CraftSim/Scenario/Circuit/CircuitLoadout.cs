using System.Collections.Concurrent;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CraftSim.Scenario.Circuit
{
    /// <summary>
    /// The perks a simulated Engineering crafter has bought and the style they play: every purchase of the chosen lines that the
    /// crafter's rank allows, read from the real perk definitions, and the style's status effect from rank 20. It is a
    /// <see cref="CraftStyle"/>'s configuration; a crafter without one plays without perks.
    /// </summary>
    /// <param name="Style">The active style, or <see cref="CircuitStyle.None"/>.</param>
    /// <param name="Overclocker">True when the crafter has bought the Overclocker line.</param>
    /// <param name="Technician">True when the crafter has bought the Technician line.</param>
    public sealed record CircuitLoadout(CircuitStyle Style, bool Overclocker, bool Technician)
    {
        private static readonly Lazy<IReadOnlyDictionary<PerkType, PerkDetail>> Perks =
            new(() => new EngineeringPerkDefinition().BuildPerks());

        // The game's stat code fills its caches without locking, so each loadout and rank is worked out once, under one lock.
        private static readonly object StatLock = new();
        private static readonly ConcurrentDictionary<(CircuitLoadout Loadout, int Rank), CircuitPerkStats> Computed = new();

        /// <summary>The perk stats at the given Engineering rank.</summary>
        public CircuitPerkStats StatsAt(int rank)
        {
            if (Computed.TryGetValue((this, rank), out var known))
                return known;

            lock (StatLock)
            {
                return Computed.GetOrAdd((this, rank), key =>
                {
                    var owned = (key.Loadout.Overclocker ? CircuitPerkLines.Overclocker : Array.Empty<PerkType>())
                        .Concat(key.Loadout.Technician ? CircuitPerkLines.Technician : Array.Empty<PerkType>());
                    return CircuitPerkLines.StatsAt(Perks.Value, owned, key.Rank, key.Loadout.Style);
                });
            }
        }

        /// <summary>A style named for the gates, such as "Overclock (both lines)".</summary>
        public CraftStyle ToStyle(string name) => new(name, this);
    }
}
