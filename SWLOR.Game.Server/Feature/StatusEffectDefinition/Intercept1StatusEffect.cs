using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class Intercept1StatusEffect : StatusEffectBase
    {
        public override string Name => "Intercept I";
        public override EffectIconType Icon => EffectIconType.Intercept1StatusEffect;

        public Intercept1StatusEffect()
        {
            StatGroup.Stats[StatType.DamageTakenRedirectToStatusSourcePercent] = 35;
        }
    }
}
