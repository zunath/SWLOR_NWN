# SWLOR.Game.Server In-Engine Integration Testing

This document describes the in-engine integration test system: how it works, the full
`EngineTestContext` API, how to write a new test, and how to run the suite locally and in CI.

## What It Is and Why

`SWLOR.Game.Server.Tests` (the NUnit suite) covers pure C# logic and data - calculations, builder
wiring, Bible/2DA consistency - but it never touches the NWN engine. It cannot spawn a creature,
run a real attack roll through the native combat hooks, or drive a perk through the actual feat
activation pipeline.

The in-engine test system closes that gap. When the server boots with
`SWLOR_ENGINE_TESTS_ENABLED=true`, every method tagged `[EngineTest]` is discovered by reflection
and executed inside the live NWN server, after the module has finished loading. Tests run against
real spawned creatures in an instanced arena and exercise the real systems directly:

- **Real feat activation** - `UsePerkFeat.TryUseAbility` is called exactly as it would be from a
  player's feat use, including activation delays, FP/Stamina costs, and impact effects.
- **Real status-effect ticks** - effects are applied through `StatusEffect.ApplyStatusEffect` and
  observed on the shared status-effect tick interval, not simulated.
- **Real native combat hooks** - `ActionAttack` drives the actual attack-roll/damage-roll pipeline
  through NWNX, and results are read back off the target's real hit points.

This is the intended venue for integration-testing new combat-upgrade perks and abilities
end-to-end, in addition to their NUnit coverage.

## Project Layout and Isolation

All engine-test code lives in its own project, **`SWLOR.Game.Server.EngineTests`**, which
references the game project - never the other way round. Nothing in `SWLOR.Game.Server` references
a test type.

```
SWLOR.Game.Server.EngineTests/
  EngineTest.cs            the runner
  Framework/               EngineTestContext, [EngineTest], result/report/exception types
  Definitions/             hand-written suites
    AbilityBehaviors/      per-tree ability behavior cases
    PerkCoverage/          per-perk coverage cases
```

**Test code cannot reach production.** The production deploy stages only the game project's
output, which does not contain `SWLOR.Game.Server.EngineTests.dll` (the test project deliberately
omits the game project's post-build deploy target). The engine-test runner scripts stage the
*test* project's output instead - it carries the game assembly along as a project reference, so
that one directory holds everything the server needs.

Because nothing references it, the test assembly is never loaded implicitly. `ServerBootstrapper`
loads it explicitly - only when `SWLOR_ENGINE_TESTS_ENABLED` is set, and only if the file exists
(so it is a silent no-op in production) - just before `LoadScripts()`, so the script registry's
assembly scan finds its `[NWNEventHandler]` methods. It is loaded into the game assembly's own
`AssemblyLoadContext`; a plain `Assembly.LoadFrom` would land in the default context and hand the
test code a second, incompatible copy of every game type it references.

What *does* live in game code is a small set of public observability/determinism seams the tests
consume: `Combat.SetAbilityHitResolutionOverride` / `SetAutoAttackHitResolutionOverride`,
`Ability.GetLastCompletedAbilityImpactSummary`, `Ability.GetLastActivationDenialReason`, and
`Stat.SuppressNaturalRegenVariable`.

## Architecture Overview

The runner lives in `SWLOR.Game.Server.EngineTests/EngineTest.cs`, with its supporting types in
`SWLOR.Game.Server.EngineTests/Framework/`.

1. **Trigger** - `EngineTest.ScheduleEngineTests` is an `[NWNEventHandler(ScriptName.OnModuleLoad)]`
   handler. It checks `ApplicationSettings.EngineTestsEnabled`; if false, it does nothing. If the
   server's `ServerEnvironment` is `Production`, it logs an error and refuses to run even if
   enabled. The environment must also be EXPLICIT (`ServerEnvironmentIsExplicit`): an unset or
   mistyped `SWLOR_ENVIRONMENT` still resolves to `Development`, but the runner fails closed on
   it - a typo'd production value must not silently become a test-eligible environment.
   Otherwise it schedules `RunAllTestsAsync` via `DelayCommand` after
   `EngineTestStartupDelaySeconds` (default 15s), giving the module time to finish booting. A
   static `_hasRun` flag guards against scheduling twice on repeated `OnModuleLoad` events.

2. **Discovery** - `DiscoverTests` reflects over every loaded assembly's public static methods,
   collects the ones carrying `[EngineTest]`, and orders them by `Category` then `Name`. If
   `SWLOR_ENGINE_TEST_FILTER` is set, only tests whose `Name` or `Category` contains the filter
   text (case-insensitive) survive.

3. **Arena resolution** - `ResolveArena` takes the module's starting location/area, resolves an
   arena resref (the `SWLOR_ENGINE_TEST_ARENA_RESREF` override if set, otherwise the starting
   area's own resref), and calls `CreateArea` to instance a fresh copy of it. If instancing fails,
   it logs a warning and falls back to running directly in the actual module starting area. The
   spawn `Location` (the anchor `EngineTestContext.GetArenaLocation` offsets from) is the module's
   starting position when the arena is a starting-area copy, or the area's geometric center when
   an override resref is used - the module entry coordinates aren't meaningful in an unrelated
   area, so pick a small, flat area when overriding. After arena creation the runner yields for
   one second so the instanced area's initialization scripts (which only run once the creating
   script finishes) settle before the first test acts inside it.

4. **Per-test execution** - `RunSingleTestAsync` builds a fresh `EngineTestContext` for each test,
   validates the method signature, invokes it via reflection, and races it against a timeout task
   (`attribute.TimeoutSeconds`, default 60s) using `NwTask.WhenAny`. `EngineTestSkippedException`
   maps to `Skipped`, `EngineTestAssertionException` maps to `Failed`, any other exception is
   unwrapped from `TargetInvocationException` and also maps to `Failed` (and is logged to
   `LogGroup.EngineTest`). A timed-out test is marked `Failed` with a "Timed out after Xs" message
   and is cooperatively cancelled via the context's `CancellationToken`; the runner grace-waits up
   to 10s for it to unwind before cleanup. If the test still hasn't stopped after the grace
   period, the **remainder of the suite is aborted** - every not-yet-run test is reported as
   `Skipped` with an explanatory message - because a task still acting on the shared arena would
   make later results untrustworthy. `context.Cleanup()` always runs in a `finally` block,
   destroying every object/area the test tracked (and restoring the shared RNG if the test
   seeded it), and the runner waits one frame (`NwTask.NextFrame()`) between tests so destroyed
   objects are fully gone before the next test starts.

5. **Report + console markers** - Every test line is echoed to the console prefixed
   `[ENGINE_TESTS]` as `PASS`/`FAIL`/`SKIP {Name} ({duration}ms) - {message}`, so CI log scraping
   doesn't need the JSON. After the run, `EngineTest.WriteReport` serializes an `EngineTestReport`
   (`StartedUtc`, `FinishedUtc`, `Total`, `Passed`, `Failed`, `Skipped`, `Results`) to
   `engine-test-results.json` under `EngineTestResultsDirectory` (or `./engine_tests/` if that
   settings value is unset), followed by a `[ENGINE_TESTS] SUMMARY total=... passed=... failed=...
   skipped=...` line and a final `[ENGINE_TESTS] COMPLETE` marker.

6. **Auto-shutdown** - If `EngineTestShutdownOnCompletion` is true (the default), the server calls
   `AdministrationPlugin.ShutdownServer` three seconds after the summary is printed, so a headless
   run (Docker/CI) terminates on its own once results are captured.

## Environment Variable Reference

All values are read once into `ApplicationSettings` (`SWLOR.Game.Server/ApplicationSettings.cs`).

| Variable | Settings property | Default | Meaning |
|---|---|---|---|
| `SWLOR_ENGINE_TESTS_ENABLED` | `EngineTestsEnabled` | `false` | Master switch. When false, `ScheduleEngineTests` does nothing on module load. |
| `SWLOR_ENGINE_TEST_RESULTS_DIRECTORY` | `EngineTestResultsDirectory` | `{SWLOR_APP_LOG_DIRECTORY}engine_tests/` if `SWLOR_APP_LOG_DIRECTORY` is set, otherwise `null` (the runner then writes to `./engine_tests/`) | Directory the JSON report is written to. |
| `SWLOR_ENGINE_TEST_FILTER` | `EngineTestFilter` | `null` (no filter, all tests run) | Case-insensitive substring matched against a test's `Name` or `Category`. |
| `SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS` | `EngineTestStartupDelaySeconds` | `15` (float) | Seconds after `OnModuleLoad` before the test run starts. |
| `SWLOR_ENGINE_TEST_SHUTDOWN` | `EngineTestShutdownOnCompletion` | `true` | Whether the server shuts itself down ~3s after the run completes. Set to `false` to keep the server up for debugging. |
| `SWLOR_ENGINE_TEST_ARENA_RESREF` | `EngineTestArenaResref` | `null`/empty (uses the module's starting area's own resref) | Overrides which area resref is instanced as the test arena. Override arenas anchor spawns at the area's geometric center, so pick a small, flat area. |
| `SWLOR_ENVIRONMENT` | `ServerEnvironment` | `Development` (any unset/unrecognized value) | `"prod"`/`"production"` -> `Production` (engine tests refuse to run); `"test"`/`"testing"` -> `Test`; `"dev"`/`"development"` -> `Development`. Anything else also resolves to `Development` but is NOT explicit (`ServerEnvironmentIsExplicit=false`) and the engine test runner fails closed on it. |

Booleans accept `true`/`1`/`yes` as true and `false`/`0`/`no` as false (case-insensitive); any
other value - including unset and typos - keeps the setting's declared default, so a mistyped
`SWLOR_ENGINE_TEST_SHUTDOWN` can't leave a headless server running forever. Floats fall back to
their default on a parse failure.

## How to Write a New Engine Test

### The attribute contract

```csharp
[EngineTest("Human-readable test name", Category = "MyCategory", TimeoutSeconds = 30f)]
public static async Task MyTest(EngineTestContext ctx) { /* ... */ }
```

- The method must be **public static**.
- It must take exactly **one parameter of type `EngineTestContext`**.
- It must return **`Task`** - and only `Task` (`Task<T>` does not pass the signature check,
  `EngineTest.IsValidTestMethod`). `void` bodies are rejected because the per-test timeout is
  COOPERATIVE: the engine is single-threaded, so a timeout can only preempt a test at an
  `await`, and a synchronous `void` body would run entirely outside its reach. `async void` is
  additionally unobservable: the runner would have no task to await, so it would report a pass
  and clean up while the test was still running.
- **Timeout semantics**: `TimeoutSeconds` bounds a test only at its awaits. The runner yields one
  frame before invoking the body (arming the timeout first), and the context's wait helpers honor
  cancellation on every poll - but a synchronous segment between awaits cannot be interrupted.
  Sweep-style tests must yield periodically inside long loops (`await NwTask.NextFrame()` every
  N items - see `PerkSweepEngineTests`). The NWNX thread watchdog (below) is the hard backstop
  for a segment that never returns.
- A misplaced attribute (private or instance method, wrong parameters) is reported as a **failed**
  test with an invalid-signature message rather than silently skipped.
- `Name` (constructor argument) is required. `Category` defaults to `"General"`. `TimeoutSeconds`
  defaults to `60f`.
- Tests failing the signature check are reported as `Failed` with a descriptive message rather than
  crashing the runner.

### `EngineTestContext` API

Every `[EngineTest]` method receives one of these, scoped to that single test run.

| Member | Description |
|---|---|
| `TestName` | The test's `Name` from its attribute. |
| `Arena` | The instanced arena area (`uint`) this test runs in. |
| `GetArenaLocation(xOffset = 0f, yOffset = 0f, facing = 0f)` | Builds a `Location` in the arena, offset from the arena's known-walkable spawn point. |
| `SpawnCreature(resref, xOffset = 0f, yOffset = 0f)` | Creates a creature blueprint at an arena location, asserts it's valid, normalizes it to the standard Defender faction (blueprint factions vary - stock `nw_rat001` ships as Hostile), and tracks it for automatic cleanup. Returns the creature. |
| `Track(uint obj)` | Registers any object for automatic destruction when the test finishes. |
| `CreateInstancedArea(areaResref)` | Creates an additional instanced area copy, asserts it's valid, and tracks it for cleanup. Returns the area. |
| `SetNPCPerkLevel(npc, PerkType, level)` | Caps an NPC's effective perk level via the `PERK_LEVEL_{id}` local int. NPCs default to a perk's max level when this is unset. |
| `SetNPCResources(npc, fp, stamina)` | Sets an NPC's current FP/Stamina pools (stored as the `FP`/`STAMINA` local ints NPCs use). |
| `MakeHostile(creature)` | Moves a creature to the standard `Hostile` faction so other spawned creatures treat it as an enemy. |
| `SeedRandom(seed)` | Reseeds the shared combat RNG (`Service.Random.SetSeed`) so hit/crit/damage rolls are deterministic for this test. Cleanup restores a fresh time-seeded RNG afterward, so determinism never leaks into later tests or a server kept running for debugging. |
| `Assert(condition, message)` | Throws `EngineTestAssertionException` (-> `Failed`) if `condition` is false. |
| `AssertEqual<T>(expected, actual, label)` | Throws with a formatted `"{label}: expected 'X' but was 'Y'."` message if not equal. |
| `Fail(message)` | Unconditionally throws `EngineTestAssertionException`. |
| `Skip(reason)` | Throws `EngineTestSkippedException` (-> `Skipped`). |
| `WaitUntilAsync(Func<bool> condition, float timeoutSeconds, string description)` | Polls `condition` every 250ms; throws an assertion failure naming `description` if `timeoutSeconds` elapses first. Honors runner cancellation. |
| `DelaySecondsAsync(seconds)` | Awaits a fixed real-time delay (`NwTask.Delay`). Honors runner cancellation. |
| `CancellationToken` | Signaled by the runner when the test exceeds its `TimeoutSeconds`. The wait helpers above honor it automatically; pass it to any direct `NwTask.Delay`/`NwTask.WaitUntil` calls so a timed-out test stops promptly instead of running on underneath the next test. |
| `Log(message)` | Writes to `LogGroup.EngineTest`, prefixed with `[TestName]`. |
| `Cleanup()` | Destroys every tracked object/area. Called automatically by the runner after the test - you normally don't call this yourself. |

### Determinism

Combat rolls go through the shared `Service.Random` instance. Call `ctx.SeedRandom(someFixedSeed)`
at the start of a test that depends on hit/crit/damage outcomes so repeated runs behave the same
way (see `CombatPipelineEngineTests`, which seeds with a fixed integer before commanding an
attack).

### NPC perk level and FP/Stamina setup

NPCs resolve an uncapped perk level to that perk's max rank. Use
`ctx.SetNPCPerkLevel(npc, PerkType.X, level)` to pin it to a specific level for the test. Spend
pools (FP/Stamina) aren't otherwise initialized on a freshly spawned NPC - set them explicitly with
`ctx.SetNPCResources(npc, fp, stamina)` before activating any ability that costs one.

### Faction control

Freshly spawned creatures aren't hostile to each other by default. Call `ctx.MakeHostile(target)`
before commanding an attack so the attacker's `ActionAttack` treats it as a valid enemy.

### Timing guidance

Status effects tick on the shared status-effect interval, roughly every 6 real seconds - not every
frame. When waiting for a status effect to apply, expire, or otherwise react to a tick, give
`WaitUntilAsync` generous margin well past the nominal duration (the shipped suite waits up to 40s
for a 5-second-duration effect to expire). Ability activation delays are typically only a second or
two, but still poll rather than assume synchronous application.

### Worked example

This is `AbilityActivationEngineTests` (`SWLOR.Game.Server.EngineTests/Definitions/AbilityActivationEngineTests.cs`)
in full - it drives a real ability through `UsePerkFeat.TryUseAbility` and verifies both the FP
spend and the resulting status effect. Note that all NWScript functions (`GetLocation`,
`GetIsObjectValid`, `AssignCommand`, ...) are available unqualified inside test methods - the
project globally imports `SWLOR.NWN.API.NWScript.NWScript` (see `GlobalUsings.cs`):

```csharp
public static class AbilityActivationEngineTests
{
    private const int StartingFP = 50;
    private const int StartingStamina = 50;

    [EngineTest("Renewal I activation spends FP and applies its regeneration status effect", Category = "Ability", TimeoutSeconds = 30f)]
    public static async Task RenewalActivationSpendsFPAndAppliesStatusEffect(EngineTestContext ctx)
    {
        var npc = ctx.SpawnCreature("nw_rat001");
        ctx.SetNPCResources(npc, StartingFP, StartingStamina);

        var used = UsePerkFeat.TryUseAbility(npc, npc, FeatType.Renewal1, GetLocation(npc));
        ctx.Assert(used, "TryUseAbility should report success activating Renewal I on its caster.");

        // Renewal I has a 1s activation delay before its impact (and cost deduction) apply;
        // give it generous margin.
        await ctx.WaitUntilAsync(
            () => StatusEffect.HasStatusEffect<RegenerativeHealingStatusEffect>(npc),
            10f,
            "Renewal I's regeneration status effect to appear on the caster after its activation delay");

        var remainingFP = Stat.GetCurrentFP(npc);
        ctx.Assert(remainingFP < StartingFP, $"FP should have decreased below {StartingFP} after casting Renewal I, but is still {remainingFP}.");
    }
}
```

### The ability behavior coverage program

Beyond hand-written suites, per-ability coverage is data-driven. Each ability tree has an
`IAbilityBehaviorSource` in `Definitions/AbilityBehaviors/` declaring one
`AbilityBehaviorCase` per registered `FeatType`: who the ability targets, what weapon (if any) must
be equipped, and which observable outcomes to assert (status effects and stat adjustments on the
activator/target, status removal, healing, revival health, temporary HP, movement, target damage,
FP/Stamina cost, and recast). The shared `AbilityBehaviorExecutor` turns every case into a live
activation through `UsePerkFeat.TryUseAbility`:

- **Casted** abilities assert declared status effects/damage after the activation delay, plus costs
  and recast. Damage assertions also require the caster to be the target's last damager, so a
  placed arena creature engaging the hostile target can't produce a false pass. During the sweep,
  `Combat.SetAbilityHitResolutionOverride(true)` forces ability hit resolution (restored in a
  `finally`) - cases assert what an ability does on a hit, and a legitimate 5% miss chance across
  hundreds of cases would otherwise make every sweep red. Auto-attacks are simultaneously forced
  to miss (`Combat.SetAutoAttackHitResolutionOverride(false)`) so the caster's resumed melee
  swings can't satisfy a damage assertion on a broken ability's behalf. Hostile targets get a
  large temporary-HP buffer so ability damage registers without killing them - deaths would
  route through the corpse/loot pipeline and leave non-destroyable bodies between cases.
  Case-level skips are surfaced in the tree test's result message (and therefore the JSON
  report), not just the log.
- **Weapon** (queued-on-hit) abilities assert queue state (`UsePerkFeat.IsWeaponAbilityQueued`),
  costs (applied at queue time), and recast - then LAND the queued hit: the executor flips the
  auto-attack override to forced hits for just this attack, orders `ActionAttack` at the target
  (spawning a temporary hostile dummy for self-target queued cases), and requires the queue slot
  to be consumed within the wait window (queue expiry alone takes 30s, so consumption proves a
  landed hit exercised the real on-hit impact pipeline). The override is restored to forced
  misses in a `finally` so a timeout can't leak forced hits into subsequent cases. Because the
  ability's damage rides the same landed hit as the weapon swing, damage-declaring queued cases
  additionally require the ability's own completed impact summary to report
  `AttributedDamage > 0` - damage the impact actually queued, not merely a target it visited -
  so a queued ability that loses its damage cannot pass on the swing's HP drop.
- **Stance** abilities assert their stance status effect on the activator.

Cost assertions verify the exact AMOUNT, not just that the pool shrank: the executor derives the
expected FP/STM deduction from the definition's activation requirements (via the same public
adjustment seams the runtime cost path consults) and requires the post-deduction pool to equal
before-minus-expected. This is stable because the fixture suppresses the caster's natural
regeneration (HP, FP, and STM - `Stat.SuppressNaturalRegenVariable`), which also prevents an
out-of-combat regen tick from satisfying a healing assertion on a deliberately wounded caster.
The one exception is abilities whose impact refunds part of their own cost in the same window -
their cases set `ImpactRefundsCosts` naming the rider in Notes, and fall back to requiring a net
dip. **The rule for new cases**: if a casted ability declares a cost and its definition carries a
resource-restoring rider that targets the SAME pool (`RestoreStaminaOnHit`,
`RestoreStaminaIfAnyCriticalHit`, `RestoreSecondaryTargetStamina`, a direct `Stat.RestoreStamina`
in the impact action, and their FP equivalents), the case must set the flag. A rider on the OTHER
pool does not need it - an FP refund cannot mask a stamina deduction. Conditional riders
(crit-gated, all-hits-gated) matter most: they fire only sometimes, so an unflagged case fails
intermittently rather than every run.

Beyond status effects/damage, cases can assert exact activator or target stat adjustments,
removal of pre-applied ailments, healing on either actor, revival plus a minimum post-revival HP
floor, movement to a maximum target distance, and new raw temporary-HP effects. Healing fixtures
are deliberately wounded and have natural regeneration suppressed. Temporary HP compares effect
counts before and after impact, so a hostile target's fixture damage buffer cannot create a false
pass. `SetupNPCPerkLevels` seeds `PERK_LEVEL_` locals on the caster before activation, pinning
perk-investment-gated branches (e.g. Leadership toggle auras read their aura level through
`Perk.GetStatBonus`, which resolves NPC perk levels from those locals with no max-level fallback)
so the asserted branch is deterministic.

When a tree sweep accumulates 25 case failures it aborts and reports the remaining cases as not
run: that volume signals a systemic problem (broken fixture, dead arena, stale build) rather
than 25 independent ability bugs, and running out hundreds of remaining cases would just burn
the suite's wall clock repeating the same failure.

Coverage is enforced by an NUnit ratchet (`SWLOR.Game.Server.Tests/Feature/AbilityBehaviorCoverageTests.cs`):
every feat registered by an `IAbilityListDefinition` must have exactly one behavior case unless its
tree is on the ratchet's explicit not-yet-covered list. **Adding a new ability without adding a
behavior case fails the unit test suite.** Cases that genuinely can't run in-engine yet carry a
`SkipReason` (they count as declared, execute as skipped, and should be burned down over time).
The same ratchet requires each executable case to assert every FP/STM cost declared by the
definition and each executable impact to declare at least one observable gameplay outcome.
Focused waiver fields exist for same-tick resource refunds and effects the harness genuinely
cannot observe (currently enmity-only impacts); generic notes do not bypass either check. Contract
checks also reject vacuous movement thresholds, revival without a dead target, cleanse assertions
without pre-applied ailments, and stale waivers after a real assertion is added.

Batch status:

| Batch | Trees | Status |
|---|---|---|
| 1 | Force, FirstAid, Leadership, Espionage, Armor, CombatAnalyzer, top-level, Vibroblade, HeavyVibroblade, TwinBlade, Vibroknife, Katar, Spear, Staff, Lightsaber, Saberstaff, Rifle, Pistol, Throwing, Devices | LIVE-VALIDATED (2026-07-24 full sweep: green) |
| 2 | NPC, Mimicry, Beastmaster | LIVE-VALIDATED (2026-07-24 full sweep: green) |

The ratchet's not-yet-covered list is now **empty**, and the full suite runs GREEN against a live
server (37/37 tests; 688 cases: 677 passed, 0 failed, 11 skipped). Ally-target abilities are
covered via `AbilityTargetKind.FriendlyCreature` (same-faction spawned ally), with fixture flags
for dead targets (`TargetStartsDead`), party membership (`TargetJoinsCasterParty`, formed through
the real associate-add pipeline), and pre-applied source-tracked status effects
(`TargetSetupStatusEffects` or `TargetSetupStatusEffectFactory`, source reassigned to the caster -
Guarded tracking registers by the instance's source). Use `ExpectedRemovedTargetStatusEffects` to
prove a cleanse actually removes the named setup effects. The 11 remaining skips are the
beast-management flows hard-gated on
`GetIsPC` plus live player DB records; they genuinely require a connected client and each carries
a precise `SkipReason`.

Perk passives are behaviorally verified per level by `PerkLevelBehaviorEngineTests`: for every
perk level declaring stat bonuses, an NPC holding that level must receive exactly the bonus the
built data declares from `Perk.GetStatBonus` (expectations derive from the perk data itself), and
a canary proves `Stat.GetStatAdjustment` consumes the contribution.

To cover a new ability: add one `AbilityBehaviorCase` to its tree's `*AbilityBehaviors.cs` (create
the source class for a brand-new tree and remove the tree from the ratchet's exemption list). Run
the behavior tests alone with `SWLOR_ENGINE_TEST_FILTER=AbilityBehavior` - the full behavior sweep
is minutes long and intended for nightly/CI rather than every local iteration.

### The perk coverage program

Every registered perk (462 across all trees, including Beast) has a `PerkCoverageCase` in
`Definitions/PerkCoverage/` declaring its structure: level count, per-level SP
prices, and granted feats in order. Enforcement is split by where each check can actually run
(PerkBuilder.Build() reads 2DAs, so perks cannot be built in plain NUnit):

- **NUnit ratchet** (`PerkCoverageTests`): source-scans the perk definitions and requires exactly
  one case per registered perk (no dupes, no orphans), verifies every ability's perk reference
  resolves to a registered perk (the static HackingBlade regression guard), and sanity-checks
  case coherence. Runs at merge time.
- **In-engine sweep** (`PerkSweepEngineTests`): verifies every case against the perk actually
  BUILT by its definition (levels/prices/feats must match exactly - an unintended progression
  change fails until the case is deliberately updated), validates stat-bonus StatTypes, and
  exercises NPC `Perk.GetPerkLevel` plus the PERK_LEVEL cap round-trip for all 462 perks and
  all ~600 ability perk references. LIVE-VALIDATED (2026-07-24: 462/462, 0 failures).

Active perks' behavior is covered transitively: their granted feats flow through the ability
behavior cases above. To cover a new perk: add one `PerkCoverageCase` to its tree's
`*PerkCoverage.cs` - the ratchet fails until you do.

New hand-written tests belong in `SWLOR.Game.Server.EngineTests/Definitions/`, one file per suite,
following the same shape: `SpawnCreature`, drive the real system under test, assert on the real
resulting state via `WaitUntilAsync` where a delay or tick is involved. The current suites (see
that directory) also cover harness sanity (`HarnessSanityEngineTests`), ability registration
(`AbilityRegistrationEngineTests`), status effect application/expiration/removal
(`StatusEffectEngineTests`), the native combat pipeline (`CombatPipelineEngineTests`), and NPC perk
level resolution (`PerkEngineTests`) - each is a good reference for a different corner of the API.

## How to Run Locally

`scripts/run-engine-tests.ps1` (Windows) and `scripts/run-engine-tests.sh` (bash) are functionally
identical and are kept in sync. Both run against a **server home** directory - the folder mounted
as `/nwn/home`, holding `modules/`, `hak/`, `tlk/`, `swlor.env` and receiving `dotnet/` and
`app_logs/`. The home resolves in this order: the `-ServerHome`/`--server-home` argument, the
`SWLOR_ENGINE_TEST_SERVER_HOME` env var, the repo's `debugserver/` directory when it exists (the
dev-machine convention used by the normal `SWLOR.Runner` flow), and finally
`SWLOR.Game.Server/Docker/` (the layout the CI workflow stages - that folder is tracked compose
*configuration*, not inherently a runtime home). Both scripts:

1. Build `SWLOR.Game.Server` in `Release` (unless skipped) with `-p:RunPostBuildEvent=Never` (so
   the normal Windows-only CLI post-build deploy step doesn't run), then copy the build output from
   `SWLOR.Game.Server.EngineTests/bin/{Configuration}/net10.0` into `<server home>/dotnet`.
2. Delete any stale `engine-test-results.json` from a previous run.
3. Run `docker compose -p swlor-engine-tests -f <repo>/SWLOR.Game.Server/Docker/docker-compose.enginetests.yml
   up --abort-on-container-exit --exit-code-from swlor-server` from the server home (the dedicated
   project name keeps these containers isolated from the normal dev stack), then tear down.
4. Parse the resulting JSON report, print a table of every test (category, name, outcome, duration,
   message), print the summary line, and exit non-zero unless the server container exited cleanly,
   at least one test ran, and none failed.

**Prerequisites**: the `dotnet` SDK (unless `--skip-build`), `docker compose`, and - for the bash
script only - `jq` (used to parse the JSON report; the run fails at the reporting step without it).

**This assumes the server home already has `modules/`, `hak/`, and `tlk/` populated with the
current module and hak assets** - the normal dev/deploy-machine flow. The script itself only
builds and stages the compiled .NET assembly; it does not pack the module or build the haks.

Usage:

```bash
# PowerShell
./scripts/run-engine-tests.ps1
./scripts/run-engine-tests.ps1 -SkipBuild -Filter Combat
./scripts/run-engine-tests.ps1 -ArenaResref my_test_arena -Configuration Debug

# bash
scripts/run-engine-tests.sh
scripts/run-engine-tests.sh --skip-build --filter Combat
scripts/run-engine-tests.sh --arena-resref my_test_arena --configuration Debug

# explicit server home (otherwise: env var, then debugserver/, then SWLOR.Game.Server/Docker/)
./scripts/run-engine-tests.ps1 -ServerHome D:\nwn\home
scripts/run-engine-tests.sh --server-home /opt/nwn/home
```

`-Filter`/`--filter` and `-ArenaResref`/`--arena-resref` are passed straight through as
`SWLOR_ENGINE_TEST_FILTER` and `SWLOR_ENGINE_TEST_ARENA_RESREF` env vars picked up by
`docker-compose.enginetests.yml`.

`docker-compose.enginetests.yml` itself only starts the two services actually needed to run tests
(`redis`, `swlor-server`) - not the full dev stack in `docker-compose.yml`. Redis uses a `tmpfs`
data directory so every run starts against a fresh, empty database, there are no restart policies
(a container exit is final and observable), and it overrides `SWLOR_ENGINE_TESTS_ENABLED=true`,
`SWLOR_ENVIRONMENT=test`, `SWLOR_ENGINE_TEST_RESULTS_DIRECTORY=/nwn/home/app_logs/engine_tests/`
(pinning the JSON report to where the runner scripts look, regardless of where the server home's
`swlor.env` points `SWLOR_APP_LOG_DIRECTORY`), and `NWNX_METRICS_INFLUXDB_SKIP=y` (no InfluxDB
service exists in this compose file) on top of the normal `swlor.env` defaults.

**Hard wall clock**: both runner scripts enforce a timeout on the containerized run
(`-TimeoutMinutes` / `--timeout-minutes`, default 90; a full sweep takes roughly 45). On expiry
the stack is torn down and the run is reported as failed. This exists because
`docker compose up --abort-on-container-exit` blocks until a container exits, and a server that
boots healthy but schedules no tests - a missing harness assembly, a filter matching nothing -
idles forever without exiting. Such a server is fully responsive, so the thread watchdog below
does NOT catch it; only the wall clock does.

**Blocked-main-loop backstop**: the compose file force-enables the NWNX thread watchdog
(`NWNX_THREADWATCHDOG_SKIP=n`, 30s check period, 20 missed checks) - per-test timeouts are
cooperative (`CancellationToken` polled between frames), so a test or product bug that hard-blocks
the server's main thread would otherwise hang the container until the CI job timeout. The watchdog
kills the process when the main loop stops responding for ~10 minutes, turning a wedged run into
an observable non-zero container exit. It is set in the compose `environment:` block precisely so
a server home whose `swlor.env` disables the watchdog can't silently remove the backstop.

**Keeping the server up for debugging**: export `SWLOR_ENGINE_TEST_SHUTDOWN=false` in your shell
before invoking the script (or before running `docker compose` directly) - the compose file reads
it as `${SWLOR_ENGINE_TEST_SHUTDOWN:-true}`, so the container will stay up after the run completes
instead of self-shutting-down 3 seconds later.

## CI

`.github/workflows/engine-tests.yml` runs on `ubuntu-latest` with a 60-minute timeout, triggered by:

- `workflow_dispatch`, with an optional `filter` input (`SWLOR_ENGINE_TEST_FILTER`).
- `schedule` - nightly at `0 9 * * *`, outside normal contributor hours.
- `pull_request` into `feature/combat-upgrade`, path-filtered to
  `SWLOR.Game.Server/**`, `SWLOR.NWN.API/**`, `SWLOR.CLI/**`, `Module/**`,
  `SWLOR.Game.Server.sln`, and the workflow file itself.

It checks out with `submodules: recursive` and `fetch-depth: 1`, builds `SWLOR.CLI` (which pulls in
`SWLOR.Game.Server`/`SWLOR.NWN.API`), downloads the `neverwinter.nim` Linux release tools
(`nwn_erf`/`nwn_gff`/`nwn_tlk`) and renames them to `*.exe` next to `SWLOR.CLI.dll` so `HakBuilder`/
`ModulePacker`'s hardcoded process names resolve, builds the hak files via `SWLOR.CLI --hak`
(retargeting `Build/hakbuilder.json`'s `OutputPath` to `SWLOR.Game.Server/Docker/`), packs the
module via `SWLOR.CLI --pack` into `SWLOR.Game.Server/Docker/modules/`, stages the
already-built server assembly (a transitive output of the CLI build) into
`SWLOR.Game.Server/Docker/dotnet/`, then runs `scripts/run-engine-tests.sh --skip-build --filter`
so the server is not compiled a second time. Engine test results and server logs are uploaded as
artifacts on every run (`if: always()`), each with 14-day retention.

**This workflow has not yet had a successful (or attempted) run on GitHub-hosted runners**, per its
own header comment - treat early runs as a debugging exercise, not a working pipeline. The biggest
risk is `SWLOR_Haks`: it's a roughly 13GB git submodule, and even with `fetch-depth: 1` the checked
out working tree is still that large, which may exceed a hosted runner's disk space and/or the job
time budget. The header also flags, in rough order of likelihood after that: the Linux
`neverwinter.nim` binaries haven't been confirmed to produce byte-identical or even game-compatible
output versus the pinned Windows tools in `SWLOR_Haks/`, nor has their CLI argument compatibility
with `HakBuilder.cs`/`ModulePacker.cs` been verified; renaming the extension-less Linux binaries to
`*.exe` is a workaround, not an upstream-supported flow; and NWN server boot inside the container
(NWNX/NWSync/licensing, engine startup timing) has only been exercised via the normal
`docker-compose.yml` dev flow, not in a CI sandbox network context.

## Known Gaps and Non-Goals

- **No real player client.** Flows that require an actual connected PC - NUI-driven perk purchase
  UI, DB-backed player character persistence, GUI button/event handling - are not covered. Tests
  drive spawned NPC actors directly through the same shared pipeline code a player action would
  eventually reach (e.g. `UsePerkFeat.TryUseAbility`, `StatusEffect`, `Perk.GetPerkLevel`), rather
  than simulating a client.
- **Base-game blueprint resrefs are assumed present.** The shipped suites spawn stock blueprints
  like `nw_rat001` and `nw_bandit001` from the module's palette; this is only verified at runtime
  (a failed `CreateObject` fails the `Assert` inside `SpawnCreature`), not checked ahead of time.
- **Production guard.** The runner refuses to start if `SWLOR_ENVIRONMENT` resolves to `Production`,
  even when `SWLOR_ENGINE_TESTS_ENABLED=true` - this is a live game server and the suite is not
  meant to run against it.
