using System.Collections.Generic;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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
        public bool DisplaysActivationMessage { get; set; }
        public bool BreaksStealth { get; set; }
        public bool RequiresTarget { get; set; }
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
            RequiresTarget = false;
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
        }
    }
}
