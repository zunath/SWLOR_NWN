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
    public class ForceLinkAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureGeneratedWeaponAbility(
                builder.Create(FeatType.ForceLink1, PerkType.SaberForceLink)
                    .Name("Force Link")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ForceLink, 24.0f),
                SkillType.Lightsaber,
                8,
                45,
                null,
                null,
                4,
                3,
                0.0f,
                false,
                false,
                true,
                Spell.Invalid,
                AbilityTargetingShapeType.None,
                0.0f,
                0.0f,
                AbilityTargetingFlags.HarmsEnemies,
                Animation.DoubleStrike,
                20.0f,
                AbilityType.Invalid,
                new GeneratedWeaponAbilityProfile
                {
                    FriendlyTargetStatusEffectFactory = () => new WardBondStatusEffect(45, 0, 0, 0, 20.0f)
                });

            return builder.Build();
        }
    }
}
