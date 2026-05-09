using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceBody1StatusEffect : ForceRestoreStatusEffectBase
    {
        public override string Name => "Force Body I";
        protected override bool RestoresFP => true;
        protected override int Level => 1;

        public ForceBody1StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Vitality] = -2;
        }

        public override List<Type> MorePowerfulEffectTypes => new()
        {
            typeof(ForceBody2StatusEffect)
        };
    }
}
