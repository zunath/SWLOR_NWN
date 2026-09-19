# Healing balance review

Reviewed 2026-09-19 for general group PvE. This is a code and budget review, not
live encounter telemetry. The changes need a group playtest before treating the
new values as final encounter tuning.

## Findings

The strongest repeatable heals were too generous once maximum-HP scaling,
Willpower, healing bonuses, and tank mitigation combined. Increasing the tank's
HP increases every percentage heal; reducing incoming damage further increases
the amount of enemy pressure that those heals can offset.

At 26 WIL, direct-effect scaling grants 25%. Medical Injector Rig II adds 10%.
A recipient with Defensive Harmony and a fully scaled Triage Protocol II
has another 35% healing received. These multiply to 1.85625 before rounding and
Combat Readiness. Med Kit IV therefore restored about 66.8% of maximum HP per
cast, or 76.8% with 15% Combat Readiness. A routine six-second cooldown should
not provide nearly the same burst recovery as an emergency heal.

Full Light affinity adds up to another 50% to the relevant Force powers.
Renewal III's old 60% total could consequently reach about 167.1% of maximum HP
over 30 seconds with the same healing bonuses. Emergency Sealant contributed
40% base HP over 30 seconds after a successful cleanse, at no additional cost.
Kolto Mist could be recast while its previous cloud was still healing: its
30-second duration exceeded its 18-second cooldown plus 1.5-second cast.

Resource costs and casting time constrain these rates, but First Aid and Force
use different resource pools, periodic effects continue while the healer acts,
and a stronger tank makes each resource point buy more healing. The appropriate
first adjustment is to reduce repeatable output while retaining healer
investment and emergency recovery.

## Changes

All healing values below are percentages of the recipient's maximum HP before
bonuses. Periodic effects still tick every three seconds for 30 seconds.

| Ability | Before | After |
| --- | --- | --- |
| Med Kit I / II / III / IV | 10 / 20 / 28 / 36 per cast | 8 / 14 / 20 / 24 per cast |
| Benevolence I / II / III | 8 / 14 / 20 per cast | 6 / 10 / 14 per cast |
| Infusion I / II | 3 / 5 per tick | 2 / 3 per tick |
| Renewal I / II / III | 2 / 4 / 6 per tick | 1 / 2 / 3 per tick |
| Emergency Sealant | 4 per tick after cleansing | 1 per tick after cleansing |
| Force Mend | 10 per trigger | 6 per trigger |
| Kolto Mist I / II | 18-second cooldown | 30-second cooldown |

Infusion now expresses its total healing across all ten ticks directly. The old
15 / 25 totals divided by five actually produced 30 / 50 over ten ticks. The
new 20 / 30 totals divided by ten produce the intended 20 / 30.

Emergency Triage retains its 18% heal, doubled at or below 35% HP, its instant
cast, and its 24-second cooldown. Costs, WIL scaling, affinity, the ally bonus on
Benevolence, cleanses, shields, resurrection, and healer stat bonuses are
unchanged. Kolto Mist keeps its radius and per-tick healing. Its new baseline
cooldown removes routine self-overlap; cooldown reductions and multiple healers
can still produce overlap.

The smaller support heals remain within their existing budgets: Innervate III
heals 14% every 12 seconds without WIL scaling; Purifying Wave heals 8% on a
45-second cooldown; Force Sanctuary heals 2% per tick in a 4m area for 30 seconds
on a 45-second cooldown. Harmonic Restoration remains conditional on healing a
target below half HP, affects at most two other allies, and has a 20-second
source cooldown. These do not justify the same reductions as the primary
repeatable healing lines.

## Budget comparison

With the 1.85625 multiplier above and no Combat Readiness, Med Kit IV moves from
66.8% to 44.6% per cast. With maximum Combat Readiness, it moves from 76.8% to
51.2%. Infusion II moves from about 92.8% to 55.7% over 30 seconds, Renewal III
at full Light affinity from 167.1% to 83.5%, and Emergency Sealant from 74.3% to
18.6%. These examples ignore per-stage integer rounding and overhealing.

A deliberately optimistic First Aid sustain budget combines Med Kit IV at one
cast every 7.5 seconds, continuous Infusion II, continuous Emergency Sealant
after successful cleanses, and repeated Kolto Mist II on the same injured ally:

- Before: `(36 / 7.5 + 5 / 3 + 4 / 3 + 20 / 19.5) * 1.85625`, approximately
  16.38% of maximum HP per second.
- After: `(24 / 7.5 + 3 / 3 + 1 / 3 + 20 / 31.5) * 1.85625`, approximately
  9.59% per second, a 41% reduction.

This is an output ceiling, not a sustainable rotation or a prediction of
encounter DPS. It assumes sufficient resources, ideal scheduling, permanent
need for healing, maintained recipient bonuses, and repeated cleanse triggers.
Other casts can delay Med Kit. Renewal and Infusion share a regeneration status
type and must not be counted as independent simultaneous HoTs on one target.

For encounter context, the reviewed World NPCs data includes the level-50
Untouchable Instinct Adept at 708 HP / 46 weapon DMG and its Warden at 4,611 HP /
92 weapon DMG. Those DMG values are inputs to combat resolution, not final
damage per hit or DPS. Hit chance, attacks, abilities, armor, Guard, and other
mitigation prevent an honest numeric encounter comparison without combat logs.

## Playtest checks

Compare the same group, gear, and encounter before and after the change. Record
effective healing, overhealing, healer resource use, tank HP dips, and deaths.
Include a First Aid healer, a Force healer, a mixed healer, and a highly
mitigated tank. Test both single-healer and overlapping multi-healer groups.
Check that Emergency Triage still saves a critically injured ally and that
ordinary healing requires active attention during sustained group pressure.

## Verification

- Build succeeded with the post-build deployment disabled.
- 81 focused tests passed across First Aid, Force healing, status cadence,
  Design Bible/TLK parity, workbook formatting, and NPC balance audits.
- Compared the saved workbook against the original: only 18 intended cells
  changed, all 28,290 formulas and cached values survived, and cell style
  assignments stayed identical. The bundled spreadsheet renderer was
  unavailable; the repository's formatting tests passed.
- Regenerated the binary TLK from its updated JSON source. No new string IDs,
  icons, or 2DA targeting changes were required.
