using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid
{
    public class KoltoRecoveryAbilityDefinition: FirstAidBaseAbilityDefinition
    {
        private readonly IRandomService _random;
        private readonly IPerkService _perkService;
        private readonly IPartyService _partyService;
        private readonly CombatPoint _combatPoint;

        public KoltoRecoveryAbilityDefinition(IRandomService random, IPerkService perkService, IPartyService partyService, CombatPoint combatPoint, IEnmityService enmityService, IAbilityService abilityService) : base(random, perkService, combatPoint, enmityService, abilityService)
        {
            _random = random;
            _perkService = perkService;
            _partyService = partyService;
            _combatPoint = combatPoint;
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            KoltoRecovery1();
            KoltoRecovery2();
            KoltoRecovery3();

            return Builder.Build();
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
            var distance = 6f + _perkService.GetPerkLevel(activator, PerkType.RangedHealing);
            var party = _partyService.GetAllPartyMembersWithinRange(activator, distance);

            foreach (var member in party)
            {
                var amount = baseAmount + willpowerMod * 5 + _random.D10(1);

                ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), member);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Heal), member);
            }

            TakeMedicalSupplies(activator);
        }

        private void KoltoRecovery1()
        {
            Builder.Create(FeatType.KoltoRecovery1, PerkType.KoltoRecovery)
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

                    _enmityService.ModifyEnmityOnAll(activator, 150);
                    _combatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
        private void KoltoRecovery2()
        {
            Builder.Create(FeatType.KoltoRecovery2, PerkType.KoltoRecovery)
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

                    _enmityService.ModifyEnmityOnAll(activator, 300);
                    _combatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
        private void KoltoRecovery3()
        {
            Builder.Create(FeatType.KoltoRecovery3, PerkType.KoltoRecovery)
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

                    _enmityService.ModifyEnmityOnAll(activator, 450);
                    _combatPoint.AddCombatPointToAllTagged(activator, SkillType.FirstAid, 3);
                });
        }
    }
}
