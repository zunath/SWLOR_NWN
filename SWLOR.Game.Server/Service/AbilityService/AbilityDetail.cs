using System.Collections.Generic;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public delegate bool AbilityActivationAction(uint activator, uint target, int effectivePerkLevel, Location targetLocation);
    public delegate void AbilityImpactAction(uint activator, uint target, int effectivePerkLevel, Location targetLocation);
    public delegate float AbilityActivationDelayAction(uint activator, uint target, int effectivePerkLevel);
    public delegate float AbilityRecastDelayAction(uint activator);
    public delegate string AbilityCustomValidationAction(uint activator, uint target, int effectivePerkLevel, Location targetLocation);

    public class AbilityDetail
    {
        public string Name { get; set; }
        public AbilityActivationAction ActivationAction { get; set; }
        public AbilityImpactAction ImpactAction { get; set; }
        public AbilityActivationDelayAction ActivationDelay { get; set; }
        public AbilityRecastDelayAction RecastDelay { get; set; }
        public AbilityCustomValidationAction CustomValidation { get; set; }
        public List<IAbilityActivationRequirement> Requirements { get; set; }
        public VisualEffect ActivationVisualEffect { get; set; }
        public string ActivationSound { get; set; }
        public string ImpactSound { get; set; }
        public RecastGroup RecastGroup { get; set; }
        public AbilityActivationType ActivationType { get; set; }
        public PerkType EffectiveLevelPerkType { get; set; }
        public Animation AnimationType { get; set; }
        public string AnimationSourceAnimationName { get; set; }
        public string AnimationReplacementAnimationName { get; set; }
        public float AnimationRestoreDelaySeconds { get; set; }
        public Animation ImpactAnimationType { get; set; }
        public string ImpactAnimationSourceAnimationName { get; set; }
        public string ImpactAnimationReplacementAnimationName { get; set; }
        public float ImpactAnimationRestoreDelaySeconds { get; set; }
        public bool CanBeUsedInSpace { get; set; }
        public float MaxRange { get; set; }
        public bool IsHostileAbility { get; set; }
        public bool IsHealingAbility { get; set; }
        public bool DisplaysActivationMessage { get; set; }
        public bool BreaksStealth { get; set; }
        public bool PreservesStealthDuringActivation { get; set; }
        public bool RequiresTarget { get; set; }
        public bool HasExplicitMaxRange { get; set; }
        public bool UsesActiveAttackTarget { get; set; }
        public int AbilityLevel { get; set; }
        public SkillType SkillType { get; set; }
        public AbilityType CombatImpactDamageAbility { get; set; }
        public bool IsAreaAbility { get; set; }
        public bool IsSingleTargetAbility { get; set; }
        public bool TriggersDarkForceConversion { get; set; }
        public bool SuppressesSourceStatusStackRiders { get; set; }
        public AbilityTargetingDetail Targeting { get; set; }
        public List<AbilityTargetingDetail> AdditionalActivationTargeting { get; set; }
        public List<Type> StatusEffectTypesRemovedOnPerkRefund { get; set; }
        public AITargetSelector AITargetSelector { get; set; }
        public AIScoreCalculation AIScore { get; set; }
        public bool IsMimicryTechnique { get; set; }
        public FeatType MimicrySourceFeat { get; set; }
        public int MimicrySkillRequirement { get; set; }
        public int MimicrySlotCost { get; set; }

        /// <summary>
        /// True when this area ability uses a player-selected location or direction. This is
        /// deliberately separate from <see cref="RequiresTarget"/>, which requires a real target
        /// object and activates object hostility and range validation.
        /// </summary>
        public bool RequiresLocationTarget =>
            !RequiresTarget &&
            IsAreaAbility &&
            ActivationType != AbilityActivationType.Weapon &&
            Targeting is { UpdatesClientTargeting: true } &&
            (Targeting.Shape is AbilityTargetingShapeType.Rect or AbilityTargetingShapeType.Cone ||
             Targeting.Shape == AbilityTargetingShapeType.Sphere &&
             !Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf));

        /// <summary>
        /// When true the activation delay is a channel: the ability's impact, costs, and recast delay
        /// all apply when the channel starts and the granted effects run for the channel itself.
        /// Interrupting the channel ends it early via <see cref="ChannelInterruptAction"/> without
        /// refunding the recast delay.
        /// </summary>
        public bool IsChanneled { get; set; }

        /// <summary>
        /// Invoked against the activator when a channeled ability is interrupted so the effects granted
        /// at channel start can be ended early.
        /// </summary>
        public Action<uint> ChannelInterruptAction { get; set; }

        /// <summary>
        /// When true this mimicked technique is a passive trait rather than an activated ability:
        /// while it is slotted its <see cref="MimicryTraitStats"/> and <see cref="MimicryTraitResistances"/>
        /// are summed into the wielder's totals, and it has no hotbar action.
        /// </summary>
        public bool IsMimicryTrait { get; set; }

        /// <summary>
        /// Flat stat adjustments contributed by this trait while it is equipped. Read directly by the
        /// stat pipeline rather than applied as a status effect, so the bonus cannot drift out of sync
        /// with the equipped loadout (status effects are cleared on death and would need re-granting).
        /// </summary>
        public Dictionary<StatType, int> MimicryTraitStats { get; set; }

        /// <summary>
        /// Resistance adjustments contributed by this trait while it is equipped.
        /// </summary>
        public Dictionary<ResistanceType, int> MimicryTraitResistances { get; set; }

        /// <summary>
        /// When true this mimicked technique is a self-toggle stance (via the toggle model) rather than
        /// a hostile cast, so the contract tests exempt it from the hostility / damage-element /
        /// combat-scaling assertions the way passive traits are exempt.
        /// </summary>
        public bool IsMimicryStance { get; set; }

        /// <summary>
        /// When true this mimicked technique is an active but non-damaging utility (control, debuff,
        /// support, or zone) — it targets and casts like any active, but declares no damage element or
        /// scaling attribute, so the contract tests exempt it from those assertions.
        /// </summary>
        public bool IsMimicryUtility { get; set; }

        /// <summary>
        /// The damage type this mimicked technique deals, used for damage-type loadout set bonuses
        /// (elemental resonance). <see cref="CombatDamageType.Invalid"/> for techniques with no
        /// damage element (passive flat/self-buff traits), which do not contribute to a set.
        /// </summary>
        public CombatDamageType MimicryElement { get; set; }

        public AbilityDetail()
        {
            ActivationVisualEffect = VisualEffect.None;
            ActivationSound = string.Empty;
            ImpactSound = string.Empty;
            AnimationType = Animation.Invalid;
            AnimationSourceAnimationName = string.Empty;
            AnimationReplacementAnimationName = string.Empty;
            AnimationRestoreDelaySeconds = 0f;
            ImpactAnimationType = Animation.Invalid;
            ImpactAnimationSourceAnimationName = string.Empty;
            ImpactAnimationReplacementAnimationName = string.Empty;
            ImpactAnimationRestoreDelaySeconds = 0f;
            Requirements = new List<IAbilityActivationRequirement>();
            MaxRange = 5.0f;
            IsHostileAbility = false;
            DisplaysActivationMessage = true;
            BreaksStealth = false;
            PreservesStealthDuringActivation = false;
            RequiresTarget = false;
            HasExplicitMaxRange = false;
            UsesActiveAttackTarget = false;
            AbilityLevel = 1;
            SkillType = SkillType.Invalid;
            CombatImpactDamageAbility = AbilityType.Invalid;
            IsAreaAbility = false;
            IsSingleTargetAbility = false;
            TriggersDarkForceConversion = false;
            SuppressesSourceStatusStackRiders = false;
            AdditionalActivationTargeting = new List<AbilityTargetingDetail>();
            StatusEffectTypesRemovedOnPerkRefund = new List<Type>();
            MimicrySourceFeat = FeatType.Invalid;
            MimicryElement = CombatDamageType.Invalid;
            MimicryTraitStats = new Dictionary<StatType, int>();
            MimicryTraitResistances = new Dictionary<ResistanceType, int>();
        }
    }
}
