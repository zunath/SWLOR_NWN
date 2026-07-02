using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Armor
{
    public class ProvokeAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Provoke();
            Provoke2();

            return _builder.Build();
        }

        private string Validation(uint target)
        {
            if (GetIsPC(target))
            {
                return "This ability cannot be used on players.";
            }

            if (BeastMastery.IsPlayerBeast(target) || Droid.IsDroid(target))
            {
                return "This ability cannot be used on associates.";
            }

            return string.Empty;
        }

        private bool IsValidProvokeTarget(uint activator, uint target)
        {
            return GetIsObjectValid(target) &&
                   GetIsReactionTypeHostile(target, activator) &&
                   !GetIsPC(target) &&
                   !BeastMastery.IsPlayerBeast(target) &&
                   !Droid.IsDroid(target);
        }

        private void Impact(uint activator, uint target, int enmity)
        {
            if (!IsValidProvokeTarget(activator, target) ||
                !LineOfSightObject(activator, target))
            {
                return;
            }

            Enmity.ModifyEnmity(activator, target, enmity);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), target);
        }

        private void Provoke()
        {
            _builder
                .Create(FeatType.Provoke1, PerkType.Provoke)
                .Name("Provoke I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Provoke, 6f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(15f)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasImpactAction((activator, target, _, _) =>
                {
                    var enmity = Stat.ScaleEffect(700, GetAbilityScore(activator, AbilityType.Vitality));
                    Impact(activator, target, enmity);
                });
        }

        private void Provoke2()
        {
            _builder
                .Create(FeatType.Provoke2, PerkType.Provoke)
                .Name("Provoke II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Provoke2, 12f)
                .HasActivationDelay(1f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(15f)
                .IsAreaAbility()
                .IsHostileAbility()
                .HasCustomValidation((_, target, _, _) => Validation(target))
                .HasTargetingSphere(
                    Spell.Provoke2,
                    8f,
                    AbilityTargetingFlags.HarmsEnemies)
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    var impactLocation = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
                    foreach (var hostile in AbilityTargeting.GetHostileTargetsNearLocation(
                                 activator,
                                 impactLocation,
                                 8f,
                                 0,
                                 target,
                                 creature => IsValidProvokeTarget(activator, creature) &&
                                             LineOfSightObject(activator, creature)))
                    {
                        var enmity = Stat.ScaleEffect(400, GetAbilityScore(activator, AbilityType.Vitality));
                        Impact(activator, hostile, enmity);
                    }
                });
        }
    }
}
