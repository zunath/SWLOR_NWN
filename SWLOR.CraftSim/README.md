# SWLOR.CraftSim

Balance simulator for the crafting revamp (`SWLOR.Game.Server/Readmes/CraftingRevampPlan.md`, "Balance gates and tooling"), built on the `SWLOR.CombatSim` pattern: a console tool that reads the real game data, plays it with repeatable seeds, and writes markdown and JSON evidence. Tests are in `SWLOR.CraftSim.Tests`.

This project holds the **legacy baseline**: today's rules for the old craft minigame, harvesting, scavenging, refining and the fishing catch roll. It also holds the generic **balance gates** that each new minigame passes before it ships, which compare against that baseline.

## Run

```powershell
dotnet build SWLOR.CraftSim -p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll baseline --section crafting --runs 500 --seed 1 --out design/testing/crafting-revamp/legacy-crafting-baseline.md
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll baseline --section gathering --seed 1 --out design/testing/crafting-revamp/legacy-gathering-baseline.md
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll legacy-enhancements --runs 200 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/legacy-enhancement-baseline.md
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gear
```

`legacy-enhancements` plays the old minigame with one, two and three enhancements slotted and writes today's expected transferred enhancements per band, tier and slot count, the yardstick of gate 7. It takes `--runs`, `--seed`, `--recipes-per-band` (enhanceable recipes sampled per skill and band), `--skills` (Smithery, Engineering, Fabrication, Agriculture), `--gear all|best`, `--out` and `--json`. At the committed settings it takes about ten minutes.

`baseline` takes `--section crafting|gathering|all`, `--runs` (crafts per recipe level, skill, gear level and policy), `--seed`, `--gathering-runs` (nodes and sites played per row), `--skills Smithery,Engineering,...`, `--out report.md` and `--json results.json`. The same seed repeats a report exactly. The crafting section takes about two minutes at 500 runs.

## Data

Recipes come from the server's `IRecipeListDefinition` classes and the level chart from `Craft.GetRecipeLevelDetail`, so the simulator follows the game. Gear levels come from the crafting stats in `Module/uti`: Smithery recipes make every skill's wearable crafting gear, the recipes ordered by level give tiers 1-5 per slot, and a gear level is none, tier 3 or tier 5 (`CraftingGearCatalog`). XP comes from `Skill.GetDeltaXP` after `ServerXp` runs the chart's startup step.

## Legacy model

`Legacy/Crafting` ports `CraftViewModel.cs` (formulas in `LegacyCraftRules`, the 11 actions in `LegacyActionSpec`, resolution in `LegacyCraftEngine`) and plays it with two policies: the dominant rotation players report and a rollout lookahead. `Legacy/Gathering` ports harvesting, scavenging, refining and the fishing catch roll. Where the server exposes a static (`GetDeltaXP`, `GetRecipeLevelDetail`) it is called; the rest are re-implemented.

Every ported rule is registered in `Legacy/LegacySource.cs` with its file, lines and source text, and cited in a code comment. `LegacySourceTests` fails when a quoted line moves more than 40 lines or changes, and when a comment cites lines no registered rule covers. When Closeout deletes `CraftViewModel.cs`, retire the references to it with the file and keep the committed baseline report as the record.

Enhancements (`LegacyEnhancementRules`, `LegacyEnhancementBaseline`, `Data/Enhancement*`): each enhancement item adds its `ProgressPenalty` item property to the progress the piece needs (`LegacyCraftContext` takes a `CraftEnhancementConfiguration` of a count and a penalty each), and on a success each enhancement transfers by a roll at a chance equal to the quality percentage, truncated in floats. The model draws no transfer dice: a craft that is made adds slots x chance / 100 expected enhancements, a craft that breaks the piece adds none. The enhancement items are read from `Module/uti` (`EnhancementItemReader`: an item with a progress penalty, one enhancement property and an enhancement level). A recipe is played with the **representative enhancement** of its kind: the real item at the best level the recipe accepts (the old window refuses one more than 5 levels above the recipe) whose penalty is the lower median of that level, ties by resref (`EnhancementCatalog.ForRecipe`). The committed `legacy-enhancement-baseline.md` lists them.

Gathering rules depend on Might, Perception and Willpower. They are swept over modifiers -1 to +5 (`AttributeSweep`) until the real medians of active characters are measured.

## Balance gates

`Gates/` holds the plan's seven gates (`GateNames`): an `IBalanceGate` receives a `GateContext` (recipes, gear, runs, seed) and returns a `GateResult` with a verdict and evidence tables, and `GateReport` writes them as markdown with the inputs and limitations `SkillPerkStandards.md` asks for. The gates are generic over `ICraftMinigameEngine`: a minigame supplies a scenario and never writes its own solver or gate framework.

```powershell
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --list
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll gates --scenario forge --runs 100 --seed 1 --recipes-per-band 6 --out design/testing/crafting-revamp/forge-gates.md --json forge-gates.json
```

`gates` takes `--scenario <name>` (required; `--list` shows them), `--runs` (attempts per case, default 100), `--seed`, `--recipes-per-band` (an even sample of each band, default 6) or `--all-recipes`, `--gates rotation,styles,perks,length,materials,xp,enhancements` (a subset; a gate's number, 1 to 7, names it too, so `--gates 7` runs enhancement parity), `--legacy-seconds-per-action`, `--thorough` (a much longer fixed-rotation search for a final run), `--out` (default `design/testing/crafting-revamp/<scenario>-gates.md`), `--json` and `--strict` (exit 2 when a gate failed or was not run). The same seed repeats a report exactly. Cases are played on every core. Gate 1 dominates the time: it solves a fixed rotation for every sampled recipe and gear tier, and again one rotation for every band and gear tier across its recipes, so use the sample, not `--all-recipes`, for it (`--gates` skips it).

| Gate | What it checks | Starting threshold (`GateThresholds`) |
| --- | --- | --- |
| 1 No single rotation (named "No fixed rotation") | The adaptive player's average quality beats the best single fixed rotation, one sequence for every sampled recipe of a band at a gear tier, in every band and gear tier. The best rotation per recipe alone is reported, not gated. | at least 10 quality points (`RotationMarginPoints`) |
| 2 Style balance | For each style pair, each style is the better choice on enough recipes and neither leads on average. Not applicable until a scenario offers `StylePairs`. | each style at least 30% of recipes, mean gap at most 5 points, ties within 0.5 point count half |
| 3 Perks optional | At rank 50 with the best gear and no perks, the share of adaptive crafts that reach maximum quality, per at-level band (41-45, 46-50, 51-53). | at least 50% in every band |
| 4 Length | Mean actions per attempt of the adaptive player at level, per band and gear tier. | the scenario's `LengthTarget` (item craft 10-20) |
| 5 Material parity (economy parity) | Component stacks consumed per item made, failures included, against the legacy lookahead player on the same recipes, ranks and gear. | In a band and gear tier where the legacy player succeeds at least 90% of the time (`MaterialTwoSidedMinimumLegacySuccess`), within 10% either way; where it succeeds less, at most 10% above it (lower use passes and the report says so). Either way at an average quality no more than 5 points below the legacy player's (`MaterialQualityShortfallPoints`); recipes never made may exceed the legacy share by at most 10 points |
| 6 XP rate | XP per hour against the legacy loop over the plan's item-craft cohort: each band with rank equal to level (capped at 50), no gear, mid and best gear, equal weight per recipe, no blueprint, not a first craft, failures at 15% XP. Each band, gear tier and enhancement count must pass: the cohort is played plain and again with one, two and three enhancements (the representative enhancement of gate 7, each side applying its own enhancement difficulty). A scenario that does not model enhancements leaves the enhanced cases unplayed and the gate not run. | at least 1.0 times the legacy rate |
| 7 Enhancement parity | At rank 50 with the best gear and at level (41-45, 46-50, 51-53), the expected number of enhancements transferred per enhanced craft attempt, for items with one, two and three filled slots, against the old window's chance roll (quality percentage per enhancement) with the same enhancements slotted. The new count is read from the quality distribution of the adaptive player's crafts under the thresholds in `CraftOutcomeRules`. The report also gives the thresholds that would meet parity. Not run until the scenario models enhancements. | at least 1.0 times today's in every band and slot count (`EnhancementParityMinimumRatio`) |

Quality is the quality meter's share at the end, in points, with a broken or abandoned piece counting 0. Time for gate 6 is actions times a seconds-per-action estimate (the scenario's `SecondsPerAction`, 6 s for the old window), plus an optional per-craft overhead; both are unmeasured until the client evidence replaces them. A failing XP case reports the multiplier it would need.

### Gate 7: enhancement parity

Today each enhancement transfers at a chance equal to the quality percentage, so 80% quality averages 2.4 of 3 enhancements; thresholds of 50/85/85 give exactly 1. Each enhancement also raises the progress the piece needs by its progress penalty, which makes the craft harder. The gate plays both:

- **Today:** `LegacyEnhancementBaseline` plays the old minigame's lookahead player with the representative enhancement of the recipe's kind slotted 1, 2 and 3 times. A craft that is made adds slots x chance / 100; one that breaks adds none.
- **New:** the scenario builds its engine with `CraftEngineRequest.Enhancements` (a `CraftEnhancementConfiguration`: the count and the progress penalty each, in the old window's units, default none), and the adaptive player crafts. A made item transfers the enhancement of slot n when its quality percent (`MinigameMeter.Percent`, rounded down) meets `CraftOutcomeRules.GetEnhancementThreshold(n)`, exactly as `CraftOutcomeRules.CalculateSuccess` decides (a test checks every quality and slot count). Changing a threshold constant there flows into the gate. A craft that breaks the piece transfers none.
- **Report:** per band and slot count, today's and the new figure, the ratio and the verdict; the thresholds (first, second, third) that would meet parity for the measured quality distribution, for every band together and for each band alone, exact and in steps of 5 (`EnhancementThresholdSolver`: the strictest non-decreasing set that keeps every case at least at today's); the quality percentiles of the items made; and the representative enhancements used.

The recipes are the cohort's recipes that have an enhancement slot, an even sample of each band. Recipes have one or two slots and research and blueprints add more, so each is played with every slot count. A scenario must say how it treats enhancements:

| `ICraftScenario.Enhancements` | Meaning | Gate 7 |
| --- | --- | --- |
| `NotModeled` (default) | `CreateEngine` ignores `request.Enhancements`. | Not run, which blocks shipping (`GateReport.AllPassed`) |
| `Modeled` | `CreateEngine` maps the request's total progress penalty onto the engine's progress meter. | Runs. It first checks that the engine's progress maximum rises with the enhancements and fails if not. |
| `NoSlots` | The minigame has no enhancement slots (Reaction). | Not applicable |

Mapping a penalty onto a minigame: `request.Enhancements.TotalProgressPenalty` (count x penalty each, in the old window's progress units) goes through the engine's own enhancement handling, the same one its window uses when the player slots enhancements, as each phase 1 PR mapped the old progress penalty:

- **Forge (Smithery):** extra Shape. `ForgeRules.EnhancementShapePercent(total, recipe.Level)` gives the percent, which `ForgeSetup` takes as its `EnhancementShapePercent`.
- **Circuit (Engineering):** extra Assembly. `CircuitSetup` takes the total as `enhancementProgressPenalty` and adds it to `MaxAssembly`.
- **Kitchen (Agriculture):** extra Doneness. `KitchenTuning.EnhancementDoneness(total)` is passed to `KitchenEngine` as `extraDoneness`.
- **Reaction:** `EnhancementSupport.NoSlots`.

A scenario then sets `public EnhancementSupport Enhancements => EnhancementSupport.Modeled;`. Nothing else in the scenario changes: `request.Enhancements` is `CraftEnhancementConfiguration.None` for every other gate.

### The players

- `AdaptivePolicy` is a depth-limited expectimax. For every available action it reads the exact results and chances from `PreviewAll()`, copies the engine with `Fork` to reach the state each result leads to (forcing that result and sampling the forecast), and averages the best follow-up. It never touches the real engine. `AdaptivePolicyOptions` sets the depth (3 by default: with two it never sets up and plays far worse), forecast samples, an engine-copy budget per decision, a small time preference and the leaf heuristic. `DefaultLeafHeuristic` weighs finishing safely (integrity and CP against the best progress rate the previews show) and quality, using only the shared meters and previews.
- `FixedRotationSolver` is gate 1's adversary. The gate asks whether **one** rotation wins every craft of a band (owner decision, October 2026: the player complaint was one rotation across every craft, and a minigame with full state and almost no randomness always has a rotation per recipe that matches adaptive play, so that figure is reported and not gated). `SolveShared` searches one sequence across several recipes (`RotationCase`: an engine factory and its stream), every recipe weighing the same and each played on its own dice; an action a recipe's engine does not offer is skipped there. `Solve` is the one-recipe case, which the gate still runs for each recipe as the reported metric and to seed the shared search. Candidates come from a beam search, every repeating cycle of up to three actions, and the action lines an adaptive player takes; a local search (replace, swap, delete, insert) improves the best few on whole attempts; the finalists are re-scored on different attempts, and the gate scores the winner on a third set. A fixed rotation skips an action that is unavailable and repeats its last action once the list ends. `FixedRotationSolverOptions` scales the effort.

Both are repeatable from the seed: every attempt, search and policy takes its dice from its own stream of the seed (`SimStreams`), so the machine's core count cannot change a result.

### Plugging a minigame in

Phase 2 of a minigame workstream adds one class to this project, normally under `Scenario/<Minigame>/`:

1. Implement `ICraftScenario` in a public class with a public parameterless constructor. `ScenarioRegistry.Discover` finds it by reflection, so `gates --scenario <Name>` runs it with no registration.
2. `CreateEngine(CraftEngineRequest)` builds an attempt that has not started from the recipe (`Recipe.Detail` holds the recipe's per-recipe minigame parameters when it came from the server's definitions), the rank, the gear (`Gear.Control` powers quality, `Gear.Craftsmanship` progress, `Gear.CpBonus` the CP budget), the style (null for no perks) and the source of chance (`Random`). It must build the engine on `request.Random` and keep no mutable state of its own, because attempts run on many threads.
3. The engine must keep every mutable value in its `TState`, with only `readonly` configuration fields: `Fork` copies it with a shallow clone. A game server test checks the fields of every engine.
4. Fill in the rest as the minigame needs, all optional: `Cohort` (which recipes and which ranks; the default is every recipe of `Skill` at its level up to rank 50), `LengthTarget`, `StylePairs` (alternative styles and the rank they open at, built from `CraftStyle`), `AdaptiveOptions` (a deeper search or a heuristic of the minigame's own), `SolverOptions` and `Limitations` (appended to the report).
5. `SecondsPerAction` is required: the seconds a player takes per action in the new window. Measure it in the client evidence and replace the estimate.
6. `Enhancements`: apply `request.Enhancements` through the engine's own enhancement handling (see "Gate 7: enhancement parity" above) and return `EnhancementSupport.Modeled`, or `NoSlots` for a minigame without enhancement slots. Until then gates 6 and 7 do not run their enhanced cases and the minigame cannot ship.

Gathering and fishing implement `IXpRateCohort` instead: it pairs the legacy loop with its replacement over the plan's activity cohorts (harvesting per node tier, scavenging per site level, fishing per fish-level band) at the parity attribute medians in `GateContext.Attributes`, and `XpRateGate` compares them. `LengthTarget` already carries their 3-8, 3-6 and 5-8 action targets.

The game server changes only by `ICraftMinigameEngine.Fork`, which every engine inherits from `MinigameEngineBase<TState>`, and by the third enhancement slot's own threshold constant in `CraftOutcomeRules` (a starting value that gate 7 sets).

### Research Experiment

`Scenario/Experiment` plugs the research Experiment in as the `experiment` scenario (`gates --scenario experiment --gates length` checks its 5-8 turn target; the other gates do not apply to an experiment, which has no quality, styles or legacy loop). The `research` command writes the Experiment's own evidence: length, the size of the reductions against today's dice and the change in supply (licensed runs stay random at today's odds, so theirs is unchanged), the time cut at fixed levels, and Fabrication XP per real hour against at-level crafting.

```powershell
dotnet SWLOR.CraftSim/bin/Debug/net10.0/SWLOR.CraftSim.dll research --runs 400 --adaptive-runs 40 --seed 1 --out design/testing/crafting-revamp/research-experiment-report.md
```

`research` takes `--runs` (experiments per case for the focused player), `--adaptive-runs` (for the adaptive player), `--seed`, `--seconds-per-action`, `--job-overhead-seconds`, `--out` and `--repo`. The focused player (`ExperimentFocusedPolicy`) states a strategy instead of searching for one; the XP values it reports are the named constants in `ResearchXpRules`.

### Test bench

`SWLOR.CraftSim.Tests/Support` holds a test-only engine (`BenchEngine`: a working window the player must keep Heat inside, a chance-dependent Stoke and a telegraphed opportunity), a variant with a dominant action, a one-action table engine whose quality is set per recipe and style, a deterministic engine where each recipe has its own favored action (`ComboEngine`: no randomness, yet no single rotation serves the recipes, so gate 1 passes where a per-recipe rule would have failed), and their scenarios. The tests run every gate runner end to end on them with seeds, including a dominant rotation (gate 1 fails) and an adaptive game (gate 1 passes). Both model enhancements (a quarter of the penalty becomes extra progress, one for one in the table engine), and the table engine sets the quality of an enhanced craft by its enhancement count so gate 7's arithmetic and the threshold solver can be checked exactly.
