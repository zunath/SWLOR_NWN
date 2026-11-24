using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The companion questions the lower domains ask (<c>ICompanionLookup</c>).</summary>
        public static ICompanionLookup CompanionQuery => FacadeAccessor.Resolve<ICompanionLookup>();

        /// <summary>The combat recorder as the character services see it (<c>ICharacterRecording</c>).</summary>
        public static ICharacterRecording CharacterRecording => FacadeAccessor.Resolve<ICharacterRecording>();

        /// <summary>Achievements: the catalog and granting them to an account (the static facade <c>Achievement</c> until it was removed).</summary>
        public static IAchievementService Achievement => FacadeAccessor.Resolve<IAchievementService>();

        /// <summary>The temporary "busy" status of a creature (the static facade <c>Activity</c> until it was removed).</summary>
        public static IActivityService Activity => FacadeAccessor.Resolve<IActivityService>();

        /// <summary>The runtime state of the appearance renderers (head scale, neck projections) (the static facade <c>AppearanceRenderState</c> until it was removed).</summary>
        public static IAppearanceRenderStateService AppearanceRenderState => FacadeAccessor.Resolve<IAppearanceRenderStateService>();

        /// <summary>Disguises: saving, activating and retiring them, and their limits and cooldowns (the static facade <c>Disguise</c> until it was removed).</summary>
        public static IDisguiseService Disguise => FacadeAccessor.Resolve<IDisguiseService>();

        /// <summary>One infiltration attempt per player and hostile pair (the static facade <c>EspionageInfiltration</c> until it was removed).</summary>
        public static IEspionageInfiltrationService EspionageInfiltration => FacadeAccessor.Resolve<IEspionageInfiltrationService>();

        /// <summary>
        /// The stats the equipped Mimicry traits and the elemental resonance add, which the stat read reads (part 4 of the stat
        /// split; until then <c>Mimicry.GetStatBonus</c>, <c>GetStatSources</c> and <c>GetSetBonusPotency</c>).
        /// </summary>
        public static IMimicryTraitStatsService MimicryTraitStats => FacadeAccessor.Resolve<IMimicryTraitStatsService>();

        /// <summary>Languages: translators, the active language and mastering them (the static facade <c>Language</c> until it was removed).</summary>
        public static ILanguageService Language => FacadeAccessor.Resolve<ILanguageService>();

        /// <summary>
        /// Perks: the definition caches, a creature's perk levels, the stat bonuses and granted feats that follow from
        /// them, and refunds (the static facade <c>Perk</c> until it was removed).
        /// </summary>
        public static IPerkService Perk => FacadeAccessor.Resolve<IPerkService>();

        /// <summary>The public descriptors players see for unknown characters (the static facade <c>PlayerDescriptor</c> until it was removed).</summary>
        public static IPlayerDescriptorService PlayerDescriptor => FacadeAccessor.Resolve<IPlayerDescriptorService>();

        /// <summary>Player introductions and the offers a player has pending (the static facade <c>PlayerIntroduction</c> until it was removed).</summary>
        public static IPlayerIntroductionService PlayerIntroduction => FacadeAccessor.Resolve<IPlayerIntroductionService>();

        /// <summary>Player names, known names and the AFK flag (the static facade <c>PlayerName</c> until it was removed).</summary>
        public static IPlayerNameService PlayerName => FacadeAccessor.Resolve<IPlayerNameService>();

        /// <summary>The names a player gives other characters: validation, setting, forgetting and searching them.</summary>
        public static IPlayerKnownNameService PlayerKnownName => FacadeAccessor.Resolve<IPlayerKnownNameService>();

        /// <summary>Racial appearances and race rules (the static facade <c>Race</c> until it was removed).</summary>
        public static IRaceService Race => FacadeAccessor.Resolve<IRaceService>();

        /// <summary>The model the roleplay XP chat check loaded at server start (the static facade <c>RoleplayXPDetection</c> until it was removed).</summary>
        public static IRoleplayXPDetectionService RoleplayXPDetection => FacadeAccessor.Resolve<IRoleplayXPDetectionService>();

        /// <summary>
        /// The skill catalog, XP awards and rank rules (the static facade <c>Skill</c> until it was removed). Its constants are on
        /// <see cref="SkillService"/>.
        /// </summary>
        public static ISkillService Skill => FacadeAccessor.Resolve<ISkillService>();

        /// <summary>
        /// Stats: the stat adjustment read, the FP and Stamina pools, NPC stat skins and the Detection, Stealth and crafting reads
        /// (the static facade <c>Stat</c> until it was removed). Its constants are on <see cref="StatService"/>; the pure formulas are on
        /// <c>StatRules</c>. The rest of the facade is <see cref="CreatureStat"/>, <see cref="StatRecord"/>,
        /// <c>CombatRating</c>, <c>CombatDeflectionReaction</c> and <c>CombatResources</c>.
        /// </summary>
        public static IStatService Stat => FacadeAccessor.Resolve<IStatService>();

        /// <summary>
        /// Movement and walk mode, maximum HP and food HP, player attributes, NPC loading and regeneration (part 2 of the stat
        /// split; until then members of <c>Stat</c>). Its constants are on <see cref="CreatureStatService"/>.
        /// </summary>
        public static ICreatureStatService CreatureStat => FacadeAccessor.Resolve<ICreatureStatService>();

        /// <summary>
        /// The player records fed by worn gear, the Combat Readiness equipment and the gear stat writers (part 3 of the stat
        /// split; until then members of <c>Stat</c>). Its constants are on <see cref="StatRecordService"/>.
        /// </summary>
        public static IStatRecordService StatRecord => FacadeAccessor.Resolve<IStatRecordService>();

        /// <summary>
        /// Status effects: the catalog, applying, reading and removing them, and their ticks (the static
        /// facade <c>StatusEffect</c> until it was removed). The pure duration rules and their constant are on
        /// <c>StatusEffectRules</c>.
        /// </summary>
        public static IStatusEffectService StatusEffect => FacadeAccessor.Resolve<IStatusEffectService>();

        /// <summary>The status effect definitions the module cache finds and the creation of an effect by type (part 2 of the status effect split).</summary>
        public static IStatusEffectCatalogService StatusEffectCatalog => FacadeAccessor.Resolve<IStatusEffectCatalogService>();

        /// <summary>What survives logout, a planned restart and a hot reload: the logged-out effects and the saved toggles (part 7 of the status effect split).</summary>
        public static IStatusEffectSessionService StatusEffectSession => FacadeAccessor.Resolve<IStatusEffectSessionService>();

        /// <summary>Applying a status effect: the apply family, its checks, conflicts and messages (part 3 of the status effect split).</summary>
        public static IStatusEffectApplicationService StatusEffectApplication => FacadeAccessor.Resolve<IStatusEffectApplicationService>();

        /// <summary>The timing of an application, the refresh in place, the restart, the extension and the refresh of a status effect (part 4 of the status effect split).</summary>
        public static IStatusEffectDurationService StatusEffectDuration => FacadeAccessor.Resolve<IStatusEffectDurationService>();

        /// <summary>The damage and attack notices, the guarded hit, the native attack swing and the limited attack-timing reads (part 6 of the status effect split).</summary>
        public static IStatusEffectCombatEventService StatusEffectCombatEvent => FacadeAccessor.Resolve<IStatusEffectCombatEventService>();

        /// <summary>The native effect that carries each status effect, the three effect scripts and the ticks (part 5 of the status effect split).</summary>
        public static IStatusEffectNativeService StatusEffectNative => FacadeAccessor.Resolve<IStatusEffectNativeService>();

        /// <summary>
        /// Timed stat modifiers a creature carries (the facade <c>Service.TemporaryStatModifier</c> until it was removed). Combat and content
        /// write them and the stat read reads them, so they are Character's.
        /// </summary>
        public static ITemporaryStatModifierService TemporaryStatModifier => FacadeAccessor.Resolve<ITemporaryStatModifierService>();

        /// <summary>The runtime state of the tint maps (pending equipment-swap color carries and material tables) (the static facade <c>TintMapState</c> until it was removed).</summary>
        public static ITintMapStateService TintMapState => FacadeAccessor.Resolve<ITintMapStateService>();
    }
}
