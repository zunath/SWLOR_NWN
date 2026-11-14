using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Intercept2StatusEffect : StatusEffectBase
    {
        public override string Name => "Intercept II";
        public override EffectIconType Icon => EffectIconType.Intercept2StatusEffect;

        public Intercept2StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenRedirectToStatusSourcePercent] = 50;
        }
    }
}
