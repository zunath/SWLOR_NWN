using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;


namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public class DeflectorShieldAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            DeflectorShield1();
            DeflectorShield2();
            DeflectorShield3();

            return _builder.Build();
        }

        private void Impact(uint activator, float percent, bool affectsParty, float duration)
        {
            ApplyEffect(activator, percent, duration);
            if (affectsParty)
            {
                var member = GetFirstFactionMember(activator);
                while (GetIsObjectValid(member))
                {
                    if (member != activator &&
                        GetDistanceBetween(activator, member) <= 10f)
                    {
                        ApplyEffect(member, percent, duration);
                    }

                    member = GetNextFactionMember(activator);
                }
            }

            Enmity.ModifyEnmityOnAll(activator, 45);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Devices, 3);
        }

        private void ApplyEffect(uint target, float percent, float duration)
        {
            var maxHP = (int)(GetMaxHitPoints(target) * percent);
            var effect = EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Pulse_Cyan_Blue);
            effect = EffectLinkEffects(effect, EffectTemporaryHitpoints(maxHP));

            ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);
        }

        private void DeflectorShield1()
        {
            _builder.Create(FeatType.DeflectorShield1, PerkType.DeflectorShield)
                .Name("Deflector Shield I")
                .HasRecastDelay(RecastGroup.DeflectorShield, 300f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .UsesAnimation(Animation.Kneel)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, 0.15f, false, 180f);
                });
        }

        private void DeflectorShield2()
        {
            _builder.Create(FeatType.DeflectorShield2, PerkType.DeflectorShield)
                .Name("Deflector Shield II")
                .HasRecastDelay(RecastGroup.DeflectorShield, 300f)
                .HasActivationDelay(3f)
                .RequirementStamina(7)
                .UsesAnimation(Animation.Kneel)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, 0.30f, false, 300f);
                });
        }

        private void DeflectorShield3()
        {
            _builder.Create(FeatType.DeflectorShield3, PerkType.DeflectorShield)
                .Name("Deflector Shield III")
                .HasRecastDelay(RecastGroup.DeflectorShield, 300f)
                .HasActivationDelay(3f)
                .RequirementStamina(9)
                .UsesAnimation(Animation.Kneel)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, _, targetLocation) =>
                {
                    Impact(activator, 0.40f, true, 300f);
                });
        }
    }
}
