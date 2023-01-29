using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public class SnarlGrowlAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();


        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Snarl();
            Growl();

            return _builder.Build();
        }

        private string Validation(uint activator)
        {
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

        private void Snarl()
        {
            _builder.Create(FeatType.Snarl, PerkType.Snarl)
                .Name("Snarl")
                .Level(1)
                .HasRecastDelay(RecastGroup.SnarlGrowl, 30f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, activator);
                    var masterEnmity = Enmity.GetEnmityTowardsAllEnemies(activator);

                    foreach (var (enemy, amount) in masterEnmity)
                    {
                        var halfAmount = amount / 2;
                        Enmity.ModifyEnmity(activator, enemy, -halfAmount);
                        Enmity.ModifyEnmity(beast, enemy, halfAmount);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Crt_Red), activator);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Crt_Yellow), beast);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }

        private void Growl()
        {
            _builder.Create(FeatType.Growl, PerkType.Growl)
                .Name("Growl")
                .Level(1)
                .HasRecastDelay(RecastGroup.SnarlGrowl, 30f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(1f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasCustomValidation((activator, target, level, location) => Validation(activator))
                .HasImpactAction((activator, _, _, targetLocation) =>
                {
                    var beast = GetAssociate(AssociateType.Henchman, activator);
                    var beastEnmity = Enmity.GetEnmityTowardsAllEnemies(beast);

                    foreach (var (enemy, amount) in beastEnmity)
                    {
                        var halfAmount = amount / 2;
                        Enmity.ModifyEnmity(activator, enemy, halfAmount);
                        Enmity.ModifyEnmity(beast, enemy, -halfAmount);
                    }

                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Crt_Red), beast);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Com_Blood_Crt_Yellow), activator);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.BeastMastery);
                });
        }
    }
}
