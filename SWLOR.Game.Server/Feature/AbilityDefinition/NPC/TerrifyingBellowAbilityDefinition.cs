using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class TerrifyingBellowAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.TerrifyingBellow, profile.PlayerPerkType)
                .Name("Terrifying Bellow")
                .HasActivationDelay(1.0f)
                .HasRecastDelay(RecastGroup.TerrifyingBellow, 20f)
                .UsesAnimation(Animation.FireForgetTaunt)
                .IsCastedAbility()
                .HasMaxRange(6f)
                .IsAreaAbility()
                .IsHostileAbility()
                .RequirementStamina(4)
                .HasActivationTargetingSphere(
                    6f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        0,
                        18,
                        typeof(TerrifiedStatusEffect),
                        CombatImpactAreaShape.Sphere,
                        0f,
                        6f,
                        centerOnActivator: true,
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Mind,
                        targetVisualEffect: VisualEffect.Vfx_Fnf_Howl_Mind,
                        enmityBonus: 8,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
