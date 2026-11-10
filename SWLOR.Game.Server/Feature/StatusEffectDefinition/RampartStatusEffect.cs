using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RampartStatusEffect : StatusEffectBase
    {
        public override string Name => "Rampart";
        public override EffectIconType Icon => EffectIconType.RampartStatusEffect;
        public RampartStatusEffect()
        {
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -15;
            StatGroup.Stats[StatType.HeavyVibrobladeDefenseRecoveryWindow] = 1;
        }

    }
}
