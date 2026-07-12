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
    public class ShatteringStrikeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.ShatteringStrike1, PerkType.ShatteringStrike)
                    .Name("Shattering Strike I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ShatteringStrike, 24.0f),
                SkillType.Lightsaber,
                18,
                30,
                typeof(SunderStatusEffect),
                null,
                4,
                6,
                0.0f,
                false,
                true,
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
                    StatusEffectFactory = () => new SunderStatusEffect(10)
                });

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.ShatteringStrike2, PerkType.ShatteringStrike)
                    .Name("Shattering Strike II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.ShatteringStrike, 24.0f),
                SkillType.Lightsaber,
                28,
                30,
                typeof(SunderStatusEffect),
                null,
                6,
                8,
                0.0f,
                false,
                true,
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
                    StatusEffectFactory = () => new SunderStatusEffect(12)
                });

            return builder.Build();
        }
    }
}
