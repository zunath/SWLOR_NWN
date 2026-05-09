using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceMind2StatusEffect : ForceRestoreStatusEffectBase
    {
        public override string Name => "Force Mind II";
        protected override bool RestoresFP => false;
        protected override int Level => 2;

        public ForceMind2StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Willpower] = -4;
        }

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceMind1StatusEffect)
        };
    }
}
