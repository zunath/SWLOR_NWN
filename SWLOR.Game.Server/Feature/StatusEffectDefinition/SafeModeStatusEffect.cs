using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>The Engineering Circuit style Safe Mode. The craft window applies it when the crafter picks the style.</summary>
    public sealed class SafeModeStatusEffect : StatusEffectBase
    {
        public override string Name => "Safe Mode";
        public override float Frequency => 1f;
        public override EffectIconType Icon => EffectIconType.SafeModeStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public SafeModeStatusEffect()
        {
            StatGroup.Stats[StatType.CircuitOverloadGrace] = CircuitPerkRules.SafeModeOverloadGrace;
            StatGroup.Stats[StatType.CircuitLoadBoostPercent] = CircuitPerkRules.SafeModeLoadBoostPercent;
        }
    }
}
