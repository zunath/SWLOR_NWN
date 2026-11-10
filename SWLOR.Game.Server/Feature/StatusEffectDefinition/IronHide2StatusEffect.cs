using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class IronHide2StatusEffect : StatusEffectBase
    {
        public override string Name => "Iron Hide II";
        public override EffectIconType Icon => EffectIconType.IronHide2StatusEffect;

        public IronHide2StatusEffect()
        {
            LessPowerfulEffectTypes.Add(typeof(IronHide1StatusEffect));
            MorePowerfulEffectTypes.Add(typeof(IronHide3StatusEffect));
            StatGroup.Stats[StatType.PhysicalDamageTakenPercentAdjustment] = -8;
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -8;
        }
    }
}
