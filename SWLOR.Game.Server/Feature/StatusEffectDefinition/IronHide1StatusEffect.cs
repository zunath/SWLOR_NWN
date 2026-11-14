using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronHide1StatusEffect : StatusEffectBase
    {
        public override string Name => "Iron Hide I";
        public override EffectIconType Icon => EffectIconType.IronHide1StatusEffect;

        public IronHide1StatusEffect()
        {
            MorePowerfulEffectTypes.Add(typeof(IronHide2StatusEffect));
            MorePowerfulEffectTypes.Add(typeof(IronHide3StatusEffect));
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -5;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
        }
    }
}
