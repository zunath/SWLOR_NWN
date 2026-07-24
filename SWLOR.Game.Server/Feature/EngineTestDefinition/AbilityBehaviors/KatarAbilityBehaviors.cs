using System.Collections.Generic;
using System.Threading.Tasks;
using SWLOR.Game.Server.Service.EngineTestService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.EngineTestDefinition.AbilityBehaviors
{
    public class KatarAbilityBehaviors : IAbilityBehaviorSource
    {
        // Module blueprint (Module/uti/t_katar.uti.json) whose BaseItem is 310 (BaseItem.Katar).
        // No stock NWN blueprint uses this custom base item type.
        private const string KatarResref = "t_katar";

        [EngineTest("Katar ability behaviors", Category = "AbilityBehavior", TimeoutSeconds = 1800f)]
        public static async Task Run(EngineTestContext ctx)
        {
            await AbilityBehaviorExecutor.RunAsync(ctx, new KatarAbilityBehaviors().BuildCases());
        }

        public List<AbilityBehaviorCase> BuildCases()
        {
            return new List<AbilityBehaviorCase>
            {
                // AdamantineGuardAbilityDefinition - capstone self status (not hostile/friendly).
                new()
                {
                    Feat = FeatType.AdamantineGuard1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(AdamantineGuardStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // GuardCounterAbilityDefinition - IsQueuedWeaponAbility: weapon-queued ability.
                new()
                {
                    Feat = FeatType.GuardCounter1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Bonus damage after a recent Guarded hit is conditional; not asserted (weapon-queued anyway).",
                },
                new()
                {
                    Feat = FeatType.GuardCounter2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.GuardCounter3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // HookingStrikeAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.HookingStrike1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.HookingStrike2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.HookingStrike3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.HookingStrike4,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // InterruptingSweepAbilityDefinition - Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.InterruptingSweep1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                    Notes = "Clears the target's action queue on hit; not independently assertable here.",
                },
                new()
                {
                    Feat = FeatType.InterruptingSweep2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // IronWallStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.IronWallStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(IronWallStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // JointLockAbilityDefinition - Casted single-target hostile strike.
                new()
                {
                    Feat = FeatType.JointLock1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.JointLock2,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
                new()
                {
                    Feat = FeatType.JointLock3,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ScrapheapLockdownAbilityDefinition - capstone Casted sphere AoE centered on self.
                new()
                {
                    Feat = FeatType.ScrapheapLockdown1,
                    Target = AbilityTargetKind.HostileCreature,
                    EquipMainHandResref = KatarResref,
                    ExpectedTargetStatusEffects = new[] { typeof(HamstringStatusEffect), typeof(DazedStatusEffect) },
                    ExpectsTargetDamage = true,
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },

                // ScrapperStanceAbilityDefinition - self stance path.
                new()
                {
                    Feat = FeatType.ScrapperStance1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(ScrapperStanceStatusEffect) },
                    ExpectsRecast = true,
                },

                // SteelShoulderAbilityDefinition (FeatType.TwinGuardStance1) - isFriendlyTarget:
                // cast on a spawned same-faction ally.
                new()
                {
                    Feat = FeatType.TwinGuardStance1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetJoinsCasterParty = true,
                    ExpectedTargetStatusEffects = new[] { typeof(GuardedStatusEffect) },
                    Notes = "Applies Guarded to a spawned same-faction ally.",
                },

                // TagInAbilityDefinition (FeatType.TwinIntercept1) - isFriendlyTarget AND
                // RequiresGuardedTarget: the fixture pre-applies Guarded to the spawned ally.
                new()
                {
                    Feat = FeatType.TwinIntercept1,
                    Target = AbilityTargetKind.FriendlyCreature,
                    TargetJoinsCasterParty = true,
                    TargetSetupStatusEffectFactory = () => new GuardedStatusEffect(50, 5.0f),
                    Notes = "Requires an already-Guarded ally; the fixture pre-applies GuardedStatusEffect to the spawned ally.",
                },

                // WhirlingGuardAbilityDefinition - self status path (not hostile/friendly, statusEffect set).
                new()
                {
                    Feat = FeatType.WhirlingGuard1,
                    Target = AbilityTargetKind.Self,
                    ExpectedActivatorStatusEffects = new[] { typeof(WhirlingGuardStatusEffect) },
                    ExpectsSTMCost = true,
                    ExpectsRecast = true,
                },
            };
        }
    }
}
