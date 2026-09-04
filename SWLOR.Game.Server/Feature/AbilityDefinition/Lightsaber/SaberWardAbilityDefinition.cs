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
    public class SaberWardAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Build(builder, FeatType.SaberWard1, "Saber Ward I", 1, 8, 3, 2, 15, 3, 4);
            Build(builder, FeatType.SaberWard2, "Saber Ward II", 2, 18, 5, 3, 20, 4, 5);
            Build(builder, FeatType.SaberWard3, "Saber Ward III", 3, 28, 8, 4, 25, 5, 7);
            Build(builder, FeatType.SaberWard4, "Saber Ward IV", 4, 38, 12, 5, 30, 6, 9);

            return builder.Build();
        }

        private static void Build(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int skillRequirement,
            int stamina,
            int fp,
            int conversionPercent,
            int defensePercent,
            int forceDefensePercent)
        {
            ConfigureWeaponAbility(
                builder.Create(feat, PerkType.SaberWard)
                    .Name(name)
                    .Level(level)
                    .HasRecastDelay(RecastGroup.SaberWard, 18.0f),
                SkillType.Lightsaber,
                skillRequirement,
                30,
                null,
                null,
                stamina,
                fp,
                0.0f,
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
                new WeaponAbilityProfile
                {
                    SelfStatusEffectFactory = () => new SaberWardStatusEffect(conversionPercent, defensePercent, forceDefensePercent),
                    SelfStatusEffectsToReplace = new[] { typeof(PerfectAegisStatusEffect) }
                });
        }
    }
}
