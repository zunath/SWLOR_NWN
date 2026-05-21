using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class CripplingDefenseAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureAreaStatus(
                builder
                    .Create(FeatType.CripplingDefense1, PerkType.CripplingDefense)
                    .Name("Crippling Defense")
                    .Level(1)
                    .SkillType(SkillType.Spear)
                    .HasTargetingSphere(
                        Spell.CripplingDefense1,
                        5f,
                        AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                    .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds),
                typeof(CripplingDefenseStatusEffect),
                CapstoneAbility.ActiveDurationSeconds,
                CapstoneAbility.StaminaCost,
                true,
                restoreStamina: 15,
                activationDelay: 3f);

            return builder.Build();
        }
    }
}
