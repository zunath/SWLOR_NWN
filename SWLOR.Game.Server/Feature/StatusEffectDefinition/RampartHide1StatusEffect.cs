using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RampartHide1StatusEffect : StatusEffectBase
    {
        public override string Name => "Rampart Hide";
        public override EffectIconType Icon => EffectIconType.RampartHide1StatusEffect;
        public override bool PersistsOnLogout => false;

        public RampartHide1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenPercentAdjustment] = -20;
        }
    }
}
