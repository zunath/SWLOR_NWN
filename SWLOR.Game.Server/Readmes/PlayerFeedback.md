# Player message policy

`SWLOR_ENVIRONMENT` in `swlor.env` controls optional messages. `Testing`/`Test`
and explicit `Development`/`Dev` enable them. `Production`/`Prod`, missing values,
and unrecognized values suppress them. Matching is case insensitive. Restart the
server after changing the setting; application settings are cached at startup.

Production suppresses routine messages **for everyone**, including the actor,
target, and nearby players. There is no participant fallback for status messages.
Resource changes, status icons, abilities, rewards, and server audit logs still
work; the policy gates message delivery only.

Useful gameplay state changes remain visible: ability queueing, readying,
interruption, and queued-ability expiry/cancellation inform the actor and nearby
players in every environment. Combat-related text is not automatically diagnostic;
the cutoff is repetitive detail rather than information players need to react.

## Suppressed in Production

| Source | Messages |
| --- | --- |
| `Stat` | Actual STM/FP restoration amounts from abilities, consumables, status ticks, and passive triggers. |
| `StatusEffect`, `GuardedStatusEffect` | Application, expiry, guarding links, duration-resistance details, and repeated resistance diagnostics. |
| `Combat`, weapon ability base | Proc names, bonus damage/accuracy/critical-rate numbers, stack changes, readiness popups, guard/critical-ward reports, resource-drain popups, reflection/critical/temporary-HP detail. Status icons still indicate readiness and active effects. |
| `ResolveAttackRoll` | Custom attack-roll/hit-rate, critical-immunity, and deflection feedback strings. NWN's own damage notifications are unchanged. |
| `Ability` | Supplemental per-target ability hit/miss results. |
| `Space`, ship module definitions | Per-shot hit/miss/damage chatter, repair and capacitor restoration amounts, E-War/repair-field announcements. Ship resource displays still update. |
| `Skill`, `BeastMastery`, `Guild`, `Faction`, `RoleplayXP` | Incremental skill/beast/RP XP, partial debt repayment, guild/faction point changes, faction-standing increments and repeated cap notices. |
| `QuestObjectives` | Every-kill/every-item remaining counters. Requirement completion is retained. |
| `Mimicry` | Automatic analyzer observations. Permanent technique learning and rate-limited failure warnings are retained. |
| `Fishing`, `Weather` | Nearby casting announcements, per-catch bait counts, ambient weather chatter. Catch/failure/depletion and lightning damage warnings are retained. |
| `SpeederItemDefinition`, `Property` | Duplicate overhead text where the same event already gives a log message. |
| `ScavengePoint` | Roll/DC arithmetic; Production gets only the short success/failure result. |

Restoration reports the **actual positive amount gained** after caps and FP
modifiers, for example `Restored 3 STM.`. Zero gains are silent. It is private to
the receiving player, never a nearby broadcast. Natural regeneration and rest
remain silent even in Testing. Double Shot uses its existing named diagnostic
popup instead of duplicating that restoration in the generic log.

## Retained in Production

| Family | Reason |
| --- | --- |
| Validation errors, rejected status applications, insufficient resources, bad targets, denied access, missing content | The player needs to know why their attempted action failed, including an incompatible or stronger existing status. |
| Ability queueing/readying, interrupted casts, expired/cancelled queued abilities | Useful state changes go to the actor and nearby players in Production and Testing. The actor receives the nearby notice once. Existing activation-message flags and silent dequeue callers are respected. |
| Empty-target casts | Private feedback to the actor explains the failure. |
| Paralysis preventing action | Private warning limited to once every 5 seconds in Production. |
| Skill cap blocking XP | Actionable warning limited to once per minute in Production, shared across blocked/overflow XP attempts. Testing shows every occurrence. |
| Mimicry rank gates and failed learning attempts | Private warnings explain the required rank or retry. Each failure reason is limited to once per technique per minute in Production, across NPCs. Testing shows every occurrence. |
| Level/rank increases, ability points, new techniques/recipes, achievements, fully cleared XP debt | Discrete milestones or new player options, not repeated increments. |
| Quest acceptance, requirement/stage completion, quest completion, cancellations, deliveries, key items, explicit reward payouts | The player needs to act on or know the result. Intermediate automatic quest counters are suppressed. |
| Craft/research/incubation/harvest/fishing/scavenge results | Confirm an explicit interaction, job state, failure, or depleted resource. |
| Item use, bank/market/property/civic transactions, costs/refunds, permissions, destructive-action warnings | Confirm persistent actions and explain consequences. |
| Travel boarding/missed boarding/arrival, safe-rest zones, detected traps, forced stealth exit, dismounts, ship destruction | Actionable environmental state or loss of control; not routine combat telemetry. |
| Chat/tells, HoloCom/HoloNet, dice/emotes, NPC/dialogue/encounter text, area descriptions | Intentional communication, authored content, or encounter cues. |
| Player commands and inspections, GUI validation, DM/admin tools | User-requested output. Production administration must remain usable. |
| Restart notices, migration/refund notices, server errors | Operational warnings and persistent data changes. |

The global NWScript APIs are not disabled: they also deliver essential messages.
Each routine call site explicitly routes through `PlayerFeedback`, or has a
local diagnostic guard for native attack strings. New messages must be reviewed
by trigger and frequency, not by their color or whether they are combat related.

## Call-site inventory and validation

[PlayerMessageAudit.json](PlayerMessageAudit.json) records each message invocation
with its file, line, containing method, delivery policy, and complete call text.
It includes `SendMessageToPC`, `FloatingTextStringOnCreature`, nearby broadcasts,
native `SendFeedbackString`/`SendFeedbackMessage`, chat transport, speech, screen
text, and the diagnostic wrappers. It excludes comments and method declarations.
This is an audit of source call sites, not a measured runtime message rate.

`Retained` means the call's existing trigger/validation still applies, such as a
quest counter reaching zero; it does not imply that all calls in that subsystem
are retained. Shared transport records defer to the caller's policy. The table
above gives the reasons for each family.

After reviewing a message change, refresh the inventory from the repository root
with `pwsh -File tools/ExportPlayerMessageAudit.ps1`. `PlayerMessageAuditTests`
checks inventory coverage, prevents raw message calls in repetitive combat/status
paths, and verifies that all custom native attack strings are diagnostic-gated.
`PlayerFeedbackTests` covers environment recognition, positive-only restoration
formatting, and warning interval boundaries. Existing gameplay regressions still
verify the underlying combat behavior.

In-game acceptance checks:

1. In Testing, restore STM/FP below the cap and near the cap, including an FP
   modifier, an ally restore, a passive restore, and an ability tick. Check the
   logged amount against the actual resource change. Zero gain, natural regen,
   and resting should produce no restoration message.
2. Repeat in Production. Neither the recipient nor bystanders should see the
   restoration, proc/readiness, status lifecycle, or detailed combat messages.
   Resources, status icons, damage, and ability behavior must still update.
3. Gain skill/beast XP, partial debt repayment, faction/guild points, and quest
   progress in Production. Routine increments should be silent; level-ups,
   cleared debt, and quest completion should still appear.
4. Exercise ship attacks/repairs and passive analyzer observations in both modes.
   Testing shows diagnostics; Production does not.
5. Check invalid targets, insufficient resources, interruption, access denial,
   explicit commands, and transactions. Keep their actionable response. Repeated
   blocked XP should produce at most one cap warning per minute in Production.
6. Queue/ready an ability, interrupt a cast, and let a queued weapon ability expire
   in Production. The actor and nearby players should see each state change,
   including `XYZ no longer has weapon ability ABC readied.` once per recipient.
   Silent cleanup and stale expiry timers must not announce a readiness change.

Native delivery/rendering needs these live checks; automated tests do not replace
an in-game test session.
