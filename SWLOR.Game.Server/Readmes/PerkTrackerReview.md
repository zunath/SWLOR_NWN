# Perk tracker review — September 4, 2026

Scope: every row without a `Pass` test status in the [Perk Testing tracker](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203) at the start of this review. This is 281 implemented rows (208 Not Tested, 68 Retest, four In Progress, one Fail), plus five Not Testable Agriculture rows. The 717 previously passed rows were excluded from the review scope; shared fixes can also correct other ranks of a scoped perk.

[PerkTrackerReview.csv](PerkTrackerReview.csv) records each tracker row, original human test status, Bible location, registry evidence, and finding. `MetadataReview=PASS` means the automated registry audit agrees with the Bible; it does not mark the tracker row as human-tested.

## Sources and reconciliation

The local `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx` is authoritative. Compared descriptions, requirements, character restrictions, FP/STM costs, cast times, cooldowns, and development status with all 286 scoped tracker rows. The initial values agreed after normalizing blank/dash and numeric formatting.

Three ambiguities were resolved with the design owner:

- Tempest Bloom: one immediate pulse per landed hostile area ability during the 45-second buff, centered on its first struck target, dealing 8 physical damage to enemies within 5m.
- Finishing Drive: a 10-second cooldown; each cast adds one +8% Momentum stack (maximum three) and refreshes all stacks to 30 seconds. A 30-second cooldown could not reliably build stacks before the old duration expired.
- Ground Quake: replace existing Dazed with six seconds of Knockdown, while preserving Dazed if explicit Knockdown immunity rejects the conversion. This makes the existing Bible combo work under the shared hard-control rule; its description is unchanged.

The Tempest Bloom and Finishing Drive Bible cells, tracker cells, generated manifest, and existing TLK descriptions were synchronized. Workbook edits preserved unrelated ZIP entries and cached formula results. The workbook formatter then refreshed the generated audit files. The Haks companion PR contains regenerated binary TLK data.

## Implementation review

| Group | Rows | Review focus and corrections |
|---|---:|---|
| Beast Mastery | 73 | Rank progression, natural-weapon impacts, pet/master conditions, stat buffs and AI scoring. Iron Hide and Evasive Maneuver avoid redundant casts; Unbreakable Beast has the precise three immunities promised by the Bible. Evasive Challenge retains evasion after its one-use refund. |
| Mimicry | 59 | Learning/slots, hit and damage stats, shapes, control durations, trait chances, party targets and capstones. Corrected Warden pulls and healing/auras, Last Bastion's target cap, Static Burst's arc fanout, arc potency and Finishing Drive's cooldown. |
| Rifle | 24 | Queued impacts, critical refunds, suppression/kill zones, piercing targeting, weapon requirements and feedback. Existing fixes and regression coverage cover the recorded retest concerns. |
| Force | 24 | Control/Alter/Sense gates, ally versus self triggers, damage and healing riders, persistent fields, threshold/cooldown conditions and aimed areas. Creeping Terror now shares pulse identity across ticks. |
| Espionage | 23 | Coating charges and potency, trap tiers/arming, crafting and feat availability, stealth movement and native feat-use limits. |
| Lightsaber | 19 | Auto-attack timing and resource riders, stances, Sunder conditions, Deflecting Return's caps/cooldown, Embattled, Force Link and Aegis damage conversion. |
| Saberstaff | 13 | Corrected cross-skill area rewards, activation versus landed-hit triggers, stance damage modifiers and independent strict resource thresholds. |
| Vibroknife | 12 | Source-owned venom conditions and spreading, cross-skill riders, stack/duration caps, status extension and Stamina cooldowns. |
| Twin Blade | 11 | Corrected cross-skill rewards, per-enemy haste stacking, Blade Vortex's three-target refund and Tempest Bloom's pulse. Checked area target limits, Cyclone Stance and Edge Rhythm. |
| Vibroblade | 8 | Repeated-hit resource restoration, third-hit damage, execute threshold, cross-skill applicability and stance adjustments. |
| Engineering | 5 | Droid Assembly rank requirements, CPU/part tiers, instruction tiers and AI-slot limits. Programming now rejects unowned items, non-discs and controllers before consumption. |
| Katar | 5 | Guard versus deflection, Tag In healing, interruption and control duration. |
| Spear | 3 | Control category, Knockdown/Dazed behavior and aimed versus self-centered targeting. |
| Heavy Vibroblade | 2 | Flash's physical/Force accuracy reduction and enmity; Soul Ascension's physical life-steal behavior. |
| Agriculture | 5 | Bible and tracker identify these as not implemented/not testable. No gameplay implementation was added. |

The complete engine sweep also exposed Ground Quake's blocked Dazed-to-Knockdown combo outside the original tracker scope. Its explicit conversion is declared in ability metadata and regenerated consistently.

Shared infrastructure consumes stats for the new behavior. Cast identity limits pulses and chains across delayed impact contexts. Generators were updated alongside generated definitions. Character-sheet resource-condition descriptions show the separate permanent and temporary thresholds.

## Validation

The build passed with the post-build deployment disabled. The full unit suite passed 2,080/2,080 tests, followed by 78 affected unit checks after the final pull correction. All 61 NWN engine checks passed across the complete sweep and affected follow-up run. The complete sweep passed 58/61; Warden Maul's queued pull and two Serrated Arc fixtures crossing raised floors were then corrected, and the combined perk/status-effect follow-up passed 26/26. The sweep included 678/689 ability-behavior cases with 11 explicitly skipped player-dependent cases and no failed behavior cases.

The Bible registry audit has no findings; all 286 scoped tracker rows agree with the Bible after the three authorized metadata-cell edits. GUI layout validation passed and the isolated server boot produced no NUI layout warnings.

The 14 native perk regressions cover:

- Area ability pulses hit nearby enemies once per cast across impact phases.
- Area combat traits grant per-target haste and capped stamina across skills.
- Area-use deflection and FP rewards work across skills without requiring a hit.
- Blade Vortex restores stamina once only after three targets land.
- Droid programming accepts only an owned instruction disc.
- Evasive Challenge refunds stamina once while retaining its evasion and timer.
- Finishing Drive reaches three stacks through its real cooldown.
- Ground Quake converts Dazed into Knockdown while preserving immune targets.
- Last Bastion includes every enemy in its radius.
- Resource damage bonuses retain independent strict thresholds.
- Static Burst creates at most two extra arcs for a multi-target cast.
- Unbreakable Beast blocks knockdown, daze, and pulls without blanket resistance.
- Warden Order heals party members with outgoing, readiness, and received modifiers.
- Warden Wall grants one defense bonus to its source and nearby party.

The engine harness grants each NPC fixture the feat it activates, executes area impacts in the caster's script context, and fixes fixture positions where spawn collision would otherwise alter area boundaries. Its Mimicry fixtures bypass the player-only technique-loadout gate; real player learning/equipment validation is not exercised.

The 11 skipped beast cases are Call Beast, Guarding Bond, Predatory Bond, Reward I–III, Revive Beast I–III, Soothe Pet, and Tame. Those player-owned pet interactions still require in-game playtesting. Human tracker statuses are preserved. The five Agriculture rows remain intentionally unimplemented/not testable in the Bible.
