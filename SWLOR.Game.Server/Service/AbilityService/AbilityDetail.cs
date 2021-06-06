using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Service.AbilityService
{
    public delegate void AbilityImpactAction(uint activator, uint target, int effectivePerkLevel);
    public delegate float AbilityActivationDelayAction(uint activator, uint target, int effectivePerkLevel);
    public delegate float AbilityRecastDelayAction(uint activator);
    public delegate string AbilityCustomValidationAction(uint activator, uint target, int effectivePerkLevel);

    public class AbilityDetail
    {
        public string Name { get; set; }
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
        public StatusEffectType ConcentrationStatusEffectType { get; set; }
        public bool CanBeUsedInSpace { get; set; }

        public AbilityDetail()
        {
            ActivationVisualEffect = VisualEffect.None;
            AnimationType = Animation.Invalid;
            Requirements = new List<IAbilityActivationRequirement>();
            ConcentrationStatusEffectType = StatusEffectType.Invalid;
        }
    }
}
