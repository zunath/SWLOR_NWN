using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
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
                // combat-entry stealth via TemporaryStatModifier/ActionMode, neither of which is a
                // tracked StatusEffect type.
                new()
                {
                    Feat = FeatType.GhostProtocol,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Grants TemporaryStatModifier bonuses and toggles stealth ActionMode; neither is a tracked StatusEffect type, so only cost/recast are asserted.",
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
                // evasion buff (not a tracked status effect), no damage/status.
                new()
                {
                    Feat = FeatType.ShadowStep1,
                    Target = AbilityTargetKind.HostileCreature,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Evasion buff is a TemporaryStatModifier, not a tracked status effect.",
                },
                new()
                {
                    Feat = FeatType.ShadowStep2,
                    Target = AbilityTargetKind.HostileCreature,
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
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Evasion buff is a TemporaryStatModifier, not a tracked status effect; enmity reduction is likewise not observable via these fields.",
                },
                new()
                {
                    Feat = FeatType.TacticalEscape2,
                    Target = AbilityTargetKind.Self,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
