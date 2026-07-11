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
    public class SarlaccSweepAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.SarlaccSweep1, PerkType.SarlaccSweep)
                    .Name("Sarlacc Sweep I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.SarlaccSweep, 24.0f),
                SkillType.Lightsaber,
                8,
                0,
                typeof(SunderStatusEffect),
                null,
                4,
                6,
                0.0f,
                true,
                true,
                false,
                Spell.Invalid,
                AbilityTargetingShapeType.Sphere,
                5.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                null);

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.SarlaccSweep2, PerkType.SarlaccSweep)
                    .Name("Sarlacc Sweep II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.SarlaccSweep, 24.0f),
                SkillType.Lightsaber,
                12,
                0,
                typeof(SunderStatusEffect),
                null,
                5,
                8,
                0.0f,
                true,
                true,
                false,
                Spell.Invalid,
                AbilityTargetingShapeType.Sphere,
                5.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                null);

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.SarlaccSweep3, PerkType.SarlaccSweep)
                    .Name("Sarlacc Sweep III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.SarlaccSweep, 24.0f),
                SkillType.Lightsaber,
                16,
                0,
                typeof(SunderStatusEffect),
                null,
                6,
                10,
                0.0f,
                true,
                true,
                false,
                Spell.Invalid,
                AbilityTargetingShapeType.Sphere,
                5.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                null);

            return builder.Build();
        }
    }
}
