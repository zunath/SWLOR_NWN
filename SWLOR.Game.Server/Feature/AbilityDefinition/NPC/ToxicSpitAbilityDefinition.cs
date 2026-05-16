using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ToxicSpitAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.ToxicSpit, profile.PlayerPerkType)
                .Name("Toxic Spit")
                .HasActivationDelay(1.0f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasRecastDelay(RecastGroup.ToxicSpit, 18f)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        8,
                        30,
                        typeof(PoisonStatusEffect),
                        false,
                        damageType: CombatDamageType.Poison,
                        statusResistanceType: ResistanceType.Poison,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Poison_S,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
