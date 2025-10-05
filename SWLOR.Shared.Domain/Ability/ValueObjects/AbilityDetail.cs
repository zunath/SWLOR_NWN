using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Perk.Enums;

namespace SWLOR.Shared.Domain.Ability.ValueObjects
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
        public VisualEffectType ActivationVisualEffect { get; set; }
        public RecastGroupType RecastGroup { get; set; }
        public AbilityActivationType ActivationType { get; set; }
        public PerkType EffectiveLevelPerkType { get; set; }
        public AnimationType AnimationType { get; set; }
        public bool CanBeUsedInSpace { get; set; }
        public bool IgnoreHeavyArmorPenalty { get; set; }
        public float MaxRange { get; set; }
        public bool IsHostileAbility { get; set; }
        public bool DisplaysActivationMessage { get; set; }
        public bool BreaksStealth { get; set; }
        public int AbilityLevel { get; set; }

        public AbilityDetail()
        {
            ActivationVisualEffect = VisualEffectType.None;
            AnimationType = AnimationType.Invalid;
            Requirements = new List<IAbilityActivationRequirement>();
            MaxRange = 5.0f;
            IsHostileAbility = false;
            DisplaysActivationMessage = true;
            BreaksStealth = false;
            AbilityLevel = 1;
        }
    }
}
