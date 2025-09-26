using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Communication.Contracts;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.FirstAid
{
    public class KoltoRecoveryAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        private readonly IServiceProvider _serviceProvider;

        public KoltoRecoveryAbilityDefinition(
            IServiceProvider serviceProvider,
            ICombatPointService combatPointService, 
            IEnmityService enmityService, 
            IAbilityService abilityService,
            IStatusEffectService statusEffect) 
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        // Lazy-loaded services to break circular dependencies
        private IRandomService Random => _serviceProvider.GetRequiredService<IRandomService>();
        private IPerkService PerkService => _serviceProvider.GetRequiredService<IPerkService>();
        private IPartyService PartyService => _serviceProvider.GetRequiredService<IPartyService>();

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            KoltoRecovery1(builder);
            KoltoRecovery2(builder);
            KoltoRecovery3(builder);

            return builder.Build();
        }

        private string Validation(uint activator, uint target, int level, Location location)
        {
            if (!HasMedicalSupplies(activator))
            {
                return "You have no medical supplies.";
            }

            return string.Empty;
        }

        private void Impact(uint activator, int baseAmount)
        {
            var willpowerMod = GetAbilityModifier(AbilityType.Willpower, activator);
            var distance = 6f + PerkService.GetPerkLevel(activator, PerkType.RangedHealing);
            var party = PartyService.GetAllPartyMembersWithinRange(activator, distance);

            foreach (var member in party)
            {
                var amount = baseAmount + willpowerMod * 5 + Random.D10(1);

                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), member);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), member);
            }

            TakeMedicalSupplies(activator);
        }

        private void KoltoRecovery1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoRecovery1, PerkType.KoltoRecovery)
                .Name("Kolto Recovery I")
                .Level(1)
                .HasRecastDelay(RecastGroup.KoltoRecovery, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, _) =>
                {
                    Impact(activator, 15);

                    EnmityService.ModifyEnmityOnAll(activator, 150);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
        private void KoltoRecovery2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoRecovery2, PerkType.KoltoRecovery)
                .Name("Kolto Recovery II")
                .Level(2)
                .HasRecastDelay(RecastGroup.KoltoRecovery, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(6)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, _) =>
                {
                    Impact(activator, 60);

                    EnmityService.ModifyEnmityOnAll(activator, 300);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
        private void KoltoRecovery3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.KoltoRecovery3, PerkType.KoltoRecovery)
                .Name("Kolto Recovery III")
                .Level(3)
                .HasRecastDelay(RecastGroup.KoltoRecovery, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.LoopingGetMid)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation(Validation)
                .HasImpactAction((activator, _, _, _) =>
                {
                    Impact(activator, 100);

                    EnmityService.ModifyEnmityOnAll(activator, 450);
                    CombatPointService.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
