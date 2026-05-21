using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class GuardiansInfluenceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.GuardiansInfluence1, PerkType.GuardiansInfluence)
                    .Name("Guardian's Influence")
                    .Level(1)
                    .HasTargetingSphere(
                        Spell.GuardianSInfluence1,
                        5f,
                        AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                    .HasRecastDelay(RecastGroup.GuardiansInfluence, 300f)
                    .IsAreaAbility(),
                typeof(DeflectingAuraStatusEffect),
                60f,
                7,
                false,
                2f);

            return builder.Build();
        }
    }
}
