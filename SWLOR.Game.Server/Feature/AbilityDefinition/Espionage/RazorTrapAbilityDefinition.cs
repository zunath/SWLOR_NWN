using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    public class RazorTrapAbilityDefinition : IAbilityListDefinition
    {
        private const int StatusDurationSeconds = 30;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            RazorTrap(builder, FeatType.RazorTrap1, Spell.RazorTrap1, "Razor Trap I", 1, 5, 14);
            RazorTrap(builder, FeatType.RazorTrap2, Spell.RazorTrap2, "Razor Trap II", 2, 7, 30);

            return builder.Build();
        }

        private static void RazorTrap(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level,
            int stamina,
            int baseDamage)
        {
            builder
                .Create(feat, PerkType.RazorTrap)
                .Name(name)
                .Level(level)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasRecastDelay(RecastGroup.RazorTrap, 12f)
                .SkillType(SkillType.Espionage)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .IsAreaAbility()
                .HasTargetingSphere(spell, 3f, AbilityTargetingFlags.HarmsEnemies)
                .HasImpactAction((activator, target, _, targetLocation) =>
                    PlaceRazorTrap(activator, target, targetLocation, baseDamage))
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void PlaceRazorTrap(uint activator, uint target, Location targetLocation, int baseDamage)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            Traps.TryPlaceTrap(
                activator,
                location,
                baseDamage,
                CombatDamageType.Physical,
                typeof(BleedStatusEffect),
                StatusDurationSeconds,
                VisualEffect.Vfx_Com_Blood_Spark_Medium,
                VisualEffect.Vfx_Dur_Aura_Pulse_Orange_White);
        }
    }
}
