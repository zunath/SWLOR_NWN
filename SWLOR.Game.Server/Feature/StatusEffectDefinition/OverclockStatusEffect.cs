using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>The Engineering Circuit style Overclock. The craft window applies it when the crafter picks the style.</summary>
    public sealed class OverclockStatusEffect : StatusEffectBase
    {
        public override string Name => "Overclock";
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override EffectIconType Icon => EffectIconType.OverclockStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public OverclockStatusEffect()
        {
            foreach (var (stat, value) in CircuitStyles.StatsOf(CircuitStyle.Overclock))
                StatGroup.Stats[stat] = value;
        }
    }
}
