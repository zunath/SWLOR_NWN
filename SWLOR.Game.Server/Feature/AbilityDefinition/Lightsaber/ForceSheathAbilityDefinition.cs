using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class ForceSheathAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeaponAbility(
                builder.Create(FeatType.ForceSheath1, PerkType.ForceSheath)
                    .Name("Force Sheath I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ForceSheath, 16.0f),
                SkillType.Lightsaber,
                12,
                0,
                null,
                null,
                2,
                4,
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
                new WeaponAbilityProfile
                {
                    IsQueuedWeaponAbility = true,
                    DamageType = CombatDamageType.Force
                });

            ConfigureWeaponAbility(
                builder.Create(FeatType.ForceSheath2, PerkType.ForceSheath)
                    .Name("Force Sheath II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.ForceSheath, 16.0f),
                SkillType.Lightsaber,
                17,
                0,
                null,
                null,
                3,
                5,
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
                new WeaponAbilityProfile
                {
                    IsQueuedWeaponAbility = true,
                    DamageType = CombatDamageType.Force
                });

            ConfigureWeaponAbility(
                builder.Create(FeatType.ForceSheath3, PerkType.ForceSheath)
                    .Name("Force Sheath III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.ForceSheath, 16.0f),
                SkillType.Lightsaber,
                23,
                0,
                null,
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
                new WeaponAbilityProfile
                {
                    IsQueuedWeaponAbility = true,
                    DamageType = CombatDamageType.Force
                });

            ConfigureWeaponAbility(
                builder.Create(FeatType.ForceSheath4, PerkType.ForceSheath)
                    .Name("Force Sheath IV")
                    .Level(4)
                    .HasRecastDelay(RecastGroup.ForceSheath, 18.0f),
                SkillType.Lightsaber,
                30,
                0,
                null,
                null,
                5,
                7,
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
                new WeaponAbilityProfile
                {
                    IsQueuedWeaponAbility = true,
                    DamageType = CombatDamageType.Force
                });

            return builder.Build();
        }
    }
}
