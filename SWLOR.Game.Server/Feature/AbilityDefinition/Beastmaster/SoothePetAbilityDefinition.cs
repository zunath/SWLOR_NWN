using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public class SoothePetAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly CombatPoint _combatPoint;
        private readonly BeastMastery _beastMastery;
        private readonly IEnmityService _enmityService;

        public SoothePetAbilityDefinition(CombatPoint combatPoint, BeastMastery beastMastery, IEnmityService enmityService)
        {
            _combatPoint = combatPoint;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            SoothePet();

            return _builder.Build();
        }

        private void SoothePet()
        {
            _builder.Create(FeatType.SoothePet, PerkType.SoothePet)
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

                    StatusEffect.Remove(beast, StatusEffectType.Bleed);
                    StatusEffect.Remove(beast, StatusEffectType.Poison);
                    StatusEffect.Remove(beast, StatusEffectType.Shock);
                    StatusEffect.Remove(beast, StatusEffectType.Burn);
                    StatusEffect.Remove(beast, StatusEffectType.Disease);

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
                    _combatPoint.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}