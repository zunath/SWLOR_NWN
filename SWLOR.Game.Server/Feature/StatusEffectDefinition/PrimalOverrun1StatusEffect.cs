using System.Collections.Generic;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class PrimalOverrun1StatusEffect : StatusEffectBase
    {
        public override string Name => "Primal Overrun";
        public override EffectIconType Icon => EffectIconType.PrimalOverrun1StatusEffect;
        public override bool PersistsOnLogout => false;

        public PrimalOverrun1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageDealtPercentAdjustment] = 12;
        }
    }
}
