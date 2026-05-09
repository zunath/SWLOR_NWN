using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public abstract class AbilityEnhancementStatusEffectBase : StatusEffectBase
    {
        private static readonly List<Type> EnhancementTypes = new()
        {
            typeof(CombatEnhancement1StatusEffect),
            typeof(CombatEnhancement2StatusEffect),
            typeof(CombatEnhancement3StatusEffect),
            typeof(ForceInspiration1StatusEffect),
            typeof(ForceInspiration2StatusEffect),
            typeof(ForceInspiration3StatusEffect)
        };

        public override List<Type> LessPowerfulEffectTypes => EnhancementTypes
            .Where(type => type != GetType())
            .ToList();
    }
}
