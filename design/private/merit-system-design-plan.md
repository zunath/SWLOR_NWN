# Merit System Design Plan

## Purpose

Merits are a post-cap advancement system modeled conceptually after Final Fantasy XI merits and EverQuest alternate advancement. The system gives capped characters long-term progression through Limit Points (LP) and Merit Points (MP), while keeping XP, SP, AP, and existing perk progression separate.

This document is the design and implementation handoff. The Bible remains the source of truth. If implementation discovers a discrepancy between this plan, the Bible, and engine reality, ask for a decision before resolving it.

## Documentation Model

- Bible workbook tabs should stay readable and catalog-first.
- The public Merits Bible tab should display Merit options: category, display name, technical ID, status, ranks, effects, costs, and short notes.
- System mechanics, hidden rules, formulas, implementation notes, and safety details belong in Markdown design files.
- Do not duplicate long rule explanations in the workbook.
- Public-facing workbook data must not reveal hidden unlock criteria/path, exact enemy LP reward table, exact LP death formula, or internal safety mechanics.

## Unlock Rules

- Merits are fully hidden until both requirements are met.
- The character must have at least 400 total historical SP.
- The character must complete the hidden Merit unlock quest line.
- The unlock quest is unavailable and invisible before 400 total historical SP.
- The quest start and quest completion/turn-in both validate the 400 historical SP requirement.
- Completing the quest grants a hidden/internal unlock marker.
- Do not show the unlock marker as a normal key item.
- Before unlock, there is no Merit button, no LP/MP display, no Merit window access, and no Merit terminology.
- Existing capped characters are not grandfathered into Merits. They may start the hidden quest if they already meet the 400 SP requirement.
- No retroactive LP, MP, or Merit ranks are awarded for pre-launch or pre-unlock activity.
- On unlock, show the one-time message: `You feel your experience begin to take a new shape.`

## XP, LP, And MP Rules

- XP and LP are separate systems.
- XP bonuses from food, buffs, Social, events, companions, or other sources do not affect LP.
- XP debt does not affect LP earning.
- A kill can grant XP and LP if both systems qualify independently.
- LP comes only from eligible enemy kills at launch.
- Only enemies level 50 or higher can grant LP.
- The player must have contributed to the enemy through the existing combat contribution path.
- Any contribution amount qualifies.
- Party membership alone does not qualify.
- The player must be alive and otherwise eligible when the enemy dies.
- The player must still pass existing same-area/range reward checks.
- LP is not split. Each eligible contributor receives the full LP value.
- Environmental/scripted kills with no eligible combat contributor grant no LP.
- Companion/pet contribution follows existing combat-point ownership behavior. Merit logic does not add a separate pet-specific rule.
- DM-spawned enemies can grant LP if they have a valid level and all rules pass.

## LP Reward Curve

- Merit LP uses its own explicit reward table.
- Do not call XP reward code at runtime to calculate LP.
- The launch LP table should mirror the shape of the established XP curve at one-third values.
- Even match target reward is 200 LP.
- Maximum positive delta target reward is 400 LP.
- Too-far-below-player target reward is 0 LP.
- The exact table lives in this Markdown design file.
- Public workbook/Bible material must not expose the exact enemy LP reward table.
- LP is calculated once per eligible player per enemy death.
- If multiple combat skills receive XP/contribution, use the highest relevant raw contributing skill rank for LP difficulty comparison.
- Do not use unrelated skills.
- Do not use Expertise or other effective-rank bonuses.
- Do not use total SP for LP difficulty comparison.
- If the curve returns 0 LP, show no player message and write no normal Merit log entry.

Private LP table:

| Delta | LP Award | Notes |
| ---: | ---: | --- |
| 6 or higher | 400 | Cap higher positive deltas to +6 value. |
| 5 | 350 |  |
| 4 | 325 |  |
| 3 | 300 |  |
| 2 | 250 |  |
| 1 | 225 |  |
| 0 | 200 | Even match target reward. |
| -1 | 150 |  |
| -2 | 100 |  |
| -3 | 50 |  |
| -4 | 25 |  |
| -5 or lower | 0 | No message or normal Merit log entry. |

## LP To MP Conversion

- LP is progress toward MP.
- First MP requires 10000 LP.
- LP required for the next MP is calculated from lifetime MP earned:

```text
LPRequiredForNextMP = min(10000 + LifetimeMPEarned * 250, 1000000)
```

- Requirement is based on lifetime MP earned, not MP spent or current MP.
- On LP gain, add LP first.
- If LP reaches the requirement and MP bank is not full, subtract the requirement, grant 1 MP, increment lifetime MP earned, and recalculate the next requirement.
- Preserve normal overflow after one conversion.
- One LP award event can grant at most 1 MP.
- If an award would cross multiple thresholds, grant only 1 MP and clamp remaining LP below the next threshold.
- If MP bank is full, LP caps at `required - 1`.
- Additional LP that would cross the threshold while MP is capped is discarded.
- Spending MP reduces current MP only.
- Spending MP increases total MP spent.
- Spending MP does not reduce lifetime MP earned and does not lower future LP requirements.

## MP Bank

- Base MP cap is 10.
- MP cap is calculated from base cap plus purchased Merit ranks.
- Do not store the calculated MP cap directly.
- If a future change or migration leaves current MP above cap, do not delete excess MP.
- While over cap, the player cannot gain additional MP from LP conversion until current MP is below cap.

## Death Penalty

- LP death loss applies only after Merit unlock.
- On normal player death, lose 10 percent of the current LP required for the next MP.
- This is based on the requirement, not current LP.
- Current LP cannot go below 0.
- MP is never lost on death.
- Purchased Merit ranks are never lost on death.
- Lifetime MP earned is never reduced.
- LP death loss follows the existing normal/no-penalty death-flow decision.
- Existing death penalties run first, then LP death loss runs as a separate Merit penalty step.
- Show a death message only when LP actually changes: `You lose X LP.`
- Public Bible may say unconverted LP can be lost on death and MP is safe, but should not include the formula.

## Player Messages

- LP gain: `You gain X LP.`
- MP conversion: `You gain 1 MP!`
- LP death loss: `You lose X LP.`
- No message for 0 LP awards.
- No current/needed LP values in kill messages.

## Purchase Rules

- Merit purchases are permanent.
- No player refunds ever.
- No standard respec path.
- No DM/admin tools to grant LP, grant MP, change ranks, reset Merits, or refund Merits.
- Migration code may include case-by-case refund/adjustment helpers for explicit redesign or correction migrations only.
- Purchases are blocked while in combat.
- Merit window can still be viewed in combat.
- Purchases apply immediately after confirmation.
- Derived stats and UI state refresh immediately after purchase.
- Every purchase buys exactly one rank.
- No bulk purchase flow.
- Every purchase requires confirmation.

Confirmation format:

```text
Purchase Rank 2 for 3 MP?

Effect: +1 Max AP

Merit purchases are permanent.
```

- Confirmation must show the Merit name, rank, MP cost, resulting MP after purchase, full effect for the rank being purchased, and permanent-purchase warning.

## UI Plan

- Add a new Merit window.
- Add a Character Sheet Merit button.
- Add `/merit` and `/merits` chat commands.
- Character Sheet button and chat commands are gated behind full unlock.
- Before unlock, chat commands must not reveal Merit details. Recommended generic response: `That option is not available.`
- Hide `/merit` and `/merits` from player-facing help/command listings until unlock. If help cannot be character-gated cleanly, omit them from public help.
- Do not document `/merit` or `/merits` in the public Bible.
- Character Sheet shows Merit counters in the same area where XP/progression is shown.
- Character Sheet Merit counters appear only after full unlock.
- Character Sheet display format:

```text
LP: 500 / 10500
MP: 4 / 10
```

- Merit window shows the same compact LP/MP values.
- Character Sheet and Merit window update live after LP gain, MP conversion, Merit purchase, LP death loss, and support/migration correction when open.
- Use existing GUI refresh event standards where practical.

## Merit Window UX

- Follow the Perks window standards.
- Use category tabs and search.
- Include an `All` category if practical.
- Search filters within the selected category by default.
- Reuse Perk-style purchase requirements display.
- Show satisfied and missing requirements according to existing Perk UI standards.
- List view shows compact summary:
  - display name
  - current rank / max rank
  - next rank cost, if not maxed
  - category/status if useful
- Detail pane shows:
  - all ranks
  - all MP costs
  - all effects
  - requirements section
  - purchase button/confirmation
- Maxed Merits remain visible and clearly marked as maxed.
- Show every rank, cost, and effect because purchases are permanent.
- Unavailable ranks should be explainable through the requirements section.

## Bible Workbook Rules

The Bible workbook should remain compact:

- Display the Merit catalog/options only.
- Include display name and technical enum identifier.
- Include category, status, rank count, per-rank effects, per-rank MP costs, total cost, and short notes.
- Mark feasibility-dependent entries as tentative.
- Do not include long explanations of LP, MP, unlocks, death loss, logging, migration, UI behavior, or implementation sequencing.
- Do not include screenshots/mockups for this design pass.

The Markdown design file owns system mechanics:

- hidden unlock requirements and quest path
- `MeritUnlockSPRequirement = 400`
- hidden unlock marker details
- exact enemy LP reward table
- LP death penalty formula
- one-MP-per-award safety rule
- effective enemy level cap/internal anti-abuse rules
- feasibility notes and engine concerns
- implementation notes

## Launch Catalog

### Status Labels

- `Planned`: expected launch Merit unless implementation finds an unexpected blocker.
- `Tentative`: public proposal, but engine feasibility must be confirmed before final implementation.

### Core

| Display Name | Technical ID | Status | Ranks | Effect | MP Costs |
| --- | --- | --- | ---: | --- | --- |
| Max SP | TBD enum | Planned | 50 | +1 total SP cap per rank. Does not raise individual skill rank cap. | Ranks 1-10: 2 each; 11-20: 3 each; 21-30: 4 each; 31-40: 5 each; 41-50: 6 each |
| Max AP | TBD enum | Planned | 5 | +1 AP cap per rank. AP is earned through normal AP flow. | 10, 20, 30, 40, 50 |
| MP Bank Increase | TBD enum | Planned | 10 | +5 MP cap per rank. | 1, 1, 2, 2, 3, 3, 4, 4, 5, 5 |

Max SP rules:

- Individual skill cap remains 50.
- Max SP raises the total earnable SP cap only.
- Launch max total SP becomes 450.
- Extra SP can be earned only in skills that already contribute to the normal SP cap.
- Once the expanded total SP cap is reached, normal SP-gated progression stops.
- Character Sheet shows current SP over effective cap, for example `423 / 450`.

Max AP rules:

- AP cap increases are real AP capacity.
- The player earns AP through normal SP progression.
- Max AP does not immediately grant AP.
- No hard prerequisite requires Max SP before Max AP.
- Character Sheet shows current AP over effective cap, for example `42 / 45`.

### Attributes

Each attribute Merit is separate and uses the same structure.

| Display Name | Technical ID | Status | Ranks | Effect | MP Costs |
| --- | --- | --- | ---: | --- | --- |
| Might | TBD enum | Planned | 5 | +1 Might per rank. | 10, 20, 30, 40, 50 |
| Perception | TBD enum | Planned | 5 | +1 Perception per rank. | 10, 20, 30, 40, 50 |
| Vitality | TBD enum | Planned | 5 | +1 Vitality per rank. | 10, 20, 30, 40, 50 |
| Willpower | TBD enum | Planned | 5 | +1 Willpower per rank. | 10, 20, 30, 40, 50 |
| Agility | TBD enum | Planned | 5 | +1 Agility per rank. | 10, 20, 30, 40, 50 |
| Social | TBD enum | Planned | 5 | +1 Social per rank. | 10, 20, 30, 40, 50 |

Attribute rules:

- Attribute Merit bonuses are direct stat bonuses.
- They do not grant or consume AP.
- They are stored as Merit ranks, not rewritten into base stats.
- They appear only as final effective attribute values in normal UI.
- Do not show base/AP/Merit breakdown in Character Sheet.
- Merit stat bonuses count for prerequisites by default.
- Merit stat bonuses apply to derived stats immediately after purchase.
- Merit stat bonuses do not count against normal AP upgrade caps.
- Normal AP/racial caps remain separate.
- Each attribute has a launch Merit contribution cap of +5.
- Temporary item and buff bonuses stack with Merit stat bonuses subject to existing engine/stat cap behavior.
- Social Merit bonuses affect normal XP and RP XP if those systems use effective Social.
- Social Merit bonuses do not affect LP.
- Attribute Merits apply only to the player character, not beasts, droids, companions, or summons.

### Combat Resources

| Display Name | Technical ID | Status | Ranks | Effect | MP Costs |
| --- | --- | --- | ---: | --- | --- |
| Max HP | TBD enum | Planned | 10 | +10 HP per rank. | 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 |
| Max FP | TBD enum | Planned | 10 | +5 FP per rank. | 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 |
| Max STM | TBD enum | Planned | 10 | +5 STM per rank. | 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 |

### Regeneration

| Display Name | Technical ID | Status | Ranks | Effect | MP Costs |
| --- | --- | --- | ---: | --- | --- |
| HP Regen | TBD enum | Planned | 5 | +1 HP regen per rank. | 4, 6, 8, 10, 12 |
| FP Regen | TBD enum | Planned | 5 | +1 FP regen per rank. | 4, 6, 8, 10, 12 |
| STM Regen | TBD enum | Planned | 5 | +1 STM regen per rank. | 4, 6, 8, 10, 12 |

### Utility

| Display Name | Technical ID | Status | Ranks | Effect | MP Costs |
| --- | --- | --- | ---: | --- | --- |
| Carry Weight | TBD enum | Tentative | 10 | +10 carry weight per rank. | 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 |

### Crafting And Gathering

| Display Name | Technical ID | Status | Ranks | Effect | MP Costs |
| --- | --- | --- | ---: | --- | --- |
| Failure Recovery | TBD enum | Tentative | 5 | +5% chance per rank to preserve consumed components on crafting failure. | 3, 5, 7, 9, 11 |
| Tool Preservation | TBD enum | Tentative | 5 | -5% tool durability loss per rank. | 3, 5, 7, 9, 11 |

## Excluded From Launch

- Movement speed Merits.
- Direct damage Merits.
- Weapon-family damage Merits.
- Direct accuracy Merits.
- Critical chance or critical damage Merits.
- Direct defense/evasion or hit-rate manipulation Merits.
- XP gain Merits.
- LP gain Merits.
- LP death-loss reduction Merits.
- Currency/credit/gold reward Merits.
- Vendor price Merits.
- Inventory, bank, cargo, or storage expansion Merits.
- Travel convenience Merits.
- Repair convenience/cost Merits.
- Cooldown reduction or global recovery-speed Merits.
- Crafting time reduction Merits.
- Crafting success chance Merits.
- Extra output/yield Merits.
- Node depletion reduction Merits.
- Separate RP/social-system Merits beyond Social attribute.

## Persistence

- Use lazy/default Merit state initialization.
- No migration is required just to add empty Merit state for a new system.
- Reading Merit state may return defaults without persisting.
- Persist only when Merit state actually changes.
- Store Merit state as a grouped object on the player data model.
- Merit state owns:
  - LP
  - current MP
  - lifetime MP earned
  - total MP spent
  - purchased ranks
- Hidden unlock marker belongs to the quest/key-item/unlock-flag system, not Merit accounting.
- Use enum-backed Merit identifiers, following project standards.
- Store highest rank owned per Merit, not individual purchased rank history.
- Display names are separate and always shown in UI/Bible.

## Definition Architecture

- Follow existing project standards.
- Use enum-backed Merit IDs.
- Use enum-backed Merit categories if consistent with similar systems.
- Use a builder/definition style similar to Perks where appropriate.
- Merit definitions are separate from Perk definitions.
- Merit ranks use MP costs, not SP costs.
- Merit requirements follow Perk-menu requirements display standards.
- Support custom requirement predicates.
- Support passive rank-based effects and purchase-time actions.
- Recalculate passive Merit bonuses on demand from purchased ranks.
- Do not cache derived totals unless profiling proves it necessary.
- Cache Merit definitions at module startup.
- Validate definitions at startup and fail loudly on malformed data.

Startup validation should reject:

- duplicate Merit IDs
- missing category
- missing display name
- invalid rank count
- missing rank costs/effects
- zero or negative MP costs
- missing ranks or skipped rank numbers
- malformed requirements

Rank rules:

- Ranks are 1-based in Bible, UI, logs, and internal definitions where practical.
- Every rank must have explicit MP cost and effect data.
- Rank data must be contiguous from rank 1 to max rank.
- Every rank costs at least 1 MP.

## Rebuild And Respec Interaction

- Rebuild windows show only rebuildable points.
- Do not show Merit stat bonuses inside rebuild allocation UI.
- Do not let rebuilds modify Merit bonuses.
- Full rebuild and stat rebuild reset only normal rebuildable stats/AP/racial choices.
- Merit bonuses are never refunded, reset, converted into AP, or lost during rebuild.
- After rebuild save/reset, recalculate and apply effective stats including permanent Merit bonuses.
- Character Sheet shows only the final effective attribute value after rebuild.

## Logging

- Use a dedicated Merit log group/file if supported.
- Do not store purchase history in the database.
- Logs are the audit trail for Merit activity.
- Log every actual LP gain.
- Log every MP gain.
- Log every LP death loss where LP actually changes.
- Log every successful Merit purchase.
- Log migration Merit adjustments.
- Log unusual/suspicious invalid states.
- Do not log normal 0 LP awards.
- Do not log death loss if current LP is already 0 or actual LP does not change.

Recommended log fields:

- account/CD key
- character ID
- character name
- event type
- LP before/after
- MP before/after
- LP required at the time
- lifetime MP earned
- total MP spent where relevant
- enemy level for LP gain events
- effective enemy level after cap for LP gain events
- contributing skill and raw rank used for LP calculation
- level delta used for LP gain events
- LP awarded
- whether MP conversion occurred
- Merit ID/display name/rank/cost for purchase events
- death loss amount for death events
- timestamp if the logger does not add one automatically

LP gain logs are one entry per receiving player, not one shared enemy-death entry.

## Migration Refund Capability

- No player-facing refunds.
- No admin/DM refund tools.
- Migration code may include helper methods for explicit case-by-case Merit corrections.
- Helpers can refund MP, remove ranks, adjust ranks, and log changes.
- Actual migrations must remain explicit one-off migrations.
- Migration refunds restore current MP as appropriate.
- Migration refunds adjust total MP spent as appropriate.
- Migration refunds do not reduce lifetime MP earned.
- Migration refunds do not lower LP required for future MP.
- Migration refunds may temporarily put current MP above cap.
- Do not show a generic player-facing migration refund message on login.
- Specific migrations may define their own player message if needed.

## Server Safety Requirements

- Merit purchases are server-authoritative and atomic.
- Re-read current Merit state before purchase.
- Validate unlock state, combat state, current rank, next rank, MP cost, and current MP.
- Apply one rank only.
- Persist once.
- Log once.
- Refresh UI afterward.
- Double-clicks/concurrent requests must not duplicate purchases or overspend MP.
- LP gain/conversion is server-authoritative and atomic per player.
- Re-read current Merit state before LP update.
- Apply LP gain, MP cap, one-MP-per-award safety, persistence, logging, and UI refresh as one coherent operation.

## Implementation Sequence

1. Update public Bible Merit sections with visible rules, catalog, costs, effects, status labels, and technical IDs.
2. Update private Bible Merit sections with hidden unlock requirements, LP reward table, death formula, safety caps, and implementation notes.
3. Get design approval on Bible content.
4. Add Merit enums, categories, definitions, builder/validation, and cached definition lookup.
5. Add lazy Merit state model/accessors on player.
6. Implement unlock checks and hidden quest marker integration.
7. Implement LP award/conversion, MP cap, one-MP-per-award safety, and logging.
8. Implement LP death loss in the normal death penalty flow.
9. Implement Merit purchase flow, confirmation, requirements, atomic validation, and logging.
10. Implement passive effect helpers for caps, stats, resources, regen, and tentative effects where feasible.
11. Update stat/SP/AP/resource calculation paths to use Merit bonuses.
12. Update rebuild flows to preserve and reapply Merit bonuses.
13. Implement Character Sheet LP/MP display and Merit button.
14. Implement Merit window using Perk UI standards.
15. Implement `/merit` and `/merits` gated commands.
16. Add dedicated Merit logging group/file if supported.
17. Add migration helper capability for case-by-case Merit corrections.
18. Add automated tests.
19. Verify implementation against public/private Bible.
20. Bring any discrepancy or infeasible tentative effect back for decision before changing the design.

## Test Plan

Required service tests:

- default/lazy Merit state
- unlock gating
- no visibility before unlock
- LP eligibility after unlock
- level 50+ source requirement
- contribution requirement
- alive/range/same-area eligibility
- XP bonuses do not affect LP
- LP table lookup
- no LP message/log for 0 LP
- LP gain and message
- MP conversion and message
- overflow preservation
- one-MP-per-award safety
- MP bank cap and LP cap at `required - 1`
- LP death loss
- no MP/rank/lifetime MP loss on death
- purchase requirements
- purchase blocked in combat
- purchase confirmation path
- atomic purchase/double-click safety where practical
- MP spending and total MP spent
- migration refund helpers
- over-cap migration behavior

UI/viewmodel tests where practical:

- no Merit button before unlock
- no LP/MP display before unlock
- button and counters visible after unlock
- Character Sheet displays `LP current / required`
- Character Sheet displays `MP current / cap`
- Merit window list/detail requirements follow expected state
- open windows refresh after Merit state changes

Manual review:

- Bible workbook Merits tab is readable and catalog-first.
- Markdown design file contains mechanics, formulas, hidden rules, and implementation details.
- Code definitions match the Bible.
- Tentative Merit feasibility is confirmed or returned for decision.
