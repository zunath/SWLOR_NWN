using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class AegisEternalAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.AegisEternal1, PerkType.AegisEternal)
                    .Name("Aegis Eternal")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90.0f),
                SkillType.Lightsaber,
                8,
                30,
                null,
                null,
                15,
                8,
                2.0f,
                false,
                false,
                false,
                Spell.Invalid,
                AbilityTargetingShapeType.None,
                0.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new GeneratedWeaponAbilityProfile
                {
                    SelfStatusEffectFactory = () => new PerfectAegisStatusEffect(),
                    SelfStatusEffectsToReplace = new[] { typeof(SaberWardStatusEffect) }
                });

            return builder.Build();
        }
    }
}
