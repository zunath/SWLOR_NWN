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
    public class EpicenterAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeaponAbility(
                builder.Create(FeatType.Epicenter1, PerkType.Epicenter)
                    .Name("Epicenter")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90.0f),
                SkillType.Lightsaber,
                25,
                6,
                typeof(KnockdownStatusEffect),
                new[] { typeof(SunderStatusEffect) },
                10,
                15,
                3.0f,
                true,
                true,
                false,
                Spell.Epicenter1,
                AbilityTargetingShapeType.Sphere,
                6.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new WeaponAbilityProfile
                {
                    DamageType = CombatDamageType.Force,
                    ExtraDamageTargetStatusEffect = typeof(SunderStatusEffect),
                    ExtraDamageIfTargetStatusEffect = 15
                });

            return builder.Build();
        }
    }
}
