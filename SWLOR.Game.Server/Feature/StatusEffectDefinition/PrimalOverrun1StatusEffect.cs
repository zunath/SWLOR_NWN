using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
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
