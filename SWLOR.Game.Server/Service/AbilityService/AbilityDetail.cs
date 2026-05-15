using System.Collections.Generic;
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
        public RecastGroup RecastGroup { get; set; }
        public AbilityActivationType ActivationType { get; set; }
        public PerkType EffectiveLevelPerkType { get; set; }
        public Animation AnimationType { get; set; }
        public Animation ImpactAnimationType { get; set; }
        public bool CanBeUsedInSpace { get; set; }
        public float MaxRange { get; set; }
        public bool IsHostileAbility { get; set; }
        public bool DisplaysActivationMessage { get; set; }
        public bool BreaksStealth { get; set; }
        public bool RequiresTarget { get; set; }
        public int AbilityLevel { get; set; }
        public SkillType SkillType { get; set; }
        public bool IsAreaAbility { get; set; }
        public bool IsSingleTargetAbility { get; set; }
        public bool TriggersDarkForceConversion { get; set; }

        public AbilityDetail()
        {
            ActivationVisualEffect = VisualEffect.None;
            AnimationType = Animation.Invalid;
            ImpactAnimationType = Animation.Invalid;
            Requirements = new List<IAbilityActivationRequirement>();
            MaxRange = 5.0f;
            IsHostileAbility = false;
            DisplaysActivationMessage = true;
            BreaksStealth = false;
            RequiresTarget = false;
            AbilityLevel = 1;
            SkillType = SkillType.Invalid;
            IsAreaAbility = false;
            IsSingleTargetAbility = false;
            TriggersDarkForceConversion = false;
        }
    }
}
