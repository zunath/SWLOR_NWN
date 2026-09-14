using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class StonewakeRelayStatusEffect : StatusEffectBase
    {
        public override string Name => "Stonewake Relay";
        public override EffectIconType Icon => EffectIconType.StonewakeRelayStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public StonewakeRelayStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 8;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 8;
        }
    }
}
