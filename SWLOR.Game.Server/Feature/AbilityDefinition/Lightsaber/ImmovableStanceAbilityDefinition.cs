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
    public class ImmovableStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeaponAbility(
                builder.Create(FeatType.ImmovableStance1, PerkType.ImmovableStance)
                    .Name("Immovable Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ImmovableStance, 30.0f),
                SkillType.Lightsaber,
                8,
                0,
                typeof(ImmovableStanceStatusEffect),
                null,
                0,
                0,
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
                null);

            return builder.Build();
        }
    }
}
