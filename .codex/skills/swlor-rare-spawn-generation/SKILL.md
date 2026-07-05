---
name: swlor-rare-spawn-generation
description: Use when adding or modifying SWLOR weighted rare spawn-table enemies, named elite variants, Bible-backed NPC stats, unique rare loot tables/items, module UTC/UTI assets, spawn definition wiring, and focused validation in C:\Projects\SWLOR_NWN.
---

# SWLOR Rare Spawn Generation

## Overview

Use this skill when a task adds or changes rare enemies selected from SWLOR spawn tables, especially named variants or elite versions of existing creatures. The expected pattern is a normal weighted spawn-table entry marked as rare, backed by Design Bible NPC stats and focused tests.

## Workflow

1. Inspect the target area spawn definition, the nearest existing creature UTC/UTI assets, and the Design Bible row for the base creature before editing. If AGENTS or the user asks for grill-me behavior, ask only one blocking design question at a time; answer repo-discoverable questions by inspection.
2. Keep rare spawn selection on the existing weighted system. Do not add a separate guaranteed spawn chance unless the user explicitly asks for one.
3. Add or reuse spawn-system support for a rare marker only if it is not already present. The intended contract is:
   - `SpawnObject.IsRare` stores the marker.
   - `SpawnTableBuilder.AsRare()` marks the active entry.
   - `SpawnTable.GetNextSpawn(includeRareSpawns)` filters rare entries only when rare spawns are currently suppressed, then still calls `Random.GetRandomWeightedIndex(weights)`.
   - `Spawn` records `ActiveSpawn.IsRare` and suppresses additional rare entries from the same spawn table while a rare from that table is active in that area.
4. Wire the target spawn table with the same fluent pattern as nearby entries:

```csharp
.AddSpawn(ObjectType.Creature, "rare_resref")
.WithFrequency(1)
.AsRare()
.RandomlyWalks()
.ReturnsHome();
```

5. Avoid changing NWN spawn wrapper scripts such as `nw_c2_default*` or wrapper waypoint behavior for rare table selection. Rare selection belongs in the C# spawn services unless the task has direct evidence that script behavior is the actual problem.

## Bible Stats

Use `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx` as the stats source when creating named or elite enemies.

- For `World NPCs`, hand-enter columns `A:H`, `AE:AL`, and `AP:AR` as appropriate. Preserve formulas in `I:AD` and `AM:AO`.
- Choose level, rank, role, species, and modifier from the Bible tables. Confirm the derived values from `Enemy Stat Presets`, `Enemy Resistance Packages`, `Enemy Ability Packages`, and `Enemy Modifiers`.
- Resistance formulas clamp package values to non-negative before applying adjustment columns. Enter negative vulnerabilities explicitly in `AE:AL` when the final NPC should have a negative resistance.
- Extend filters and data validations when a new row is added.
- After editing the workbook, run:

```powershell
powershell -ExecutionPolicy Bypass -File tools\UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible
```

## Creature Assets

Prefer copying the closest existing creature, stat skin, and creature weapon assets, then narrowing the edits.

- Keep new resrefs within NWN limits and consistent with nearby assets.
- Update UTC `Tag`, `TemplateResRef`, display name, description, hit points, ability scores, class level, feats, and equipped creature slots.
- Creature weapon slot is `32768`; creature armor/stat-skin slot is `131072`.
- Put `DMG` and `Delay` item properties on the equipped creature weapon. Do not put delay on the stat skin.
- Weapon `Delay` item properties use normalized cost-table values, such as `24` for a creature weapon whose Bible delay is `240`; verify with `ModuleWeaponDelayProperties_AreNormalized`.
- Put NPC level, HP, stamina, FP, attack, force attack, evasion, defenses, and resistances on the stat skin using the existing custom item-property IDs.
- Encode negative resistance cost values with the runtime resistance encoding pattern and verify by decoding in tests.
- Preserve quest group, loot table, AI, appearance, soundset, and beast metadata from the base creature unless the concept requires a deliberate change.

## Unique Rare Loot

When a named rare enemy needs unique loot, add a dedicated loot table for that enemy instead of overloading the base creature's normal or rare table.

- Keep the loot table weighted like existing loot tables. Use `LootTableBuilder.Create(...).IsRare()` and add each unique drop with `.AddItem(resref, frequency, maxQuantity, isRare: true)` so normal weighted selection and Treasure Hunter rare-item weighting still apply.
- Follow the existing loot-definition organization: declare each loot table directly with fluent `_builder.Create(...).AddItem(...)` chains in the relevant creature or named-elite method. Do not hide item entries behind generic helper methods, resref arrays, or loops such as `CreateRecipeLoot`.
- Create new item resrefs and new `Module/uti/*.uti.json` assets for the unique drops. Do not satisfy a unique-drop request by reusing existing items.
- Use 10-20 unique drops when the user asks for a broad named-rare pool. Equal weights are acceptable when every unique item should be equally likely; otherwise use explicit weights that match the intended rarity split.
- Prefer recipe or schematic drops instead of finished gear when the reward should support the crafting economy. Recipe pools do not all need to be equipment: mix in appropriate crafting domains such as Fabrication furniture/decor, Engineering schematics, or other trade outputs when they better fit the named enemy and level range.
- Place every new recipe enum in the correct `RecipeType.cs` section for its crafting skill, and document it on the matching Design Bible recipe sheet such as `Smithery Recipes`, `Fabrication Recipes`, `Cooking Recipes`, or `Engineering Recipes`.
- Place recipe definition classes in the existing craft-skill folder pattern and name them for the recipe family, set, product line, or concrete item they define. Do not lump unrelated recipes into planet/rarity aggregate classes such as `ViscaraRareEliteSmitheryRecipes`. Production recipe declarations must use explicit fluent `_builder.Create(...).Category(...).Resref(...)` blocks like the existing recipe files; do not compress recipe entries into helper calls, tuple arrays, generic entry records, or other packed table formats.
- Do not name unique rare loot after the named elite. Player-facing UTI names, descriptions, dropped recipe names, crafted output names, guaranteed component names, tool names, status-effect names, and the new `RecipeType` identifiers should use reusable thematic item identities instead of the elite's name, alias, or title. Descriptions should explain the item's function or material use, not who dropped it.
- When broad named-rare recipe pools are requested, review whether at least one drop should support non-equipment trades. Organic enemies are good candidates for Agriculture/Cooking food recipes using `RecipeEnhancementType.Food`; humanoid, droid, or tech-themed enemies are good candidates for Engineering schematics. Engineering outputs must either be directly usable items with registered item-use behavior, or be consumed by an existing follow-on recipe. Avoid dead-end `RecipeCategoryType.DroidComponent` rewards unless the new component is immediately wired into droid recipes.
- Engineering tool outputs should use creative reusable names. If these outputs grant direct temporary buffs or restores, place every such tool in one shared item-use recast group with a minimum 5 minute cooldown so players cannot keep the effects up indefinitely by cycling tools.
- Use the existing `RECIPE` item convention: recipe UTI tag `RECIPE`, `TemplateResRef` set to the recipe item resref, and a `RECIPES` local string containing the numeric `RecipeType`.
- For recipe-driven rewards, keep the finished gear as crafted output assets, register locked recipes with `.RequirementUnlocked()`, and require an encounter-specific component when the item should stay tied to that rare enemy. Add a guaranteed component drop if the recipe should be usable after trading with a crafter instead of requiring many repeat kills.
- To guarantee exactly one unique rare item, add one UTC loot local for the named enemy using the dedicated table at `100,1`, such as `VISCARA_EXAMPLE_RARES,100,1`.
- To allow one additional low-chance unique item, add a second UTC loot local for the same table with a low chance and one attempt, such as `VISCARA_EXAMPLE_RARES,10,1`. Do not raise the guaranteed entry's attempt count, because that makes multiple guaranteed drops possible.
- Keep unique item stats appropriate for the enemy's level range and only slightly better than nearby normal loot unless the user asks for a larger power jump. Prefer existing slot, property, required-skill, and appearance conventions from nearby items.
- If adding weapons, include normalized `Delay` and appropriate `DMG` item properties. If adding wearables, avoid weapon-only properties such as `DMG`, `Delay`, and `UnlimitedAmmunition`.

## Validation

Add focused tests that protect both content and reusable behavior.

- Spawn definition tests should assert the rare entry resref, object type, weight, `IsRare`, and that existing normal weights remain unchanged.
- If spawn infrastructure changed, add a focused system test proving rare entries still use weighted selection and that active rare spawns suppress additional rare entries from the same table/area.
- NPC balance tests should verify the UTC equips the expected skin/weapon, the stat skin and weapon match Bible values, resistance values decode correctly, and the UTC feat list matches the selected ability package.
- Workbook tests should verify the new World NPC row inputs, formulas, resistance adjustment cells, filter range, and data-validation range.
- Unique-loot tests should verify the dedicated rare table exists, every unique drop is marked rare, the table has the requested item count, the UTC has one guaranteed `100,1` roll and any requested low-chance `chance,1` roll, and the new UTI files match the expected level, slot, stat, and required-skill conventions.
- Recipe-drop tests should verify dropped recipe UTI files use tag `RECIPE`, point at registered `RecipeType` IDs through `RECIPES`, recipes are `.RequirementUnlocked()`, recipes produce the intended new item resrefs, and any encounter component is both referenced by recipes and dropped by the named rare. For Engineering recipes, tests should also prove the output is usable through `IItemListDefinition` or is consumed by another recipe.
- Run the narrow affected tests first, then the project test suite when feasible.

