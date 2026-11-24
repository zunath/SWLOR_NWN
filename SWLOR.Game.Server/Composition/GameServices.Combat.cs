using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The DM battle grids of the areas (the facade <c>Service.BattleGrid</c> until it was removed).</summary>
        public static IBattleGridService BattleGrid => FacadeAccessor.Resolve<IBattleGridService>();

        /// <summary>The ability impacts in progress and the last completed summary of each activator (<c>IAbilityImpactState</c>).</summary>
        public static IAbilityImpactState AbilityImpactState => FacadeAccessor.Resolve<IAbilityImpactState>();

        /// <summary>Who is in whose party, as the domains below Social read it (<c>IPartyRoster</c>, implemented by the party roster).</summary>
        public static IPartyRoster PartyRoster => FacadeAccessor.Resolve<IPartyRoster>();

        /// <summary>What is left of combat once its parts are split out: the damage roll and the stat-driven tracker refresh (the facade <c>Service.Combat</c> until it was removed). Combat has one property per part below, because its members live on the parts; no call goes through another service first.</summary>
        public static ICombatService Combat => FacadeAccessor.Resolve<ICombatService>();

        /// <summary>The ability activation part of combat, a part of combat.</summary>
        public static ICombatAbilityActivationService CombatAbilityActivation => FacadeAccessor.Resolve<ICombatAbilityActivationService>();

        /// <summary>The ability and attack bonuses of combat, a part of combat.</summary>
        public static ICombatAbilityBonusService CombatAbilityBonus => FacadeAccessor.Resolve<ICombatAbilityBonusService>();

        /// <summary>The cost riders of an ability activation (recast reduction, next skill bonuses, HP spend, Force FP cost), a part of combat.</summary>
        public static ICombatAbilityCostEffectsService CombatAbilityCostEffects => FacadeAccessor.Resolve<ICombatAbilityCostEffectsService>();

        /// <summary>The defense riders of an ability activation (evasion, deflection, ally defense), a part of combat.</summary>
        public static ICombatAbilityDefenseEffectsService CombatAbilityDefenseEffects => FacadeAccessor.Resolve<ICombatAbilityDefenseEffectsService>();

        /// <summary>The ability hit roll of combat, a part of combat.</summary>
        public static ICombatAbilityHitService CombatAbilityHit => FacadeAccessor.Resolve<ICombatAbilityHitService>();

        /// <summary>The ability impact part of combat, a part of combat.</summary>
        public static ICombatAbilityImpactService CombatAbilityImpact => FacadeAccessor.Resolve<ICombatAbilityImpactService>();

        /// <summary>How Force affinity and combat readiness scale an ability's magnitude, a part of combat.</summary>
        public static ICombatAbilityMagnitudeService CombatAbilityMagnitude => FacadeAccessor.Resolve<ICombatAbilityMagnitudeService>();

        /// <summary>The combat activity records, a part of combat.</summary>
        public static ICombatActivityService CombatActivity => FacadeAccessor.Resolve<ICombatActivityService>();

        /// <summary>The attack timing of combat, a part of combat.</summary>
        public static ICombatAttackTimingService CombatAttackTiming => FacadeAccessor.Resolve<ICombatAttackTimingService>();

        /// <summary>The damage type catalog, a part of combat.</summary>
        public static ICombatDamageCatalogService CombatDamageCatalog => FacadeAccessor.Resolve<ICombatDamageCatalogService>();

        /// <summary>The damage roll modifiers, a part of combat.</summary>
        public static ICombatDamageModifiersService CombatDamageModifiers => FacadeAccessor.Resolve<ICombatDamageModifiersService>();

        /// <summary>Critical rate and positional rate modifiers, a part of combat.</summary>
        public static ICombatRateModifiersService CombatRateModifiers => FacadeAccessor.Resolve<ICombatRateModifiersService>();

        /// <summary>Repeated-target and same-target pressure state, a part of combat.</summary>
        public static ICombatRepeatedTargetService CombatRepeatedTarget => FacadeAccessor.Resolve<ICombatRepeatedTargetService>();

        /// <summary>
        /// What a deflection sets off (part 6 of the stat split; until then <c>Stat.ApplyDeflectionEffectsNative</c>).
        /// </summary>
        public static ICombatDeflectionReactionService CombatDeflectionReaction => FacadeAccessor.Resolve<ICombatDeflectionReactionService>();

        /// <summary>The damage taken part of combat, a part of combat.</summary>
        public static ICombatDamageTakenService CombatDamageTaken => FacadeAccessor.Resolve<ICombatDamageTakenService>();

        /// <summary>The damage taken handler, low HP, melee damage taken, avoided attack and deflection effects, a part of combat.</summary>
        public static ICombatDamageTakenEffectsService CombatDamageTakenEffects => FacadeAccessor.Resolve<ICombatDamageTakenEffectsService>();

        /// <summary>Damage reflection, the physical-to-Force conversion and Deflecting Return, a part of combat.</summary>
        public static ICombatDamageReflectionService CombatDamageReflection => FacadeAccessor.Resolve<ICombatDamageReflectionService>();

        /// <summary>A guarded hit: the reduction, the stamina recovery, the retaliation and the guard enmity, a part of combat.</summary>
        public static ICombatGuardedHitService CombatGuardedHit => FacadeAccessor.Resolve<ICombatGuardedHitService>();

        /// <summary>The effects a guarded hit grants the guard, a part of combat.</summary>
        public static ICombatGuardedHitEffectsService CombatGuardedHitEffects => FacadeAccessor.Resolve<ICombatGuardedHitEffectsService>();

        /// <summary>The damage a guard returns on a guarded hit, a part of combat.</summary>
        public static ICombatGuardRetaliationService CombatGuardRetaliation => FacadeAccessor.Resolve<ICombatGuardRetaliationService>();

        /// <summary>The random damage roll, a part of combat.</summary>
        public static ICombatDamageRollService CombatDamageRoll => FacadeAccessor.Resolve<ICombatDamageRollService>();

        /// <summary>Who was guarded or deflected recently, a part of combat.</summary>
        public static ICombatDefenseMemoryService CombatDefenseMemory => FacadeAccessor.Resolve<ICombatDefenseMemoryService>();

        /// <summary>The combat log lines, a part of combat.</summary>
        public static ICombatFeedbackService CombatFeedback => FacadeAccessor.Resolve<ICombatFeedbackService>();

        /// <summary>The hit effects of combat, a part of combat.</summary>
        public static ICombatHitEffectsService CombatHitEffects => FacadeAccessor.Resolve<ICombatHitEffectsService>();

        /// <summary>The shape of an ability impact and the hostile creatures inside it, a part of combat.</summary>
        public static ICombatImpactGeometryService CombatImpactGeometry => FacadeAccessor.Resolve<ICombatImpactGeometryService>();

        /// <summary>The combat points players earn against NPCs (the facade <c>Service.CombatPoint</c> until it was removed).</summary>
        public static ICombatPointService CombatPoint => FacadeAccessor.Resolve<ICombatPointService>();

        /// <summary>The combat queries, a part of combat.</summary>
        public static ICombatQueriesService CombatQueries => FacadeAccessor.Resolve<ICombatQueriesService>();

        /// <summary>
        /// The Attack, Accuracy, Defense and Evasion ratings and the deflection and Guard chances (part 5 of the stat split;
        /// until then <c>Stat.GetAttack</c>, <c>GetDefense</c>, <c>GetAccuracy</c>, <c>GetEvasion</c> and the chance reads).
        /// </summary>
        public static ICombatRatingService CombatRating => FacadeAccessor.Resolve<ICombatRatingService>();

        /// <summary>
        /// The DM-toggled combat recorder (the facade <c>Service.CombatRecorder</c> until it was removed). The hooks read
        /// <c>CombatRecorder.IsRecording</c> first, through this property, which reads the same per-service slot the facade read.
        /// </summary>
        public static ICombatRecorderService CombatRecorder => FacadeAccessor.Resolve<ICombatRecorderService>();

        /// <summary>The state and event queue of a combat recording, which the native damage and heal hooks open and close their scopes on.</summary>
        public static ICombatRecordSession CombatRecordSession => FacadeAccessor.Resolve<ICombatRecordSession>();

        /// <summary>The resource bookkeeping of combat, a part of combat.</summary>
        public static ICombatResourcesService CombatResources => FacadeAccessor.Resolve<ICombatResourcesService>();

        /// <summary>The Suppression stacks, a part of combat.</summary>
        public static ICombatSuppressionService CombatSuppression => FacadeAccessor.Resolve<ICombatSuppressionService>();

        /// <summary>Player death, respawn and the respawn point (the facade <c>Service.Death</c> until it was removed).</summary>
        public static IDeathService Death => FacadeAccessor.Resolve<IDeathService>();

        /// <summary>The enmity tables and the rules that feed them (the facade <c>Service.Enmity</c> until it was removed).</summary>
        public static IEnmityService Enmity => FacadeAccessor.Resolve<IEnmityService>();

        /// <summary>Melee technique lines, setups and ordinary-attack power (the facade <c>Service.MeleeTechnique</c> until it was removed).</summary>
        public static IMeleeTechniqueService MeleeTechnique => FacadeAccessor.Resolve<IMeleeTechniqueService>();

        /// <summary>Stat-driven rewards for ordinary hostile weapon hits (the facade <c>Service.OrdinaryAttackTraits</c> until it was removed).</summary>
        public static IOrdinaryAttackTraitsService OrdinaryAttackTraits => FacadeAccessor.Resolve<IOrdinaryAttackTraitsService>();

        /// <summary>Paired ranged weapon traits (the facade <c>Service.PairedRangedTraits</c> until it was removed).</summary>
        public static IPairedRangedTraitsService PairedRangedTraits => FacadeAccessor.Resolve<IPairedRangedTraitsService>();

        /// <summary>Weapon poison coatings (the facade <c>Service.Poisons</c> until it was removed).</summary>
        public static IPoisonsService Poisons => FacadeAccessor.Resolve<IPoisonsService>();

        /// <summary>Power Glove pair equipment (the facade <c>Service.PowerGloveEquipment</c> until it was removed).</summary>
        public static IPowerGloveEquipmentService PowerGloveEquipment => FacadeAccessor.Resolve<IPowerGloveEquipmentService>();

        /// <summary>Player-vs-player combat (the facade <c>Service.PvpCombat</c> until it was removed).</summary>
        public static IPvpCombatService PvpCombat => FacadeAccessor.Resolve<IPvpCombatService>();

        /// <summary>The swing animation a readied weapon ability changes (the facade <c>Service.QueuedAttackAnimation</c> until it was removed).</summary>
        public static IQueuedAttackAnimationService QueuedAttackAnimation => FacadeAccessor.Resolve<IQueuedAttackAnimationService>();

        /// <summary>Recast groups and cooldown deadlines (the facade <c>Service.Recast</c> until it was removed).</summary>
        public static IRecastService Recast => FacadeAccessor.Resolve<IRecastService>();

        /// <summary>Resistances and vulnerabilities (the facade <c>Service.Resistance</c> until it was removed).</summary>
        public static IResistanceService Resistance => FacadeAccessor.Resolve<IResistanceService>();

        /// <summary>
        /// The riders restoring FP or Stamina grants (part 8 of the stat split; until then
        /// <c>CombatResources.ApplyFPRestoredEffects</c> and <c>ApplyStaminaRestoredEffects</c>, which took no stat reader).
        /// </summary>
        public static IResourceRestoredEffectsService ResourceRestoredEffects => FacadeAccessor.Resolve<IResourceRestoredEffectsService>();

        /// <summary>Stealth entry, detection and granted stealth (the facade <c>Service.Stealth</c> until it was removed).</summary>
        public static IStealthService Stealth => FacadeAccessor.Resolve<IStealthService>();

        /// <summary>Player targeting mode (the facade <c>Service.Targeting</c> until it was removed).</summary>
        public static ITargetingService Targeting => FacadeAccessor.Resolve<ITargetingService>();

        /// <summary>Ground telegraphs (the facade <c>Service.Telegraph</c> until it was removed).</summary>
        public static ITelegraphService Telegraph => FacadeAccessor.Resolve<ITelegraphService>();

        /// <summary>Twin pistol pair equipment (the facade <c>Service.TwinPistolEquipment</c> until it was removed).</summary>
        public static ITwinPistolEquipmentService TwinPistolEquipment => FacadeAccessor.Resolve<ITwinPistolEquipmentService>();

        /// <summary>Twin pistol mastery tracking (the facade <c>Service.TwinPistolMastery</c> until it was removed).</summary>
        public static ITwinPistolMasteryService TwinPistolMastery => FacadeAccessor.Resolve<ITwinPistolMasteryService>();

        /// <summary>Which weapons may attack in each attack phase (the facade <c>Service.WeaponAttackEligibility</c> until it was removed).</summary>
        public static IWeaponAttackEligibilityService WeaponAttackEligibility => FacadeAccessor.Resolve<IWeaponAttackEligibilityService>();

        /// <summary>The attack delay each weapon category carries (the facade <c>Service.WeaponDelay</c> until it was removed).</summary>
        public static IWeaponDelayService WeaponDelay => FacadeAccessor.Resolve<IWeaponDelayService>();
    }
}
