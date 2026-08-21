# Combat Upgrade Release Balance Plan

Last reviewed: 2026-08-17

## Purpose

This plan formalizes the pre-release balance and design review for the combat upgrade after player feedback exposed several live-play risks: cross-tree passive stacking, permanent Attack Deflection approaching cap, positional dependency, short-duration buff feel, and weapon trees with unclear or overly narrow playstyle identity.

The goal is not to reduce build variety. SWLOR should continue to support mix-and-match combat builds across weapon lines and support systems. Weapon-locked perks are an absolute last resort. The preferred solution is to set design budgets, audit legal cross-tree builds, and tune values or triggers so combinations remain healthy without hardcoding individual combo exceptions.

## Current Implementation Status

The current blocker pass is implemented in code, Bible, TLK, and regression tests. The completed fixes cover permanent Attack Deflection budget, Staff Crusher cross-tree payload, damage-plus-sustain loops, positional baseline viability, Spear Disabler non-Force value, current Lightsaber Ward/Severance behavior, short weapon setup payoff windows, and mandatory Mimicry/Espionage review coverage.

The cross-skill static gate now distinguishes bounded synergy from broken feedback. One active weapon context is combined with every legal support frontier under 400 SP; curated danger profiles cover poison/trap/Mimicry, stealth burst, cross-resource sustain, damage-derived healing, reflection/deflection, and layered control. A separate interaction test guards recursive damage, reflection, status, transfer, healing, resource, and cooldown edges.

The dependency-only weapon pass is also implemented in code, Bible, TLK, and regression tests. Perks should no longer exist solely to improve one named sibling perk unless every broader design option has failed and the exception is documented.

The Bible-first weapon identity pass is now applied in the local workbook, regenerated manifest, generated perk/ability code, and regression tests: weapon styles use the screenshot archetype names, all weapon styles follow the 18-slot/60 SP progression template, and every weapon style opens with an active combat ability. The remaining gate is live validation. `CombatUpgradeReleaseValidationMatrix.md` defines the curated archetypes, full-enumeration outlier review, representative enemy profiles, attack-delay feel checks, support-system audit surface, peak-damage target bands, and mob-tuning stance for the testing team.

## Core Principles

- Preserve cross-tree build freedom. Perks should be broadly mixable unless a specific interaction remains broken after softer fixes.
- Avoid dependency-only perks. A trait can prefer a weapon line, trigger type, status condition, or playstyle loop, but it should not exist only to make one named sibling perk usable.
- Balance by design first. Tune Bible values, trigger conditions, uptime, cooldowns, and magnitude before adding new code enforcement.
- Avoid combo-specific hardcoding. Do not add special-case logic such as "if Crusher plus Soul Devourer plus Spear, reduce damage."
- Treat any self-feeding trigger cycle as a release blocker. Secondary damage, reflection, DoT, healing, resource restoration, status application, and cooldown reduction must terminate through shared delivery types, caps, consumption, or cooldowns.
- Use shared combat and stat concepts. When code support is needed, prefer metadata-driven or shared-stat behavior over perk-specific branches.
- Treat curated archetypes as the release gate. Full build enumeration is required for comparison and outlier discovery, but curated builds are the main release decision surface.
- Judge compound profiles, not isolated screenshots. High damage alone is less dangerous than high damage plus sustain, defense, control, or support.
- Keep positional bonuses as bonus payoff, not baseline viability.
- Keep permanent Attack Deflection meaningfully below cap. If Attack Deflection can approach or reach cap, it should be temporary, active, capstone-limited, or otherwise constrained.

## Deliverables

### Balance Audit Matrix

The balance audit covers all combat systems:

- Weapons
- Force
- Devices
- Leadership
- First Aid
- Beast Mastery
- Mimicry
- Espionage

Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage are included because they affect legal build profiles and cross-tree totals. They are not targeted for broad thematic redesign unless the audit exposes a release-critical issue. Prefer numeric, uptime, cooldown, trigger, or interaction tuning for these systems.

The audit should report:

- Offensive budget totals.
- Defensive budget totals.
- Sustain and recovery totals.
- Control and debuff coverage.
- Support throughput.
- Uptime category.
- Source tier.
- Legal SP cost under the 400 SP cap.
- Tradeoffs and missing capabilities.
- Whether a finding is a design blocker, balance blocker, validation blocker, or diagnostic warning.

Repo coverage:

- `CombatReleaseBalanceAuditTests` validates curated archetype legality and hard gates.
- `CombatReleaseBalanceAuditTests` scans legal 400 SP package-frontier outliers and hard-fails permanent Attack Deflection cap access.
- `CombatReleaseBalanceAuditTests` hard-gates every active weapon package against the legal cross-skill support frontier and includes explicit high-risk Mimicry/Espionage/resource/sustain/control profiles.
- `CrossSkillPerkInteractionSafetyTests` validates that the shared proc graph has no recursive damage, reflection, transfer, healing, resource, or cooldown cycle.
- `CombatUpgradeReleaseValidationMatrix.md` records the manual real-enemy and attack-delay checks that cannot be proven by static tests.

### Weapon Identity Matrix

The identity matrix covers weapon trees only. It should clarify each tree's supported playstyles without implying hierarchy. Avoid labels such as "primary role" and "secondary role" if they suggest one path is better.

Use non-hierarchical labels such as:

- `Playstyle A`
- `Playstyle B`
- `Supported Playstyles`
- `Combat Loop`
- `Cross-Tree Value`

Each weapon tree should define:

- Supported playstyles.
- Expected combat loop.
- Expected cross-tree value.
- Main trigger language.
- Allowed offensive mechanics.
- Allowed defensive mechanics.
- Positional dependency, if any.
- Mechanics the tree should not own.
- Known conceptual mismatches.
- Positive calibration anchors, if applicable.

### Default Weapon Progression Pattern

Weapon skill perk lines should follow the default 18-slot, 60 SP progression pattern documented in `CombatUpgradeReleaseValidationMatrix.md`.

The intended template uses three ranked combat abilities, two general traits, three cross-skill traits, one stance, and one capstone. The corrected type for `Ability 3 Rank I` is `Combat`.

The local Bible, regenerated manifest, generated perk/ability code, and supporting TLK/2DA data now match the full template for every weapon style: 18 rows, 60 SP, a skill-rank 2 `Combat` opener, a skill-rank 50 `Capstone`, and the corrected `Ability 3 Rank I` `Combat` slot. The static alignment pass is complete; the remaining gate is live balance and engine-behavior validation.

### Audit Rules And Config

Create a durable rule/config surface that defines:

- Budget buckets.
- Source tiers.
- Uptime categories.
- Duration bands.
- Curated archetypes.
- Full-enumeration reporting rules.
- Blocker taxonomy.
- Warning versus blocker thresholds.

The Bible should carry lightweight row-level labels where useful: budget bucket, source tier, uptime type, expected playstyle, and duration band. The repo rule/config should define what those labels mean.

### Bible Workbook Scaffolding

The local Combat Upgrade Bible includes these planning tabs for the release balance pass:

- `Combat Balance Budgets`: shared budget categories, source tiers, uptime categories, warning conditions, and release blockers across weapon, Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, Espionage, Armor, and companion contribution.
- `Combat Archetypes`: curated legal build profiles used as the release decision surface.
- `Weapon Identity Matrix`: weapon-tree-only identity, combat loop, cross-tree value, positional dependency, and guardrail notes.
- `Combat Balance Findings`: player-feedback findings with severity, status, affected systems, and proposed audit action.
- `Combat Enumeration`: lower-priority full-enumeration queues used to find legal outliers for comparison against the curated archetypes.
- `Combat Mechanic Inventory`: current source-level evidence for high-risk stats and interactions.
- `Combat Fix Queue`: prioritized implementation queue produced by the current-state audit.

## Budget Buckets

The audit should separate at least the following offensive buckets:

- Attack percent.
- Flat weapon damage.
- Ability damage percent.
- Critical rate.
- Critical damage.
- Haste or attack delay reduction.
- Target damage taken.
- Defense or Force Defense reduction.
- Resource pressure.
- Sustain from damage dealt.

The audit should separate at least the following defensive buckets:

- Attack Deflection.
- Shield Deflection.
- Guard.
- Evasion.
- Physical Defense.
- Force Defense.
- Damage reduction.
- Temporary HP or absorption.
- Status resistance.
- Healing received and recovery.

The same numeric stat may need different budget targets depending on its source tier and uptime.

## Global Stacking Caps And Control Rules

These engine-enforced rules bound how buckets combine across trees. Perk authors must price new content against these ceilings, not against each bucket in isolation.

- Outgoing percent-damage bonuses (outgoing damage percent, weapon/Force damage percent, target-low-HP percent, target-status percent, and related percent stages) apply sequentially but the combined bonus is capped at +100% of the pre-stage damage (`Combat.MaximumDamageBonusPercent`). Flat weapon damage bonuses are outside this cap.
- Incoming damage reduction from the target-status damage-taken stage and the generic damage-taken stage is capped at 85% combined (`Combat.MaximumCombinedDamageReductionPercent`); each stage additionally keeps its own 95% clamp. Guard is a separate damage-stage mechanic with its own 85% cap (`Combat.MaximumGuardDamageReductionPercent`) and is not part of this bucket.
- Ability critical rate is clamped to the same 5-50 range as auto-attack critical rate. Treat 50 as the hard crit ceiling when budgeting conditional crit bonuses.
- Hard crowd control (Dazed, Knockdown, Stunned, Immobilized, Blind, Sleep/Tranquilized, Confusion) follows two rules. First, an ability's cooldown must be at least 1.5x its hard-control duration; dedicated control tools use 30-second effects on 45-second cooldowns, while damage-primary abilities carry shorter riders (15 seconds or less) so their cooldowns stay legal. Second, when any hard-control effect expires, the target gains 20 seconds of immunity to all hard-control types (shared `HardCrowdControl` immunity category, plus the existing per-type immunity), so alternating control types cannot chain-lock a target. Design for roughly 45-60% single-source control uptime, never 100%.
- Uncapped AoE control is not allowed: area control tools must declare a target cap (grenades cap at 5; Force Push caps by rank).
- Only one Leadership command effect (Press the Attack, Cleanse Order, Decisive Command) can be active per leader at a time; a new command replaces the previous one. Leadership damage-reduction sources (Watchful Presence, Cleanse Order, Bolster Resolve, Hold the Line) do not sum - the strongest active source applies.
- Only one companion (beast or droid) may be active per player; both spawn paths enforce the same shared guard.

## Source Tiers

Use separate budget expectations for different source tiers:

- Always-on passive.
- Conditional passive.
- Active ability.
- Stance or toggle.
- Aura or party support.
- Capstone.

The rough power allowance should follow this shape:

`always-on passive < conditional passive < active/stance < capstone`

Always-on passives need the strictest budget because they require no timing, cost, or playstyle execution. Conditional passives can have more room when they require enemy state, position, crits, guard, deflect, kill, low HP, or resource thresholds. Active abilities, stances, and capstones can spike higher because they require attention, resources, cooldowns, uptime windows, or drawbacks.

## Uptime Categories

Uptime must be part of the audit. The same magnitude has different value depending on how often it is realistically available.

Use categories such as:

- Always-on.
- High uptime.
- Medium uptime.
- Burst window.
- Next hit or next few hits.
- Requires enemy state.
- Requires position.
- Requires kill.
- Requires guard, deflect, or crit.
- Party-dependent.

Short-duration effects should be reviewed after budget issues are addressed. Extend durations before rewriting mechanics. Mechanic rewrites such as converting timer buffs into "next N attacks" should be reserved for cases where playtest proves duration tuning cannot solve the problem.

Recommended duration guidance:

- Quick proc or next-hit payoff: generally 15-30 seconds.
- Self setup for a follow-up: generally 20-45 seconds.
- Multi-step setup: generally 30-60 seconds, or simplify the setup.
- Party support buff: generally 60-240 seconds with lower magnitude.
- Capstone or burst mode: 45 seconds remains an acceptable baseline because it is constrained by shared capstone timing.

These are review bands, not automatic values.

## Archetypes And Enumeration

### Curated Archetypes

Curated archetypes are the release gate. At minimum, model:

- Single weapon specialist.
- Two weapon-line hybrid.
- Three weapon-line combat maximizer.
- Weapon plus Leadership.
- Weapon plus Force support.
- Weapon plus Devices support.
- Weapon plus First Aid sustain.
- Weapon plus Beast Mastery companion pressure.
- High-MGT damage stack.
- High-PER crit stack.
- Attack Deflection stack.
- Shield Deflection stack.
- Guard tank stack.
- Sustain tank.
- High-control/debuff stack.
- Positional low-uptime build.
- Positional high-uptime build.

The curated set should include known scary builds from player testing, including high-MGT melee stacks, high-PER crit stacks, Attack Deflection stacks, and damage-plus-sustain stacks.

### Full Enumeration

Full enumeration is required as a comparison/reporting tool. It should identify legal outliers under the 400 SP cap and explain which buckets they maximize.

Enumeration findings are diagnostic by default, not automatic release blockers. Promote an enumeration outlier to blocker when it violates a core design rule or combines multiple high-risk axes without meaningful tradeoff.

Examples:

- High damage only: inspect.
- High damage plus high sustain: likely blocker.
- High damage plus high deflection: likely blocker.
- High damage plus high control or debuff uptime: likely blocker.
- High defense plus high sustain plus low damage: likely acceptable tank identity.
- High support plus moderate damage: likely acceptable hybrid identity.

## Weapon Identity Guidance

### Calibration Anchors

Use positive-baseline trees as feel anchors, not as templates to clone:

- Vibroblade Frenzy: satisfying throughput and clear combat loop.
- Heavy Vibroblade: strong risk/sustain identity, with implemented stacking guardrails retained as a release-validation focus.
- Staff Sentinel: clear control and temporary-deflection identity.
- Katar Scrapper: strong control identity, with cooldown windows to review.

### Positional Mechanics

Positionals are difficult to execute reliably in NWN. No weapon tree should require side or back uptime to reach acceptable baseline performance.

Positional effects may grant:

- Extra burst.
- Extra critical pressure.
- Extra sustain or recovery.
- Bonus utility.
- Stronger payoff on an already functional baseline loop.

Prefer dual-trigger designs where practical:

- A normal trigger grants a smaller baseline bonus.
- A side or back trigger upgrades the bonus.
- Enemy states such as Disoriented, Exposed, Slowed, or Hamstring can provide alternate non-positional access.

Test positional trees under both low-positional-uptime and high-positional-uptime assumptions.

### Spear Disabler

Spear Disabler should broaden from anti-Force niche into general shutdown and resource pressure.

Its identity should be:

`shutdown, interruption, FP/STM pressure, recovery suppression, ability disruption`

Force-sensitive targets can still suffer extra, but the baseline should matter against ordinary dangerous targets. FP drain should become FP/STM drain or otherwise affect the relevant resource. Against enemies without meaningful resources, resource pressure should translate into short debuffs or recovery suppression instead of doing nothing.

### Attack Deflection

Permanent Attack Deflection should not approach or reach cap. If cap access is possible at all, it should be temporary.

Rules:

- Audit permanent Attack Deflection separately from temporary Attack Deflection.
- Passive/permanent Attack Deflection near cap is a release blocker.
- Temporary Attack Deflection may approach cap during constrained windows.
- Capstone Attack Deflection may exceed normal temporary budget briefly when intentional.
- Ally-granted Attack Deflection follows the same restriction, with extra caution for permanent or group-wide uptime.
- Lightsaber Severance remains the flagship non-shield deflection-payoff style, but its identity should come from temporary windows, empowered attacks after deflection, FP interaction, and ripostes rather than large passive Attack Deflection.
- Staff Sentinel, Saberstaff Tempest, Lightsaber Ward, and Heavy Vibroblade Immortal may touch Attack Deflection or Guard, but should not casually stack into permanent cap access.
- Shield Deflection may keep a higher passive ceiling because shields are a larger equipment commitment.
- Guard remains a separate damage-stage mitigation and enmity mechanic.

## Blocker Taxonomy

### Design Blockers

Design blockers require a design decision or mechanical reshape before release.

Examples:

- Permanent Attack Deflection can approach or reach cap.
- A weapon tree requires positional uptime to function at baseline.
- Spear Disabler regresses to Force-only value.
- A weapon tree's mechanics contradict its intended playstyle.
- A cross-tree passive becomes mandatory for most builds.
- Attack Deflection, Shield Deflection, and Guard are blurred into the same role.

### Balance Blockers

Balance blockers require value, uptime, trigger, cooldown, or resource tuning.

Examples:

- A curated legal build has high damage plus high sustain.
- A curated legal build has high damage plus high defense or deflection.
- A curated legal build has high damage plus high control/debuff uptime.
- A universal passive exceeds its design budget without meaningful tradeoff.
- A non-weapon system contributes to a runaway weapon profile.
- Always-on passive throughput is too close to active, stance, or capstone throughput.

### Validation Blockers

Validation blockers mean release confidence is missing.

Examples:

- Balance audit matrix is missing, incomplete, or stale after Bible changes.
- Weapon identity matrix is missing, incomplete, or stale for any weapon tree.
- Curated archetypes have not been tested.
- Full enumeration outliers have not been reviewed.
- Hologram tests are the only available evidence.
- Real-enemy playtests have not covered representative enemy profiles.

## Release Sequence

1. Define budgets, source tiers, uptime categories, duration bands, curated archetypes, full-enumeration rules, and blocker taxonomy.
2. Build or update the all-system balance audit matrix.
3. Build or update the weapon-only identity matrix.
4. Run the audit against current data.
5. Fix design blockers.
6. Fix balance blockers.
7. Apply weapon identity cleanup.
8. Do numeric tuning using the audit as the guardrail.
9. Review effect durations after value and budget tuning.
10. Validate curated archetypes.
11. Inspect full-enumeration outliers.
12. Playtest real enemy profiles, not only holographic test targets.

## Release Gate

Do not release while any of these remain true:

- Permanent Attack Deflection can approach or reach cap.
- A legal curated archetype has high damage plus high sustain, defense, control, or support without meaningful tradeoff.
- A weapon tree needs positional uptime to function at baseline.
- A cross-tree passive is broadly mandatory for most weapon builds.
- A triggered, reflected, periodic, transferred, healing, resource, status, or cooldown edge can feed itself without a hard terminating condition.
- Spear Disabler is only meaningfully useful against Force-sensitive targets.
- The audit cannot explain known scary player-test builds.
- Full enumeration outliers have not been reviewed against the validation matrix.
- Real-enemy playtest has not been run against representative enemy profiles from the validation matrix.

## Implementation Notes

- Prefer Bible/data tuning before code changes.
- Prefer adding audit coverage before changing numbers.
- Prefer extending short durations before converting timed buffs into "next N attacks" mechanics.
- If code support becomes necessary, prefer shared `StatType`-driven systems and enum metadata over hardcoded perk checks.
- Keep Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage in the balance audit but avoid sweeping thematic redesign unless the audit identifies a release-critical issue.
