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
    public class GuardiansChallengeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Build(builder, FeatType.GuardiansChallenge1, "Guardian's Challenge I", 1, 12, 4, 1, 20, Spell.GuardiansChallenge1);
            Build(builder, FeatType.GuardiansChallenge2, "Guardian's Challenge II", 2, 24, 8, 2, 30, Spell.GuardiansChallenge2);

            return builder.Build();
        }

        private static void Build(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int baseDamage,
            int stamina,
            int fp,
            int enmityPercent,
            Spell targetingSpell)
        {
            ConfigureGeneratedWeaponAbility(
                builder.Create(feat, PerkType.GuardiansChallenge)
                    .Name(name)
                    .Level(level)
                    .HasRecastDelay(RecastGroup.GuardiansChallenge, 24.0f),
                SkillType.Lightsaber,
                baseDamage,
                30,
                null,
                null,
                stamina,
                fp,
                0.0f,
                true,
                true,
                false,
                targetingSpell,
                AbilityTargetingShapeType.Rect,
                8.0f,
                3.0f,
                AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf,
                Animation.DoubleStrike,
                0.0f,
                AbilityType.Invalid,
                new GeneratedWeaponAbilityProfile
                {
                    ProtectedTargetHitWindowSeconds = 30,
                    SelfEnmityPercentIfTargetRecentlyDamagedActivator = enmityPercent,
                    SelfEnmityDurationSecondsIfTargetRecentlyDamagedActivator = 30
                });
        }
    }
}
