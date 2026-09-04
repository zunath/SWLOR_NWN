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
    public class SunderingSweepAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.SunderingSweep1, PerkType.SunderingSweep)
                    .Name("Sundering Sweep I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SunderingSweep, 24.0f),
                SkillType.Lightsaber,
                8,
                0,
                null,
                null,
                4,
                6,
                0.0f,
                true,
                true,
                false,
                Spell.SunderingSweep1,
                AbilityTargetingShapeType.Sphere,
                3.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new GeneratedWeaponAbilityProfile
                {
                    SpreadSunderFromTarget = true,
                    SpreadSunderDurationSeconds = 30,
                    MaximumStatusSpreadsPerCast = 1
                });

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.SunderingSweep2, PerkType.SunderingSweep)
                    .Name("Sundering Sweep II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.SunderingSweep, 24.0f),
                SkillType.Lightsaber,
                12,
                0,
                null,
                null,
                5,
                8,
                0.0f,
                true,
                true,
                false,
                Spell.SunderingSweep2,
                AbilityTargetingShapeType.Sphere,
                3.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new GeneratedWeaponAbilityProfile
                {
                    SpreadSunderFromTarget = true,
                    SpreadSunderDurationSeconds = 30,
                    MaximumStatusSpreadsPerCast = 1
                });

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.SunderingSweep3, PerkType.SunderingSweep)
                    .Name("Sundering Sweep III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.SunderingSweep, 24.0f),
                SkillType.Lightsaber,
                16,
                0,
                null,
                null,
                6,
                10,
                0.0f,
                true,
                true,
                false,
                Spell.SunderingSweep3,
                AbilityTargetingShapeType.Sphere,
                3.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new GeneratedWeaponAbilityProfile
                {
                    SpreadSunderFromTarget = true,
                    SpreadSunderDurationSeconds = 30,
                    MaximumStatusSpreadsPerCast = 1
                });

            return builder.Build();
        }
    }
}
