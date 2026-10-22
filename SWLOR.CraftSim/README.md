# SWLOR.CraftSim

Balance simulator for the crafting revamp (`SWLOR.Game.Server/Readmes/CraftingRevampPlan.md`, "Balance gates and tooling"), built on the `SWLOR.CombatSim` pattern: a console tool that reads the real game data, plays it with repeatable seeds, and writes markdown and JSON evidence. Tests are in `SWLOR.CraftSim.Tests`.

This project holds the **legacy baseline**: today's rules for the old craft minigame, harvesting, scavenging, refining and the fishing catch roll. It also holds the generic **balance gates** that each new minigame passes before it ships, which compare against that baseline.

## Run

```powershell
dotnet build SWLOR.CraftSim -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll baseline --section crafting --runs 500 --seed 1 --out design/testing/crafting-revamp/legacy-crafting-baseline.md
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll baseline --section gathering --seed 1 --out design/testing/crafting-revamp/legacy-gathering-baseline.md
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gear
```

`baseline` takes `--section crafting|gathering|all`, `--runs` (crafts per recipe level, skill, gear level and policy), `--seed`, `--gathering-runs` (nodes and sites played per row), `--skills Smithery,Engineering,...`, `--out report.md` and `--json results.json`. The same seed repeats a report exactly. The crafting section takes about two minutes at 500 runs.

## Data

Recipes come from the server's `IRecipeListDefinition` classes and the level chart from `Craft.GetRecipeLevelDetail`, so the simulator follows the game. Gear levels come from the crafting stats in `Module/uti`: Smithery recipes make every skill's wearable crafting gear, the recipes ordered by level give tiers 1-5 per slot, and a gear level is none, tier 3 or tier 5 (`CraftingGearCatalog`). XP comes from `Skill.GetDeltaXP` after `ServerXp` runs the chart's startup step.

## Legacy model

`Legacy/Crafting` ports `CraftViewModel.cs` (formulas in `LegacyCraftRules`, the 11 actions in `LegacyActionSpec`, resolution in `LegacyCraftEngine`) and plays it with two policies: the dominant rotation players report and a rollout lookahead. `Legacy/Gathering` ports harvesting, scavenging, refining and the fishing catch roll. Where the server exposes a static (`GetDeltaXP`, `GetRecipeLevelDetail`) it is called; the rest are re-implemented.

Every ported rule is registered in `Legacy/LegacySource.cs` with its file, lines and source text, and cited in a code comment. `LegacySourceTests` fails when a quoted line moves more than 40 lines or changes, and when a comment cites lines no registered rule covers. When Closeout deletes `CraftViewModel.cs`, retire the references to it with the file and keep the committed baseline report as the record.

Gathering rules depend on Might, Perception and Willpower. They are swept over modifiers -1 to +5 (`AttributeSweep`) until the real medians of active characters are measured.

## Balance gates

`Gates/` holds the plan's six gates (`GateNames`): an `IBalanceGate` receives a `GateContext` (recipes, gear, runs, seed) and returns a `GateResult` with a verdict and evidence tables, and `GateReport` writes them as markdown with the inputs and limitations `SkillPerkStandards.md` asks for. The gates are generic over `ICraftMinigameEngine`: a minigame supplies a scenario and never writes its own solver or gate framework.

```powershell
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --list
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --scenario forge --runs 100 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-gates.md --json forge-gates.json
```

`gates` takes `--scenario <name>` (required; `--list` shows them), `--runs` (attempts per case, default 100), `--seed`, `--recipes-per-band` (an even sample of each band, default 6) or `--all-recipes`, `--gates rotation,styles,perks,length,materials,xp` (a subset), `--legacy-seconds-per-action`, `--thorough` (a much longer fixed-rotation search for a final run), `--out` (default `design/testing/crafting-revamp/<scenario>-gates.md`), `--json` and `--strict` (exit 2 when a gate failed or was not run). The same seed repeats a report exactly. Cases are played on every core. Gate 1 dominates the time: it solves a fixed rotation for every sampled recipe and gear tier, so use the sample, not `--all-recipes`, for it (`--gates` skips it).

| Gate | What it checks | Starting threshold (`GateThresholds`) |
| --- | --- | --- |
| 1 No fixed rotation | The adaptive player's average quality beats the best fixed rotation per recipe, in every band and gear tier. | at least 10 quality points (`RotationMarginPoints`) |
| 2 Style balance | For each style pair, each style is the better choice on enough recipes and neither leads on average. Not applicable until a scenario offers `StylePairs`. | each style at least 30% of recipes, mean gap at most 5 points, ties within 0.5 point count half |
| 3 Perks optional | At rank 50 with the best gear and no perks, the share of adaptive crafts that reach maximum quality, per at-level band (41-45, 46-50, 51-53). | at least 50% in every band |
| 4 Length | Mean actions per attempt of the adaptive player at level, per band and gear tier. | the scenario's `LengthTarget` (item craft 10-20) |
| 5 Material parity (economy parity) | Component stacks consumed per item made, failures included, against the legacy lookahead player on the same recipes, ranks and gear. | within 10%; recipes never made may exceed the legacy share by at most 10 points |
| 6 XP rate | XP per hour against the legacy loop over the plan's item-craft cohort: each band with rank equal to level (capped at 50), no gear, mid and best gear, equal weight per recipe, no blueprint, not a first craft, failures at 15% XP. Each band and tier must pass. | at least 1.0 times the legacy rate |

Quality is the quality meter's share at the end, in points, with a broken or abandoned piece counting 0. Time for gate 6 is actions times a seconds-per-action estimate (the scenario's `SecondsPerAction`, 6 s for the old window), plus an optional per-craft overhead; both are unmeasured until the client evidence replaces them. A failing XP case reports the multiplier it would need.

### The players

- `AdaptivePolicy` is a depth-limited expectimax. For every available action it reads the exact results and chances from `PreviewAll()`, copies the engine with `Fork` to reach the state each result leads to (forcing that result and sampling the forecast), and averages the best follow-up. It never touches the real engine. `AdaptivePolicyOptions` sets the depth (3 by default: with two it never sets up and plays far worse), forecast samples, an engine-copy budget per decision, a small time preference and the leaf heuristic. `DefaultLeafHeuristic` weighs finishing safely (integrity and CP against the best progress rate the previews show) and quality, using only the shared meters and previews.
- `FixedRotationSolver` is gate 1's adversary. Candidates come from a beam search, every repeating cycle of up to three actions, and the action lines an adaptive player takes; a local search (replace, swap, delete, insert) improves the best few on whole attempts; the finalists are re-scored on different attempts, and the gate scores the winner on a third set. A fixed rotation skips an action that is unavailable and repeats its last action once the list ends. `FixedRotationSolverOptions` scales the effort.

Both are repeatable from the seed: every attempt, search and policy takes its dice from its own stream of the seed (`SimStreams`), so the machine's core count cannot change a result.

### Plugging a minigame in

Phase 2 of a minigame workstream adds one class to this project, normally under `Scenario/<Minigame>/`:

1. Implement `ICraftScenario` in a public class with a public parameterless constructor. `ScenarioRegistry.Discover` finds it by reflection, so `gates --scenario <Name>` runs it with no registration.
2. `CreateEngine(CraftEngineRequest)` builds an attempt that has not started from the recipe (`Recipe.Detail` holds the recipe's per-recipe minigame parameters when it came from the server's definitions), the rank, the gear (`Gear.Control` powers quality, `Gear.Craftsmanship` progress, `Gear.CpBonus` the CP budget), the style (null for no perks) and the source of chance (`Random`). It must build the engine on `request.Random` and keep no mutable state of its own, because attempts run on many threads.
3. The engine must keep every mutable value in its `TState`, with only `readonly` configuration fields: `Fork` copies it with a shallow clone. A game server test checks the fields of every engine.
4. Fill in the rest as the minigame needs, all optional: `Cohort` (which recipes and which ranks; the default is every recipe of `Skill` at its level up to rank 50), `LengthTarget`, `StylePairs` (alternative styles and the rank they open at, built from `CraftStyle`), `AdaptiveOptions` (a deeper search or a heuristic of the minigame's own), `SolverOptions` and `Limitations` (appended to the report).
5. `SecondsPerAction` is required: the seconds a player takes per action in the new window. Measure it in the client evidence and replace the estimate.

Gathering and fishing implement `IXpRateCohort` instead: it pairs the legacy loop with its replacement over the plan's activity cohorts (harvesting per node tier, scavenging per site level, fishing per fish-level band) at the parity attribute medians in `GateContext.Attributes`, and `XpRateGate` compares them. `LengthTarget` already carries their 3-8, 3-6 and 5-8 action targets.

The game server changes only by `ICraftMinigameEngine.Fork`, which every engine inherits from `MinigameEngineBase<TState>`.

### Test bench

`SWLOR.CraftSim.Tests/Support` holds a test-only engine (`BenchEngine`: a working window the player must keep Heat inside, a chance-dependent Stoke and a telegraphed opportunity), a variant with a dominant action, a one-action table engine whose quality is set per recipe and style, and their scenarios. The tests run every gate runner end to end on them with seeds, including a dominant rotation (gate 1 fails) and an adaptive game (gate 1 passes).
