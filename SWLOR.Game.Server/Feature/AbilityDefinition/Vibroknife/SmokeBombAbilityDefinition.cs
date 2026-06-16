using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class SmokeBombAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SmokeBomb(builder);

            return builder.Build();
        }

        private static void SmokeBomb(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SmokeBomb1, PerkType.SmokeBomb)
                .Name("Smoke Bomb I")
                .Level(1)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.ThrowGrenade)
                .HasRecastDelay(RecastGroup.SmokeBomb, 30f)
                .IsAreaAbility()
                .HasImpactAction(SmokeBombImpactAction)
                .HasTargetingSphere(
                    Spell.SmokeBomb1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void SmokeBombImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroknife,
                0,
                12,
                typeof(SmokeBombStatusEffect),
                CombatImpactAreaShape.Sphere,
                0.25f,
                5f,
                statusResistanceType: ResistanceType.Trauma,
                targetVisualEffect: VisualEffect.Vfx_Fnf_Smoke_Puff,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Smoke_Puff);
        }
    }
}
