# Combat Upgrade Implementation Plan

Last reviewed against the Combat Upgrade Bible on 2026-08-17.

## Source Of Truth

The authoritative design source for this work is the Google Sheet:

https://docs.google.com/spreadsheets/d/1rppEkwp2dX0oGKY1ftSbDTcg7GhopODseqbDb4cpNSU/edit?gid=207006097#gid=207006097

Use this sheet as the combat upgrade Bible. Experimental branches are implementation references only; if they conflict with the Bible, the Bible wins.

The checked-in local workbook snapshot is `design\bible\SWLOR Design Bible - Combat Upgrade.xlsx`. Regenerate the local manifest and audit from that workbook with `tools\UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible`.

The skill-cap decision is settled: the combat-upgrade cap is 400. Armor remains an active skill for equipment requirements and contributes to the cap/SP flow like other active skills; it is not a weapon-style perk tree.

Current Bible `General` perks use Armor skill requirements because Armor is the closest thing SWLOR has to a general character-level proxy. These General rows are valid when they are present in the current Bible and should not be confused with obsolete Heavy/Light/General Armor perk-tree rows from older snapshots.

Do not carry over the Heavy Armor activation-time penalty. That mechanic was removed from the plan and should be ignored even if it appears in experimental branches or partial local carryover.

Do not carry over the refactor-project branch as part of this plan.

## Current Status

The scoped static implementation phases in this document are complete: Bible rows, generated perk/ability code, stat consumers, feat/spell/TLK/icon/recast wiring, hotbar cooldown visuals, telegraph infrastructure, and migration entry points are implemented with no current scoped audit findings. The phase descriptions below remain as architecture and acceptance-criteria reference; they are not an open implementation queue. Use `CombatUpgradeImplementationStatus.md` for the current mechanic status and `CapstoneQuestLinePlan.md` for the remaining eight world-content packages.

## Tabs Reviewed

All visible tabs were exported/reviewed, not only the linked tab:

- System Changes & Migrations
- Character Stats
- Status Effects
- Espionage
- Mimicry
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

The only Bible perk rows intentionally outside the combat-upgrade implementation surface are the five unimplemented Agriculture rows such as Crop Management. Agriculture is deferred beyond the first iteration.

Espionage and Mimicry are mandatory review scope. Future audits must include every Espionage perk row, every Mimicry core perk row, and every Mimicry technique row. The five current Agriculture rows are unimplemented and explicitly excluded from the first iteration; do not count them as unreviewed combat-upgrade work.

## Hard Rules

- Every combat upgrade implementation decision must be traceable to a row or formula in the Bible.
- Espionage and Mimicry must be reviewed with the same row-by-row implementation, text, balance, targeting, feat/spell, TLK, and icon checks as every other included category.
- If a player-facing perk is currently implemented in code but does not exist in the Bible, remove its implementation from the feature branch.
- Perk removal means removing the perk definition, ability definition, feat grant/use path, migration/refund handling, 2DA/TLK exposure when applicable, and any combat/stat hooks that only exist for that removed perk.
- Existing implemented perks that remain in the Bible must be updated to the Bible's name, skill requirement, SP price, character type, type, resource cost, activation time, cooldown, description, status effects, and notes.
- Experimental branches may be used to copy working implementation mechanics, such as telegraphs, status-effect service patterns, attack-delay hooks, and data-entry examples, but only after confirming the Bible still calls for that behavior.
- Heavy Armor activation-time penalty support must be excluded or removed before release.
- Migration must force every player through the rebuild flow without requiring a rebuild token.
- The skill cap is 400. Do not reintroduce a 350-cap/Armor-exception split or make Armor exempt from normal active-skill SP behavior.
- Combat-upgrade ability and perk scaling must be balanced around the practical player stat band. A focused character is expected to reach 26 in one ability stat, with rare 27 cases when the build uses a racial stat point. Food and other temporary item effects can push a stat a little higher for short windows, so scaling formulas must remain bounded above that normal band: either clamp at the documented cap or use an explicit soft-overcap rule. Do not tune baseline perk/ability values around temporary food-buffed stats.
- Weapon combat styles should target 4-6 distinct active buttons per style, counting rank-replacement chains as one button and counting Combat, Stance, Toggle, and Aura rows as active. Devices and Force are explicit exceptions to that weapon-style button ceiling. Use `CombatUpgradeActivePerkBudgetReview.md` as the current design pass for active-to-trait conversions before changing the Bible workbook or generated active ability surfaces.

## Bible-Level Scope

### System And Migration

Implement the system changes listed in `System Changes & Migrations`:

- Equipment requirements move from proficiency perks to prerequisite skill levels.
- Launch requires a free full rebuild for all characters, implemented as a forced rebuild flow rather than token distribution.
- Elemental damage applies to the whole attack instead of bypassing defenses.
- Resistances use a direct -100 to 100 percentage scale. Elemental resistances reduce matching elemental damage, hostile status duration, and matching status damage ticks. Status-family resistances reduce matching hostile status duration and matching status damage ticks. Negative scores increase matching effects, and 100 grants temporary immunity while active. Player totals from gear, food, perks, auras, and smaller stacked buffs cap below immunity unless an active finite-duration status explicitly grants 100 resistance.
- Resistance item properties must follow NWN cost-table storage. Persisted `CostValue` entries are non-negative row ids in SWLOR's `iprp_swlrescost.2da` cost table `54`: rows `0` through `100` represent positive or zero resistance, and rows `101` through `200` decode to vulnerability amounts `-1` through `-100`.

Acceptance criteria:

- Equipping all relevant weapons, armor, crafting equipment, droids, and enhancements checks skill prerequisites from the Bible.
- Old proficiency purchases are refunded or made irrelevant through the forced rebuild.
- Damage and resistance calculations match the `Damage Calc`, `Hit Rate`, `Status Effects`, and `Character Stats` tabs.

### Character Stats And Combat Math

Implement `Character Stats`, `Hit Rate`, and `Damage Calc` as the baseline formulas for stats, accuracy, evasion, attack, defense, critical damage, and damage ranges.

Acceptance criteria:

- Stat descriptions and derived effects match the Bible.
- Accuracy/evasion produce the expected hit-rate table behavior.
- Damage calculations use the Bible's attack/defense ratio behavior and critical damage rows.
- Elemental damage uses the normal Physical/Force defense path first, then applies the matching resistance after damage is calculated.
- Resistance-targeting enemy abilities are represented in the Bible `NPC Abilities`, `Enemy Ability Packages`, and `World NPCs` sheets so placement and source-of-truth documentation stay aligned.

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

As of the 2026-05-23 local-workbook audit refresh, the scoped perk/ability/recast/static-status audit findings for these tabs are closed. Treat the remaining weapon-tree work as live behavior validation and release hardening rather than missing row implementation.

Acceptance criteria:

- All Bible perks exist in the correct skill category with correct rank/SP/requirements.
- Existing old perks not represented in these tabs are removed from the feature branch.
- Ability definitions, recast groups, activation times, resource costs, scaling, status applications, and requirements match the Bible.
- Weapon delay values from `Equipment - Weapons` are reflected in haks/item properties and server-side attack delay calculations; natural creature weapons intentionally use the fastest-category delay so haste still has room above the engine floor.

### Armor

Armor is a core equipment skill and general character-level proxy, not a weapon-style combat-upgrade perk tree. Current Bible `General` perks may use Armor skill requirements for character-level gating. Do not implement obsolete Heavy Armor, Light Armor, or older Armor perk-tree rows from stale Bible/workbook snapshots; those rows should not be counted as remaining combat-upgrade work.

Do not implement or retain the old Heavy Armor activation-time penalty. Armor mechanics include equipment prerequisites, normal SP/cap progression, and current Bible General perks that use Armor requirements; they do not include a blanket activation-time penalty or Heavy/Light Armor specialization unlocks.

The cap/SP decision is final:

- The new skill cap is 400.
- Armor contributes to the skill cap.
- Armor grants SP through the normal active-skill rank-up path.

Acceptance criteria:

- Armor equip requirements use skill prerequisites from the Bible.
- Current Bible General perks use Armor skill requirements when they are intended to act as general character-level gates.
- No obsolete Heavy Armor, Light Armor, or stale Armor perk-tree definitions, active feats, instruction discs, or UI entries remain in the final combat-upgrade surface.
- Any existing heavy-armor activation-time penalty hooks are removed before the feature branch is considered complete.

### Force, Devices, Leadership, First Aid

Update these tabs to Bible values:

- Force
- Devices
- Leadership
- First Aid

The static audit is clean for the current scoped Force, Devices, Leadership, and First Aid rows. Continue to spot-check live values during playtest because implementation status in the sheet is not a substitute for runtime behavior validation.

Acceptance criteria:

- Existing abilities are updated to Bible costs, cooldowns, activation times, ranges, DCs, statuses, and scaling.
- Perks present in code but absent from the Bible are removed.
- Status-effect conversions remain consistent with the XM-style service work already carried into the feature branch.

### Espionage And Mimicry

Implement and audit the complete `Espionage` and `Mimicry` tabs. Espionage includes Tradecraft, Infiltrator, and Saboteur. Mimicry includes the core analyzer perks and all 88 techniques, including passive traits, stances, damage abilities, control, support, and threat tools.

Acceptance criteria:

- Every included row has an implemented perk or technique surface with matching price, requirements, type, resources, casting time, cooldown, scaling, description, notes, and status behavior.
- Every active surface has matching feat/spell/TLK data and correct aimed-versus-self-centered targeting metadata.
- Mimicry technique tier, slot cost, analyzer source feat, loadout limits, damage scaling, status duration, and unique icon are validated.
- Espionage stealth, Back Attack, slicing, disguise, Venom, trap, and cap mechanics match their documented formulas and limits.

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
- Equipment - Weapons, Equipment - Armor, Equipment - Enhancements, Equipment - Droids
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

Create a generated manifest from the local Bible workbook or Bible CSV exports:

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

1. Weapon tabs, because they drive combat identity.
2. Force, Devices, Leadership, First Aid, Espionage, and Mimicry.
3. Beast Mastery with supporting beast calculation tabs.
4. Crafting/gathering/equipment/recipe support tabs, including Armor equipment requirements.
5. Starships, XP, merits, and world NPC adjustments.

Each perk should be implemented row-by-row with an acceptance note linking it back to the Bible tab and row.

### Phase 6: Haks And Data Files

Apply haks changes only where required by the Bible:

- weapon delays and item properties
- feat rows
- TLK strings
- shader/telegraph support assets
- item and equipment 2DAs
- Hotbar cooldown readiness feedback is implemented:
  - `tools/GenerateCooldownIcons.ps1` generates the standardized `pr0_` through `pr5_` cooldown variants from every source ability icon with ImageMagick.
  - `AbilityCooldownVisual` applies and clears per-player texture overrides through `SetTextureOverride()` as recast state changes, including login restoration and DM cooldown resets.
  - `AbilityCooldownVisualTests` and the gameplay icon audit cover cooldown-stage selection, resource naming, and generated-asset availability. New or changed ability icons must regenerate the cooldown variants through the standard icon workflow.

Telegraph support is implemented through `Ability.ApplyTelegraphedCombatImpact`, shared weapon-area handling, and ability-specific integrations. The shader/support assets remain production dependencies while those integrations use them; only bespoke behavior and live presentation remain validation concerns.

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
- Smoke-test Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage, including the curated cross-skill danger profiles.
- Run `CrossSkillPerkInteractionSafetyTests` and the active-context 400 SP support frontier so recursive procs, transfers, resource conversion, cooldown resets, and compound profiles remain statically bounded.

## Resolved Local Follow-Up

The feature branch previously contained partial Heavy Armor activation-time penalty carryover:

- `UsePerkFeat` applies a heavy armor activation penalty.
- `AbilityDetail` and `AbilityBuilder` contain `IgnoreHeavyArmorPenalty` / `UnaffectedByHeavyArmor`.
- Several First Aid abilities call `UnaffectedByHeavyArmor`.

Per the current Bible direction, these should be removed in the implementation phase unless the Bible is later changed to explicitly restore that mechanic.

Status: removed from the feature branch implementation on 2026-05-05.

## Generated Audit Artifacts

- `CombatUpgradePerkAudit.csv` compares Bible perk rows against current perk definitions by normalized perk name.
- `CombatUpgradeBiblePerkManifest.csv` is the exported perk-row manifest from all audited Bible tabs with perk tables.
- Current local-workbook audit summary from the checked-in workbook snapshot. The current manifest includes current Bible General rows that use Armor requirements, but excludes stale Heavy/Light Armor perk-tree rows from required-work totals:
  - Manifest rows: 1004
  - Scoped implemented rows: 999
  - Intentionally skipped unimplemented, first-iteration-out-of-scope Agriculture rows: 5
  - Scoped audit findings: 0
  - Missing Bible perk names in code: 0
  - Active Bible rows missing ability definitions: 0
  - Active Bible rows missing detected recast wiring: 0
  - Bible-described status applications absent from matching ability implementation: 0

The audit is intentionally a work queue, not final truth. A clean audit means the static checks found no scoped gaps; it does not replace playtesting, value spot-checking, migration dry-runs, or target metadata review.

If an older local workbook or generated manifest includes obsolete Heavy/Light Armor or stale Armor perk-tree rows, treat those rows as stale design data. Current Bible General rows that use Armor requirements remain in scope as general character-level perks, not as an Armor specialization tree.

Audit totals must include Espionage and Mimicry. Only the five unimplemented Agriculture rows, explicitly out of scope for the first iteration, are intentionally skipped in the current workbook snapshot.

Implemented cleanup so far:

- Removed Heavy Armor activation-time penalty support.
- Removed `Dash` as a player-facing perk/ability because it is implemented in code but absent from the Bible.
- Cleaned the character sheet combat display so baseline Physical/Force Defense is separate from typed elemental/status Resistances.
- Updated resistance gameplay to the direct -100 to 100 scale, including temporary immunity at 100 and vulnerabilities below 0.
- Added distinct Hutlar Ice, CZ220 Electrical, and Korriban Disruption NPC pressure abilities to the Bible NPC ability/package/world NPC sheets.
- Added conservative enemy resistance vulnerabilities, capped at -20, to the Bible enemy resistance packages and synchronized World NPC skin resistance item properties.
- Added Coolant-Scarred Mynock, Byysk Cryo Adept, and Sith Frostbinder as spawned resistance-pressure variants that expand Ice-resistance pressure beyond the original Hutlar Qion set.
- Confirmed logged-out status effects are process-local runtime cache and do not survive the fresh boot migration path.
- Routed Marked for Death bonus damage through the shared triggered-damage path while preserving recursion protection.
- Added static cross-skill feedback-loop coverage and an active-context 400 SP support frontier, with curated poison/trap/Mimicry, stealth, resource, sustain, reflection/deflection, and control profiles.

Release-validation follow-up:

- Droid instruction disc availability is covered by `CombatUpgradeBibleSyncTests.DroidInstructionResources_MatchCurrentRecipeDefinitionsAndMigration`. Retained or replacement disc availability still needs a release smoke test in game.
- Migration entry points and storage-surface coverage are guarded by `CombatUpgradeMigrationCoverageTests`, including forced rebuild flagging, live player migrations, stored serialized items, constructed droids, and ship/module serialized items.
- Finish live-module validation for weapons, armor, crafting equipment, enhancements, and droid equipment. Recent item work normalized weapon `Delay`, `DMG`, `WeaponDamageType`, and resistance enhancement values, including natural creature weapons, legacy Sling-based pistol resources, embedded `.git` area/store/NPC weapon copies for `Delay`, and the Bible `World NPCs` delay calculations; release still needs representative equip, crafting, migration, and combat smoke tests.
