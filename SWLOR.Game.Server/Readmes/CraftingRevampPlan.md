# Crafting Revamp Implementation Plan

Owner-approved design from the October 2026 design interview, written for the agent that implements it. Every decision below was confirmed by the owner. Numbers marked *starting value* are first guesses for the simulator to tune; everything else is a decision. A change to a decision needs the owner's approval.

Sections: Goals · Scope · Read first · Current system · Shared framework · Minigames · Progression and perks · Migration and release · Art and animation · Fixes and things left alone · Delivery · Verification · Defaults · Prior attempts

## Goals

Players report that one rotation works for every crafted item, and every craft plays the same. The audit confirmed it.

- **One minigame for every craft.** Smithery, Engineering, Fabrication, cooking and Espionage share the 11 actions in `CraftViewModel.cs`, with the same unlock ranks and formulas. Recipes differ only by level (`RecipeLevelChart.cs`).
- **One rotation wins.** Rank 40's flat +115 quality bonus makes Basic Touch the best value, so Waste Not, then Basic Touch repeated, repaired with Master's Mend, wins nearly everywhere.
- **Gathering and research are gated by perks, and nothing else is active.**
  - Gathering rank alone does nothing; every tier is a perk.
  - Research is a Fabrication perk line, and its loop is a passive timer.
  - Fishing, harvesting, scavenging and refining are click-and-wait.

The revamp aligns crafting with the combat upgrade:

1. **Progression comes from skill rank.** No perk only unlocks tiers or blueprints.
2. **Perks are optional build choices**, as in combat.
3. **Every craft gets its own minigame** with its own actions.
4. **Every loop is turn-based**, with no real-time reactions. Pressure comes from limited actions and resources.

## Scope

**In scope**
- Smithery, Engineering, Fabrication (including Research) and Agriculture (cooking and fishing).
- Gathering (harvesting, scavenging, refining).
- Espionage's poison and trap crafting.
- **Progression pace** (added by the owner after player feedback, October 2026): crafting XP speed and the crafting guilds' task cap. Players report slow crafting progression and guild ranks that are a required chore, and bypass both with RP XP or rebuilds (workstream 11).

**Out of scope**
- **Farming.** It comes later and will add a third Agriculture line, Farmer, at 25 SP.
- **Space.** Asteroid mining and ship content wait for the space revamp. Engineering's starship, module, ship enhancement and ammo recipes (about 250) only switch to the new Engineering minigame, with no changes to their items, stats or recipes. The space revamp may move them later.
- **Lightsaber workbenches.**
- **Espionage's perks.** Poisoncraft, Trapcraft and Master Saboteur stay as they are.
- **NPC store gear.** NPC stores are meant to sell unenhanced copies of crafted equipment.
- **Skills.** Research stays with Fabrication and fishing with Agriculture.

**Hard constraints**
- **Power ceiling.** Crafted item stats do not change. Fully enhanced crafted gear stays the ceiling that `RareLootBalanceTests` measures. Research cannot grant more blueprint bonuses than today.
- **Research.** Research keeps its base credit costs and real-time durations; only the experiment's capped Analyze cut shortens a job. Researchable types stay as today: weapons, armor and food.
- **Attributes.** No attributes in item crafting, gathering or fishing. Gathering rank and tool tier replace Might, Perception and Willpower.
- **Espionage.** Espionage crafting stays tied to Espionage rank and XP.

## Read first

- `AGENTS.md`, then these guides in `Readmes/`:
  - `CodingStandards.md` (StatType design, renames, Module JSON, TLK/2DA)
  - `ContentStandards.md` (items, balance)
  - `SkillPerkStandards.md` (lines, mechanical contract, balance evidence)
  - `Migrations.md`
  - `PullRequests.md`
  - `AgentEnvironment.md`
  - `InEngineVisualVerification.md`
  - `EngineTesting.md`
  - `NuiLayoutRules.md` and `GuiWindowAuthoring.md`
  - `IconStandards.md`
  - `DesignBibleWorkbookRules.md`
  - `EconomyRestrictedItems.md`
- Skills:
  - `skills/swlor-gui-window-generation/SKILL.md` for every new window.
  - `skills/swlor-blaster-models/SKILL.md` and `Readmes/BlasterModelImport.md` for the Trellis2 workflow.
  - `skills/swlor-release-notes/SKILL.md` at release.
- `design/animations/README.md` and `ANIMATION-PLAN.md` for authored animations. The tooling is in `tools/SWLOR.AnimationDrafts/`.
- The Bible tabs in `design/bible/SWLOR Design Bible.xlsx`:
  - Smithery, Engineering, Fabrication, Research, Agriculture, Gathering, Fishing, General.
  - Crafting Calc and the four recipe tabs.
  - System Changes & Migrations.

## Current system

Paths are under `SWLOR.Game.Server/`.

| Area | Files |
| --- | --- |
| Stations and recipe window | `Service/Craft.cs` (`UseCraftingDevice`, `RecipeSkillUnlockOffset = 3`, enhancements, blueprints, research costs `:1147-1195`), `Feature/GuiDefinition/ViewModel/RecipesViewModel.cs` |
| Minigame (being replaced) | `Feature/GuiDefinition/ViewModel/CraftViewModel.cs` (all rules inline: actions `:1727-1864`, CP `:446-454`, progress/quality `:1354-1384`, success `:1407-1500`, 65% loss on failure `:1601`), `Feature/GuiDefinition/CraftDefinition.cs`, `Service/CraftService/RecipeLevelChart.cs` |
| Recipes | `Service/CraftService/RecipeBuilder.cs`, `RecipeDetail.cs`, `Feature/RecipeDefinition/<Skill>RecipeDefinition/`. About 3,400 recipes: Engineering 1,826, Smithery 775, Fabrication 456, Agriculture 318, Espionage 15 |
| Research | `Feature/GuiDefinition/ViewModel/ResearchViewModel.cs`, `Entity/ResearchJob.cs`, `Craft.UseResearchTerminal` (`Craft.cs:980-1034`), `Craft.IsResearchableRecipe` (`:178`) |
| Gathering | `Feature/ItemDefinition/HarvesterItemDefinition.cs`, `Feature/ScavengePoint.cs`, `Feature/GuiDefinition/ViewModel/RefineryViewModel.cs`, `Feature/LootTableDefinition/HarvestingLootTableDefinition.cs`, `*ResourceSpawnDefinition.cs` |
| Fishing | `Service/Fishing.cs`, `Service/FishingService/`, `Feature/FishingLocationDefinition/`, `Feature/ItemDefinition/FishingRodItemDefinition.cs`, clips `sw_fishing6/7/8` (`design/animations/fishing/`) |
| Perks | `Feature/PerkDefinition/GatheringPerkDefinition.cs`, `FabricationPerkDefinition.cs`, `EngineeringPerkDefinition.cs`; direct `Perk.GetPerkLevel` checks in `Loot.cs`, `ScavengePoint.cs`, `HarvesterItemDefinition.cs`, `RefineryViewModel.cs`, `ResearchViewModel.cs`, `RecipesViewModel.cs:579`, `Craft.cs:553-561,986-992`, `Droid.cs`, `DroidAssemblyViewModel.cs:519`, `DroidAIViewModel.cs:121`, `DroidControlItemDefinition.cs:127`. Smithery and Agriculture have no registered perks today |
| Gear stats | Item properties Control 88, Craftsmanship 89, CPBonus 115 (subtypes 1-4 = Smithery, Engineering, Fabrication, Agriculture), applied in `Feature/EquipmentStats.cs:929-1075`, stored per skill on `Player.Control`/`Craftsmanship`/`CPBonus` |
| XP | `Service/Skill.XPChart.cs` (`GetDeltaXP`; 0 XP at 5+ ranks past the activity level) |
| Saved data | `Player.UnlockedRecipes`, `Player.CraftedRecipes` (first craft per recipe), `ResearchJob`, blueprint item properties (`ItemPropertyType.Blueprint`) |
| Stations | Placeables `smithery_bench`, `engineering_term`, `fabrication_term`, `cookpot`, `espionage_bench` (all `craft_on_used` with local `CRAFTING_SKILL_TYPE_ID`; the Espionage bench is module data only), `research_term`; StructureTypes Cookpot 219, EngineeringTerminal 220, FabricationTerminal 221, Refinery 222 (resref `cft_forge`), SmitheryBench 223, ResearchTerminal 272. Stations have no lock today: several players can craft at one station at once |
| NUI board precedent | `Feature/GuiDefinition/ViewModel/SlicingViewModel.cs` (Espionage slicing). No new minigame may copy slicing's tile-board play |
| Balance tooling precedent | `SWLOR.CombatSim/` |

## Shared framework

Build this first, because every minigame depends on it. It is a dependency, not a pilot.

### Rules engine and windows

- **One engine per minigame.** Each is a pure C# rules engine under `Service/CraftService/`, unit-testable without NUI.
  - Each engine owns its meters, actions, previews, opportunities and outcome.
  - Its view model only binds and forwards input.
  - Shared pieces live once: session lifecycle, previews, the outcome contract, quick craft, XP settlement and the help page.
- **Windows.** Each minigame has its own window, following the GUI guides. The recipe window (`RecipesViewModel`) still selects the recipe, enhancements and blueprint, then opens the skill's minigame.
- **Rules every minigame follows:**
  - **Previews.** Every action shows its exact cost and its result, or the result range when a chance is involved, before the click. One rules implementation feeds both the preview and the resolution.
  - **Budgets.** Each attempt has a limited action or resource budget.
  - **Visible randomness.** Success chances are visible, and the next state or opportunity is telegraphed at least one step ahead.
  - **Opportunities only.** Random events only create opportunities and never punish.
  - **One exception:** harvesting seams start hidden, and discovering them is the point.
- **Length targets:**
  - At-level manual craft: 10-20 actions, about 1-2 minutes.
  - Gathering node: 3-8 actions.
  - Fishing fight: 3-6 turns.
  - Research experiment: 5-8 turns.
- **Help.** Each window has a Help page, like slicing's `HelpPartial`. It opens automatically on a character's first use of that minigame; record first use per minigame on the player.

### Outcome contract

These rules apply to every item craft, in every minigame.

- **Success.** The craft succeeds when its progress meter completes, and the item is created at once. The action that would complete it shows a "Finishes the item" warning. Each minigame names its own progress meter.
- **Quality.** Every craft ends with a quality percentage.
- **Enhancements.** Each enhancement slot has a visible quality threshold.
  - Meeting the threshold guarantees the transfer. *Starting values:* first slot 50%, second slot 85%. A third slot (research level 10 adds one) needs its own value, set with the gate below.
  - **Enhancement parity** (owner decision after player feedback, October 2026). Today each enhancement transfers at a chance equal to quality, so 80% quality averages 2.4 of 3 mods; thresholds of 50/85/85 give exactly 1. Crafters report about 80% on triple-enhanced endgame crafts with best gear. CraftSim must model the extra difficulty enhancements add (today's progress penalty, and each minigame's equivalent) and tune the thresholds so the expected number of transferred enhancements per enhanced craft, at rank 50 with best gear and at level, is at least today's for one, two and three slots. Thresholds stay deterministic and visible.
  - This replaces today's per-enhancement chance roll (`quality / max quality`).
  - The window shows which thresholds the current quality meets.
- **Food.** Duration scales across the whole quality range, up to today's 100% bonus. This replaces the `(int)qualityPercent` branch that only paid at 100%.
- **XP and sale value.** Both keep today's formulas (`CraftViewModel.cs:1393-1432`), which scale with quality.
  - XP: base XP from recipe level against rank (`Skill.XPChart.cs`; Espionage's practice XP through `Craft.GetBaseRecipeXP`), +20% on a recipe's first craft, +50% per blueprint level, then +quality%.
  - Vendor value: `max(round(levelBonus × quality%), max(25, round(1.6 × level))) × 1.225`, with `levelBonus = 48 × (level / 10 + 1) + 9 × level`.
- **Failure.** The piece breaks when the minigame's integrity meter reaches 0. Each component stack and each selected enhancement then has today's 65% loss chance, and the window shows this rule before the materials are committed. A failed craft still gives 15% of the base XP, as today.
- **Blueprints.** Fees, licensed runs and blueprint bonuses behave as today.

### Gear stats

The three existing per-skill gear stats stay. No new item properties are added, and about 323 crafting-gear blueprints are untouched.

- **Craftsmanship** powers each minigame's progress actions.
- **Control** powers its quality actions.
- **CP** (gear CP + 0.75 × rank) is the action budget.
- **The +31 CP passive at rank 25** is re-tuned per minigame by the simulator.
- **Retired passives.** Today's hard-coded rank passives (+21 progress at rank 20, +115 quality at rank 40) and action unlock ranks (`CraftViewModel.cs:39-48`) are retired with the old minigame.
- **Action unlocks** (owner decision, October 2026), the same rule for every minigame:
  - Every action needed to play the loop is available from rank 0, including completion actions such as the Fabricator's Assemble.
  - The integrity-repair action unlocks at rank 10: Rebond (Forge), Patch (Circuit), Service (Fabricator) and Stabilize (Reaction). The Kitchen has none.
  - The once-per-craft finisher unlocks at rank 20. Only the Forge has one (Cryo-Quench).
  - Nothing else is gated by rank; rank changes power and CP only. Gathering, fishing and the Experiment have no rank-gated actions.
  - The Forge, Circuit and Reaction merged with other unlock ranks before this rule. Their phase-2 tuning PRs align them.
- **Espionage** has no crafting gear today and gets none. Its minigame scales from Espionage rank only.

### Per-recipe parameters

Every recipe gets its own minigame parameters.

- **The generator.** A deterministic generator under `tools/`, or `SWLOR.CLI`, computes each recipe's values:
  - It starts from the recipe's category and its components.
  - It then applies a fixed per-recipe variation within bounds, seeded by the recipe's identity, so values never change between runs.
  - Examples: heat range, cooling, overload line, sub-assembly sizes, flavor target, reaction cycle.
- **Storage.**
  - Values are written explicitly into the recipe definitions, through new `RecipeBuilder` methods and `RecipeDetail` fields, so they are reviewable and stable.
  - They are also written into new columns on the Bible recipe tabs. Extend `CombatUpgradeBibleRecipeParityTests` to cover them.
- **Coverage test.** Add a test that fails on any active recipe with missing parameters, values outside the generator's bounds, or a difficulty outside its level's budget.
- **Overrides.** A category or a single recipe may override generated values where it matters, and the override is recorded in code and the Bible.
- **Sequencing.** Foundation builds the generator, the `RecipeDetail`/`RecipeBuilder` extension point, the coverage-test framework and Bible-column support. Each minigame workstream then adds its own parameter type, builder method, Bible columns and generated values for its recipes.
- **Loop-built recipes.** About 400 recipes are created in loops or helpers (droid instructions and enhancements, trace fuses, slicing caches, concentrated venoms, a cooking loop). Their values go into the helper's data rows, still explicit and reviewable.
- **Bible tabs.** `CombatUpgradeBibleRecipeParityTests` resolves recipe-tab columns by header text, so new columns are safe; renaming a header breaks it. Espionage recipes sit on the Engineering (10) and Cooking (5) recipe tabs.

### Quick craft

- **Availability.** Quick craft unlocks when rank is at least recipe level + 5, the range where XP is already 0, and after one manual success of that recipe.
  - `Player.CraftedRecipes` counts, so recipes crafted before the revamp qualify.
- **Batches.** Up to 10 per batch, materials permitting. Each item takes a short crafting animation, and the player stays at the station.
  - Cancelling stops the batch. Completed items are kept and unused materials stay in the inventory.
- **Output.** The base item only, with no enhancements, no blueprint, no blueprint bonuses and no XP.
- **Threshold kept.** Players asked for easier bulk orders (October 2026). The owner kept rank >= recipe level + 5, so recipes above level 45 stay manual even at rank 50.
- **Tied to the XP cutoff.** Level + 5 is where `GetDeltaXP` stops paying XP. Workstream 11 kept that cutoff (the guild award and the crafting pace multiplier both use the chart), so quick craft never costs a player XP they could still earn.
  - Practice-limited recipes (Espionage) keep paying practice XP up to `PracticeRankLimit`, so the threshold is the higher of level + 5 and that limit. `QuickCraftRules.GetRequiredRank` already does this.

### Balance gates and tooling

Build `SWLOR.CraftSim` (tool and tests) on the `SWLOR.CombatSim` pattern. It drives the real rules engines rather than a copy of them, with repeatable seeds. A minigame ships only when all of these pass:

1. **No single rotation** (owner decision, October 2026; CraftSim still names the gate "No fixed rotation").
   - The player complaint was one rotation winning across every craft. A solver searches for the best single fixed action sequence per recipe band and gear level, searched across the band's sampled recipes (an action that is unavailable when its turn comes is skipped).
   - Adaptive play must beat it in average quality, by the target below, on every recipe band and gear level.
   - *Starting target:* at least 10 quality points.
   - The best fixed sequence for each recipe alone stays in the report as a metric and is not a pass/fail gate. The Forge and Fabricator show their full state and have almost no randomness, so a sequence per recipe that matches adaptive play always exists, and a gate on it could only be passed by adding the randomness players asked to reduce.
   - **Open (owner, October 2026).** Gate 1 is open for the Circuit, the Fabricator and perked builds (Forge, Chef). A prototype tried recipe-sized outputs, wider per-recipe splits, a per-recipe overload line and share-scaled Precision, then one repair per craft with wear scaled by gear: the best single fixed sequence still matched adaptive play (margins -2.2 to 0 on bands 11-20 and 41-53, with no gear and best gear). Cause: the minigames disable actions that would be wasted, and a fixed sequence skips unavailable actions, so a sequence that covers every recipe costs nothing extra. Closing it would need wasted actions to cost something; the owner chose not to. Do not retune numbers for gate 1; judge it from player feedback.
2. **Style balance.**
   - For every style pair, each style must be the better choice on at least 30% of recipes, or fish for the Angler.
   - Neither style may beat the other by more than about 5 quality points on average.
3. **Perks optional.** An unperked rank-50 crafter with good gear can reach max quality on at-level recipes through skilled play. Perks change how, and how efficiently.
4. **Length.** The targets above are met.
5. **Economy parity.**
   - Gathering yield per node and per scavenge site at each rank stays near today's expected yield. Quick Harvest and Quick Search equal it, and a skilled session stays within +15% of it (starting value).
   - Fish caught per bite at each rank stays near today's catch rate. **Quick Reel** is today's roll at a fixed Willpower score, so it passes by construction and its fish and XP per hour equal today's. An unperked skilled fight's expected catch rate stays within +15% (starting value) of Quick Reel's for the same fish and angler. The whole Angler line may lift it to about 90% of the fish at the angler's own level, with the fight perks at half strength on a rare fish and Trophy Catch the only full-strength rare-fish perk (owner decisions, October 2026; see Agriculture fishing).
   - Research base credits and times are unchanged; the Analyze cut stays within its cap.
   - Material use per crafted item at matched quality (owner decision, October 2026), per recipe band and gear level:
     - Where the legacy policy's success rate is at least 90% (starting value), component stacks per item stay within 10% of today's, either way.
     - Where it is lower, today's failures waste material, so the cell fails only if the new minigame uses more than 10% more than today. Lower use passes, and the report shows it.
     - The comparison covers the recipes the legacy policy makes at least once in the cell. A recipe it never makes has no legacy figure, so it is reported separately and not counted in the stacks per item.
6. **XP rate** (owner decision after player feedback, October 2026).
   - At matched rank and gear, XP per hour of play in each minigame is at least today's.
   - **Item-craft cohort**, identical for the legacy and new runs: each recipe band (1-10, 11-20, 21-30, 31-40, 41-53) with rank equal to recipe level, capped at rank 50 (levels 51-53 run at rank 50); no gear, mid and best gear tiers; the band's active recipes weighted equally; no blueprint and not a first craft; quality and failures as each policy actually plays them (legacy: the lookahead policy; new: CraftSim's adaptive policy), counting the 15% failure XP. Run it with no enhancements and with representative enhancements for each slot count the recipes support (one, two, three), applying each side's enhancement difficulty. Each band, gear tier and enhancement count must pass on its own.
   - **Activity cohorts:** each pairs the legacy loop with its replacement at the parity attribute values, using identical tool inputs:
     - Harvesting: per node tier, rank equal to the vein level, and the highest harvester tier that rank allows (`CraftRankMilestones`); XP per item.
     - Scavenging: per site level, rank equal to 10 × site level; XP per attempt, quarter on failure. No tool.
     - Fishing: per fish-level band (0-10, ..., 41-52), rank equal to fish level and capped at 50, and the highest rod tier that rank allows. Every fishing location's (rod, bait) tables reachable with that rod are weighted equally, half by day and half by night, so both runs draw the same fish mix. XP per bite. The cohort is the fight; Quick Reel pays today's XP per bite in today's time, so it needs no multiplier.
     - Refining has no minigame, and research XP is new, so neither has a legacy rate to match. Research XP is bounded instead by workstream 11's research ceiling.
   - CraftSim estimates minutes per craft, node, site or fight from actions taken and a seconds-per-action estimate, plus each loop's fixed time (fishing's 6-8 s cast wait, the harvester delay, window setup). Measure both in the client evidence, separately for each legacy loop and each new one.
   - If a loop falls short, scale its XP per craft, item, attempt or bite up, recorded in code and the Bible. Do not shorten it below its length target to pass.
   - **Legacy timing input:** Foundation 1d (PR #2803) commits today's actions per craft in `design/testing/crafting-revamp/legacy-crafting-baseline.md` (5-9 at levels 1-10, rising to 20-28 at levels 21-53 with gear) and the gathering baseline beside it. Re-run them with the committed seeds when the inputs change.
7. **Enhancement parity** (owner decision after player feedback, October 2026). With enhancement difficulty modeled for both the legacy model and each minigame, the expected number of transferred enhancements per enhanced craft at rank 50, best gear and at level is at least today's, for items with one, two and three enhancement slots. The gate reports the threshold values that meet it; the thresholds are shared by every minigame.

**Perked builds** (owner decision, October 2026; these apply to every minigame).
- (a) A perked build, a crafter who bought perk-line purchases (a style included), has an 8-action floor in every band and gear tier; the ceiling stays 20 (`LengthTarget.PerkedItemCraft`). Unperked builds keep the 10-action floor. Tune the perks to fit; do not slow the base minigame.
**Baselines.**
- **Legacy model.** CraftSim ports today's rules as a legacy baseline, checked formula by formula against the code: the old minigame (`CraftViewModel.cs`), harvesting, scavenging, refining and the fishing catch roll. Build it in Foundation, before Closeout deletes the old minigame.
- **Attributes.** Today's yields depend on Might (harvest bonus item), Perception (scavenge attempts and success) and Willpower (catch chance). Parity matches the median of active characters, measured once from a production data copy: Might and Perception among characters with Gathering rank 10 or more, Willpower among those with Agriculture rank 10 or more. Record the method and medians (aggregates only) in `design/testing/`, and delete every production-derived copy afterward.
  - The agent environment refused to load the production copy (October 2026), so the medians are not measured yet. Until the owner runs the measurement, report parity across an attribute sweep, and get the owner's reference value before workstream 9 ships.

Report inputs, seeds, results and limitations as `SkillPerkStandards.md` requires, and keep the reports under `design/testing/` (new folder).

## Minigames

These are the starting designs. Action names and meters are final unless the owner approves a change. Effect magnitudes are starting values the simulator tunes. Unless stated otherwise, every action costs the minigame's integrity meter a small amount; repair actions restore it.

### Smithery: the Plasma Forge

You keep the piece within its metal's thermal working range while shaping and finishing it. The setting is Star Wars, not fantasy: plasma forge, hydraulic press, vibro tools.

- **Meters:** Thermal (0-100, working range marked), Shape (progress), Finish (quality), Integrity, CP.
  - Thermal falls by the recipe's cooling rate after every action.
- **Actions:**
  - **Ramp Plasma:** raises Thermal, costs CP.
  - **Hydro-Press:** large Shape gain that rises with Thermal across the range. Below the range it does little and cracks Integrity; above it, it works more but cracks Integrity harder.
  - **Vibro-Hone:** Finish, best in the cool half of the range and weak outside it.
  - **Rebond:** molecular rebond that restores Integrity at a Thermal and CP cost.
  - **Cryo-Quench:** once per craft. A large Finish bonus if Thermal is in range, more near the center. Afterward the piece is set: Ramp Plasma and Hydro-Press are disabled, and Vibro-Hone works at a flat rate.
- **Telegraphed opportunity:** **Stable Containment** next turn: the next Hydro-Press or Vibro-Hone gets +50%.
- **Per-recipe parameters:**
  - Working range center and width, cooling rate, starting Thermal.
  - Shape/Finish split: armor needs more Shape, weapons more Finish.
  - Leather and synthweave goods use a low, wide range ("heat-sealing").
- **The decision:** press while it's hot or ramp first, and spend heat on shaping or let it cool into finishing range.

### Engineering: the Circuit

You build circuit load to amplify your work, then discharge it before it overloads. All Engineering recipes use it, including space recipes and droid parts.

- **Meters:** Load (0-10, overload line marked), Assembly (progress), Calibration (quality), Integrity, CP.
  - Each Load point raises output; *starting value* +10% per point.
- **Actions:**
  - **Wire:** Assembly, +1 Load.
  - **Tune:** Calibration, +1 Load, costs CP.
  - **Boost:** +3 Load, costs CP.
  - **Discharge:** spends all Load as a burst into Assembly or Calibration, the player's choice, scaled by the Load spent.
  - **Ground:** safely lowers Load, costs CP.
  - **Patch:** restores Integrity, costs CP.
- **Overload:**
  - Above the line, each action shows an overload chance that rises per point.
  - An overload costs a large amount of Integrity and resets Load to 0. The triggering action still resolves.
- **Telegraphed opportunity:** **Clean Signal**: the next Tune is doubled.
- **Per-recipe parameters:**
  - Component grade sets the overload line.
  - Category sets the per-Load multiplier: droid parts, enhancements and modules amplify differently.
  - The Assembly/Calibration split.
- **The decision:** push your luck for a bigger discharge, or bank it now.

### Fabrication: the Fabricator

You run an industrial fabricator with three tool heads to build the item's three sub-assemblies. It must not resemble slicing's tile board.

- **Meters:** progress and quality for each of Frame, Shell and Fittings; Machine Condition (integrity); CP.
- **Tool heads:**
  - Cutter serves Frame, Extruder serves Shell, Bonder serves Fittings.
  - One head is active at a time. **Retool** switches heads and costs CP.
- **Actions with the active head:**
  - **Run:** progress on the head's own sub-assembly, or half effect on another without retooling.
  - **Precision Pass:** quality on the head's own sub-assembly only.
  - **Service:** restores Machine Condition, costs CP.
- **Assemble:** available once all three sub-assemblies are complete; it finishes the item.
  - Quality is the average of the three, capped at the weakest + 20 points.
- **Telegraphed opportunity:** **Prime Feedstock** for one named head next turn: that head's next action gets +50%.
- **Per-recipe parameters:**
  - Sub-assembly sizes and quality shares: a bed is mostly Frame, a terminal mostly Fittings.
  - Wear rate from the materials.
- **The decision:** when to retool and when to work off-head, and which sub-assembly to lift.

### Agriculture cooking: the Kitchen

You steer a dish's flavor toward the recipe's target while it cooks. Pet food and cooking enhancements use it too.

- **Meters:**
  - A three-axis flavor profile (Savory, Sweet, Spice), shown against the recipe's target zone.
  - Doneness (progress).
  - Freshness (integrity; falls every turn, and the dish is ruined at 0).
  - CP.
- **Heat setting:**
  - Low, Medium or High, changed with **Adjust Heat**, which takes a turn.
  - Low cooks slowly and holds flavor steady.
  - High cooks fast and drifts flavor each turn. The next drift is telegraphed.
- **Actions:**
  - **Add:** one of the recipe's committed ingredients, or a free pantry seasoning (salt, sugar, spice). Each push is shown as a vector and range, and Control narrows the range.
  - **Simmer:** Doneness at Low or Medium.
  - **Sear:** large Doneness at High, costs Freshness.
  - **Stir:** cancels the current drift, costs CP.
  - **Cover:** halves Freshness loss for 3 turns, costs CP.
- **Quality:** 100% inside the target's inner radius, falling to 0 at the outer radius.
- **Telegraphed opportunity:** **Peak Aroma**: the next Add is twice as strong.
- **Per-recipe parameters:**
  - Target center and radii, by food category: soups, roasts, sweets, pet food.
  - Ingredient flavor vectors from a component table.
  - Freshness maximum.
- **The decision:** what to add and when, and when to turn the heat up or down.

### Espionage: the Reaction

You work a volatile poison or trap mechanism through a visible reaction cycle.

- **Cycle:** Calm, Rising, Peak, Settling. It advances every turn, and the current phase plus the next two are shown. Poisons and traps use different cycle patterns.
- **Meters:** Potency (progress), Subtlety (quality), Stability (integrity), CP.
- **Actions:**
  - **Measure:** small Potency, safe in any phase.
  - **Charge:** large Potency, doubled at Peak. It costs heavy Stability unless the phase is Calm.
  - **Refine:** Subtlety, doubled in Settling and halved at Peak.
  - **Dampen:** costs CP; holds the current phase one more turn and restores a little Stability. Owner decision, October 2026: Dampen keeps its hold and also restores a little Stability. It wears the piece by nothing. Tuned with the gates: it restores 8 Stability and costs 3 CP for every 20 recipe levels (at least 2), a quarter of Stabilize's CP, so a crafter at the recipe's level has one to spare beside a Stabilize, and a crafter who has not learned Stabilize yet has a few.
  - **Stabilize:** restores Stability, costs CP.
- **Per-recipe parameters:** cycle pattern and length; tier sets Peak intensity.
- **Progression:**
  - It scales from Espionage rank only, with Espionage XP.
  - Poisoncraft, Trapcraft and Master Saboteur ownership gates and the character-type restriction stay.

### Fabrication research: the Experiment

Research stays a real-time job that runs offline, with today's credit costs and durations (`Craft.cs:1147-1195`). Each research level now starts with a short experiment.

Today's outcome per level (`ResearchViewModel.ClickCompleteJob`), plus a small chance of an extra guaranteed bonus at every level (0.2% at level 1 rising to 12.1% at level 10):

| Level | Grant today |
| --- | --- |
| 1, 6, 9 | Item bonus +1 |
| 2, 4 | Licensed run +1 |
| 3, 5, 8 | One random blueprint bonus, weighted 300:400:400: licensed runs +d3, credit reduction +d10%, time reduction +d10% |
| 7 | Guaranteed tier-3 bonus |
| 10 | Enhancement slot +1 |

1. **Start.** Starting a level charges credits exactly as today, then opens the experiment. Every level has one (owner decision, October 2026).
2. **Grants.**
   - **Fixed levels** (1, 2, 4, 6, 7, 9, 10) keep their grant. Their experiment earns the time cut, XP and the extra-bonus opportunity.
   - **Random levels** (3, 5, 8): licensed runs stay random (owner decision, October 2026). When the experiment opens, the level rolls whether it grants licensed runs at today's weight (300 of 1100, or 310 of 1150 with the enhancement-slot weights, as `AddBlueprintBonus` does today) with today's d3, and shows the result. Expected licensed runs per blueprint equal today's.
     - **Licensed runs rolled:** the level grants those runs, and the experiment there earns only the time cut, XP and the extra-bonus opportunity, like a fixed level.
     - **Otherwise** the candidates are credit reduction and time reduction only, and evidence decides which one wins and how large it is, within today's d10.
   - **Extra-bonus opportunity.** Today's extra guaranteed bonus is rolled at today's chance when the experiment opens. If it appears, it is shown and granted on completion. Evidence never raises its chance.
3. **Trials.** *Starting value:* 6 trials.
   - **Test** adds evidence to a candidate, with the success chance shown.
   - **Refine Method** costs Fabrication CP and raises the next Test's chance.
   - **Analyze** spends spare evidence to shorten the job. *Starting cap:* -25% of the job's time.
4. **Result.**
   - At a random level that did not roll licensed runs, the best-evidenced reduction is granted when the real-time job completes. A tie goes to one of the tied reductions at random.
   - Its magnitude is set by the evidence earned, within today's d10: 1% with one piece of evidence, 10% with six, never more.
   - **Leaving early.** Closing the window or logging out ends the experiment with the evidence earned so far. A random level with no evidence on either reduction falls back to today's roll between credit reduction and time reduction (400:400) and its d10.
5. **XP.** Fabrication XP is awarded for the experiment and on job completion, based on recipe level against Fabrication rank. Research gives no XP today.
6. **Gates and capacity.**
   - Research tier gates come from Fabrication rank (see Progression).
   - The Research Terminal stays Lab-only furniture.
   - Blueprints stay ordinary tradable items, so Fabricators can research for others.
7. **In-flight jobs.** Jobs started before the release finish under today's random roll.

### Gathering harvesting: Prospecting

Using a harvester on a vein or tree opens a short node session. Harvester charges are the action budget.

Today (`HarvesterItemDefinition.cs`): a node holds d4 harvests, set on the first harvest. Each harvest spends one charge for one item from the node's loot table, plus a bonus item at 25% per Might modifier point. Gems are ordinary loot-table entries (arkoxit nodes have none), and nodes have no lock.

- **Node:**
  - **Yield:** a pool of units. Tune the pool against today's d4 harvests per node for economy parity.
  - **Stability:** 100; the node collapses at 0 and the session ends.
  - **Seams:** 1-3 hidden ones, either a rich seam (extra ore roll) or a gem pocket (the node's gem entries).
- **Actions:**
  - **Survey:** reveals one seam. Gathering rank milestones reveal more and replace Perception.
  - **Strike:** 1-2 units, costs Stability.
  - **Heavy Strike:** 2-4 units, costs more Stability. It shows the chance of shattering an unsurveyed gem pocket. Gathering rank milestones add yield and replace Might.
  - **Extract:** claims a revealed seam for extra charges.
  - **Leave:** ends the session. The remaining yield stays for others.
  - **Quick Harvest** (owner decision after player feedback, October 2026): resolves the node without seams. It yields today's expected units and XP for that node and harvester, spends the matching charges, and takes as long as today's harvests of that node (one harvest delay per unit) with the harvest animation, so it never trains Gathering faster than today. Playing the session adds bounded upside (rich seams, gem pockets), which the parity gate caps.
- **Rules:**
  - One player works a node at a time; others see that it is in use.
  - Harvester and node tiers work as today, gated by rank.
  - XP stays `GetDeltaXP(activity level - rank)` × units.
- **Animations:** survey, strike, heavy strike, extract.

### Gathering scavenging: Search

A scavenge site opens a 3x3 search board. It is a choose-a-spot board, not slicing's tile puzzle.

Today (`ScavengePoint.cs`): one attempt, plus a second at `HardLook × 10 + PerceptionMod × 5`%. Each attempt succeeds on `d20 + PerceptionMod` against DC 6, rising by d3 after each attempt. Treasure Hunter adds +10 weight per level to rare-flagged entries only. `FULLY_HARVESTED` marks the site, not the player. Level-0 sites need no perk.

- **Board:**
  - Some spots are marked promising; Gathering rank sets how often a Search of promising ground finds, replacing Perception.
  - Gathering rank milestones set the number of searches, absorbing Hard Look (as built, the count is the same at every rank; see Board below).
  - Tune the search count for economy parity.
- **Actions:**
  - **Search:** rolls the site's loot table; promising spots roll better.
  - **Dig Deeper:** searches the same spot again for a rare item, at a shown risk of spoiling the spot.
  - **Sift:** costs one search; reveals which neighboring spots are promising.
  - **Quick Search** (owner decision after player feedback, October 2026): resolves the site at today's expected rolls and XP, with no Dig Deeper, taking as long as today's attempts. Playing the board adds bounded upside, which the parity gate caps.
- **Rules:**
  - Treasure Hunter's rare weight still applies.
  - XP and the once-per-site rule stay as today.
- **Animations:** search, dig. The board plays the authored search clip (Search and Sift) and dig clip (Dig Deeper).

**As built (workstream 9b; numbers are starting values, `SearchRules` and `CraftRankMilestones`, tuned by `design/testing/crafting-revamp/scavenging-search-parity.md`).**

- **Window.** Opening a site closes its inventory and opens the Search window with the help page, which opens by itself on a character's first use. Quick Search is its first and encouraged button and is only a choice before the first action, as Quick Reel is for a bite. What a visit found is put in the site's inventory when it ends, and the site then opens for the scavenger. Closing the window, moving away, combat, death or logout end a visit: the finds so far are kept, the searches not spent are lost, and the site stays searched. One scavenger works a site at a time.
- **Board.** 6 searches and 2 promising spots at every rank. The promising spots are hidden; a Search shows its own spot's ground and a Sift shows the spots above, below and beside it. Every action costs a search. A Search finds 8% on ordinary ground and, on promising ground, 29% from Gathering 0, then 32%, 36%, 39% and 43% from Gathering 10, 20, 30 and 40 (`CraftRankMilestones.PromisingChanceForRank`, which is how Gathering rank improves the board in place of Perception); the window shows both chances for ground it has not learned about, never the true one. The counts do not step with rank because one more search or promising spot is worth +0.1 to +0.2 of Quick Search's rolls, more than a tier of Quick Search rises (0.065), so a stepping count cannot keep the board at the same share of Quick Search at every rank.
- **Dig Deeper.** Only at a searched spot. It risks 30%, then 50%, then 70% of ruining the spot, finds 20% of the time when it does not, and rolls the site's loot table with the rare entries (the flagged ones, or the table's least likely) weighted x3. A spoiled spot gives nothing more.
- **Quick Search.** One attempt and a Hard Look-level chance of a second (10% a level, the level being the Gathering rank's tier), each against DC 6 rising by a d3, rolling the table once per success, at the parity reference of a Perception modifier of 0 (owner decision, October 22 2026; no new code reads an attribute). It takes 2 seconds for the visit (one animation, however many attempts), as today, and pays today's XP, so its expected rolls and XP per hour equal today's exactly.
- **XP.** Every action pays Gathering XP: what the site pays against the rank when a Search or a Dig Deeper finds, a quarter of it otherwise, and a Sift the quarter, times a multiplier by site level (1.05 for levels 1-2, 1.10 for level 3, 1.12 for levels 4-5) that keeps the board's XP per hour at 1.02-1.13x of today's. The game keeps 6 seconds between two actions (the first is free).
- **Cap.** Owner rule (October 2026): playing the board adds bounded upside over Quick Search, and the cap covers perked play too. An unperked skilled board finds 1.04-1.07x of Quick Search's rolls at every Gathering rank, and the Treasure Seeker line takes a skilled scavenger to at most 1.14x, inside the +15% cap, with its value in the rare finds (a dig rolls the table with the rare entries weighted x3, and Fine Trowel and Careful Hands raise that) rather than in the count of finds. Boards are 6 actions (target 3-8).

### Gathering refining

Refining stays a quick batch conversion with no minigame.

- **Gates:** ore tier gates come from Gathering rank.
- **Batch size:** each power core's batch grows with rank (see Progression).
- **XP:** keep today's flat XP per unit for each ore (25-150) at the ore's level, and scale it by the rank on harvesting's curve: `flat XP x GetDeltaXP(ore level - rank) / GetDeltaXP(0)` per unit (`GatheringLevels.RefiningXp`). An ore's level is the level of the vein of its tier (5, 15, 25, 35, 45). Refining XP per hour at level is today's; a refiner five ranks past the ore's level earns nothing, so cheap ore stops training high ranks.
- **Unrefinable ores:** Ochne, Croknor, Hemorgite and Bisteiss are not in the game. Leave their entries alone.

### Agriculture fishing: the Line

Casting, bait, location and time of day work as today, and still pick the fish (`FishingLocationDetail.GetRandomFish`). The existing `sw_fishing6/7/8` clips stay for the cast and wait. A bite now opens a choice between **Quick Reel** and a fight.

Today (`Fishing.cs`): a bite spends one bait, then lands the fish at `40 + (Agriculture rank - fish level) × 10 + 2 × Willpower score`%, clamped to 0-95. XP is awarded on every bite, caught or not. Rod tiers come from each rod's Agriculture `RequiresSkill` property (0/10/20/30/40).

**Quick Reel (owner decision, October 2026).** The player can resolve the bite at once: today's catch formula with the Willpower score fixed at one named constant (`FishingQuickReel.ReferenceWillpowerScore` = 10: today's roll for a character with no attribute investment, owner decision, October 2026), today's XP per bite with no gate 6 multiplier, bait spent as today, and no fight time. The window opens showing both choices, with Quick Reel as the first and encouraged button; it is only a choice before the first move of a fight, so a player cannot start a fight and fall back on the quick roll. Quick Reel's fish and XP per hour are today's.

**The fight is optional.** It pays today's XP at the bite and the rest of a multiplied XP for its time, and it may catch better, but an unperked skilled fight's expected catch rate is bounded to +15% (starting value, `FishingParityOptions.FightBoundRelative`) of Quick Reel's for the same fish and angler, plus 3 points for sampling error and for fish Quick Reel can hardly land. The adaptive CraftSim player stands for the skilled fight, and the bound covers the angler with no perks and each rank over the real pools. The Angler line lifts the fight further, to the owner's target of about 90% of the fish at the angler's own level (`FishingParityOptions.PerkedTargetPercent`); Quick Reel is not affected by perks. Fighting is for a slightly better catch (owner decision, October 2026): `LineRules.RungAtRank` is set so an unperked skilled fight lands about 1.03 times what Quick Reel lands at the fish's own level; a fish above the rank climbs the difficulty ladder faster, so the fight falls behind Quick Reel there and a fish Quick Reel cannot land is hopeless for the fight too; the Angler line fills all ten rows (Strong Line I-III, Hard Set I-III, Fish Sense I-II, the style and Trophy Catch, with no Steady Hold and Fish Sense limited to the first turns of a fight); and a rare fish takes the fight perks at half strength, rounded down, with Trophy Catch the only full-strength rare-fish perk.

- **Fish state:**
  - Distance: landed at 0, escapes at its maximum.
  - Stamina.
  - A telegraphed next move: Run, Dive, Thrash or Tire.
- **Your state:** line Tension. At its maximum the line snaps and the fish is lost; the bait is spent at the bite, as today.
- **Actions:**
  - **Reel:** closes Distance and adds Tension, a lot against a Run, extra Distance against Tire.
  - **Give Line:** lowers Tension and lets out Distance.
  - **Hold:** drains Stamina and is strong against Thrash.
  - **Set Hook:** first turn only.
- **Tuning:**
  - The rod tier and Agriculture rank set the Tension cap and reel strength, replacing Willpower.
  - Fish level against rank sets Stamina and aggression.
  - Rare fish get trickier move patterns.
- **Unchanged:** bait use, point bite counts, and respawn timers, which are randomized at restart on purpose. Today's XP is still awarded at the bite, caught or not; a fought bite adds the rest of its multiplied XP when the fight ends on its own.
- **Animations:** reel, give line, hold, land, added to the existing fishing animation set.

## Progression and perks

### Retired perks and their rank milestones

Retire every perk in this table, refund it (see Migration), and grant its effect to everyone at the same skill rank the perk required. Keep the `PerkType` members for the refund migration.

| Retired perk (PerkType) | Replacement for everyone |
| --- | --- |
| Harvesting I-V (157) | Harvester tiers 1-5 at Gathering 0/10/20/30/40 |
| Refining I-V (158) | Ore tiers 1-5 at Gathering 0/10/20/30/40 |
| Refinery Management I-VI (159) | Power cores refine +1 item at each of Gathering 0/10/20/30/40/45 (3 + 6 = 9 at 45) |
| Scavenging I-V (160) | Scavenge site levels 1-5 at Gathering 0/10/20/30/40 |
| Hard Look I-V (161) | Absorbed into the Search board's rank milestones (`CraftRankMilestones.SearchesForRank`) |
| Research I-V (297) | Research tiers 1-5 at Fabrication 10/20/30/40/50 (tier = recipe level / 10 + 1) |
| Research Projects I-II (299) | +1 concurrent research job at Fabrication 25 and 50 |
| Scientific Networking I-II (256) | +1 licensed run on new blueprints at Fabrication 25 and 50 |
| Droid Assembly I-V (264) | Droid tiers 1-5 at Engineering 0/10/20/30/40 |

Replace each direct `Perk.GetPerkLevel` gate in the files listed under Current system with a rank check. Update `GatheringPerkRegressionTests` and `ResearchPerkRegressionTests` (they pin the exact `GetPerkLevel` source strings) to the new rules, and remove the retired perks' entries from the `GatheringPerkCoverage`, `FabricationPerkCoverage` and `EngineeringPerkCoverage` data sources that `PerkCoverageTests` and `PerkSweepEngineTests` consume.

- **Retiring a perk.** Delete its definition and keep its `PerkType` member, as the retired Synthesis and Touch perks already are. `Perk.GetPerkLevel` throws for an undefined perk on an NPC (`Perk.cs:955`), so no call site may still query a retired perk.
- **Hard Look and the Search board.** The Search board absorbed Hard Look into its rank milestones (`CraftRankMilestones.SearchesForRank`, and the promising chance that steps at the same ranks). Quick Search keeps today's second-attempt chance through the rank milestone (Gathering 0/10/20/30/40 count as Hard Look 1-5, `HardLookLevelForRank`).
- **Droid Assembly** is checked in `Droid.cs`, `DroidAssemblyViewModel.cs`, `DroidAIViewModel.cs` and `DroidControlItemDefinition.cs`; all four move to the rank gate.

### Treasure Hunter and Creditfinder

- **Move** `TreasureHunter` (181) and `CreditFinder` (182) to `PerkCategoryType.General`, keeping their IDs and effects. Today they require Gathering 15/30/45 and 20/40/50.
- **Fix Treasure Hunter's text** while moving it: it promises "+10/20/30 chance", but the code adds +10 weight per level to rare-flagged loot entries only (`LootTable.cs:50-54`).
- **Swap their requirements onto Armor:**
  - Treasure Hunter I-III: Armor 20/40/50 at 3/3/4 SP.
  - Creditfinder I-III: Armor 15/30/45 at 3/3/4 SP.
- **Refund every owned rank explicitly in the migration** (Migration and release, "Refunds and rebuild token"). Their category and requirements changed, so owners re-buy them under the new requirements.

### Line schedule

The structure is the same for every crafting skill:

- **Two independent, optional lines per skill**, 25 SP each, 50 SP per skill.
- **Purchases:** 10 per line, at ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP.
- **Rank 20 is the line's style,** the crafting equivalent of a stance.
  - Only one style can be active at a time; it is toggled in the craft window.
  - In activity-split lines (Agriculture, Fabrication), the rank-20 purchase is a choice of two mutually exclusive styles through `PerkRequirementCannotHavePerk`. Switching costs a Perk Refund Token, like any perk.
- **Rank 50 is the capstone,** with no quest requirement.
- **Ranked purchases:** the rest of the line is ranked actives and traits built on the anchor perks below. Draft the full 10 rows per line in the Bible, following `SkillPerkStandards.md`, and get the owner's approval of the rows before coding them.
- **Perks grant StatType adjustments that the minigame engines read** (`CodingStandards.md`).
  - No perk-name switches in gameplay code.
  - Declare each new StatType's classification, polarity and category on its enum entry.
  - Reserve PerkType, StatType, icon and TLK IDs against current master and the HAK. Master has since used StatType 1089-1108, which PR #2393 had reserved, so do not reuse #2393's numbers. At `e77fca53` the next free PerkType is 844 and the next free StatType is 1168. Check `iprp_resperk.2da` for whether a new PerkType needs a row.
  - New perk icons follow `IconStandards.md`.
- **Each line's perks act only on its own skill's minigame or activity.** Espionage's Reaction takes no crafting perks.

### Lines

| Skill | Line | Anchors (style at 20, capstone at 50) |
| --- | --- | --- |
| Smithery | **Forgemaster** (fast, hot work) | Style *Hot Forging*: the working range shifts up 5, Hydro-Press +25%, Vibro-Hone -5%. *Heat Retention*: Thermal falls slower. *Overdrive Press*: a large Shape active. Capstone *Red Line*: once per craft, a Hydro-Press above the range costs no Integrity and gives double Shape. |
| Smithery | **Artisan** (precise finishing) | Style *Cold Working*: the range shifts down 5, Vibro-Hone +5%, Hydro-Press -5%. It mirrors Hot Forging's shape, not its numbers: the style balance gate (gate 2) needed a point of Finish worth more than a point of Shape, and gate 6 needed Cold Working's Hydro-Press cost small, so the two styles' bonuses and costs differ (see `smithery-perk-gates.md`). *Clean Containment*: Stable Containment appears more often. *Micro-Hone*: Finish that ignores Thermal. Capstone *Perfect Temper*: a Cryo-Quench at the range's center gives +50% Finish. |
| Engineering | **Overclocker** (high load) | Style *Overclock*: extra Load per action and a higher boost per point. *Heat Sinks*: the overload line rises by 2. *Cascade Discharge*: one Discharge into both meters. Capstone *Surge*: once per craft, an overload becomes a full-strength Discharge instead of costing Integrity. |
| Engineering | **Technician** (stable, precise) | Style *Safe Mode*: no overload chance until 2 past the line, but a lower boost per point. *Diagnostics*: the forecast shows two turns ahead. *Micro-Tune*: Calibration with no Load gain. Capstone *Failsafe*: the first time Integrity would reach 0, it stays at 1. |
| Gathering | **Prospector** (time and power cores) | Style *Strip Mining*: Strikes take less time at a higher Stability cost, and Surveys and Extracts take more. *Efficient Strikes* and *Rich Veins*: Strikes and seam work take less time. *Bulk Refining*: more items per power core. *Core Sample*: reveals every seam's type. Capstone *Mother Lode*: the first Strike or Heavy Strike of each node takes half the time. As built; see "Gathering lines as built" below. |
| Gathering | **Treasure Seeker** (rare finds) | Style *Careful Hands*: on the Search board, Searches find 1 point less often and Dig Deeper favors rare entries more. *Keen Eye*: Searches find 1 point more often. *Steady Dig*: Dig Deeper risks 15% less. *Deep Scan*: Survey reveals more seams. Capstone *Jackpot*: the first Dig Deeper find of each board rolls rare entries at a much higher weight. As built; see "Gathering lines as built" below. |
| Agriculture | **Chef** (cooking) | Style choice *Slow Cooking* (a Low heat Simmer gives 80% more Doneness, and the flavor drifts 25% less at Medium heat) or *Flash Cooking* (the flavor drifts 15% less at High heat). *Seasoned Palate*: a switch in the setup view that halves every Add of a committed ingredient (pantry pinches excepted); its second rank makes pushes 25% less random while it is on. *Reduce*: takes a turn and 12 CP (9 at the second rank) to pull the profile 20% (35%) of the way toward the target's center. Capstone *Signature Dish*: once per craft, snaps one flavor axis exactly onto the target's center (see `design/testing/crafting-revamp/agriculture-chef-perk-line-draft.md`). |
| Agriculture | **Angler** (fishing) | Style choice *Patient Angler* (Give Line lets out 1 less Distance) or *Aggressive Angler* (each Reel pulls the fish in 1 more Distance). *Strong Line I-III*: +2/+5/+8 Tension cap. *Hard Set I-III*: Set Hook drains 1/2/3 more Stamina. *Fish Sense I-II*: shows the fish's next two moves on the first 2/4 turns of a fight. Capstone *Trophy Catch*: a rare fish fights 4 steps easier on the difficulty ladder. The whole line lifts the skilled fight to about 90% of the fish at the angler's own level (owner decision, October 2026; the +15% bound over Quick Reel applies to the unperked fight only); the fight perks work at half strength, rounded down, on a rare fish, Fish Sense not at all, and Trophy Catch is the only rare-fish perk at full strength; no Angler perk touches Quick Reel (see `design/testing/crafting-revamp/agriculture-angler-perks.md`). |
| Fabrication | **Builder** (Fabricator) | Style choice *Production Run* (Run +60%, Precision Pass -3%) or *Fine Tolerance* (Precision Pass +25%, Run -25%). *Quick Change*: cheaper Retool. *Dual Feed*: one Run advances two sub-assemblies at 60% each. *Field Service*: Service restores more. Capstone *Master Assembler*: the Assemble cap rises from weakest + 20 to weakest + 35. |
| Fabrication | **Researcher** (Experiment) | Style choice *Methodical* (+2 trials, -15% Test chance) or *Intuitive* (+20% Test chance, -1 trial). *Broader Hypotheses*: +1 trial. *Peer Review*: moves half of one candidate's evidence to the other, in two directions (credit to time, and time to credit), only where evidence decides between credit and time reduction. *Efficient Analysis*: Analyze cuts more time. Capstone *Breakthrough*: a free action, once per experiment, that makes the next Test succeed automatically and doubles what each piece of spare evidence cuts from the job's time, inside the 25% cap. It grants no extra bonuses. (Broader Hypotheses, Breakthrough, Intuitive and Peer Review changed by the owner in October 2026: the random pool has only two evidence-decided candidates, credit and time reduction, so there is nothing to draw a third candidate or a redraw from. A style may move the experiment's length off the 5-8 turn target by one or two turns. The style balance study of 23 October 2026 found *Broader Hypotheses* favors Intuitive, whose Tests find evidence more often, and a purchase that spends CP does nothing for a researcher with no gear until their rank pays for it. The order is Field Notes I (5), Efficient Analysis I (10), Field Notes II (15), Broader Hypotheses (25), Efficient Analysis II (30), Steady Hands (35), Peer Review (40), Second Opinion (45), and *Field Notes* gives +10 and +20 points.) |

**Gathering lines as built (October 2026).** A skilled Prospecting session already claims nearly every seam its Stability allows and yields 1.14x of today's items per node, so under the +15% cap no Prospector perk adds an item, a gem or a roll or saves Stability, and none cuts the session's actions (the 3-8 target). The Prospector line therefore saves time (*Efficient Strikes*, *Rich Veins*, *Mother Lode*: the first Strike or Heavy Strike of each node takes half the time) and power cores (*Bulk Refining* +1/+2/+3, 12 per core at most), *Core Sample* shows what a node holds, and *Strip Mining* makes Strikes quicker at the price of Stability and slower seam work. The Treasure Seeker line spends the rare weights of the Search board (*Fine Trowel*, *Rich Pickings*, *Jackpot*, *Careful Hands*) and keeps *Sharp Chisel* and *Deep Scan* for Prospecting; its gem rows are gone. Careful Hands acts on the Search board only. See `design/testing/crafting-revamp/gathering-perks.md`.

Espionage gets no new lines.

## Migration and release

Follow `Migrations.md`.

### One migration per release

The newest migrations today are server `_29_CorrectStoredCarapaceBracers` and player `_24_CorrectCarapaceBracers`, both pending as of October 19, 2026. The revamp releases much later, so write its character migration as player version 25 on the feature branch and update `MigrationDataTests`, which pins the released and pending versions. At release, ask the owner which versions shipped, and fold the revamp into the newest unshipped one if that is no longer 25.

### Refunds and rebuild token (required)

Every character gets SP back for each perk this rework removes or changes, plus one rebuild token.

- **Rebuilding is optional.** The token only gives players the option. Do not force a rebuild: leave `Player.RebuildComplete` alone.
- **Refund every perk in this table explicitly.** Treasure Hunter and Creditfinder stay in the game, but their category and requirements changed, so they are refunded too and their owners re-buy them under the new requirements.

| Perk (PerkType ID) | Ranks | SP per rank on current master | Full refund |
| --- | --- | --- | --- |
| Harvesting (157) | I-V | 1/1/2/3/4 | 11 |
| Refining (158) | I-V | 1/1/2/3/4 | 11 |
| RefineryManagement (159) | I-VI | 1/1/2/2/3/4 | 13 |
| Scavenging (160) | I-V | 1/1/2/3/4 | 11 |
| HardLook (161) | I-V | 1/1/2/3/3 | 10 |
| TreasureHunter (181) | I-III | 3/3/4 | 10 |
| CreditFinder (182) | I-III | 3/3/4 | 10 |
| Research (297) | I-V | 2/2/3/3/4 | 14 |
| ScientificNetworking (256) | I-II | 3/4 | 7 |
| ResearchProjects (299) | I-II | 2/3 | 5 |
| DroidAssembly (264) | I-V | 1/1/2/3/4 | 11 |

**Steps.** Do both the refund and the token in this release's character migration's `MigratePlayerData(Player)`. It runs once per character record, including characters whose players have not logged in. `_15_RemoveObsoleteCombatInstructionDiscs` grants its token there.

1. For each perk in the table that the record owns:
   - Add the price of every owned rank to `Player.UnallocatedSP`.
   - Remove the perk from `Player.Perks`, and from `Player.UnlockedPerks` if present.
   - Add a line to `Player.UpgradeNotices` naming the perk and the SP returned.
2. Grant the token with `RebuildTokenGrant.Grant(player)`.
3. Add a notice that one rebuild token was granted and that rebuilding is optional.

**Price source.** Price refunds from a dedicated table in the migration: the values above, read from master `e77fca53`. Re-check them against master when you write the migration.

- Not from perk definitions: the retired definitions are deleted.
- Not from `LegacyPerkRefundMigration`'s historical table: its prices for Harvesting, Refining, Refinery Management, Research, Scientific Networking and Droid Assembly are older than what players paid after the combat upgrade's rebuild. Droid Assembly, for example, is 3 SP per rank there.

**Rules.**
- Keep the retired `PerkType` members and never reuse their IDs.
- **"Changed" includes any existing perk.** If any workstream changes an existing perk's behavior, requirements, category or price, add that perk to this table in the same PR.
  - Espionage's Poisoncraft, Trapcraft and Master Saboteur are unchanged, so they are not refunded unless a workstream changes them.
  - The new crafting lines are new purchases and need no refund.
- This refund departs from `Migrations.md`'s rule against one-off refund migrations, at the owner's request. Update that line in the same PR to record the exception.

**Tests.** Start from legacy records that own each perk at each rank, and check that the migration:
- refunds exactly the table's SP;
- removes the perks;
- grants exactly one token, including to characters that own none of the perks;
- adds the notices;
- changes nothing on a second run.

### Saved data

- `Player.CraftedRecipes` stays and now gates quick craft.
- Record first-use flags for each minigame's help page.
- `ResearchJob` gains whatever the experiment needs to store: the chosen candidate, its magnitude and the time cut. Jobs created before the release finish under the old roll.
- Existing blueprints and crafted items are not changed.
- Any enum rename needs its migration (`Migrations.md`, "Enum renames"). Prefer adding members over renaming.

### Bible and release notes

- Update the Bible's System Changes & Migrations tab.
- Write release notes with `skills/swlor-release-notes/SKILL.md`.

## Art and animation

### Stations

The seven stations get new models made with the Trellis2 workflow. They are replaced in place: resrefs, tags, scripts and StructureTypes stay the same, so no placed station in the module or in player properties moves or breaks. Stations use a retro-futuristic Star Wars "used future" look (light grey and off-white panelling with small red or yellow accents, dense greebles, light wear, small inset screens, physical controls), not sleek modern designs and not rusty or military-green ones.

| Station | New look |
| --- | --- |
| Smithery | Plasma forge |
| Engineering | Droid and electronics workbench: grey and off-white panelled bench with a wiring back panel, dials, inset green readouts, droid parts and a circuit board on the work surface |
| Fabrication | Enclosed industrial fabricator, about 1.8 m tall, grey with a yellow stripe: an open bay of tool heads and pistons over a work bed, vents and a control box |
| Cooking | Starship galley unit: dark and mid grey cabinet with two round heating plates and a grill, a vent hood, pipe runs, rows of buttons, a small inset readout and storage doors |
| Espionage | Covert chemistry bench: mid grey cabinet with a rail of vials and canisters, a gauge, push buttons and indicator lamps |
| Research | Rebel-base analysis console: slanted grey and off-white console with an inset green screen, buttons, toggles, dials and a sample drawer |
| Gathering | Refinery |

- **Adapting Trellis2.** The documented workflow (`Readmes/BlasterModelImport.md`, `tools/workflows/trellis2_*.json`, `tools/Trellis*.py`) covers weapons and shields. Adapting it to placeables is part of this work: an image-to-GLB workflow, Blender export to MDL, walkmesh, textures and placeables.2da.
- **Documentation.** Record the placeable procedure beside the weapon one in that readme.
- **Where the work lands:**
  - Model and 2DA work goes in the HAK.
  - Furniture-type stations keep their StructureType mapping.
  - Check whether the Refinery appearance is a placeable or a structure before swapping it.

### Animations

Author these with the established animation tooling (`design/animations/`, `tools/SWLOR.AnimationDrafts/`) for the playable species on the normal phenotype only:

- **Stations:** one looping work animation per station craft (Smithery, Engineering, Fabrication, cooking, Espionage), also used by quick craft.
- **Gathering:** survey, strike, heavy strike, extract, search, dig.
- **Fishing:** reel, give line, hold, land. Reuse the existing `sw_fishing6/7/8` cast-and-wait clips.

Every model and animation needs client evidence (`InEngineVisualVerification.md`).

## Fixes and things left alone

**Fix, in the workstream that touches each system:**
- **Research terminal message.** It says "Another player's incubation job is active" (`Craft.cs:1021`). It should name a research job.
- **Bible SP totals.** The Engineering, Fabrication, Agriculture and Gathering tabs show cached SP totals that disagree with their rows, and some SP Price cells are stored as text. Fix them with the workbook rules, then rewrite those tabs for the new lines.
- **Refining XP.** Scale today's flat XP by the rank-difference curve, as above.
- **Character sheet tooltips.** They describe a retired "auto-craft chance" (`CharacterSheetDefinition.cs:258-259`). Rewrite them for the new stat roles.
- **Player guide text.** Update the crafting, gathering, fishing and research text in `PlayerGuideViewModel.cs`, keeping `PlayerGuideContentTests` passing.
- **Old minigame defects.** These disappear when the old minigame is deleted; make sure none survives in shared code:
  - CP labels: Careful Synthesis and Precise Touch show 15 CP but spend 10.
  - The max-progress sign errors.
  - Veneration spending its step on a refused action.
  - The food duration cast that only paid at 100%.
  - The unused `DifficultyAdjustment`.
- **Harvesting bonus-item chance.** `d100 <= 5*MightMod*5` is removed with the attribute dependency.

**Leave alone:**
- NPC store equipment.
- The four unavailable ores.
- Fishing point respawn randomization.
- Espionage's perks and slicing.
- Lightsaber workbenches.
- Ship content.
- Droid assembly beyond its rank gate.
- Item stats.
- Recipe components.

## Delivery

The combat upgrade's model applies, with no pilot: build every minigame, then test them together.

- **Branch.** Create one long-lived feature branch from up-to-date `origin/master`, for example `feature/crafting-revamp`; `feature/crafting-upgrade` was #2393's. Pair it with a companion HAK branch.
- **PRs.** Land each workstream as its own reviewable PR into the feature branch, following `Readmes/PullRequests.md`: Codex review loop, resolved threads, base merged in.
  - Keep each PR under about 100 files so automated review can run; #2393's 133 files exceeded the limit.
- **Release.** Merge the feature branch to master once for one player release. That release carries the migration, refunds, rebuild token and Treasure Hunter/Creditfinder move.
- **The Bible across parallel PRs.** Branches that edit the `.xlsx` cannot be merged, and workstreams 2-9 all edit it.
  - Each PR makes its Bible edits through a re-runnable script committed with it, following `tools/UpdateSlicingDesignBible.ps1` (cell-level zip/XML edits).
  - Merge Bible-editing PRs into the feature branch one at a time. After a conflict, take the base's workbook and re-run the PR's script.
- **Merging master in.** Merge `origin/master` into the feature branch regularly, and re-run the focused tests after each merge.

**Workstreams.** 1 comes first; 2-11 can run in parallel after it; 12 closes.

1. **Foundation**, as four PRs that can run in parallel:
   - **1a Progression and migration:** rank milestones replacing retired perks, Treasure Hunter/Creditfinder move, migration with refunds and token, the `Migrations.md` exception, research message fix, Bible SP total fix and the Bible perk-row changes. The only Foundation PR that edits the Bible.
   - **1b Session framework:** rules-engine contract, session lifecycle and persistence, outcome contract, quick craft, help pages and first-use flags, and routing from the recipe window to each skill's minigame (the old Craft window until that skill's minigame lands).
   - **1c Parameter generator:** the generator, the `RecipeDetail`/`RecipeBuilder` extension point, the coverage-test framework and Bible-column support.
   - **1d CraftSim:** the project, the legacy baseline, gate and report framework, and the attribute medians from production data.
2. **Smithery:** Plasma Forge, plus the Forgemaster and Artisan lines.
3. **Engineering:** Circuit, plus the Overclocker and Technician lines.
4. **Fabrication:** Fabricator, plus the Builder line.
5. **Research:** Experiment, plus the Researcher line.
6. **Cooking:** Kitchen, plus the Chef line.
7. **Fishing:** Line, plus the Angler line and the fishing animations.
8. **Espionage:** Reaction.
9. **Gathering:** Prospecting, Search and refining changes, plus the Prospector and Treasure Seeker lines and the gathering animations.
10. **Art:** Trellis2 placeable adaptation, seven station models, and the five station work loops.
11. **Progression pace:** crafting XP speed and the crafting guilds' task cap. It can run beside 2-10. Owner decisions (October 2026), after research. Phase 1 (PR #2819) lifted the task cap and measured the pace in `design/testing/crafting-revamp/progression-pace-baseline.md`; phase 2 (11b) applied the three XP decisions below:
    - **Today.** One XP table serves every skill; rank 50 costs 411,125 XP, about 340-420 at-level crafts plus 2,000-4,000 components, with Gathering needing its own 411,125. Guild rank 5 needs 45,000 GP (about 240 hand-in tasks), and each tier offers only 10 tasks per server boot (`Guild.cs:127-169`, `GuildTasksViewModel.cs:140-146`), so rank 5 takes at least 14-21 days. RP XP pays about 3,000 XP per hour with no materials.
    - **Guild task cap.** For the Smithery, Engineering, Fabrication and Agriculture guilds, completing a task no longer hides it until the next boot; the board refreshes after completion. GP thresholds, GP per task and guild rank gates stay as they are. The Hunter's Guild is unchanged.
    - **Guild tasks give XP (decided, applied in 11b).** Each Smithery, Engineering, Fabrication and Agriculture hand-in awards 50% of `GetDeltaXP(task level - rank)` per item handed in, in that guild's skill (`GuildTaskXp`, `GuildTaskXpReward`). The task level is the recipe level for crafted items and the fish's `FishType` level for Agriculture catch tasks (such as `agr_tsk_017`). The XP goes through the crafting XP path, so Social and food bonuses apply, and it is not multiplied by the crafting pace. The chart's cutoff stays, so a hand-in five or more levels below the rank pays nothing, as crafting does and as quick craft opens. The Hunter's Guild gives none.
    - **XP speed: measure, then set a target.** Model today's hours to rank 50 for each crafting skill in CraftSim, including the gathering that supplies it, then bring the owner a target and the crafting-only multiplier or curve that meets it. Measure the XP-rate gate against today's XP first, then apply the change on top.
    - **Crafting pace x1.05 (decided, applied in 11b).** `CraftMinigameXp.CraftingPaceMultiplier` multiplies the base XP of crafts in Smithery, Engineering, Fabrication and Agriculture's cooking by 1.05, layered on top of each minigame's gate-6 band multiplier (`CraftMinigameXp.MultiplierFor`). Gate 6 stays measured against today's XP, before this multiplier. It does not apply to Gathering, Espionage, fishing, guild hand-ins, research, combat or Piloting. With the guild award and this multiplier the median crafting skill reaches rank 50 in 43.8 focused hours at the model's unmeasured timings, against 58.6 today (Smithery 100.1 to 67.9, Engineering 63.7 to 48.4, Fabrication 53.4 to 39.3, Agriculture 36.5 to 30.6; see the report). Required XP per rank is unchanged, so this changes gains only.
    - **Quick gathering in the model.** The hours-to-50 model includes Quick Harvest and Quick Search cohorts as well as the played sessions, and reports Gathering XP per hour for each; neither may exceed today's.
    - **Guild tasks in the model.** With the cap lifted, hand-ins become a repeatable XP source. The hours-to-50 model includes a representative task cohort (task tier for the rank, item quantity, how the item is obtained, including quick craft, and turn-in time) and reports hand-in XP's share of the pace, so the proposed award neither dominates nor vanishes.
    - **Research ceiling (decided, applied in 11b).** Research gives Fabrication XP. The model includes a research cohort (experiment time, job duration, concurrent jobs at Fabrication 25 and 50, blueprint acquisition), and research XP per real hour stays below at-level Fabrication crafting XP per hour. The Research workstream's starting values (100% of the chart XP for the experiment, 50% for completion) broke that ceiling at Fabrication 10, so `ResearchXpRules` now pays 62% and 31%, each still raised 10% per research level. Re-run `SWLOR.CraftSim progression` after any change to research or crafting XP.
    - **Isolate the change.** `Skill.GetRequiredXP` and `GetDeltaXP` take no skill and also drive combat and Piloting, and guild code is shared with the Hunter's Guild. Changes apply only to the crafting skills and crafting guilds, through crafting-specific tables or a skill or guild parameter, with tests that combat, Piloting and Hunter's Guild progression are unchanged.
    - **Espionage is excluded** from the pace change: its crafting XP stays tied to Espionage rank, and `Craft.GetBaseRecipeXP`, `EspionageInfiltration.CalculateXp` and slicing's `Skill.CalculateEspionageXP` share Espionage progression. Add regression tests that infiltration and slicing XP are unchanged.
    - **Existing progress.** If required XP per rank changes, convert stored XP and ranks (a migration, with tests on legacy records) so no character loses a rank. Guild points and ranks need no conversion, because their thresholds do not change. The 11b decisions change what is gained, never what a rank costs, so they need no migration.
12. **Closeout.**
    - Delete the old minigame (`CraftViewModel` rules, `CraftDefinition`, retired level-chart fields). Done: every crafting skill routes to its minigame and nothing opens the old window. `GuiWindowType.Craft` stays reserved, because saved window geometry keys on its name.
    - Final Bible sync, player guide text and tooltips.
    - Joint test pass across every minigame.
    - Release notes.

## Verification

- **Build and test.** Build once with `-p:RunPostBuildEvent=Never -v q -clp:ErrorsOnly`, then run focused filters for each change.
- **New unit tests.**
  - Each rules engine: every action, preview, opportunity, threshold, failure and lifecycle path.
  - Quick craft.
  - Per-recipe parameter coverage.
  - Rank milestones.
  - Perk StatType effects, including style exclusivity and switching.
  - Migration from legacy data, refunds and the token (`Migrations.md`, Testing).
- **Existing tests to keep green or update:**
  - `CraftTests`, `RecipeSearchTests`, `RecipeLearningSourceTests`, `FoodRecipeBalanceTests`
  - `EquipmentRecipeProgressionTests`, `RecipeCategoryBaseItemTests`, `CombatUpgradeBibleRecipeParityTests`
  - `GatheringPerkRegressionTests`, `ResearchPerkRegressionTests`
  - `EconomyObtainabilityCoverageTests`, `RareLootBalanceTests`, `PlayerGuideContentTests`
  - `PerkRankProgressionTests`, and the GUI layout validation tests.
- **Engine tests:**
  - `FishingBaitEngineTests`
  - `GatheringPerkCoverage` and `FabricationPerkCoverage`
  - New engine coverage for each station flow, node sessions, the fight window, research jobs, and migration on saved characters.
- **Balance evidence:** CraftSim reports for every gate, stored under `design/testing/`.
- **Client evidence:** every new window, action, help page, station model and animation, and a quick-craft batch, through `InEngineVisualVerification.md`.
- **Handoff reports.** Each handoff states separately:
  - Module repack, HAK and TLK rebuild, and server deploy.
  - What was observed in the client.
  - Anything unverified, with the reason.

## Defaults

The owner did not decide these explicitly; they follow today's behavior or the nearest established rule. Raise any that turn out wrong.

- **Ending a craft early.** Closing the window, logging out, dying, moving away or a server restart mid-craft ends the attempt as a failure under the 65% rule, as today. Other players cannot bump a crafter off a station (fixed in #2776; keep it fixed).
  - Save the session's committed materials, enhancements and blueprint on the player record, so a logout or restart can settle it. Today they are held only in memory.
- **Shared stations.** Crafting stations stay shared, as today: each crafter has their own session (owner decision, October 2026). Gathering nodes are exclusive per session, and research terminals stay one job per terminal.
- **Thresholds.** Enhancement thresholds are shown per slot. An item with one slot uses the first threshold.
- **Pantry seasonings** in the Kitchen are free and unlimited, with small pushes; recipe ingredients push harder and are limited to the committed quantity.
- **Research XP** is awarded for the experiment and on completion. The other research rules are as stated above.
- **Fishing XP** is awarded per bite. Quick Reel pays today's XP; a fought bite pays today's at the bite and the rest of its band multiplier when the fight ends on its own, in proportion to how long it lasted (owner decision after player feedback, October 2026).

## Declined after player feedback

The owner reviewed these in October 2026 and declined them. Do not reintroduce them without new approval.

- Quick craft at recipe level (it stays at level + 5).
- Batch crafting several copies of one recipe in one minigame.
- A beginner gate requiring a simple policy to finish at-level crafts.

Taking crafting skills out of the 400-rank skill cap, or capping how many crafts a character may train, is outside this revamp's scope.

## Prior attempts

- **[zunath/SWLOR_NWN#2393](https://github.com/zunath/SWLOR_NWN/pull/2393)**, "Add adaptive crafting and distinct profession perk trees", closed unmerged. Its plan is readable at commit `d9538da6` as `SWLOR.Game.Server/Readmes/CraftingEngagementPlan.md`.
  - **What it did:** it kept one shared action set and added recipe profiles, a random condition deck, and 20 SP perk trees.
  - **Keep its good ideas:** exact action previews, opportunities-only randomness, turn-based pacing, a rules engine separate from the view model, and simulator-backed gates.
  - **Do not repeat:** one shared action set across crafts, identity coming only from perks, and a single oversized PR.
- **[zunath/SWLOR_NWN#2056](https://github.com/zunath/SWLOR_NWN/pull/2056)**, "Add Farming system", closed unmerged. Farming is out of scope.
