# Combat Upgrade Implementation Plan

Last reviewed against the Combat Upgrade Bible on 2026-05-05.

## Source Of Truth

The authoritative design source for this work is the Google Sheet:

https://docs.google.com/spreadsheets/d/1rppEkwp2dX0oGKY1ftSbDTcg7GhopODseqbDb4cpNSU/edit?gid=207006097#gid=207006097

Use this sheet as the combat upgrade Bible. Experimental branches are implementation references only; if they conflict with the Bible, the Bible wins.

Do not carry over the Heavy Armor activation-time penalty. That mechanic was removed from the plan and should be ignored even if it appears in experimental branches or partial local carryover.

Do not carry over the refactor-project branch as part of this plan.

## Tabs Reviewed

All visible tabs were exported/reviewed, not only the linked tab:

- System Changes & Migrations
- Character Stats
- Status Effects
- Armor
- Vibroblade
- Vibroknife
- Lightsaber
- Heavy Vibroblade
- Spear
- Twin Blade
- Saberstaff
- Katar
- Staff
- Pistol
- Rifle
- Throwing
- Force
- Devices
- Beast Mastery
- Beast Calcs
- Beast Levels
- Beast Lookups
- Beast Purity Calc
- Piloting
- Leadership
- First Aid
- Smithery
- Engineering
- Fabrication
- Research
- Agriculture
- Gathering
- Smithery Recipes
- Engineering Recipes
- Cooking Recipes
- Fabrication Recipes
- Fishing
- Equipment - Weapons
- Equipment - Armor
- Equipment - Crafting
- Equipment - Enhancements
- Equipment - Droids
- Starships
- Hit Rate
- Damage Calc
- Crafting Calc
- World NPCs
- XP Chart
- Merits

## Scope Exclusions

The following Bible areas are intentionally out of scope for this combat upgrade and must not be counted as missing combat-upgrade work:

- Espionage
- Farming, including Agriculture/Farming-only rows such as Crop Management

If future audits compare every visible Bible tab against code, filter these areas out of the required-work totals. Agriculture rows that support Cooking or combat-upgrade-adjacent crafting can still be in scope when they are explicitly tied to the combat upgrade, but Farming-only mechanics are not.

## Hard Rules

- Every combat upgrade implementation decision must be traceable to a row or formula in the Bible.
- If a player-facing perk is currently implemented in code but does not exist in the Bible, remove its implementation from the feature branch.
- Perk removal means removing the perk definition, ability definition, feat grant/use path, migration/refund handling, 2DA/TLK exposure when applicable, and any combat/stat hooks that only exist for that removed perk.
- Existing implemented perks that remain in the Bible must be updated to the Bible's name, skill requirement, SP price, character type, type, resource cost, activation time, cooldown, description, status effects, and notes.
- Experimental branches may be used to copy working implementation mechanics, such as telegraphs, status-effect service patterns, attack-delay hooks, and data-entry examples, but only after confirming the Bible still calls for that behavior.
- Heavy Armor activation-time penalty support must be excluded or removed before release.
- Migration must force every player through the rebuild flow without requiring a rebuild token.

## Bible-Level Scope

### System And Migration

Implement the system changes listed in `System Changes & Migrations`:

- Equipment requirements move from proficiency perks to prerequisite skill levels.
- Launch requires a free full rebuild for all characters, implemented as a forced rebuild flow rather than token distribution.
- Elemental damage applies to the whole attack instead of bypassing defenses.
- Elemental resistances scale from 0% to 100%, with 100% fully resisting that damage type.

Acceptance criteria:

- Equipping all relevant weapons, armor, crafting equipment, droids, and enhancements checks skill prerequisites from the Bible.
- Old proficiency purchases are refunded or made irrelevant through the forced rebuild.
- Damage and resistance calculations match the `Damage Calc`, `Hit Rate`, and `Status Effects` tabs.

### Character Stats And Combat Math

Implement `Character Stats`, `Hit Rate`, and `Damage Calc` as the baseline formulas for stats, accuracy, evasion, attack, defense, critical damage, and damage ranges.

Acceptance criteria:

- Stat descriptions and derived effects match the Bible.
- Accuracy/evasion produce the expected hit-rate table behavior.
- Damage calculations use the Bible's attack/defense ratio behavior and critical damage rows.
- Elemental resistance is integrated with the same mitigation model instead of bypassing defenses.

### Status Effects

Implement only the statuses in `Status Effects`, and use the XM-style status-effect service for new or converted combat-upgrade effects where practical.

Acceptance criteria:

- Each Bible status has one implementation path and one authoritative behavior definition.
- Existing legacy effects not present in the Bible are removed or isolated from player-facing combat upgrade perks.
- Telegraphs are used where the relevant perk/ability design calls for delayed, shaped, or area-targeted effects.

### Weapon And Combat Perk Trees

Implement the full weapon tabs as written:

- Vibroblade
- Vibroknife
- Lightsaber
- Heavy Vibroblade
- Spear
- Twin Blade
- Saberstaff
- Katar
- Staff
- Pistol
- Rifle
- Throwing

The initial sheet read shows 36-37 perk rows per weapon tab, and most weapon tabs are marked `Not Implemented` in the Bible. Treat this as the largest remaining feature block.

Acceptance criteria:

- All Bible perks exist in the correct skill category with correct rank/SP/requirements.
- Existing old perks not represented in these tabs are removed from the feature branch.
- Ability definitions, recast groups, activation times, resource costs, scaling, status applications, and requirements match the Bible.
- Weapon delay values from `Equipment - Weapons` are reflected in haks/item properties and server-side attack delay calculations.

### Armor

Implement `Armor` exactly as written, including General, Heavy, and Light perk rows.

Do not implement or retain the old Heavy Armor activation-time penalty. Bible armor mechanics include heavy/light armor traits and equipment prerequisites, not a blanket activation-time penalty.

Open decision: Armor is effectively a required skill under the combat upgrade, so choose one of these paths before finalizing skill-cap and SP behavior:

- Armor skill levels do not count toward the 350 skill cap, and Armor does not grant SP.
- Players receive an extra 50 skill points to account for Armor effectively being required.

Acceptance criteria:

- Armor equip requirements use skill prerequisites from the Bible.
- Armor perk definitions match Bible rows.
- Any existing heavy-armor activation-time penalty hooks are removed before the feature branch is considered complete.

### Force, Devices, Leadership, First Aid

Update these tabs to Bible values:

- Force
- Devices
- Leadership
- First Aid

The Bible marks many Force, Devices, First Aid, and Leadership rows as implemented, but existing code still needs a value-by-value audit because implementation status in the sheet is not proof the feature branch matches current Bible numbers.

Acceptance criteria:

- Existing abilities are updated to Bible costs, cooldowns, activation times, ranges, DCs, statuses, and scaling.
- Perks present in code but absent from the Bible are removed.
- Status-effect conversions remain consistent with the XM-style service work already carried into the feature branch.

### Beast Mastery

Implement `Beast Mastery` using the supporting `Beast Calcs`, `Beast Levels`, `Beast Lookups`, and `Beast Purity Calc` tabs.

Acceptance criteria:

- Beast perk rows match the Bible.
- Beast level, stat, purity, and lookup formulas match the supporting calculation tabs.
- Beast attack delay interactions continue to respect the combat-upgrade attack cadence work.

### Crafting, Gathering, Equipment, Recipes, Starships

Update non-combat and support systems only where the Bible requires them:

- Smithery, Engineering, Fabrication, Research, Agriculture, Gathering
- Smithery Recipes, Engineering Recipes, Cooking Recipes, Fabrication Recipes
- Fishing
- Equipment - Weapons, Equipment - Armor, Equipment - Crafting, Equipment - Enhancements, Equipment - Droids
- Starships
- Crafting Calc
- World NPCs
- XP Chart
- Merits

Disregard Farming-specific Agriculture rows for this combat upgrade. Do not implement or retain Crop Management or other Farming-only perk definitions as part of this branch.

Acceptance criteria:

- Equipment stats, credits, tiers, damage, delay, armor, and prerequisite skills match the Bible.
- Recipe requirements and outputs match the Bible.
- XP and merit adjustments are implemented only where the Bible requires them.
- Do not pull unrelated experimental branch changes unless tied directly to a Bible row.

## Implementation Phases

### Phase 1: Bible Manifest And Diff

Create a generated manifest from the Bible CSV exports:

- Tab name
- Row number
- Perk or item name
- Skill/category
- Type
- Requirements
- Costs
- Activation/cooldown
- Dev status
- Notes

Then generate a code-to-Bible diff:

- Bible perks missing from code
- Code perks missing from Bible
- Matching perks with mismatched values
- Haks/TLK/2DA entries that are obsolete or missing

No gameplay implementation should start until this diff exists.

### Phase 2: Removal Pass

Remove player-facing perks and abilities that are implemented in code but absent from the Bible.

Required cleanup surfaces:

- `Feature/PerkDefinition/*`
- `Feature/AbilityDefinition/*`
- `Service/PerkService/PerkType.cs`
- feat enums and `feat.2da`
- TLK strings
- ability registration
- status-effect definitions used only by removed perks
- migration refund mappings
- droid/default perk grants

### Phase 3: Core Combat Math

Implement the Bible's stat, hit-rate, damage, elemental damage, elemental resistance, and attack cadence rules.

Use the already-carried attack-delay/native work as the starting point, then reconcile it against `Equipment - Weapons` delay values and Bible attack-speed perks.

### Phase 4: Equipment Requirements And Migration

Move equipment gating to prerequisite skill levels.

Migration must:

- Refund or obsolete old proficiency perks.
- Force every player through the rebuild flow by setting `RebuildComplete = false`.
- Avoid rebuild-token distribution.
- Unequip or validate gear that no longer meets Bible requirements.
- Update all item definitions and generated item data to use the new skill restriction item properties instead of legacy proficiency requirements.
- Preserve a documented rollback/recovery path for failed migrations.

### Phase 5: Perk Tree Implementation

Implement Bible perk trees in this order:

1. Armor and weapon tabs, because they drive combat identity and equipment requirements.
2. Force, Devices, Leadership, and First Aid.
3. Beast Mastery with supporting beast calculation tabs.
4. Crafting/gathering/equipment/recipe support tabs.
5. Starships, XP, merits, and world NPC adjustments.

Each perk should be implemented row-by-row with an acceptance note linking it back to the Bible tab and row.

### Phase 6: Haks And Data Files

Apply haks changes only where required by the Bible:

- weapon delays and item properties
- feat rows
- TLK strings
- shader/telegraph support assets
- item and equipment 2DAs

The restored shader should remain only if the telegraph implementation still depends on it and it validates in game.

### Phase 7: Verification

Before release:

- Build `SWLOR.Game.Server`.
- Validate all 2DA and TLK edits.
- Run a script that confirms every Bible perk exists and every code perk is present in the Bible.
- Smoke-test forced rebuild login flow.
- Smoke-test equipment gating for weapon, armor, crafting, enhancement, and droid equipment.
- Smoke-test weapon delay cadence by weapon family.
- Smoke-test elemental damage and 0%-100% resistance behavior.
- Smoke-test representative combat perks from every weapon tab.
- Smoke-test Force, Devices, Leadership, First Aid, and Beast Mastery.

## Current Known Local Follow-Up

The feature branch previously contained partial Heavy Armor activation-time penalty carryover:

- `UsePerkFeat` applies a heavy armor activation penalty.
- `AbilityDetail` and `AbilityBuilder` contain `IgnoreHeavyArmorPenalty` / `UnaffectedByHeavyArmor`.
- Several First Aid abilities call `UnaffectedByHeavyArmor`.

Per the current Bible direction, these should be removed in the implementation phase unless the Bible is later changed to explicitly restore that mechanic.

Status: removed from the feature branch implementation on 2026-05-05.

## Generated Audit Artifacts

- `CombatUpgradePerkAudit.csv` compares Bible perk rows against current perk definitions by normalized perk name.
- `CombatUpgradeBiblePerkManifest.csv` is the exported perk-row manifest from all Bible tabs with perk tables.
- Current audit summary:
  - Bible perk rows: 1082
  - Current code perk definitions: 263
  - Bible rows missing from code: 463
  - Code perk definitions missing from Bible: 64

The audit is intentionally a work queue, not final truth. Some rows need human review where the Bible consolidated names, changed categories, or converted old proficiency concepts into skill prerequisites.

Audit totals should exclude Espionage and Farming-only rows. If an all-tab export includes those rows, keep them in the raw manifest for traceability but omit them from combat-upgrade missing-work counts.

Implemented cleanup so far:

- Removed Heavy Armor activation-time penalty support.
- Removed `Dash` as a player-facing perk/ability because it is implemented in code but absent from the Bible.

Additional follow-up:

- Add droid instruction discs for the combat-upgrade perk set after the core perk implementation is stable.
- Audit and update all existing items so weapons, armor, crafting equipment, enhancements, and droid equipment use the new skill restriction requirements consistently.
