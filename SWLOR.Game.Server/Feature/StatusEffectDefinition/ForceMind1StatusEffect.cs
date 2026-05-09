using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceMind1StatusEffect : ForceRestoreStatusEffectBase
    {
        public override string Name => "Force Mind I";
        protected override bool RestoresFP => false;
        protected override int Level => 1;

        public ForceMind1StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Willpower] = -2;
        }

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceMind2StatusEffect)
        };
    }
}
