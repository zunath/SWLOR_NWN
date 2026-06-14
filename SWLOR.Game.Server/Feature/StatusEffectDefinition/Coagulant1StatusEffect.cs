using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Coagulant1StatusEffect : StatusEffectBase
    {
        public override string Name => "Coagulant I";
        public override EffectIconType Icon => EffectIconType.Coagulant1StatusEffect;
        public override bool PersistsOnLogout => false;

        public Coagulant1StatusEffect()
        {
            StatGroup.Stats[StatType.TraumaResistance] = 50;
            StatGroup.Stats[StatType.PhysicalDamageOverTimeTakenPercentAdjustment] = -10;
        }
    }
}
