using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class InterruptionStrikeAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCurrentAttackTargetInterrupt(builder.Create(FeatType.InterruptionStrike1, PerkType.InterruptionStrike).Name("Interruption Strike I").Level(1), SkillType.Spear, 0, 30, typeof(FoggyMindStatusEffect), 5, FoggyMind(2));
            ConfigureCurrentAttackTargetInterrupt(builder.Create(FeatType.InterruptionStrike2, PerkType.InterruptionStrike).Name("Interruption Strike II").Level(2), SkillType.Spear, 0, 30, typeof(FoggyMindStatusEffect), 7, FoggyMind(2));

            return builder.Build();
        }
    }
}
