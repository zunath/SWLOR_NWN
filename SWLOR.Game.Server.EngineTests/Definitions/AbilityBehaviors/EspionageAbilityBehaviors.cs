using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors
{
    public class EspionageAbilityBehaviors : IAbilityBehaviorSource
    {
        [EngineTest("Espionage ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new EspionageAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // GhostProtocolAbilityDefinition - capstone self buff. Grants an enmity reset and
                // combat-entry stealth via TemporaryStatModifier/ActionMode; the stealth ActionMode
                // toggle isn't a tracked StatusEffect type, but the TemporaryStatModifier bonuses are
                // observable stat adjustments and are asserted.
                new()
                {
                    Feat = FeatType.GhostProtocol,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatAdjustments = new() { [StatType.BackAttackCriticalRatePercentAdjustment] = 100, [StatType.BackAttackExposedPercent] = 20, [StatType.BackAttackExposedDurationSeconds] = 30 },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants three TemporaryStatModifier bonuses (PrimedBackAttackCriticalRate=100, PrimedBackAttackExposedPercent=20, PrimedBackAttackExposedDurationSeconds=30) and toggles stealth ActionMode; the stat adjustments are observable and asserted, the ActionMode toggle is not a tracked StatusEffect type.",
                },

                // RazorTrapAbilityDefinition - hostile trap placed at the resolved target location
                // (the hostile creature's own position). Arms after a 3s delay
                // (Traps.ArmingDelaySeconds) and immediately finds the stationary target within its
                // 2m trigger radius, dealing unconditional damage + Bleed - well inside the 20s wait.
                new()
                {
                    Feat = FeatType.RazorTrap1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Trap is placed at the target's own location (ResolveImpactLocation), so it triggers on the stationary target ~3s after activation.",
                },
                new()
                {
                    Feat = FeatType.RazorTrap2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(BleedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ShadowStepAbilityDefinition - hostile gap-closer; grants a TemporaryStatModifier
                // evasion buff (not a tracked status effect), no damage/status. The evasion amount is
                // a fixed per-feat constant (not gated on any target/activator stat), so it is a
                // deterministic, observable stat adjustment for a bare NPC and is asserted.
                new()
                {
                    Feat = FeatType.ShadowStep1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatAdjustments = new() { [StatType.EvasionPercentAdjustment] = 10 },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Evasion buff is a TemporaryStatModifier (fixed evasionPercent=10 for this tier), not a tracked status effect, but is asserted via ExpectedActivatorStatAdjustments.",
                },
                new()
                {
                    Feat = FeatType.ShadowStep2,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedActivatorStatAdjustments = new() { [StatType.EvasionPercentAdjustment] = 15 },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ShockTrapAbilityDefinition - same trap-trigger pattern as RazorTrap.
                new()
                {
                    Feat = FeatType.ShockTrap,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectedTargetStatusEffects = new[] { typeof(ShockStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Trap is placed at the target's own location, triggering on the stationary target ~3s after activation.",
                },

                // Stealth1-4 were removed as registered abilities when stealth moved to NWN's
                // native Stealth action (#2134); the coverage ratchet no longer requires them.

                // TacticalEscapeAbilityDefinition - self buff; evasion is a TemporaryStatModifier,
                // not a tracked status effect.
                new()
                {
                    Feat = FeatType.TacticalEscape1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatAdjustments = new() { [StatType.EvasionPercentAdjustment] = 8 },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Asserts the +8 temporary evasion modifier; enmity reduction remains private state.",
                },
                new()
                {
                    Feat = FeatType.TacticalEscape2,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatAdjustments = new() { [StatType.EvasionPercentAdjustment] = 12 },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
