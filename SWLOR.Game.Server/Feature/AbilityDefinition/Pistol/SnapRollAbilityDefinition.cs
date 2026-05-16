using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class SnapRollAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float RecastDelay = 60f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SnapRoll1(builder);
            SnapRoll2(builder);

            return builder.Build();
        }

        private static void SnapRoll1(AbilityBuilder builder)
        {
            var ability = builder
                .Create(FeatType.SnapRoll1, PerkType.SnapRoll)
                .Name("Snap Roll I")
                .Level(1)
                .SkillType(SkillType.Pistol)
                .HasRecastDelay(RecastGroup.SnapRoll, RecastDelay);

            ConfigureSelfStatus(
                ability,
                typeof(SnapRollStatusEffect),
                duration: 6f,
                stamina: 6,
                activator => Enmity.ReduceEnmityOnAll(activator, 10));
        }

        private static void SnapRoll2(AbilityBuilder builder)
        {
            var ability = builder
                .Create(FeatType.SnapRoll2, PerkType.SnapRoll)
                .Name("Snap Roll II")
                .Level(2)
                .SkillType(SkillType.Pistol)
                .HasRecastDelay(RecastGroup.SnapRoll, RecastDelay);
            Func<IStatusEffect> statusEffectFactory = () => new SnapRollStatusEffect(35);

            ConfigureSelfStatus(
                ability,
                statusEffectFactory,
                duration: 8f,
                stamina: 8,
                GrantSnapRoll2DamageBonus);
        }

        private static void GrantSnapRoll2DamageBonus(uint activator)
        {
            TemporaryStatModifier.Replace(
                activator,
                StatType.NextSkillAutoAttackDamageBonusSkillType,
                (int)SkillType.Pistol,
                8f,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
            TemporaryStatModifier.Replace(
                activator,
                StatType.NextSkillAutoAttackDamageBonus,
                10,
                8f,
                StatType.NextSkillAutoAttackDamageBonusSkillType);
        }
    }
}
