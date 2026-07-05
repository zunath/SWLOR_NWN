using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class TidecallBeaconStatusEffect : StatusEffectBase
    {
        public override string Name => "Tidecall Beacon";
        public override EffectIconType Icon => EffectIconType.TidecallBeaconStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public TidecallBeaconStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDefensePercentAdjustment] = 6;
            StatGroup.Stats[StatType.ForceDefensePercentAdjustment] = 6;
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -4;
        }
    }
}
