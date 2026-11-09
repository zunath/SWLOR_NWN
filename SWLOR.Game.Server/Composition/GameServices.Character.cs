using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>
        /// Perks: the definition caches, a creature's perk levels, the stat bonuses and granted feats that follow from
        /// them, and refunds (the static facade <c>Perk</c> until P2-ZD).
        /// </summary>
        public static IPerkService Perk => FacadeAccessor.Resolve<IPerkService>();

        /// <summary>
        /// The skill catalog, XP awards and rank rules (the static facade <c>Skill</c> until P2-ZD). Its constants are on
        /// <see cref="SkillService"/>.
        /// </summary>
        public static ISkillService Skill => FacadeAccessor.Resolve<ISkillService>();

        /// <summary>
        /// Stats: the FP and Stamina pools, the player records fed by worn gear, walk mode and movement rate, the ratings,
        /// deflection and healing reactions, and NPC stat skins (the static facade <c>Stat</c> until P2-ZD). Its constants
        /// are on <see cref="StatService"/>; the pure formulas are on <c>StatRules</c>.
        /// </summary>
        public static IStatService Stat => FacadeAccessor.Resolve<IStatService>();

        /// <summary>
        /// Status effects: the catalog, applying, reading and removing them, and their ticks (the static
        /// facade <c>StatusEffect</c> until P2-ZD). The pure duration rules and their constant are on
        /// <c>StatusEffectRules</c>.
        /// </summary>
        public static IStatusEffectService StatusEffect => FacadeAccessor.Resolve<IStatusEffectService>();
    }
}
