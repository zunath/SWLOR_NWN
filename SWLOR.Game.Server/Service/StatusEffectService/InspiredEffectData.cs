namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// What an Inspired status effect grants, composed from the dancer and musician a watcher follows.
    /// Value equality lets the audience tick tell whether the composition changed (rebuild the effect)
    /// or not (only refresh its remaining time).
    /// </summary>
    public sealed record InspiredEffectData
    {
        /// <summary>Percent skill XP: the followed performers' SOC-scaled bonuses plus 1% per venue point.</summary>
        public int XPBonusPercent { get; init; }

        /// <summary>Percent movement speed while out of combat (Light Feet).</summary>
        public int OutOfCombatMovementSpeedPercent { get; init; }

        public int Might { get; init; }
        public int Agility { get; init; }
        public int Vitality { get; init; }
        public int Perception { get; init; }
        public int Willpower { get; init; }
        public int Social { get; init; }

        /// <summary>Control and Craftsmanship for every crafting skill (Steady Hands).</summary>
        public int CraftBonus { get; init; }

        /// <summary>Percent-point fishing catch chance (Angler's Lullaby).</summary>
        public int FishingCatchChancePercent { get; init; }

        /// <summary>The holder's next death within this Inspired adds no XP debt.</summary>
        public bool Requiem { get; init; }

        /// <summary>Once per ten minutes, a hit that leaves the holder below 25% HP heals them for 10% of their maximum.</summary>
        public bool StandingOvation { get; init; }
    }
}
