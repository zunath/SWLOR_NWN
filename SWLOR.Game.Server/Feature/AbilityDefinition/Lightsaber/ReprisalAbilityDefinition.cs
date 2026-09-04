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
    public class ReprisalAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Build(builder, FeatType.Reprisal1, "Reprisal I", 1, 16, 6, 2);
            Build(builder, FeatType.Reprisal2, "Reprisal II", 2, 30, 9, 3);

            return builder.Build();
        }

        private static void Build(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int baseDamage,
            int stamina,
            int fp)
        {
            ConfigureWeaponAbility(
                builder.Create(feat, PerkType.Reprisal)
                    .Name(name)
                    .Level(level)
                    .HasRecastDelay(RecastGroup.Reprisal, 30.0f),
                SkillType.Lightsaber,
                baseDamage,
                0,
                null,
                null,
                stamina,
                fp,
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
                    ConditionalTargetStatusEffect = typeof(DazedStatusEffect),
                    ConditionalTargetStatusDurationSeconds = 15,
                    ProtectedTargetHitWindowSeconds = 30,
                    RequireTargetRecentlyDamagedActivatorForConditionalStatus = true
                });
        }
    }
}
