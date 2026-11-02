using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    /// <summary>
    /// Force Sensitive Espionage capstone. Out of combat, the caster and party members within 5m
    /// enter stealth for up to 30 seconds and then move freely. Members need no Stealth perk: they
    /// use their own Stealth rating plus the caster's Stealth perk bonus and pay no upkeep. The
    /// caster's own stealth goes through the normal Stealth perk path.
    /// </summary>
    public class SharedPresenceAbilityDefinition : IAbilityListDefinition
    {
        public const float Radius = 5f;
        public const float StealthWindowSeconds = 30f;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder
                .Create(FeatType.SharedPresence, PerkType.SharedPresence)
                .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_GhostProtocol)
                .Name("Shared Presence")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesImmediateAuthoredAnimation()
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                .SkillType(SkillType.Espionage)
                .IsForcePower()
                .PlaysSoundOnImpact("ksfx_frc_mind")
                .HasCustomValidation((activator, _, _, _) => ValidateOutOfCombat(GetIsInCombat(activator)))
                .HasImpactAction((activator, _, _, _) => ApplySharedPresence(activator))
                .IsCastedAbility()
                .PreservesStealthDuringActivation()
                // The nearby "readies" broadcast would reveal a hidden caster to other players.
                .HideActivationMessage()
                .RequirementFP(CapstoneAbility.ForceCost);

            return builder.Build();
        }

        public static string ValidateOutOfCombat(bool isInCombat)
        {
            return isInCombat
                ? "Shared Presence can only be used out of combat."
                : string.Empty;
        }

        private static void ApplySharedPresence(uint activator)
        {
            var casterBonus = Stat.GetStatAdjustment(activator, StatType.ActiveStealthBonus);

            HideCaster(activator);

            foreach (var member in Party.GetAllPartyMembersWithinRange(activator, Radius))
            {
                if (member == activator)
                    continue;

                if (!Stealth.TryGrantStealth(member, casterBonus, StealthWindowSeconds))
                    continue;

                // Each member sees only their own smoke; it would mark them for everyone else.
                Ability.ShowVisualEffectToPlayer(member, member, VisualEffect.Vfx_Dur_Ghost_Smoke);
                SendMessageToPC(member, $"{PlayerName.GetDisplayName(member, activator)}'s Shared Presence hides you for up to {(int)StealthWindowSeconds} seconds.");
            }

            Ability.ShowVisualEffectToPlayer(activator, activator, Ability.GetSuccessfulImpactVisualEffect(activator));
        }

        /// <summary>
        /// The caster enters stealth through the normal Stealth perk path for up to 30 seconds.
        /// A caster already in stealth keeps that stealth without a time limit.
        /// </summary>
        private static void HideCaster(uint activator)
        {
            if (GetActionMode(activator, ActionMode.Stealth))
                return;

            Stealth.ScheduleTimedExit(activator, StealthWindowSeconds);
            AssignCommand(activator, () =>
            {
                SetActionMode(activator, ActionMode.Stealth, true);
            });
            Ability.ShowVisualEffectToPlayer(activator, activator, VisualEffect.Vfx_Dur_Ghost_Smoke);
        }
    }
}
