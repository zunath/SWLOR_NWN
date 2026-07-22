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

## Architecture Overview

The runner lives in `SWLOR.Game.Server/Service/EngineTest.cs`, with its supporting types in
`SWLOR.Game.Server/Service/EngineTestService/`.

1. **Trigger** - `EngineTest.ScheduleEngineTests` is an `[NWNEventHandler(ScriptName.OnModuleLoad)]`
   handler. It checks `ApplicationSettings.EngineTestsEnabled`; if false, it does nothing. If the
   server's `ServerEnvironment` is `Production`, it logs an error and refuses to run even if
   enabled. Otherwise it schedules `RunAllTestsAsync` via `DelayCommand` after
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
| `SWLOR_ENVIRONMENT` | `ServerEnvironment` | `Development` (any unset/unrecognized value) | `"prod"`/`"production"` -> `Production` (engine tests refuse to run); `"test"`/`"testing"` -> `Test`; anything else -> `Development`. |

Booleans accept `true`/`1`/`yes` (case-insensitive) as true, anything else (including unset) as
false unless noted otherwise above. Floats fall back to their default on a parse failure.

## How to Write a New Engine Test

### The attribute contract

```csharp
[EngineTest("Human-readable test name", Category = "MyCategory", TimeoutSeconds = 30f)]
public static void MyTest(EngineTestContext ctx) { /* ... */ }
```

- The method must be **public static**.
- It must take exactly **one parameter of type `EngineTestContext`**.
- It must return **`void` or `Task`** (an `async Task` method works; `Task<T>` does not - the
  runner's signature check (`EngineTest.IsValidTestMethod`) only accepts those two exact return
  types). `async void` is explicitly rejected: the runner would have no task to observe, so it
  would report a pass and clean up while the test was still running.
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
| `SpawnCreature(resref, xOffset = 0f, yOffset = 0f)` | Creates a creature blueprint at an arena location, asserts it's valid, and tracks it for automatic cleanup. Returns the creature. |
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

This is `AbilityActivationEngineTests` (`SWLOR.Game.Server/Feature/EngineTestDefinition/AbilityActivationEngineTests.cs`)
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

New tests belong in `SWLOR.Game.Server/Feature/EngineTestDefinition/`, one file per suite,
following the same shape: `SpawnCreature`, drive the real system under test, assert on the real
resulting state via `WaitUntilAsync` where a delay or tick is involved. The current suites (see
that directory) also cover harness sanity (`HarnessSanityEngineTests`), ability registration
(`AbilityRegistrationEngineTests`), status effect application/expiration/removal
(`StatusEffectEngineTests`), the native combat pipeline (`CombatPipelineEngineTests`), and NPC perk
level resolution (`PerkEngineTests`) - each is a good reference for a different corner of the API.

## How to Run Locally

`scripts/run-engine-tests.ps1` (Windows) and `scripts/run-engine-tests.sh` (bash) are functionally
identical and are kept in sync. Both:

1. Build `SWLOR.Game.Server` in `Release` (unless skipped) with `-p:RunPostBuildEvent=Never` (so
   the normal Windows-only CLI post-build deploy step doesn't run), then copy the build output from
   `SWLOR.Game.Server/bin/{Configuration}/net10.0` into `SWLOR.Game.Server/Docker/dotnet`.
2. Delete any stale `engine-test-results.json` from a previous run.
3. Run `docker compose -f docker-compose.enginetests.yml up --abort-on-container-exit
   --exit-code-from swlor-server` from `SWLOR.Game.Server/Docker/`, then tear the containers down.
4. Parse the resulting JSON report, print a table of every test (category, name, outcome, duration,
   message), print the summary line, and exit non-zero unless at least one test ran and none
   failed.

**Prerequisites**: the `dotnet` SDK (unless `--skip-build`), `docker compose`, and - for the bash
script only - `jq` (used to parse the JSON report; the run fails at the reporting step without it).

**This assumes `SWLOR.Game.Server/Docker/` already has `modules/`, `hak/`, `tlk/`, and (after step
1) `dotnet/` populated with the current module and hak assets** - the normal deploy-machine flow
(e.g. after `SWLOR.CLI.exe -o`, or the asset-assembly steps the CI workflow performs). The script
itself only builds and stages the compiled .NET assembly; it does not pack the module or build the
haks.

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
```

`-Filter`/`--filter` and `-ArenaResref`/`--arena-resref` are passed straight through as
`SWLOR_ENGINE_TEST_FILTER` and `SWLOR_ENGINE_TEST_ARENA_RESREF` env vars picked up by
`docker-compose.enginetests.yml`.

`docker-compose.enginetests.yml` itself only starts the two services actually needed to run tests
(`redis`, `swlor-server`) - not the full dev stack in `docker-compose.yml`. Redis uses a `tmpfs`
data directory so every run starts against a fresh, empty database, there are no restart policies
(a container exit is final and observable), and it overrides `SWLOR_ENGINE_TESTS_ENABLED=true`,
`SWLOR_ENVIRONMENT=test`, and `NWNX_METRICS_INFLUXDB_SKIP=y` (no InfluxDB service exists in this
compose file) on top of the normal `swlor.env` defaults.

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
module via `SWLOR.CLI --pack` into `SWLOR.Game.Server/Docker/modules/`, then runs
`scripts/run-engine-tests.sh --filter`. Engine test results and server logs are uploaded as
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
