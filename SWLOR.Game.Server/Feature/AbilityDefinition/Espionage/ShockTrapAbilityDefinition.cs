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
    public class ShockTrapAbilityDefinition : IAbilityListDefinition
    {
        private const int StatusDurationSeconds = 30;
        private const int BaseDamage = 22;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder
                .Create(FeatType.ShockTrap, PerkType.ShockTrap)
                .Name("Shock Trap")
                .Level(1)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasRecastDelay(RecastGroup.ShockTrap, 18f)
                .SkillType(SkillType.Espionage)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .IsAreaAbility()
                .HasTargetingSphere(Spell.ShockTrap, 3f, AbilityTargetingFlags.HarmsEnemies)
                .HasImpactAction(PlaceShockTrap)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);

            return builder.Build();
        }

        private static void PlaceShockTrap(uint activator, uint target, int level, Location targetLocation)
        {
            var location = AbilityTargeting.ResolveImpactLocation(activator, target, targetLocation);
            Traps.TryPlaceTrap(
                activator,
                location,
                BaseDamage,
                CombatDamageType.Electrical,
                typeof(ShockStatusEffect),
                StatusDurationSeconds,
                VisualEffect.Vfx_Imp_Lightning_S,
                VisualEffect.Vfx_Dur_Aura_Pulse_Blue_White);
        }
    }
}
