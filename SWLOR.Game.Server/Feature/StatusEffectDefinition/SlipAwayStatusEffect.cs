using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class SlipAwayStatusEffect : StatusEffectBase
    {
        public override string Name => "Slip Away";
        public override EffectIconType Icon => EffectIconType.SlipAwayStatusEffect;
        public override StatusEffectActivationType ActivationType => StatusEffectActivationType.Passive;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;
        public SlipAwayStatusEffect() : this(20, 15) { }
        public SlipAwayStatusEffect(int evasion, int movement)
        {
            StatGroup.Stats[StatType.EvasionPercentAdjustment] = evasion;
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = movement;
        }
        public override IStatusEffect Clone() => new SlipAwayStatusEffect(
            StatGroup.Stats[StatType.EvasionPercentAdjustment], StatGroup.Stats[StatType.MovementSpeedPercentAdjustment]);
    }
}
