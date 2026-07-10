using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Passive effect applied while its Mimicry trait technique is equipped. Grants a flat offensive bonus.
    /// </summary>
    public sealed class BattleFocusStatusEffect : StatusEffectBase
    {
        public override string Name => "Battle Focus";
        public override EffectIconType Icon => EffectIconType.AttackIncrease;

        public BattleFocusStatusEffect()
        {
            StatGroup.Stats[StatType.AttackPercentAdjustment] = 3;
        }
    }
}
