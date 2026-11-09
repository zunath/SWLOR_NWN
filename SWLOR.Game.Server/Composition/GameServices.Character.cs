using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>Achievements: the catalog and granting them to an account (the static facade <c>Achievement</c> until P2-ZE).</summary>
        public static IAchievementService Achievement => FacadeAccessor.Resolve<IAchievementService>();

        /// <summary>The temporary "busy" status of a creature (the static facade <c>Activity</c> until P2-ZE).</summary>
        public static IActivityService Activity => FacadeAccessor.Resolve<IActivityService>();

        /// <summary>The runtime state of the appearance renderers (head scale, neck projections) (the static facade <c>AppearanceRenderState</c> until P2-ZE).</summary>
        public static IAppearanceRenderStateService AppearanceRenderState => FacadeAccessor.Resolve<IAppearanceRenderStateService>();

        /// <summary>Disguises: saving, activating and retiring them, and their limits and cooldowns (the static facade <c>Disguise</c> until P2-ZE).</summary>
        public static IDisguiseService Disguise => FacadeAccessor.Resolve<IDisguiseService>();

        /// <summary>One infiltration attempt per player and hostile pair (the static facade <c>EspionageInfiltration</c> until P2-ZE).</summary>
        public static IEspionageInfiltrationService EspionageInfiltration => FacadeAccessor.Resolve<IEspionageInfiltrationService>();

        /// <summary>Languages: translators, the active language and mastering them (the static facade <c>Language</c> until P2-ZE).</summary>
        public static ILanguageService Language => FacadeAccessor.Resolve<ILanguageService>();

        /// <summary>
        /// Perks: the definition caches, a creature's perk levels, the stat bonuses and granted feats that follow from
        /// them, and refunds (the static facade <c>Perk</c> until P2-ZD).
        /// </summary>
        public static IPerkService Perk => FacadeAccessor.Resolve<IPerkService>();

        /// <summary>The public descriptors players see for unknown characters (the static facade <c>PlayerDescriptor</c> until P2-ZE).</summary>
        public static IPlayerDescriptorService PlayerDescriptor => FacadeAccessor.Resolve<IPlayerDescriptorService>();

        /// <summary>Player introductions and the offers a player has pending (the static facade <c>PlayerIntroduction</c> until P2-ZE).</summary>
        public static IPlayerIntroductionService PlayerIntroduction => FacadeAccessor.Resolve<IPlayerIntroductionService>();

        /// <summary>Player names, known names and the AFK flag (the static facade <c>PlayerName</c> until P2-ZE).</summary>
        public static IPlayerNameService PlayerName => FacadeAccessor.Resolve<IPlayerNameService>();

        /// <summary>Racial appearances and race rules (the static facade <c>Race</c> until P2-ZE).</summary>
        public static IRaceService Race => FacadeAccessor.Resolve<IRaceService>();

        /// <summary>The model the roleplay XP chat check loaded at server start (the static facade <c>RoleplayXPDetection</c> until P2-ZE).</summary>
        public static IRoleplayXPDetectionService RoleplayXPDetection => FacadeAccessor.Resolve<IRoleplayXPDetectionService>();

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

        /// <summary>The runtime state of the tint maps (pending equipment-swap color carries and material tables) (the static facade <c>TintMapState</c> until P2-ZE).</summary>
        public static ITintMapStateService TintMapState => FacadeAccessor.Resolve<ITintMapStateService>();
    }
}
