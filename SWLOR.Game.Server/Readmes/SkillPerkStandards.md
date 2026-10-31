# Skill and Perk Design Standards

Read before designing, implementing, balancing, or rewriting a skill, perk line, trait, stance, or active ability. These rules apply across skill families; the weapon progression below applies only to the matching weapon family.

Sections: Established patterns · Cross-skill gameplay · Mechanical contract · Shared implementation · Balance evidence · Player descriptions · Integration and verification · Space skills and styles

## Established patterns

- Start with the relevant tabs in `design/bible/SWLOR Design Bible.xlsx` and two or three current implementations of the same kind. Record the comparators and the source of progression, SP prices, damage, costs, timing, scaling, and equipment values. Architecture examples are API illustrations, not balance templates.
- Follow the relevant family's existing line/category structure, rank schedule, character eligibility, and purchase rules. Do not make weapon conventions universal: Force, Armor, support skills, and creature systems have their own structures. A new structure, tier, or departure from the checked standards needs the user's approval under [ContentStandards.md](ContentStandards.md).
- Give each line a distinct purpose and each purchase a useful decision. Explain when players prefer one active, trait, or stance over its alternative. Avoid duplicate benefits or an ability that replaces another's entire purpose at the same opportunity cost. Use the existing mutual-exclusion mechanism when the approved design calls for alternative purchases.
- Distinguish skill-rank investment, perk-rank purchases, and spendable SP. Read the current character caps and starting SP from `Service/Skill.cs`; do not infer the character budget from one line's price. Check that an upgrade's base mechanic is available by its unlock rank and declare any external prerequisite.
- Higher ranks retain the earlier rank's benefits unless the approved design explicitly replaces them. Only the purchased rank's stat bonuses apply; restate earlier riders at later ranks. `PerkRankProgressionTests` checks whether evaluable stat bonuses on active perk definitions remain beneficial across adjacent ranks. It does not prove their magnitude is preserved; conditional/unevaluable bonuses and non-stat riders need focused coverage.

### Current weapon progression

The Bible's Pistol, Rifle, Spear, Staff, Twin Blade, and Vibroblade tabs each use two independent lines with 18 purchased rows and 60 SP per line. Their shared schedule is:

| Required skill rank | SP price |
| --- | --- |
| 2 | 2 |
| 5 | 2 |
| 8 | 2 |
| 10 | 2 |
| 12 | 2 |
| 15 | 4 |
| 18 | 3 |
| 20 | 4 |
| 22 | 4 |
| 25 | 4 |
| 28 | 3 |
| 30 | 4 |
| 32 | 3 |
| 35 | 2 |
| 38 | 4 |
| 40 | 5 |
| 45 | 4 |
| 50 | 6 |

Use this purchase-rank and SP schedule as the starting point for a new skill in that family, verifying it against the current Bible first. It does not dictate active/trait slots or how purchase rows group into distinct perks: preserve the relevant approved layout, including base attacks unlocking before their enhancements. Keep the lines independent; do not invent a shared third perk line or duplicate a purchase across both lines to fill the schedule. This table does not prescribe damage, resources, cooldowns, or cast times: take those from the relevant approved design and comparable abilities. `CombatUpgradeBiblePerkManifest.csv` is a generated index for checking the source rows, not a replacement for the workbook.

Preserve established exceptions rather than rewriting them to fit this table. Katar's Iron Guard also has 18 purchases and 60 SP, but distributes them across 14 distinct perks and uses different prices at ranks 15 and 28. A purchase-row count is not a distinct-perk count.

## Cross-skill gameplay

- Maximize useful combinations with other skills. Learning a perk from a weapon skill does not itself restrict its use to that weapon. Prefer conditions expressed by the mechanic, such as a hostile weapon hit, a movement event, or a combat ability, over an exact weapon or source-skill check.
- A purchase requirement and a use requirement are different. Put the former in progression metadata; add the latter only when the established mechanic or an explicitly approved design requires it. A blank `Cross Skill?` cell is not proof of a runtime weapon restriction. Keep the workbook, descriptions, and actual eligibility consistent.
- A cross-skill stance retains its tradeoff outside its originating skill. Do not grant a global benefit with a downside that disappears when another weapon is equipped. Check benefit and penalty scope together, including equipment changes while the stance is active.
- State which attack types a benefit affects: weapon auto-attacks, weapon abilities, Force abilities, support actions, or a declared subset. Test representative consumers outside the source skill. Do not silently broaden a weapon-only mechanic to unrelated actions or narrow a generic mechanic to its original line.
- Explicit upgrades to a named ability may remain specific to that ability. Distinguish those upgrades and legitimate attack/equipment prerequisites from broad earned bonuses; cross-skill design does not mean every effect applies to every action.
- Animation/model compatibility controls presentation, not purchase or combat eligibility. Use the established fallback presentation when another legal weapon cannot play the authored motion. Engine constraints and visual carriers must not add gameplay bonuses, duplicate real equipment, or become hidden weapon locks.

## Mechanical contract

Before implementation, make the following unambiguous in the design and its supporting notes. Put player-relevant conditions in the description; keep internal bookkeeping in implementation notes.

- **Triggers:** distinguish attempted attacks, ACC/EVA misses, deflections, landed hits, actual damage, critical hits, movement, and ability activation. State whether auto-attacks, abilities, allies, enemies, and damage over time qualify. Do not substitute one event for another.
- **Resources and state:** define generation, cap, duration, whether new stacks refresh all stacks or expire independently, spending, cooldown, target changes, and reset conditions. Specify what happens when a prerequisite is no longer met. Resource names should not obscure a different existing mechanic.
- **Benefit lifetime versus trigger availability:** track expiry/consumption separately from the trigger cooldown. Clearing or replacing a benefit must not implicitly reset its trigger gate unless the approved design explicitly calls for that reset.
- **Costs and commitment:** define when costs and cooldowns commit, cancellation behavior, discount/refund order, and any authored minimum cost. Distinguish an independently free action from one made free by a discount. Invalid or unpaid activations must not accidentally spend charges or grant rewards; paid interruptions follow the approved cancellation rules. Check that combined traits do not create an unintended repeatable refund loop.
- **Damage and proc scope:** state whether a value is total per activation, per real weapon, per target, per hit, or per tick. Additional hands, targets, projectiles, waves, and repeated callbacks must not multiply an activation-scoped budget. Explicitly per-hit effects retain their intended scope. Apply bonuses at the existing damage-formula stage instead of treating raw weapon DMG as final damage.
- **Targets and movement:** specify target count, range, shape dimensions, hostility, line of sight, collision/path requirements, and failure behavior where relevant. Match targeting and native cursor behavior to [AbilityTargeting.md](AbilityTargeting.md). A movement ability must not gain extra rewards merely because one movement or impact was observed multiple times.
- **Lifecycle:** account for equipment changes, area transitions, interruption, incapacitation, death/fatal protection, ownership or rank changes, logout, and reload where relevant. Delayed work belongs to its captured activation; an old callback must not act on a newer activation or consume newly earned state accidentally.

## Shared implementation

- Follow [CodingStandards.md](CodingStandards.md): perk definitions grant `StatType` adjustments; shared combat, ability, and status systems read them. Direct perk checks belong to ownership, unlock, purchase, UI, or progression gates. Do not add perk-name switches, hard-coded perk-specific source-skill allowlists, or perk-specific status-class checks to shared gameplay.
- **Crafting styles** (Engineering's Overclock and Safe Mode, Smithery's Hot Forging and Cold Working, and the Agriculture and Gathering styles that follow): a style the player picks in a craft window is a Passive status effect with `SourceType => StatusEffectSourceType.CraftingStyle`, `PersistsOnLogout => false`, and its `StatType` adjustments in `StatGroup`, applied with `ApplyPermanentStatusEffect`. Every bulk removal (cleanse, purge, remove by category, remove by stat, remove all, clear on death) skips that source type through `StatusEffectSelectors`, so a style is a Buff that no Buff purge can take; only the craft window, a perk refund, logout, a full rebuild, and entering space remove one. A new status removal that picks effects by a property of the effect goes through `StatusEffectSelectors` as well. Exclusivity between one skill's styles belongs to that skill's own style helper (`CircuitStyles`, `ForgeStyles`), because a crafter can hold one style of each skill at once. The engine strips every effect on death and a logout drops the style, so the helper also records the selection on the player record (`Player.SelectedCraftingStyles`, one style perk per skill) through `CraftingStyleSelection.Record`, clears it with `ClearIfSelected` when the style perk is refunded, and lists its styles in `CraftingStyleSelection.Definitions`; the revive hook, login and the craft window then apply the effect again while the perk is owned.
- Reuse existing builders, targeting metadata, equipment predicates, stat aggregation, impact/cost pipelines, and status categories. Extend a shared mechanic when necessary within the authorized scope; do not copy a second implementation for one line. Declare a new stat's classification, polarity, and category on its enum entry and use the established aggregation rules.
- Preserve the receiving ability's declared skill/attribute scaling and the actual weapon profiles used by its impacts. Captured state must retain the intended source, targets, costs, damage/proc budgets, and activation identity for queued, delayed, multi-target, and channelled abilities.
- Keep gameplay and presentation separate. A second visible projectile is not automatically another attack, damage roll, resource payment, proc, or item-stat contribution. Verify the authored damage and actual firing presentation independently.
- Fit native/authored playback to the approved combat timing; do not change cast time, cooldown, or attack cadence just to make artwork fit. Check relevant equipment and player appearances through the existing animation and client procedures.

## Balance evidence

- Compare against current abilities and builds from the same family and relevant cross-skill alternatives. Hold purchased SP, rank, gear/tier, attributes, supporting skill investment, targets, and timing assumptions comparable. Include the preparation cost of conditional burst instead of measuring only a fully precharged opening.
- Evaluate entry ranks, intermediate upgrades, and endgame; do not assume the rank-50 result protects early progression. Separate opening burst, sustained damage, STM/FP sustain, control/support value, mobility, and the cost of stance penalties or interrupted channels.
- Exercise the combinations that can change the conclusion: normal and capped attack speed, damage/accuracy scaling, high defense/evasion, single and multiple targets, stationary and moving play, resource discounts/refunds, and representative foreign weapons/abilities. Include relevant PvE/PvP conditions when the mechanic affects them; control team/action ordering when it can change a comparison. Select cases for the actual mechanic; do not claim exhaustive build or PvP coverage from a small matrix.
- Use `SWLOR.CombatSim` for combat changes it supports, with repeatable scenarios and seeds. Check its catalogs and damage, cost, movement, critical, targeting, and proc rules against the real implementation before relying on a result. A calibrated final-damage fixture can check a declared event budget, but cannot prove native pre-formula damage scaling. An unsupported interaction remains unverified until modeled or checked by the appropriate engine/client test.
- Report inputs, comparators, SP and setup costs, simulation count/seeds, results, and limitations. Keep full scenarios/reports as evidence and summarize the material tradeoffs. A simulator result supports an authored balance decision; it does not authorize a new standard or establish universal balance.

## Player descriptions

- Reuse the phrasing of comparable current Bible/perk descriptions. Use `DMG` for damage bonuses and `weapon auto-attacks` when those are the trigger. Describe what the player does and receives; avoid internal equipment-slot, carrier, stat, or callback terminology.
- State target/area dimensions, qualifying actions, total versus per-hit damage, stack refresh/spending, duration, and material tradeoffs when needed to predict the ability. Do not describe three partial attacks as three full damage budgets, or a visual off-hand as an extra real weapon.
- Keep each rank's Bible text, C# perk description, native TLK/feat/spell references, targeting metadata, animation planning descriptions, and actual behavior in agreement. Reuse established abbreviations and rounding conventions; do not invent wording that changes the formula's meaning. Keep separate cost/cast/cooldown columns accurate.

## Integration and verification

- Check allocations against current master, the HAK revision, retained definitions, and other active skill work before reserving enum, stat, feat, spell, status, TLK, icon, and model IDs. Coordinate reservations; a locally empty slot is not proof that another branch has not reserved it. Preserve existing identities and follow the replace-by-adding and enum-migration rules in [CodingStandards.md](CodingStandards.md) and [Migrations.md](Migrations.md).
- Keep code, Bible, generated manifests, native data, recipes/items, and presentation resources in sync where affected. Follow [DesignBibleWorkbookRules.md](DesignBibleWorkbookRules.md); agents update only the repository workbook. Use [AbilityTargeting.md](AbilityTargeting.md), [IconStandards.md](IconStandards.md), and [VisualEffectSelection.md](VisualEffectSelection.md) for those surfaces rather than maintaining parallel per-skill rules.
- Build and run focused regression filters under `AGENTS.md`, covering authored progression/parity, representative cross-skill interactions, formula/budget boundaries, and the affected lifecycle. Use [EngineTesting.md](EngineTesting.md) for native behavior and [InEngineVisualVerification.md](InEngineVisualVerification.md) for the required client evidence. Offline visuals, headless engine tests, and connected-client input prove different things; state unverified coverage plainly.
- Follow [PullRequests.md](PullRequests.md) for parent/HAK integration and clean current-head reviews. Report module repacking, HAK/TLK rebuilding, server deployment, and observed client behavior separately. Documentation-only rule changes have no in-game surface and do not require client captures.

## Space skills and styles

- **Skill split:** Piloting is the only space skill; its rank gates hulls, modules and perks and adds no stats. Space mining, gas and salvage are Gathering. Engineering (Engineer seat, repair and capacitor strength), Espionage (Sensors seat, scan, jam and paint strength, Slicing) and Leadership (Command orders, crew morale) each make one space role better; none is required to fly. Ground weapon skills do nothing in space.
- **Isolation:** no ground ability works in space and no space ability works on the ground. Space abilities use `.CanBeUsedInSpace()`, act from the ship, cost STM or ship capacitor (never FP) and grant `StatType` values the shared space code reads. Ground stats are stripped on entering space; keep that.
- **Attributes:** energy weapons use PER damage and AGI accuracy; launchers use MGT damage and PER accuracy; crits use PER against VIT; AGI is evasion, VIT hull defense, WIL shields, capacitor and repair strength, SOC command and exchange prices.
- **Styles:** Piloting has four styles of 60 SP (Ace, Ordnance, Bulwark, Command) on the ground template: 3 ranked abilities, 2 general traits, 3 cross-skill traits, 1 stance and 1 capstone each, opening at rank 2 and ending with a rank-50 capstone. The SP schedule by rank is 2:2, 5:2, 8:2, 10:2, 12:2, 15:4, 18:3, 20:4, 22:4, 25:4, 28:3, 30:4, 32:3, 35:2, 38:4, 40:5, 45:4, 50:6. The two-style rule applies to weapon skills only; the 4-6 active buttons per style, one stance at a time and the crowd-control rules still apply.
- **Solo target:** at every tier a pilot at that tier's rank with one weapon style (60 SP) solos all normal content and most elites in any hull their rank allows. Bulwark, Command and the ground skills are never required. Check changes with `SWLOR.CombatSim` and `tools/SpaceRevampBalance/checks.py`.
- **Space Industry** (Gathering): Prospector adds a percentage of each cycle's ore, Deep Core adds weight to the asteroid's rare metal (as Treasure Hunter does on the ground), Rapid Extraction shortens cycles. Player text gives the real rank cost of mining and never claims it got cheaper.
- **Seat skills:** Gunner seats check Piloting, the Engineer seat Engineering, the Sensors seat Espionage. A crew member switches on only modules whose rank requirement they meet in the seat's skill; the owner may fit any station slot.
- **Descriptions:** keep the Bible's Piloting (Revamp) rows, C# descriptions and TLK in agreement, and write module and hull ranks as real rank requirements.
