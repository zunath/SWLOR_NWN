using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class ForceBody2StatusEffect : ForceRestoreStatusEffectBase
    {
        public override string Name => "Force Body II";
        protected override bool RestoresFP => true;
        protected override int Level => 2;

        public ForceBody2StatusEffect()
        {
            StatGroup.Abilities[AbilityType.Vitality] = -4;
        }

        public override List<Type> LessPowerfulEffectTypes => new()
        {
            typeof(ForceBody1StatusEffect)
        };
    }
}
