# Player message policy

Gameplay feedback stays enabled in Production. A message is not diagnostic just
because it happens often: damage, hit/miss results, mitigation, status changes,
readiness, resource loss, and progression tell players what happened and what to
do next. Their existing recipients, ranges, and message flags are preserved.

`SWLOR_ENVIRONMENT` in `swlor.env` controls only the optional messages listed
below. `Testing`/`Test` and explicit `Development`/`Dev` enable them.
`Production`/`Prod`, missing values, and unrecognized values suppress them.
Matching is case insensitive. Restart after changing the setting; application
settings are cached at startup. Gameplay and server audit logs are unaffected.

## Retained in Production

| Source | Feedback retained |
| --- | --- |
| `Space`, all ship modules | Ship and direct-hull damage amounts, hit/miss results, hull/shield/capacitor restoration, repair fields, and E-War activation. |
| `ResolveAttackRoll`, `Ability`, `Combat` | Attack and ability outcomes, critical hits/immunity, Guard and Critical Ward mitigation, deflection/reflection, temporary-HP damage, resource drains, and readiness/stack/recharge information. |
| `StatusEffect`, `GuardedStatusEffect` | Status application, expiry, resistance, duration changes, application failures, and guarding relationships. Existing per-effect message opt-outs remain respected. |
| `UsePerkFeat` | Queue, ready, interruption, and expiry/cancellation notices. Nearby delivery includes the actor once. Stale timers and silent cleanup do not produce Production notices. |
| `Skill`, `BeastMastery`, `Guild`, `Faction`, `RoleplayXP` | XP and point gains/losses, debt repayment, standings, level/rank changes, and unlocks. |
| `QuestObjectives` and quests | Item/kill progress counters, requirement/stage completion, rewards, acceptance, and cancellation. |
| `Mimicry` | Analyzer observations, rank requirements, failed decoding/retry instructions, and successful learning. Existing witness tracking prevents duplicate observations of the same technique from one NPC. |
| Fishing, scavenging, weather | Casts, catches/failures, bait remaining, skill-check results and rolls, authored weather text, and hazards. |
| Other player surfaces | Validation, transactions, travel, crafting/research results, chat/dialogue, commands, GUI responses, staff tools, and operational warnings retain their existing delivery. |

## Optional messages enabled only in Testing/Development

| Source | Optional detail | What Production still receives |
| --- | --- | --- |
| `Stat`, weapon ability restoration | Actual positive STM/FP restoration amounts, including Double Shot's named restoration popup. | Resource values update normally. All ship restoration messages remain enabled. |
| Weapon ability base | Supplemental flanking/idle/behind damage adjustments and conditional critical-rate calculation popups. | Ability outcomes, final damage, status extension, and consumed setup messages. |
| `Combat` | High Noon critical-damage percentage, automatic attack-cycle critical-rate percentage, and Overwatch accuracy arithmetic. | Actual attack results/damage and the Overwatch activation popup. |
| `Combat` | Duplicate overhead versions of Guard, Critical Ward, Pinning Fire, and First Strike messages. | The corresponding combat log message, including mitigation amounts and First Strike readiness/stacks/recharge details. |
| Speeder dismount | Duplicate overhead dismount popup. | The existing dismount log message. |
| Explicitly silent queued-ability cleanup | Debug confirmation of internal queue cleanup. | Requested expiry/cancellation announcements remain enabled. |

STM/FP restoration reports the actual positive gain after caps and FP modifiers,
for example `Restored 3 STM.`. It is private to the recipient. Zero gain, natural
regeneration (including low-resource interval restoration), and resting remain
silent even in Testing. Double Shot uses its named popup without a duplicate
generic restoration message.

Property-load failures use one private log message in every environment.
There is no duplicate diagnostic popup.

## Repeated warnings

Warnings remain visible in Production, with only repeated copies limited:

- Paralysis preventing action: nearby feedback once per five seconds per affected
  creature, including both PC and NPC attackers.
- Skill cap: once per minute across skills and blocked/overflow outcomes. A kill
  can award several skills, but the total-rank cap and unlock remedy are the same.
- Mimicry rank gates and failed decoding: once per technique/failure reason per
  minute across NPCs. Testing shows every occurrence of these warnings.

## Audit and validation

[PlayerMessageAudit.json](PlayerMessageAudit.json) records every player-message
invocation with its file, line, containing method, delivery policy, and call text.
It covers native messages, floating text, nearby broadcasts, chat, speech, screen
text, and the optional-feedback helpers. It excludes declarations and comments.
This is a source audit, not a measured runtime message rate.

After reviewing a message change, regenerate it from the repository root with
`pwsh -NoProfile -File tools/ExportPlayerMessageAudit.ps1`. The tests compare File,
Member, Delivery, and Call; Line is informational. Shared transport entries defer
to their callers. Regression tests protect Production delivery of ship modules,
damage, status/progression messages, native attack results, and combat readiness.

In-game acceptance checks:

1. In Production, exercise ship weapons, direct hull damage, repairs, capacitor
   restoration, repair fields, and E-War. Their existing messages must appear.
2. Check ground hits/misses, criticals, mitigation/deflection, temporary HP,
   status application/expiry/resistance, guarding, resource drains, and readiness.
   Keep the gameplay messages; omit only the listed supplemental/duplicate text.
3. Verify XP, quest item/kill progress, faction/guild changes, and analyzer
   observations in Production. These must remain visible.
4. Queue/ready/interruption/expiry notices must reach the actor and nearby players
   once, including `XYZ no longer has weapon ability ABC readied.`. Stale expiry
   timers and silent cleanup must not announce a false change.
5. Restore STM/FP below and near caps in Testing, including FP modifiers, ally
   restores, and ability ticks. Check the reported gain. Repeat in Production:
   the optional amount message must be absent while resources still update.
6. Confirm natural regeneration, rest, and zero gains stay silent in both modes.
   Repeated warning limits must not hide the first actionable warning.

Native delivery/rendering still needs live testing; automated checks do not
replace an in-game session.
