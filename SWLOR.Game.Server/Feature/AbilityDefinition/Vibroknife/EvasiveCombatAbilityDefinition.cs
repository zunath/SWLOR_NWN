using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class EvasiveCombatAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.EvasiveCombat1, PerkType.EvasiveCombat)
                    .Name("Evasive Combat I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.EvasiveCombat, 300f),
                typeof(EvasiveCombatStatusEffect),
                30f,
                4);
            ConfigureSelfStatus(
                builder
                    .Create(FeatType.EvasiveCombat2, PerkType.EvasiveCombat)
                    .Name("Evasive Combat II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.EvasiveCombat, 300f),
                typeof(EvasiveCombatStatusEffect),
                30f,
                8);

            return builder.Build();
        }
    }
}
