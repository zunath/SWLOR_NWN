using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Coagulant2StatusEffect : StatusEffectBase
    {
        public override string Name => "Coagulant II";
        public override EffectIconType Icon => EffectIconType.Coagulant2StatusEffect;
        public override bool PersistsOnLogout => false;

        public Coagulant2StatusEffect()
        {
            StatGroup.Stats[StatType.TraumaResistance] = 100;
            StatGroup.Stats[StatType.PhysicalDamageOverTimeTakenPercentAdjustment] = -20;
        }
    }
}
