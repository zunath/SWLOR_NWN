using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The DM battle grids of the areas (the facade <c>Service.BattleGrid</c> until P2-ZG).</summary>
        public static IBattleGridService BattleGrid => FacadeAccessor.Resolve<IBattleGridService>();

        /// <summary>What is left of combat once its parts are split out: the damage roll and the stat-driven tracker refresh (the facade <c>Service.Combat</c> until P2-ZP). Combat has one property per part below, because its members live on the parts; no call goes through another service first.</summary>
        public static ICombatService Combat => FacadeAccessor.Resolve<ICombatService>();

        /// <summary>The ability activation part of combat, a part of combat.</summary>
        public static ICombatAbilityActivationService CombatAbilityActivation => FacadeAccessor.Resolve<ICombatAbilityActivationService>();

        /// <summary>The ability and attack bonuses of combat, a part of combat.</summary>
        public static ICombatAbilityBonusService CombatAbilityBonus => FacadeAccessor.Resolve<ICombatAbilityBonusService>();

        /// <summary>The ability hit roll of combat, a part of combat.</summary>
        public static ICombatAbilityHitService CombatAbilityHit => FacadeAccessor.Resolve<ICombatAbilityHitService>();

        /// <summary>The ability impact part of combat, a part of combat.</summary>
        public static ICombatAbilityImpactService CombatAbilityImpact => FacadeAccessor.Resolve<ICombatAbilityImpactService>();

        /// <summary>The combat activity records, a part of combat.</summary>
        public static ICombatActivityService CombatActivity => FacadeAccessor.Resolve<ICombatActivityService>();

        /// <summary>The attack timing of combat, a part of combat.</summary>
        public static ICombatAttackTimingService CombatAttackTiming => FacadeAccessor.Resolve<ICombatAttackTimingService>();

        /// <summary>The damage type catalog, a part of combat.</summary>
        public static ICombatDamageCatalogService CombatDamageCatalog => FacadeAccessor.Resolve<ICombatDamageCatalogService>();

        /// <summary>The damage roll modifiers, a part of combat.</summary>
        public static ICombatDamageModifiersService CombatDamageModifiers => FacadeAccessor.Resolve<ICombatDamageModifiersService>();

        /// <summary>The damage taken part of combat, a part of combat.</summary>
        public static ICombatDamageTakenService CombatDamageTaken => FacadeAccessor.Resolve<ICombatDamageTakenService>();

        /// <summary>The combat log lines, a part of combat.</summary>
        public static ICombatFeedbackService CombatFeedback => FacadeAccessor.Resolve<ICombatFeedbackService>();

        /// <summary>The hit effects of combat, a part of combat.</summary>
        public static ICombatHitEffectsService CombatHitEffects => FacadeAccessor.Resolve<ICombatHitEffectsService>();

        /// <summary>The combat points players earn against NPCs (the facade <c>Service.CombatPoint</c> until P2-ZG).</summary>
        public static ICombatPointService CombatPoint => FacadeAccessor.Resolve<ICombatPointService>();

        /// <summary>The combat queries, a part of combat.</summary>
        public static ICombatQueriesService CombatQueries => FacadeAccessor.Resolve<ICombatQueriesService>();

        /// <summary>
        /// The DM-toggled combat recorder (the facade <c>Service.CombatRecorder</c> until P2-ZG). The hooks read
        /// <c>CombatRecorder.IsRecording</c> first, through this property, which reads the same per-service slot the facade read.
        /// </summary>
        public static ICombatRecorderService CombatRecorder => FacadeAccessor.Resolve<ICombatRecorderService>();

        /// <summary>The resource bookkeeping of combat, a part of combat.</summary>
        public static ICombatResourcesService CombatResources => FacadeAccessor.Resolve<ICombatResourcesService>();

        /// <summary>The Suppression stacks, a part of combat.</summary>
        public static ICombatSuppressionService CombatSuppression => FacadeAccessor.Resolve<ICombatSuppressionService>();

        /// <summary>Player death, respawn and the respawn point (the facade <c>Service.Death</c> until P2-ZG).</summary>
        public static IDeathService Death => FacadeAccessor.Resolve<IDeathService>();

        /// <summary>The enmity tables and the rules that feed them (the facade <c>Service.Enmity</c> until P2-ZG).</summary>
        public static IEnmityService Enmity => FacadeAccessor.Resolve<IEnmityService>();

        /// <summary>Melee technique lines, setups and ordinary-attack power (the facade <c>Service.MeleeTechnique</c> until P2-ZH).</summary>
        public static IMeleeTechniqueService MeleeTechnique => FacadeAccessor.Resolve<IMeleeTechniqueService>();

        /// <summary>Stat-driven rewards for ordinary hostile weapon hits (the facade <c>Service.OrdinaryAttackTraits</c> until P2-ZH).</summary>
        public static IOrdinaryAttackTraitsService OrdinaryAttackTraits => FacadeAccessor.Resolve<IOrdinaryAttackTraitsService>();

        /// <summary>Paired ranged weapon traits (the facade <c>Service.PairedRangedTraits</c> until P2-ZH).</summary>
        public static IPairedRangedTraitsService PairedRangedTraits => FacadeAccessor.Resolve<IPairedRangedTraitsService>();

        /// <summary>Weapon poison coatings (the facade <c>Service.Poisons</c> until P2-ZG).</summary>
        public static IPoisonsService Poisons => FacadeAccessor.Resolve<IPoisonsService>();

        /// <summary>Power Glove pair equipment (the facade <c>Service.PowerGloveEquipment</c> until P2-ZH).</summary>
        public static IPowerGloveEquipmentService PowerGloveEquipment => FacadeAccessor.Resolve<IPowerGloveEquipmentService>();

        /// <summary>Player-vs-player combat (the facade <c>Service.PvpCombat</c> until P2-ZG).</summary>
        public static IPvpCombatService PvpCombat => FacadeAccessor.Resolve<IPvpCombatService>();

        /// <summary>The swing animation a readied weapon ability changes (the facade <c>Service.QueuedAttackAnimation</c> until P2-ZH).</summary>
        public static IQueuedAttackAnimationService QueuedAttackAnimation => FacadeAccessor.Resolve<IQueuedAttackAnimationService>();

        /// <summary>Recast groups and cooldown deadlines (the facade <c>Service.Recast</c> until P2-ZG).</summary>
        public static IRecastService Recast => FacadeAccessor.Resolve<IRecastService>();

        /// <summary>Resistances and vulnerabilities (the facade <c>Service.Resistance</c> until P2-ZG).</summary>
        public static IResistanceService Resistance => FacadeAccessor.Resolve<IResistanceService>();

        /// <summary>Stealth entry, detection and granted stealth (the facade <c>Service.Stealth</c> until P2-ZH).</summary>
        public static IStealthService Stealth => FacadeAccessor.Resolve<IStealthService>();

        /// <summary>Player targeting mode (the facade <c>Service.Targeting</c> until P2-ZG).</summary>
        public static ITargetingService Targeting => FacadeAccessor.Resolve<ITargetingService>();

        /// <summary>Ground telegraphs (the facade <c>Service.Telegraph</c> until P2-ZH).</summary>
        public static ITelegraphService Telegraph => FacadeAccessor.Resolve<ITelegraphService>();

        /// <summary>Timed stat modifiers a creature carries (the facade <c>Service.TemporaryStatModifier</c> until P2-ZG).</summary>
        public static ITemporaryStatModifierService TemporaryStatModifier => FacadeAccessor.Resolve<ITemporaryStatModifierService>();

        /// <summary>Twin pistol weapon abilities (the facade <c>Service.TwinPistolAbility</c> until P2-ZH).</summary>
        public static ITwinPistolAbilityService TwinPistolAbility => FacadeAccessor.Resolve<ITwinPistolAbilityService>();

        /// <summary>Twin pistol pair equipment (the facade <c>Service.TwinPistolEquipment</c> until P2-ZH).</summary>
        public static ITwinPistolEquipmentService TwinPistolEquipment => FacadeAccessor.Resolve<ITwinPistolEquipmentService>();

        /// <summary>Twin pistol mastery tracking (the facade <c>Service.TwinPistolMastery</c> until P2-ZH).</summary>
        public static ITwinPistolMasteryService TwinPistolMastery => FacadeAccessor.Resolve<ITwinPistolMasteryService>();

        /// <summary>Which weapons may attack in each attack phase (the facade <c>Service.WeaponAttackEligibility</c> until P2-ZH).</summary>
        public static IWeaponAttackEligibilityService WeaponAttackEligibility => FacadeAccessor.Resolve<IWeaponAttackEligibilityService>();

        /// <summary>The attack delay each weapon category carries (the facade <c>Service.WeaponDelay</c> until P2-ZH).</summary>
        public static IWeaponDelayService WeaponDelay => FacadeAccessor.Resolve<IWeaponDelayService>();
    }
}
