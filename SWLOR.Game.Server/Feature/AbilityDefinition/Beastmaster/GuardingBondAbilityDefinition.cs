using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class GuardingBondAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.Snarl, PerkType.Snarl)
                .Name("Guarding Bond")
                .Level(1)
                .HasRecastDelay(RecastGroup.BeastBond, 180f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .SkillType(SkillType.BeastMastery)
                .HasCustomValidation((activator, target, level, location) => ValidateBeast(activator));

            ConfigureToggle(builder, typeof(GuardingBondStatusEffect));

            return builder.Build();
        }

        private static string ValidateBeast(uint activator)
        {
            if (StatusEffect.HasStatusEffect(activator, typeof(GuardingBondStatusEffect)))
            {
                return string.Empty;
            }

            if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
            {
                return "Only players may use this ability.";
            }

            var beast = GetAssociate(AssociateType.Henchman, activator);
            if (!BeastMastery.IsPlayerBeast(beast))
            {
                return "You do not have an active beast.";
            }

            if (GetDistanceBetween(beast, activator) >= 15f)
            {
                return "Your beast is too far away.";
            }

            return string.Empty;
        }
    }
}
