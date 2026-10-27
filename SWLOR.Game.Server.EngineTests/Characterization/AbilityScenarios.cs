using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Characterization.Support;
using SWLOR.Game.Server.EngineTests.Definitions.AbilityBehaviors;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Characterization;

/// <summary>
/// Output locking for abilities: a representative set of abilities from every skill, each activated through the real
/// <c>UsePerkFeat.TryUseAbility</c> pipeline between fixed creatures with the fixtures of its ability behavior case
/// (weapon, perk levels, wounds, pre-applied effects), under a frozen clock with the shared Random seeded for every
/// ability. Each row records whether the ability was accepted, what it cost, the damage or healing it dealt, the
/// status effects it applied with their remaining durations and whether it queued for the next hit.
/// </summary>
public static class AbilityScenarios
{
    private const int Seed = 20261031;

    [EngineTest("Characterization: abilities - Force", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Force(EngineTestContext ctx) =>
        Run(ctx, "Force", new ForceAbilityBehaviors(), FeatType.ForceLightning1, FeatType.ForceChoke1, FeatType.ForceDrain1, FeatType.Renewal1, FeatType.FuryStance1);

    [EngineTest("Characterization: abilities - FirstAid", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task FirstAid(EngineTestContext ctx) =>
        Run(ctx, "FirstAid", new FirstAidAbilityBehaviors(), FeatType.MedKit1, FeatType.AdrenalStim1, FeatType.KoltoMist1);

    [EngineTest("Characterization: abilities - Leadership", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Leadership(EngineTestContext ctx) =>
        Run(ctx, "Leadership", new LeadershipAbilityBehaviors(), FeatType.BreakMorale1, FeatType.PressTheAttack1);

    [EngineTest("Characterization: abilities - Espionage", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Espionage(EngineTestContext ctx) =>
        Run(ctx, "Espionage", new EspionageAbilityBehaviors(), FeatType.RazorTrap1, FeatType.TacticalEscape1);

    [EngineTest("Characterization: abilities - Armor", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Armor(EngineTestContext ctx) =>
        Run(ctx, "Armor", new ArmorAbilityBehaviors(), FeatType.Provoke1);

    [EngineTest("Characterization: abilities - CombatAnalyzer", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task CombatAnalyzer(EngineTestContext ctx) =>
        Run(ctx, "CombatAnalyzer", new CombatAnalyzerAbilityBehaviors(), FeatType.Overload);

    [EngineTest("Characterization: abilities - Devices", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Devices(EngineTestContext ctx) =>
        Run(ctx, "Devices", new DevicesAbilityBehaviors(), FeatType.FragGrenade1, FeatType.ArcProjector1, FeatType.DeflectorShield1, FeatType.Flamethrower1);

    [EngineTest("Characterization: abilities - Vibroblade", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Vibroblade(EngineTestContext ctx) =>
        Run(ctx, "Vibroblade", new VibrobladeAbilityBehaviors(), FeatType.CoveringStrike1, FeatType.BerserkerStance1, FeatType.RiotBlade1);

    [EngineTest("Characterization: abilities - HeavyVibroblade", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task HeavyVibroblade(EngineTestContext ctx) =>
        Run(ctx, "HeavyVibroblade", new HeavyVibrobladeAbilityBehaviors(), FeatType.Earthshatter1, FeatType.BastionStance1, FeatType.SoulStrike1);

    [EngineTest("Characterization: abilities - TwinBlade", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task TwinBlade(EngineTestContext ctx) =>
        Run(ctx, "TwinBlade", new TwinBladeAbilityBehaviors(), FeatType.BladeVortex1, FeatType.CycloneStance1, FeatType.CrossCut1);

    [EngineTest("Characterization: abilities - Vibroknife", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Vibroknife(EngineTestContext ctx) =>
        Run(ctx, "Vibroknife", new VibroknifeAbilityBehaviors(), FeatType.Backstab1, FeatType.AssassinsStance1, FeatType.PathogenStrike1);

    [EngineTest("Characterization: abilities - Katar", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Katar(EngineTestContext ctx) =>
        Run(ctx, "Katar", new KatarAbilityBehaviors(), FeatType.HookingStrike1, FeatType.GuardCounter1, FeatType.ScrapperStance1);

    [EngineTest("Characterization: abilities - Spear", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Spear(EngineTestContext ctx) =>
        Run(ctx, "Spear", new SpearAbilityBehaviors(), FeatType.DisablingStrike1, FeatType.VigorThrust1, FeatType.VigorStance1);

    [EngineTest("Characterization: abilities - Staff", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Staff(EngineTestContext ctx) =>
        Run(ctx, "Staff", new StaffAbilityBehaviors(), FeatType.LegSweep1, FeatType.Slam1, FeatType.CrusherStance1);

    [EngineTest("Characterization: abilities - Lightsaber", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Lightsaber(EngineTestContext ctx) =>
        Run(ctx, "Lightsaber", new LightsaberAbilityBehaviors(), FeatType.ShatteringStrike1, FeatType.ImbuementStance1, FeatType.SunderingSweep1);

    [EngineTest("Characterization: abilities - Saberstaff", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Saberstaff(EngineTestContext ctx) =>
        Run(ctx, "Saberstaff", new SaberstaffAbilityBehaviors(), FeatType.CircleSlash1, FeatType.DoubleStrike1, FeatType.ConduitStance1);

    [EngineTest("Characterization: abilities - Rifle", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Rifle(EngineTestContext ctx) =>
        Run(ctx, "Rifle", new RifleAbilityBehaviors(), FeatType.AimedShot1, FeatType.CripplingShot1, FeatType.SniperStance1);

    [EngineTest("Characterization: abilities - Pistol", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Pistol(EngineTestContext ctx) =>
        Run(ctx, "Pistol", new PistolAbilityBehaviors(), FeatType.DoubleShot1, FeatType.DisarmingShot1, FeatType.GamblerStance1);

    [EngineTest("Characterization: abilities - Throwing", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Throwing(EngineTestContext ctx) =>
        Run(ctx, "Throwing", new ThrowingAbilityBehaviors(), FeatType.ExplosiveToss1, FeatType.ConcussiveToss1, FeatType.FlurryStance1);

    [EngineTest("Characterization: abilities - Unarmed", Category = "Characterization", TimeoutSeconds = 300f)]
    public static Task Unarmed(EngineTestContext ctx) =>
        Run(ctx, "Unarmed", new UnarmedAbilityBehaviors(), FeatType.Jab1, FeatType.PalmStrike1, FeatType.FlowStance1);

    private static async Task Run(EngineTestContext ctx, string skill, IAbilityBehaviorSource source, params FeatType[] feats)
    {
        using var clock = new FrozenClock();
        using var arena = await ScenarioArena.CreateAsync(ctx);
        var scenario = arena.Context;
        Combat.SetAutoAttackHitResolutionOverride(null);
        Combat.SetAbilityHitResolutionOverride(null);

        var cases = source.BuildCases();
        var table = new SnapshotTable(
            "UsePerkFeat.TryUseAbility",
            $"Activates representative {skill} abilities through the real activation pipeline and records the outcome of each.",
            $"Each ability is used by a fresh {ScenarioActors.Humanoid} (2000 HP, 9999 FP and stamina, no regeneration) on the target " +
            "its behavior case names: itself, a hostile training dummy or a friendly creature one and a half metres away. The clock is frozen " +
            $"at {FrozenClock.Start:yyyy-MM-dd HH:mm:ss} UTC, so effect durations are the full applied durations. Random is seeded with {Seed} plus the " +
            "ability's position immediately before the activation. HP columns are the change from before the activation; " +
            "effects are written 'name:remainingSeconds'.",
            AbilityRun.InputColumns,
            AbilityRun.OutputColumns,
            Seed) { AllowConstantOutputs = true };

        for (var index = 0; index < feats.Length; index++)
        {
            var behaviorCase = cases.Single(candidate => candidate.Feat == feats[index]);
            table.Add(await AbilityRun.RunAsync(scenario, behaviorCase, Seed + index));
        }

        ScenarioSnapshot.Verify("Ability", skill, table);
    }
}
