using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class AuraOfCourage1StatusEffect : StatusEffectBase
    {
        public override string Name => "Courageous Resolve";
        public override EffectIconType Icon => EffectIconType.DamageReduction;
        public override bool PersistsOnLogout => false;

        public AuraOfCourage1StatusEffect()
        {
            StatGroup.Stats[StatType.ForceDamageTakenPercentAdjustment] = -5;
            StatGroup.Stats[StatType.MindResistance] = 10;
        }
    }
}
