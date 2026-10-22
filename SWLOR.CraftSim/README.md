# SWLOR.CraftSim

Balance simulator for the crafting revamp (`SWLOR.Game.Server/Readmes/CraftingRevampPlan.md`, "Balance gates and tooling"), built on the `SWLOR.CombatSim` pattern: a console tool that reads the real game data, plays it with repeatable seeds, and writes markdown and JSON evidence. Tests are in `SWLOR.CraftSim.Tests`.

This project holds the **legacy baseline**: today's rules for the old craft minigame, harvesting, scavenging, refining and the fishing catch roll. The new minigames' gates compare against it.

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

## Gates seam

`Gates/` is the extension point for the plan's five gates (`GateNames`). A gate implements `IBalanceGate`, receives a `GateContext` (recipes, gear, runs, seed and the legacy baseline results) and returns a `GateResult` with a verdict and evidence tables; `GateReport` writes them as markdown. No gate is implemented yet: each belongs with the minigame engine it drives, which comes from the engine contract under `Service/CraftService/Minigame/`.
