using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class EmergencySealant1StatusEffect : StatusEffectBase
    {
        public override string Name => "Emergency Sealant I";
        public override EffectIconType Icon => EffectIconType.EmergencySealant1StatusEffect;
        public override bool PersistsOnLogout => false;

        public EmergencySealant1StatusEffect()
        {
            StatGroup.Stats[StatType.TraumaResistance] = 2;
        }
    }
}
