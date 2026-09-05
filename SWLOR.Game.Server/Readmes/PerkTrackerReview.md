# Perk tracker review — September 4, 2026

Scope: every row without a `Pass` test status in the [Perk Testing tracker](https://docs.google.com/spreadsheets/d/1iHMKtrnh3lbUnmrgXtxEQseAJd7WIL6RU51RVSktm4s/edit?gid=2101115203#gid=2101115203) at the start of this review. This is 281 implemented rows (208 Not Tested, 68 Retest, four In Progress, one Fail), plus five Not Testable Agriculture rows. The 717 previously passed rows were excluded from the review scope; shared fixes can also correct other ranks of a scoped perk.

[PerkTrackerReview.csv](PerkTrackerReview.csv) records each tracker row, original human test status, Bible location, registry evidence, and finding. `MetadataReview=PASS` means the automated registry audit agrees with the Bible; it does not mark the tracker row as human-tested.

## Sources and reconciliation

The local `design/bible/SWLOR Design Bible - Combat Upgrade.xlsx` is authoritative. Compared descriptions, requirements, character restrictions, FP/STM costs, cast times, cooldowns, and development status with all 286 scoped tracker rows. The initial values agreed after normalizing blank/dash and numeric formatting.

Two ambiguities were resolved with the design owner:

- Tempest Bloom: one immediate pulse per landed hostile area ability during the 45-second buff, centered on its first struck target, dealing 8 physical damage to enemies within 5m.
- Finishing Drive: a 10-second cooldown; each cast adds one +8% Momentum stack (maximum three) and refreshes all stacks to 30 seconds. A 30-second cooldown could not reliably build stacks before the old duration expired.

The corresponding Bible cells, tracker cells, generated manifest, and existing TLK descriptions were synchronized. Workbook edits preserved unrelated ZIP entries and cached formula results. The workbook formatter then refreshed the generated audit files. The Haks companion PR contains regenerated binary TLK data.

## Implementation review

| Group | Rows | Review focus and corrections |
|---|---:|---|
| Beast Mastery | 73 | Rank progression, natural-weapon impacts, pet/master conditions, stat buffs and AI scoring. Iron Hide and Evasive Maneuver avoid redundant casts; Unbreakable Beast has the precise three immunities promised by the Bible. Evasive Challenge retains evasion after its one-use refund. |
| Mimicry | 59 | Learning/slots, hit and damage stats, shapes, control durations, trait chances, party targets and capstones. Corrected Warden healing/auras, Last Bastion's target cap, Static Burst's arc fanout, arc potency and Finishing Drive's cooldown. |
| Rifle | 24 | Queued impacts, critical refunds, suppression/kill zones, piercing targeting, weapon requirements and feedback. Existing fixes and regression coverage cover the recorded retest concerns. |
| Force | 24 | Control/Alter/Sense gates, ally versus self triggers, damage and healing riders, persistent fields, threshold/cooldown conditions and aimed areas. Creeping Terror now shares pulse identity across ticks. |
| Espionage | 23 | Coating charges and potency, trap tiers/arming, crafting and feat availability, stealth movement and native feat-use limits. |
| Lightsaber | 19 | Auto-attack timing and resource riders, stances, Sunder conditions, Deflecting Return's caps/cooldown, Embattled, Force Link and Aegis damage conversion. |
| Saberstaff | 13 | Corrected cross-skill area rewards, activation versus landed-hit triggers, stance damage modifiers and independent strict resource thresholds. |
| Vibroknife | 12 | Source-owned venom conditions and spreading, cross-skill riders, stack/duration caps, status extension and Stamina cooldowns. |
| Twin Blade | 11 | Corrected cross-skill rewards, per-enemy haste stacking, Blade Vortex's three-target refund and Tempest Bloom's pulse. Checked area target limits, Cyclone Stance and Edge Rhythm. |
| Vibroblade | 8 | Repeated-hit resource restoration, third-hit damage, execute threshold, cross-skill applicability and stance adjustments. |
| Engineering | 5 | Droid Assembly rank requirements and CPU tier gate; other parts cannot exceed the selected CPU tier. |
| Katar | 5 | Guard versus deflection, Tag In healing, interruption and control duration. |
| Spear | 3 | Control category, Knockdown/Dazed behavior and aimed versus self-centered targeting. |
| Heavy Vibroblade | 2 | Flash's physical/Force accuracy reduction and enmity; Soul Ascension's physical life-steal behavior. |
| Agriculture | 5 | Bible and tracker identify these as not implemented/not testable. No gameplay implementation was added. |

Shared infrastructure consumes stats for the new behavior. Cast identity limits pulses and chains across delayed impact contexts. Generators were updated alongside generated definitions. Character-sheet resource-condition descriptions show the separate permanent and temporary thresholds.

## Validation

Validation is in progress while the PR is under review. The initial focused run passed 514/515 tests; the obsolete assertion was corrected. The first full unit run passed 2,074/2,079; all five failures were corrected and the 116 affected tests passed. Nine new NWN engine regressions passed; two additional Twin Blade regressions were added afterward.

The broader engine sweep exposed a fixture omission: NPC test actors had not been granted the feats they activated, so the runtime ownership check correctly cleared queued abilities. The executor now grants the tested feat before activation. Final suite and review results will replace this paragraph before handoff.

Automated checks exercise definitions, native damage/status effects, resources and targeting. Cases requiring a real player account, learned-technique equipment or player-facing interaction remain explicitly identified as coverage gaps by the engine harness. Human tracker statuses are preserved for playtesting.
