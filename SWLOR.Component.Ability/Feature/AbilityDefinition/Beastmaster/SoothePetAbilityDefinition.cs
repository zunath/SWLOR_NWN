using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Character.ValueObjects;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Common.Enums;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beastmaster
{
    public class SoothePetAbilityDefinition : IAbilityListDefinition
    {
        private readonly IAbilityBuilder builder;
        private readonly ICombatPointService _combatPointService;
        private readonly IBeastMasteryService _beastMastery;
        private readonly IEnmityService _enmityService;
        private readonly IStatusEffectService _statusEffectService;

        public SoothePetAbilityDefinition(
            ICombatPointService combatPointService, 
            IBeastMasteryService beastMastery, 
            IEnmityService enmityService, 
            IStatusEffectService statusEffectService)
        {
            _combatPointService = combatPointService;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            SoothePet(builder);

            return builder.Build();
        }

        private void SoothePet(IAbilityBuilder builder)
        {
            builder.Create(FeatType.SoothePet, PerkType.SoothePet)
                .Name("Soothe Pet")
                .Level(1)
                .HasRecastDelay(RecastGroup.Tame, 60f * 3)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(1f)
                .RequirementStamina(2)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) =>
                {
                    if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
                    {
                        return "Only players may use this ability.";
                    }

                    var beast = GetAssociate(AssociateType.Henchman, activator);
                    if (!_beastMastery.IsPlayerBeast(beast))
                    {
                        return "You do not have an active beast.";
                    }

                    if (GetDistanceBetween(beast, activator) >= 15f)
                    {
                        return "Your beast is too far away.";
                    }

                    return string.Empty;
                })
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, activator);

                    _statusEffectService.Remove(beast, StatusEffectType.Bleed);
                    _statusEffectService.Remove(beast, StatusEffectType.Poison);
                    _statusEffectService.Remove(beast, StatusEffectType.Shock);
                    _statusEffectService.Remove(beast, StatusEffectType.Burn);
                    _statusEffectService.Remove(beast, StatusEffectType.Disease);

                    RemoveEffect(beast, 
                        EffectTypeScript.Disease, 
                        EffectTypeScript.Poison, 
                        EffectTypeScript.Confused,
                        EffectTypeScript.Paralyze,
                        EffectTypeScript.Stunned,
                        EffectTypeScript.Sleep,
                        EffectTypeScript.Slow);

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), beast);
                    _enmityService.ModifyEnmityOnAll(activator, 500);
                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}
