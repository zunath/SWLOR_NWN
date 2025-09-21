using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public class SoothePetAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly ICombatPointService _combatPointService;
        private readonly BeastMastery _beastMastery;
        private readonly IEnmityService _enmityService;
        private readonly IStatusEffectService _statusEffectService;

        public SoothePetAbilityDefinition(ICombatPointService combatPointService, BeastMastery beastMastery, IEnmityService enmityService, IStatusEffectService statusEffectService)
        {
            _combatPointService = combatPointService;
            _beastMastery = beastMastery;
            _enmityService = enmityService;
            _statusEffectService = statusEffectService;
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
