using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>The DM battle grids of the areas (the facade <c>Service.BattleGrid</c> until P2-ZG).</summary>
        public static IBattleGridService BattleGrid => FacadeAccessor.Resolve<IBattleGridService>();

        /// <summary>The combat points players earn against NPCs (the facade <c>Service.CombatPoint</c> until P2-ZG).</summary>
        public static ICombatPointService CombatPoint => FacadeAccessor.Resolve<ICombatPointService>();

        /// <summary>
        /// The DM-toggled combat recorder (the facade <c>Service.CombatRecorder</c> until P2-ZG). The hooks read
        /// <c>CombatRecorder.IsRecording</c> first, through this property, which reads the same per-service slot the facade read.
        /// </summary>
        public static ICombatRecorderService CombatRecorder => FacadeAccessor.Resolve<ICombatRecorderService>();

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
