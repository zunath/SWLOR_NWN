using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    public class GhostProtocolAbilityDefinition : IAbilityListDefinition
    {
        private const string PrimedCriticalModifierGroup = "GHOST_PROTOCOL_BACK_ATTACK";
        private const int EnmityReductionPercent = 80;
        private const float StealthWindowSeconds = 30f;
        private const int PrimedBackAttackCriticalRate = 100;
        private const int PrimedBackAttackExposedPercent = 20;
        private const int PrimedBackAttackExposedDurationSeconds = 30;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            builder
                .Create(FeatType.GhostProtocol, PerkType.GhostProtocol)
                .Name("Ghost Protocol")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.Capstone, 90f)
                .SkillType(SkillType.Espionage)
                .HasImpactAction(ApplyGhostProtocol)
                .IsCastedAbility()
                .RequirementStamina(15);

            return builder.Build();
        }

        private static void ApplyGhostProtocol(uint activator, uint target, int level, Location targetLocation)
        {
            Enmity.ReduceEnmityOnAll(activator, EnmityReductionPercent);

            // The capstone is the only stealth entry allowed while in combat; open the entry
            // window just long enough for the mode change to pass the gate.
            SetLocalInt(activator, Stealth.CombatEntryWindowVariable, 1);
            AssignCommand(activator, () =>
            {
                SetActionMode(activator, ActionMode.Stealth, true);
            });
            DelayCommand(1f, () => DeleteLocalInt(activator, Stealth.CombatEntryWindowVariable));

            // The stealth window closes on its own if nothing else has broken it first.
            DelayCommand(StealthWindowSeconds, () =>
            {
                if (GetIsObjectValid(activator) && GetActionMode(activator, ActionMode.Stealth))
                {
                    AssignCommand(activator, () =>
                    {
                        SetActionMode(activator, ActionMode.Stealth, false);
                    });
                }
            });

            TemporaryStatModifier.Replace(
                activator,
                StatType.BackAttackCriticalRatePercentAdjustment,
                PrimedBackAttackCriticalRate,
                StealthWindowSeconds,
                PrimedCriticalModifierGroup);

            // The primed back attack also inflicts Exposed; the combat damage stage consumes both
            // halves of this primer on the landed hit.
            TemporaryStatModifier.Replace(
                activator,
                StatType.BackAttackExposedPercent,
                PrimedBackAttackExposedPercent,
                StealthWindowSeconds,
                PrimedCriticalModifierGroup);
            TemporaryStatModifier.Replace(
                activator,
                StatType.BackAttackExposedDurationSeconds,
                PrimedBackAttackExposedDurationSeconds,
                StealthWindowSeconds,
                PrimedCriticalModifierGroup);

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Dur_Ghost_Smoke), activator);
        }
    }
}
