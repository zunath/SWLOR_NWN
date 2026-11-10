using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class KineticHarnessStatusEffect : StatusEffectBase
    {
        public override string Name => "Kinetic Harness";
        public override EffectIconType Icon => EffectIconType.KineticHarnessStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Buff;
        public override bool PersistsOnLogout => false;

        public KineticHarnessStatusEffect()
        {
            StatGroup.Stats[StatType.MovementSpeedPercentAdjustment] = 12;
            StatGroup.Stats[StatType.AttackDelayReductionPercent] = 6;
        }
    }
}
