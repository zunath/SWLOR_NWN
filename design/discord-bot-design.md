# SWLOR Discord Bot Design

Status: initial bot implemented in the independent Discord bot worktree; local verification completed. Live-server parity import and deployment remain pending. Updated 2026-10-03.

## Confirmed scope

- Deploy to the owner's existing Linux machine using Docker.
- Replace Ticket Tool's ticket workflow: a button in a dedicated channel creates a private text channel in the staff section; configured staff roles and the requester have access; staff can rename it; the requester can close it; closed channels are cleaned up periodically.
- Replace the Dyno features currently used for welcome messages, faction-channel joining, and quick answers to common questions.
- Leave room for conversation summaries, staff searches using AI, and game integration.
- Inspect the existing Discord server and bot dashboards to discover configuration rather than asking the owner to transcribe every setting.

The owner authorized implementation with "Ok put the bot together." The worker, persistence, workflows, tests, and Docker packaging are now implemented. Creating/installing the Discord application, supplying its token, importing the remaining verified settings, and production cutover remain deployment work. Existing bots remain operational.

## Discovery status and evidence

| Item | Status | Evidence or next action |
| --- | --- | --- |
| Hosting | Confirmed | Linux; Docker preferred. Existing database, capacity, backup location, and deployment access remain to be inspected. |
| Existing ticket bot | Confirmed | Owner identified Ticket Tool. Use its canonical dashboard at https://tickettool.xyz/manage-servers. |
| Discord guild, channels, roles, and permissions | Partially observed | Dyno identifies SWLOR as guild 484936923341651971. Dashboard counts were observed; individual channel/role IDs and permissions remain unverified. |
| Ticket Tool panels and behavior | Partially observed | Selected panel 1, SWLOR Tickets: support roles, open/closed category names, limits, logging destination, and displayed naming fields captured. Permission and message dialogs, checkbox states, IDs, and full panel inventory remain unverified. |
| Dyno configuration | Partially observed | Bot nickname Yoda; Custom Commands module enabled with 24/25 configured command slots. Prefix ?, full 24-command name/description inventory, Announcements welcome text, and 12 joinable faction role names observed. Exact command responses, switch states, and stable role/channel IDs remain unverified. |
| Live inspection access | Blocked after partial Chrome audit | Owner signed into both dashboards in Chrome. Read-only inspection reached the Dyno general dashboard, Welcome, Announcements, Autoroles, Joinable Ranks, the full custom-command listing, and Ticket Tool Manage Servers in a fresh chat. The owner-supplied panel URL subsequently enabled direct navigation through the Ticket Tool settings sections. Computer Use then explicitly stopped because it could not determine the current Chrome URL confidently enough to enforce policy. No bot settings were saved or changed, and no tickets or messages were created. Remaining configuration requires supported browser access or owner-provided configuration evidence. |
| Existing application connection | Unavailable locally | SWLOR.Admin appsettings and environment have no configured Discord bot token/guild; the declared application user-secrets file is absent. No credential values were printed or copied. |

Documentation establishes possible options, not which options SWLOR currently uses. Keep every unobserved setting pending until there is evidence from the live configuration.

## Observed live configuration (2026-10-03)

These values came from the authenticated Chrome dashboards. A fresh chat restored native accessibility reading and address-bar navigation. Screenshot capture still timed out, indexed clicks lacked coordinate geometry, and keyboard focus did not reliably expose detail controls. Computer Use subsequently ended when its URL safety check could not validate Chrome's current URL. No settings were saved, roles changed, bot commands sent, tickets created, or backup keys generated.

| Setting | Observed value | Source |
| --- | --- | --- |
| Guild | SWLOR; `484936923341651971` | Dyno account server link and selected-server dashboard. |
| Dyno nickname and prefix | `Yoda`; `?` | Dyno SWLOR general dashboard. |
| General configuration | Updates channel `discord-logs`; timezone `America/New_York`; language English; Manager Roles field shows no selected role | Dyno general dashboard. Channel ID and effective manager permissions were not read. |
| Dashboard inventory snapshot | 1,515 members; 14 categories; 89 text channels; 4 voice channels; 26 roles | Dyno general dashboard; counts are a snapshot, not proof of individual permissions. |
| Custom Commands | Enabled; 24 of 25 command slots configured | Module heading offers Disable Module. Each command's enabled switch, response, and options remain unverified. |
| Announcements | Enabled; announcement channel `welcome`; join-message text captured below | Announcements module. Join/leave/ban/DM and message/embed switches are not exposed by the accessibility text. |
| Welcome | Disabled | Newer Welcome module heading offers Enable Module. Its welcome channel field also shows `welcome`; the message field exposes a placeholder, not a verified configured response. |
| Auto Roles | Enabled; autorole table contains only its headers | Autoroles module. No automatic add/remove role rule was visible. |
| Joinable Ranks | 12 configured faction role names; command `?rank` | Joinable Ranks page. Names captured below; stable role IDs and policy switches remain unread. |
| Ticket Tool access | Authenticated Manage Servers page at `https://tickettool.xyz/dashboard` lists SWLOR under Servers with Ticket Tool, with a Manage button | Existing Chrome tab. The owner subsequently supplied the authenticated panel URL; selected-panel findings are recorded below. |

### Welcome text

The enabled Announcements module's JOIN MESSAGE field contains:

```text
Welcome to the party, {user}! Please visit our wiki at https://wiki.starwarsnwn.com/ to learn more about the project. Staff is on hand to help with any of your questions, too! You will find all important links in {#information} Please keep {#general} chat to SWLOR/Star Wars, but feel free to talk about any topics you so choose in the {#off-topic} channel. May the force be with you!
```

Preserve this template and resolve its channel variables to verified IDs during import. The source of the configured welcome content is Announcements; do not enable a second welcome sender by assuming the separate Welcome module is active. The actual join switch, DM delivery switch, and selected message type still require verification. Leave and ban fields expose default placeholders but no configured values; their enabled states are unverified.

### Joinable faction roles

The live Joinable Ranks list contains exactly these role names, each with the `Faction - ` prefix:

| Exact role name |
| --- |
| Faction - Cartel |
| Faction - Free Fleet |
| Faction - The Mandalorians |
| Faction - The Republic |
| Faction - Remnants |
| Faction - Czerka |
| Faction - Jedi Order |
| Faction - The Legion |
| Faction - Veles City Hall |
| Faction - Republic Joint Comms |
| Faction - The Underworld |
| Faction - Sith Cultists |

The page explicitly identifies `?rank` as the join command. LIMIT USERS TO ONE RANK, DELETE COMMAND AFTER USE, and AUTO DELETE RESPONSE controls exist, but their values are not exposed by the readable tree. Do not assume exclusivity, response deletion, or join/leave semantics. Resolve these exact names to role IDs and validate each mapping before accepting any faction-role changes; missing or ambiguous mappings must fail closed. Displayed member counts are not evidence of actual role membership.

### Custom-command inventory

All 24 configured names and listing descriptions were captured from the full page text. These are separate configured commands, even where descriptions match; `bible` is not yet a verified alias of `design`, and `ticket` is not yet a verified alias of `dm`.

| Command (prefix `?`) | Listing description |
| --- | --- |
| design | Links the design bible |
| weapon | Displays weapon stats |
| delete | Tells you how to delete a character |
| bug | What to do when you find a bug |
| nui | Displays known issues with NUI |
| dm | What to do if you need a DM |
| faction | How to join factions |
| wiki | Wiki info |
| skills | Displays skill info |
| damage | Displays damage info |
| race | Race info |
| based | BASED ON WHAT? |
| patreon | Patreon info |
| refund | Info about refunding perks |
| rebuild | Rebuild info |
| properties | Player property info |
| dev | Development info |
| bible | Links the design bible |
| ticket | What to do if you need a DM |
| spcap | No description shown |
| rules | No description shown |
| mastery | Displays mastery info |
| remnant | No description shown |
| jetpacks | No description shown |

Capture each command's actual response, embeds, Additional Responses, macros, aliases, arguments, enabled status, restrictions, cooldowns, and deletion options before parity is accepted. Listing descriptions are not the messages users receive. The `delete` description is informational; it does not establish a character-deletion side effect.

### Ticket Tool panel and server settings

The owner supplied `https://tickettool.xyz/dashboard/484936923341651971/configs#pc-menu`. That exact page was already open in Chrome and readable. Its section navigation IDs allowed direct address-bar navigation through the General, Category, Ticket, Permissions, Buttons, Messages, Moderator, Panel, Transcript, Logging, Automation, Limits, Claiming, Escalate, Command Style, Select Style, Thread Style, Forms, and Integrations sections. Server Info, Server Configs, and the command-config menu were also read. No configuration was saved or changed.

The selected panel is `1 | SWLOR Tickets`. This establishes one panel, not that it is the only configured or deployed panel. The account's sidebar shows Free.

| Setting | Observed value | Source |
| --- | --- | --- |
| Support Team Roles | Admin; Dungeon Master; Head DM; Quest Master | Panel menu and General. Stable IDs not exposed. |
| Additional Roles | No selected role; selector shows its placeholder | General. |
| Created/open categories | `tickets - open 1`; `Tickets - Open 2` | Panel menu and Category. Selection order/fallback behavior and IDs need verification. |
| Closed category | `tickets - closed` | Category. ID needs verification. |
| Per-user open-ticket limit | `1` | Limits, TicketLimit field. |
| Panel-wide open-ticket limit | `500` | Limits, limitsOpenAll field. |
| Server-wide open-ticket limit | `100`, across panels | Server Configs, server_globalLimit field. Preserve both scopes rather than using the larger panel cap alone. |
| Open-limit bypass roles | Admin; Dungeon Master; Patreon Supporter; Head DM; Quest Master; one `~ Deleted ~` entry | Limits. The deleted entry is stale configuration, not a role to recreate or grant. IDs and which limit scopes are bypassed require verification. |
| Naming fields displayed | Open `Ticket-{count}`; closed `Closed-{count}`; padding `4` | Ticket and General. These fields are disabled in the Free dashboard; verify actual naming behavior before treating their display as an active customization. |
| Logging channel | `discord-logs` | Logging. Enabled event switches and channel ID are unread. |
| Transcript channel | No selected channel; `Select one...` | Transcript. |
| Transcript timing fields | Send when Closed; save when Closed | Transcript. Associated DM/auto-save controls are disabled, and checkbox states are unread. This does not establish an active export policy. |
| Attached panels | None selected | Panel. Other independently deployed panels remain possible. |
| Server command prefix | `$` | Server Configs. Preserve only commands actually used and approved for replacement; this differs from Dyno's `?`. |
| Dashboard roles and blacklist roles | Neither selector has a selected role | Server Configs. This does not define administrator/owner access. |
| Storage categories | None selected | Server Configs. Storage Mode checkbox state is unread; do not assume channel recycling is enabled. |
| Role changes on create/open/close/delete | No selected add/remove role in the displayed fields | Ticket. Fields are disabled; no active role-transition behavior established. |
| Form | Title field `Please fill this out`; one collapsed Question Editor shown | Forms. Enabled state, question content, and whether results are attached are unread. |
| Escalation destinations | None selected | Escalate. Other related checkbox states are unread. |
| Command-style monitored channels | None selected | Command Style. Enable/watch-all/delete switches are unread. |
| Thread parent and claimed category | Neither has a selected channel/category; thread/claim enable controls are disabled | Thread Style and Claiming. No enabled thread/claim workflow established. |

Two-step close, two-step ticket creation, auto-pin, permission overrides, logging event switches, forms, storage, and other switches do not expose checked values in the native accessibility output. Disabled is a UI state, not proof of an unchecked or inactive setting. Preserve these gaps explicitly rather than assigning defaults.

The Permissions section exposes eight Edit buttons for opened/closed support-team, owner, additional-role, and everyone permissions. Their dialogs have not been opened. Messages and Buttons expose editors for creation, close/cancel/confirm, reopen, delete, transcript, and claim controls; these menu entries do not prove which buttons appear in deployed messages or who can use them. Actual labels, role restrictions, message/embed content, and close/rename/reopen/delete authorization remain pending.

No configured automation item or cleanup delay was visible on the Automation page. Its Add Automation control is disabled. The page's generic descriptions of minimum intervals and watcher duration are product help, not SWLOR's policy. Periodic cleanup remains an explicit requested feature whose delay and transcript prerequisite must be supplied by verified configuration or an owner decision. Do not claim existing cleanup was discovered or that its absence proves no external cleanup exists.

Design consequences: support both open categories and the closed category; keep the per-user, panel, and guild limits distinct; represent the observed staff/Patreon exemption policy using verified role IDs; exclude the stale deleted-role entry; record the logging destination; and keep export-before-delete explicit since no transcript destination was selected. Reusing legacy channel names alone must never establish ownership for cleanup or archival.

### Remaining live-audit gaps

| Gap | What is still needed |
| --- | --- |
| Ticket panels | Full panel inventory and deployed message IDs; exact labels, opening content, form prompts, eligibility, and stable category IDs. Panel 1 names and numeric limits are captured. |
| Ticket access and operations | Stable staff/observer role IDs; opened/closed requester permissions; rename, close, claim, participant, reopen, and delete behavior. |
| Ticket cleanup and archives | Cleanup timing, inactivity rules, export prerequisites, retention and attachments, existing-ticket inventory. Logging channel name is captured; no transcript channel is selected. Event/export/storage switches and destination IDs remain unread. |
| Dyno command details | Actual output and behavior for all 24 configured commands. |
| Welcome delivery | Join/leave/ban/DM switches, selected message type, stable welcome and template channel IDs. |
| Faction policy | All 12 stable role IDs, exclusivity/deletion switches, toggle/leave behavior, command restrictions and feedback. |
| Other Dyno responsibilities | Enabled-module inventory and any additional used moderation/logging/automation behavior before Dyno removal. |

These are evidence gaps, not requests for another Discord login approval. The remaining audit needs functional supported dashboard interaction. No parity or cutover claim should be made from names, placeholders, or documentation defaults.

## Published SWLOR workflow evidence

The Wiki supplies these baseline requirements. They are published player instructions, not a substitute for reading current Discord role IDs or bot settings.

| Requirement | Published evidence | Design consequence |
| --- | --- | --- |
| Faction-channel command | `SWLOR_Wiki/Lore/Factions.html` documents `?rank Faction - <exact faction name>`. Its Faction Listing link identifies channel `484945166960951297`, message `799456335058042950`, in the same observed SWLOR guild. | Preserve the documented prefix command and multiword faction argument alongside any new slash command. Verify actual joinable role IDs, aliases, toggle/leave behavior, and restrictions before enabling it. |
| Faction eligibility guidance | Faction-channel members should already have a faction character or intend in good faith to create/join one. In-game joining is described separately as roleplay with a faction member. | Discord role changes grant faction-channel access only; they do not enroll a character in an in-game faction. Use an explicit allowlist and do not infer current joinable roles from Wiki faction names. |
| Ticket destination | Both `SWLOR_Wiki/Lore/Factions.html` and `SWLOR_Wiki/Tickets_and_DM_Requests.html` link guild `484936923341651971`, channel `930228689000603708` for ticket requests. | Preserve the published ticket-entry channel during cutover, subject to confirming its current panel message and permissions. |
| Ticket request content | The ticket guide covers custom items, events, portraits, skill masteries, and rule reports. The faction guide also routes player-faction applications and faction conflicts through tickets. | Keep a general support/request workflow capable of handling these cases. Request types are content conventions; they do not prove that separate Ticket Tool panels or categories exist. |
| Custom-item request format | The guide requests Character Name, Faction & Rank, Item Name, Type, Enhancements, Description, and Appearance. | Preserve this guidance in the ticket opening/help content if the current panel includes it. Add forms only after confirming existing usage or receiving a separate request. |

The Wiki does not identify the current Dyno welcome template, the full 24-command inventory, their exact responses, or Ticket Tool support-role/cleanup configuration.

## Audit and import boundaries

Dyno documents command inspection through `customs list` and `customs show`, but command inspection omits embeds and Additional Responses. Dashboard inspection is still required for complete response parity. No documented general Dyno configuration export was established. No bot commands were sent during discovery.

Ticket Tool's Config Backup & Restore transfers configuration between Ticket Tool servers using an owner-held backup key. It does not export a general replacement-bot configuration or transfer existing tickets. Do not generate or request a backup key for this audit; inspect panel settings directly. Existing-ticket drain/import and transcript preservation remain separate cutover decisions.

The source chat had a missing-path runtime initialization failure. Only its two narrowly identified helpers were stopped; no global runtime/security configuration, browser sessions, Codex host, or unrelated helpers were changed. The owner authorized this fresh chat, where initialization and native Chrome accessibility reading succeeded. Chrome was unavailable to the browser-provider API; native screenshots and clicks remained unusable, and the native URL safety check eventually stopped Computer Use. Honor that stop: do not extract session credentials, change browser security, invent a helper protocol, or repeatedly fork tasks to evade the URL check. The owner-supplied Ticket Tool panel URL then made direct section navigation possible. Screenshots still timed out, and keyboard search to reach a message editor triggered the same explicit URL-policy stop. Further modal/switch inspection requires functional supported interaction; an additional login approval is not the missing requirement.

## Existing repository integration points

- `SWLOR.BackgroundServices` is a separate .NET 10 executable. Its DiscordWebhookJobHandler delivers queued messages using HTTP and handles rate limiting.
- `SWLOR.Game.Server/Service/BackgroundJob.cs` already publishes outbound Discord webhook jobs to Redis. The existing stream is `swlor:background-jobs`, consumed by the background-services worker.
- `SWLOR.Admin` already has Discord OAuth and guild/role settings for staff authentication. This identifies a future integration surface; its current authorization implementation is not the bot's authorization policy.
- No standalone interactive Discord bot was found in the inspected projects.

Keep the new bot deployable independently of NWN and the existing webhook worker. Game restarts should not interrupt ticket handling. Introduce a separate event contract or authenticated API when game integration is added; a competing consumer in the existing background-services group could take work away from its current handler.

## Proposed application and hosting

Use a new `SWLOR.DiscordBot` .NET 10 worker, with Discord.Net as the proposed Gateway/interaction library. Confirm and pin supported dependency versions during implementation. Keep domain services independent of the Discord client so permission decisions, ticket transitions, and command behavior can be tested without a connected bot.

Proposed components:

| Component | Responsibility |
| --- | --- |
| Discord adapter | Gateway connection, interaction acknowledgements, buttons/modals, commands, Discord API calls. |
| Ticket service | Ownership, permissions, channel naming, state transitions, audit records, reconciliation. |
| Cleanup worker | Durable scheduled cleanup, export prerequisites, retries, and bounded batches. |
| Welcome service | Existing channel/DM templates and observed role/message timing. |
| Faction service | Explicitly allowed joinable roles, join/leave semantics, and observed exclusivity rules. |
| Answer service | Existing triggers, aliases, templates, arguments, restrictions, and cooldowns. |
| Configuration service | Guild-specific settings and staff-only changes with an audit trail. |
| Persistence | PostgreSQL migrations, configuration, ticket state, scheduled work, and optional transcripts. |
| Future adapters | AI provider, permission-filtered retrieval, and authenticated game integration. |

Run one active bot worker in its own Docker Compose project. PostgreSQL may be a dedicated container or an existing host database after inspection. Keep database access on a private network, persist its data, and configure off-host backups. A single active worker plus durable job leases avoids duplicate cleanup and welcome handling; redundancy can be added later if needed.

Gateway delivery allows the initial bot to use outbound HTTPS/WebSockets without a public HTTP endpoint. A later web dashboard or inbound game API will need a separate deployment/networking decision. Configure restart-on-failure, graceful shutdown, readiness/health reporting, bounded logs, and dependency retries. Keep Discord and database credentials outside Git using host-managed secrets.

Initial sizing estimate: roughly 1 vCPU and 1-2 GB of available RAM for the bot and modest database load, with durable storage sized for the retention policy. Measure the existing machine before assigning limits. Hosted AI requests are separate usage costs; no local inference service is planned.

## Ticket workflow and invariants

1. The requester clicks an enabled ticket panel. Acknowledge or defer the interaction promptly, then validate the guild, panel, eligibility, configured ticket limit, category capacity, and bot permissions.
2. Reserve a ticket record and use interaction IDs plus a creation correlation ID to avoid duplicate channels from double clicks, retries, or a crash between channel creation and saving its ID. A configurable active-ticket constraint follows the observed existing policy.
3. Create the text channel with the intended permission overrides in the initial creation request. Explicitly deny ordinary members access and allow the requester, the bot, and configured support/observer roles according to the audited panel policy. Discord administrators retain their platform access.
4. Verify effective permissions, store the channel ID, post the configured opening text and controls, and record the ticket as open. If creation is only partially successful, reconcile or surface it to staff before accepting more operations.
5. Staff rename through an authorized action; direct authorized Discord renames should be observed without losing ticket ownership or cleanup state. Channel identity is its ID, not its name.
6. The requester can close their own ticket. Match the current confirmation, reason, staff-close, requester read/write, and closed-category behavior after auditing Ticket Tool. Persist the close time and deletion due time; keep closing separate from deletion.
7. A durable cleanup worker processes only owned, closed, eligible ticket channels after the configured delay. Recheck state and policy before deletion. Preserve required transcripts first; an incomplete export blocks deletion and produces a staff-visible failure.
8. If reopening is used today, reopening cancels pending deletion under the same ticket lock. Reopening a ticket already being deleted produces a clear result rather than racing the API call.

Proposed lifecycle: `Creating -> Open -> Closing -> Closed -> Deleting -> Deleted`. Persist failure/retry information on the relevant operation. Export status is separate from ticket status, so a failed transcript does not make an open/closed ticket disappear from staff view.

Additional rules:

- Authorize each mutation against the actor's current guild roles and ticket ownership; stale role caches and old buttons must not grant access.
- Do not synchronize a requester-specific ticket blindly to category permissions. Moves, staff-role changes, and policy changes require explicit reconciliation.
- Added participants, claims, observer roles, modals, reopening, DMs, and inactivity rules enter the initial implementation only if the audit shows they are used or the owner requests them.
- Cleanup never identifies channels by a name prefix alone and never targets legacy Ticket Tool channels unless an explicit import has established ownership.
- Missing channels, roles, or categories; member departures; manual deletion; timeouts; rate limits; and restarts have explicit recovery paths. A missing channel is reconciled without deleting an unrelated channel.
- Keep mention targets explicit so a pasted response or supplied ticket title cannot ping unintended roles.

## Dyno compatibility

Preserve existing command triggers and aliases unless the owner chooses a change. Slash commands can be added alongside existing prefix commands. Arbitrary prefix commands and transcript capture require Message Content access; welcomes require member events.

Faction operations use an explicit allowlist of faction role IDs. Match join, leave, toggle, one-faction-only, eligibility, and confirmation behavior from the live settings. Faction role changes must not grant unrelated staff permissions. The bot role must sit above the roles it assigns.

Canned replies are data-driven templates. Import each response, embed, link, supported variable/argument, channel/role restriction, cooldown, and optional deletion behavior. Detect macro commands that also change roles or call other commands; do not reduce those silently to plain text. Resolve unsupported macros individually from their actual usage.

Welcome configuration records the destination, channel vs DM behavior, exact text/embed/media, variables, any assigned roles or delays, and DM failure behavior. A replayed member event must not generate duplicate welcomes.

## Proposed persistent data

Store Discord IDs as lossless strings or suitable database numeric types, and all timestamps in UTC.

| Record | Important fields |
| --- | --- |
| Guild configuration | Guild ID, staff/configuration roles, welcome settings, command prefix, enabled modules, revision. |
| Ticket panel | Stable ID, source channel/message IDs, category IDs, support/observer role IDs, messages, permission profiles, limits, cleanup/export policy. |
| Ticket | Ticket ID, panel/guild/requester/channel IDs, state, creation/close/deletion times, current name, claim/participants if used, concurrency version. |
| Scheduled operation | Ticket/operation ID, kind, due time, attempt count, lease, status, last failure. |
| Ticket audit event | Actor, action, prior/new state, UTC time, correlation ID; retain after channel deletion according to policy. |
| Transcript | Ticket ID, export status, coverage/final message ID, storage reference, checksum, access policy and expiration. |
| Faction rule | Allowed role IDs, trigger/aliases, join/leave policy, exclusivity group and eligibility if used. |
| Quick answer | Trigger/aliases, content/embed template, argument rules, permitted roles/channels, cooldown, deletion behavior. |

Make deletion delay, transcript retention, attachment handling, and import policy explicit configuration. Their values remain unset until the live audit or owner decision supplies them. Do not infer that deleting a Discord channel also deletes stored transcripts.

## Future AI and game integration

Keep AI modules disabled for the initial bot replacement. Plan for summaries and searches through a provider adapter, asynchronous jobs, per-request limits, and a configurable monthly budget.

Decide which conversations may be retained before capturing history. If future queries should include deleted tickets, a transcript archive is required from launch; old deleted messages cannot be recovered merely by adding AI later. Record whether attachments are copied or represented only by links, since links are not a durable attachment archive.

Filter retrieval by the requesting staff member's current access before sending any content to an AI provider. Preserve source message/ticket references in answers. Archived tickets need an explicit archive access policy, especially after staff-role changes. Summaries are advisory; AI does not close tickets, grant roles, or execute game actions automatically.

Use verified Discord-to-game account linking and authenticated, scoped game APIs/events for later integration. Apply the repository's player-identity rules to public game data. Keep core Discord operations usable while game integration is offline.

## Live parity audit worksheet

For each observed setting, record its value, source panel/module, stable role/channel/message IDs, and observation date. Use a private or ignored local evidence file for transcripts or screenshots containing player discussions; the tracked design needs only configuration and test cases.

| Surface | Capture from the live configuration |
| --- | --- |
| Bot inventory | Installed bots, enabled Dyno modules, command prefixes, role hierarchy, other replacement responsibilities. |
| Ticket panels | Every deployed panel/button/dropdown, channel/message IDs, labels, prompts, routing, opening message, eligibility and limit rules. |
| Ticket access | Support/observer roles, requester/added-user permissions, everyone overrides, opened vs closed permissions. |
| Staff workflow | Rename format/actions, close permissions, confirmation/reason, claims, participant changes, reopen/delete actions actually used. |
| Cleanup/archive | Closed-category behavior, deletion delay, inactivity automations, exemptions, log/transcript destinations, export/DM/attachment policy. |
| Welcomes | Channel/DM settings, templates, embeds/images, variables, role assignment/timing, other join/leave announcements if enabled. |
| Factions | Exact commands/aliases and role IDs, leave/toggle/exclusivity behavior, restrictions, feedback messages. |
| Quick answers | All enabled custom commands, exact response/macros, arguments, restrictions, cooldowns, and auto-deletion. |
| Existing tickets | Open/closed counts and metadata, examples of actual workflows; determine whether to drain or explicitly import. |
| Hosting | Available resources, Docker/Compose version, storage/backup arrangement, existing database, deployment access and monitoring. |

Inspect Ticket Tool at https://tickettool.xyz/manage-servers, selecting SWLOR and reviewing each Panel Config. Inspect Dyno's selected-server dashboard and compare it to the visible Discord behavior. Read configuration and existing controls; do not create tickets, join factions, post messages, save settings, or disable bots during discovery.

## Implementation and cutover sequence

1. Implement against the contract below while completing the remaining configuration evidence through supported access. Import verified IDs and command content before enabling each production feature; unresolved audit fields do not block independent development.
2. Add the independent worker, PostgreSQL schema, configuration validation, and Linux Docker packaging. Keep a dedicated development application/guild configuration.
3. Implement ticket creation/rename/close/recovery and scheduled cleanup with focused tests; include observed transcript and staff workflows.
4. Implement welcomes, factions, and canned commands using the audited configuration and compatibility aliases.
5. Verify on a test server with ordinary members, every support role, an observer if used, and an unauthorized member. Compare each enabled replacement workflow against the captured evidence.
6. Plan the production cutover per feature. Preserve current bot configuration, drain existing tickets with Ticket Tool or perform an explicitly approved import, and replace old panel messages with controls owned by the new application. Old bot buttons are not automatically transferable.
7. Enable one production handler per feature after verification. Coordinate disabling old welcomes/commands/panels to avoid duplicate responses and ticket creation. Keep rollback settings available through acceptance.
8. Add AI and game integration as later work based on retained-data and account-linking requirements.

## Acceptance criteria

- A member opens a private ticket with the expected category, name, message, roles, and controls; another ordinary member cannot view it.
- Staff can rename; the requester can close only their own ticket; all observed staff/observer/claim/reopen behavior matches.
- Duplicate interactions and process restarts preserve one intended outcome. A deliberately interrupted channel-create operation is reconciled.
- Cleanup observes the audited delay, preserves required exports, survives restarts, cancels on reopen if supported, and touches no unrelated or unimported channel.
- Missing permissions/categories/roles and Discord rate limits produce actionable outcomes without a public partial ticket.
- Welcome output and timing match without duplicate posts; faction joins/leaves/exclusivity and restrictions match; all audited canned commands retain their output and restrictions.
- Backups restore ticket/configuration data; the bot reconnects and remains usable when the game is unavailable.
- No old bot feature is disabled until its replacement passes the parity check and cutover is authorized.

## Implementation contract

Independent implementation can proceed against this contract. The remaining live-audit fields gate configuration import and production activation, not independent development. `design/discord-bot-parity.json` records the verified observations in structured form. It is an audit inventory, not a runnable configuration: unresolved IDs, responses, and switches are intentionally null. Proposed behavior below is distinct from observed Ticket Tool or Dyno settings.

### Initial interfaces and authorization

| Interface | Behavior | Authorized actor |
| --- | --- | --- |
| Ticket panel button | Opens the configured private ticket; reports an existing ticket when the member limit is reached | Eligible guild member |
| Close button and `/ticket close` | Closes the current managed ticket after confirmation | Its requester or a configured support role |
| `/ticket rename name` | Renames the current managed ticket after validating and normalizing the name | Configured support role |
| `/ticket reopen` | Cancels scheduled deletion and restores the open permission profile | Configured support role; requester access remains a parity setting |
| `/ticket transcript` | Exports a managed ticket under its archive access policy | Configured support role with access to that ticket |
| `?rank <exact faction role name>` | Applies the audited join/leave policy for an allowlisted role | Eligible guild member |
| Existing 24 `?` answer commands | Produces the imported response with its original restrictions | As configured for each command |
| Member join event | Sends the imported welcome to the configured destination | Automatic handler |
| Configuration import/panel publication | Validates an explicit configuration revision; publication is separate from validation | Explicit bot configuration administrators |

Staff support and bot configuration administration are separate permissions. A support role does not automatically grant configuration access. Legacy `$` commands enter the compatibility layer only where their actual use and behavior are verified. Initial slash commands supplement the requested button workflow and existing Dyno prefix; they do not establish parity with uninspected Ticket Tool commands.

The Discord adapter defers channel-creation/export interactions before slow work, then reports the result privately. Buttons carry a versioned operation and stable ticket/panel ID; handlers look up the authoritative record and verify guild, channel, requester, and current actor roles. Reject unknown, stale, or cross-guild controls. Normalize channel names within Discord limits and preserve the ticket ID regardless of renames.

Parse prefix commands as a command token followed by an untouched argument string so faction names containing spaces remain usable. Ignore bot/webhook messages. The template renderer supports only imported, explicitly implemented variables; it never executes script text. Reject unsupported Dyno macros during import and show the affected command instead of silently changing its meaning. Resolve channel mentions to IDs and restrict allowed mentions on output.

### Proposed defaults for new behavior

These are design recommendations for behavior the existing audit did not establish. They must be visible in the deployment configuration review before activation; no existing bot setting has been changed.

| Setting | Proposed value | Reason |
| --- | --- | --- |
| Cleanup eligibility | Seven days after explicit closure; never delete an open ticket for inactivity | Provides a staff review window and meets the requested periodic cleanup |
| Cleanup polling | Every five minutes, with a persisted due time and a four-minute inactivity bound per ticket | Restarts do not reset the delay or lose work |
| Close confirmation | One confirmation; no mandatory reason | Avoids accidental closure without adding a required form |
| Requester after closure | Read-only until deletion | Allows reviewing the outcome during the cleanup window |
| Archive before deletion | Required JSON and escaped HTML export, including messages, embeds, timestamps, author IDs, and attachment metadata | Preserves a usable record independently of the deleted channel |
| Attachment handling | Copy available ticket attachments into protected archive storage; failed required copies block cleanup | Discord URLs alone do not provide a durable archive |
| Archive retention | 90 days after closure, configurable; any extended hold must be explicitly recorded | Provides a bounded support-history window for future querying |
| Reopening | Staff-only initially; persist a reopening state, clear deletion due time, and restore permissions before marking open | Prevents cleanup racing with an active support conversation |
| Open category selection | First configured category with capacity; fail with an actionable response when both are full | Supports the two observed categories without creating unapproved categories |
| Existing Ticket Tool channels | Let Ticket Tool finish them; new bot starts with new tickets | Avoids claiming old channels or deleting them based on names |
| Future AI | Disabled; archive tickets only, with no general-chat indexing | Keeps launch focused on replacement features |

A reopened ticket's later closure starts a new deletion and archive-retention window. Before final export, acquire the ticket lock, transition to Deleting, and freeze ordinary participant/support-role writes using the closed permission profile. Export staff follow-up after closure and record the final message ID and export status. Recheck for new messages before deletion; any change invalidates the export and retries it. Administrators retain platform access, so the cutover procedure must also tell staff to stop posting to tickets marked for deletion. An archive hold blocks both channel cleanup and archive expiration. Backup expiration must respect the chosen retention policy; a restore reapplies expiration and holds before serving archived data. Missing archive storage or a failed export suspends deletion while ordinary ticket operations remain usable.

### Configuration validation and deployment

- Validate stable guild/channel/category/role IDs and their types against the intended guild. Name matching is a discovery aid only; never guess between duplicate names. The stale `~ Deleted ~` exemption is excluded.
- Each feature has an independent activation flag. An enabled feature with null required fields fails validation; disabled features can remain incomplete. The validator reports all missing fields together. Support roles, permission profiles, limit-exemption scopes, templates, and archive policy must be explicit for enabled ticketing; faction semantics and role IDs must be explicit for enabled ranks; every enabled answer requires imported content and options.
- Preserve separate member, panel, and guild limits. Serialize reservation across those scopes so concurrent opens cannot exceed them. Configure exemption scopes explicitly after verification; the observed bypass role names do not establish which limits they bypass.
- Keep the bot token and database password together in the ignored host file `SWLOR.DiscordBot/secrets/secrets.json`; tracked examples contain only dummy values. The optional `databaseConnectionString` field contains non-secret PostgreSQL connection settings only. Limit the bot to the guild and relevant channels; request the permissions needed for channel management, role assignment, messages, and exports, without granting Administrator. Verify effective access and role hierarchy before activation.
- The supported deployment boundary is a dedicated, trusted Linux Docker host. Keep the worker and PostgreSQL on a private Docker network, with no published database port. Authorize host root, Docker group members, and backup operators to access mounted secrets, database backups, and private ticket archives; running the worker as non-root does not restrict these host-level principals. Restrict archive-volume and backup access to UID/GID 1654 and those authorized operators using filesystem ownership and permissions; private archive directories should use mode 0700 and files mode 0600 unless an explicitly authorized operator group needs access.
- If PostgreSQL traffic crosses an external or otherwise untrusted network, set the `databaseConnectionString` field in `secrets.json` to non-secret Npgsql connection settings including `SSL Mode=VerifyFull` and, when the CA is not installed in the worker image, `Root Certificate` pointing to the trusted CA certificate mounted into the worker. Set `Host` to the DNS name covered by the PostgreSQL server certificate. `VerifyFull` requires TLS and validates the certificate chain and host name; do not bypass validation with `Trust Server Certificate`. The database password remains in its separate JSON field. See [Npgsql security and encryption](https://www.npgsql.org/doc/security.html).
- Package a worker image and a PostgreSQL service in an independent Compose project. Persist database and protected archive storage; publish no database port or public bot endpoint. Run the worker as a non-root user, configure graceful shutdown and bounded logs, and use a database health check plus application retries. Pin image and package versions during implementation.
- Acquire a database-backed active-worker lease before handling mutations. Treat readiness as database reachable, lease held, configuration valid, and Gateway connected; a process being alive alone is insufficient. Use durable retries for cleanup and a recurring reconciliation pass for interrupted operations.
- Back up the database, archive, and non-secret configuration consistently. Record the last successful backup and periodically test restoration. Actual host storage, resources, backup destination, and deployment credentials are deployment inputs still to be inspected.
- Cut over one feature at a time with a configuration revision and a recorded comparison result. Keep Ticket Tool operational for existing tickets. Dyno stays installed until its enabled-module inventory has been reconciled; disable only a verified replaced handler to prevent duplicate output.

### Focused verification plan

| Area | Required evidence |
| --- | --- |
| Ticket access | Ordinary member, requester, each support role, removed staff member, and forged/stale control; compare open and closed access |
| Creation and limits | Concurrent opens by the same and different members; all three caps; explicit exemption scopes; full categories; crash after Discord channel creation |
| Close and cleanup | Duplicate close, restart, failed export/copy, post-close follow-up, reopen versus deletion, archive hold, manually removed channel, and unrelated legacy channel |
| Dyno compatibility | An imported fixture for each of the 24 commands, exact output/options, unsupported macros, spaces in faction names, role hierarchy, join/leave/exclusivity, and duplicate member events |
| Operations | Gateway reconnect, database interruption, rate limits, loss of permissions, graceful shutdown, competing-worker lease, backup restore, and per-feature rollback |

Do not test against the production guild by posting tickets or commands during discovery. Domain/adapter tests can use fixtures independently; a dedicated development guild supplies the final permission and interaction checks. Exact parity is accepted only after the missing live responses, switches, IDs, and deployed controls have been read and compared.

## Implemented bot and deployment inputs

`SWLOR.DiscordBot/` is an independent .NET 10 worker; it does not depend on the NWN game server or its post-build deployment. It uses Discord.Net 3.20.1 and Npgsql 10.0.3. `SWLOR.DiscordBot.Tests/` exercises the domain services, Discord request/permission helpers, configuration, archives, and PostgreSQL persistence.

Implemented behavior:

- Persistent private ticket creation, member/panel/guild caps with explicit bypass scopes, staff rename, requester/staff close confirmation, staff reopen, transcript export, cleanup holds, and scheduled cleanup.
- Maintenance enumerates retained tickets once per sweep, then uses indexed ticket-ID and channel-ID lookups for individual reads under the mutation lock. A persisted Creating/Closing/Reopening/Deleting state reconciles interrupted ticket operations. Channel identity combines the database record with an exact ticket GUID in its topic; names alone never establish ownership. The bot does not import or delete Ticket Tool channels.
- JSON and escaped HTML transcripts, paginated message history, copied CDN attachments with size checks, archive expiration, and a final-message check before channel deletion. Transcript reading and export enforce a cumulative `MaxTranscriptContentBytes` budget (32 MiB by default, at most 64 MiB), counting UTF-8 message, author, embed, poll/sticker/component/forwarded-message payload, and attachment metadata plus conservative per-object overhead. Both formatted documents stream to private temporary files instead of retaining complete output strings. Over-budget or incomplete exports retain the channel. Archives live on the protected data volume; `/ticket transcript` returns the HTML to authorized support staff. Discord interaction tokens expire after 15 minutes; a progressing export continues to completion and retains its complete archive if that interaction can no longer deliver it. A fresh `/ticket transcript saved:true` downloads the latest complete saved snapshot without rereading history or rewriting files, clearly labels it as saved, refreshes support membership and managed-channel identity before preparation and delivery, and retains per-upload authorization. Saved downloads do not change holds or retention deadlines. Copied attachments and JSON remain in the archive directory and are not embedded into that uploaded HTML.
- Configurable channel or DM welcomes; exact `?rank <full faction role name>` joins/toggles and optional exclusivity; canned `?` answers with text, embeds, role/channel restrictions, cooldowns, and command/response deletion options. Unsupported macros fail configuration validation. Welcome join events commit their original delivery intent through a bounded independent database insert before readiness or queue admission; a full queue or disconnect defers delivery to recovery. Recovery validates current channel permissions through REST before sending and before any faction role mutation, retaining denied deliveries for retry.
- Current REST member/role authorization, guild/category/channel checks, faction role hierarchy checks, bounded queues/retries, rate-limit handling, a single active-worker database lease, and readiness health checks. Transcript download authorization is refreshed after export and before each upload, including multipart deliveries.
- Readiness reconciliation and manual exports use per-ticket inactivity bounds instead of a fixed whole-job deadline. Transcript capture retains selected poll, sticker, nested component, and forwarded-message payloads in budgeted JSON and escaped HTML, and verification compares those payloads before deletion. Forwarded attachments join the ordinary archive plan; excessive forwarded nesting fails closed. Cleanup keeps advancing through large transcripts while completed pages and copied attachment chunks reset its four-minute inactivity timer; a stalled ticket yields to later tickets. Individual remote requests remain bounded.
- Completed community delivery intents, deduplication keys, expired cooldowns, and source-message deletion tombstones are pruned after 30 days in indexed batches, independently of Gateway readiness and ticket activation. Pending delivery/deletion work is retained. Repeated completion does not extend the replay window; old deletion tombstones receive a full window when schema version 3 is installed.
- Independent Docker Compose packaging with pinned .NET/PostgreSQL images, a non-root worker, one operator-managed JSON secrets file with a password-only projection for PostgreSQL, a read-only worker filesystem, persistent PostgreSQL/archive volumes, bounded logs, and no published database port.

### Fill the runtime configuration

Copy `SWLOR.DiscordBot/bot.example.json` to `SWLOR.DiscordBot/bot.json`. The example includes the observed SWLOR guild ID and prefix, the welcome template, 12 faction role names, and 24 answer command names. Unverified IDs, answers, and behavior switches remain unset; all features start disabled. `design/discord-bot-parity.json` remains a separate audit inventory and cannot be used as runtime configuration.

Before enabling a feature, supply these verified values:

| Feature | Inputs |
| --- | --- |
| Configuration administration | Administrator role IDs, or leave the list empty for guild-owner-only panel publication |
| Tickets | Panel text channel, open categories, closed category, log channel, support roles, optional bypass roles and each bypass scope, panel/opening messages |
| Welcomes | Destination and delivery type, channel IDs for the template's information/general/off-topic variables |
| Factions | Every allowlisted role ID, `Behavior` (`join` or `toggle`), and explicit `Exclusive` boolean |
| Answers | Exact verified response text/embeds, any restrictions, arguments/macros, cooldowns, and deletion behavior for each enabled command |

The example's seven-day closed-channel cleanup, five-minute polling, 90-day archive retention, requester read-only access after closing, and 100 MiB attachment limit are proposed replacement defaults, not claims about unread Ticket Tool settings. Review these explicit values before activation. A failed required attachment copy, including an oversized attachment, suspends cleanup for that ticket until staff resolves the archive issue.

Configuration is loaded at startup; restart the worker after editing it. IDs may be numbers or quoted strings containing decimal digits only; signs and whitespace are rejected. Unknown JSON fields are rejected so a misspelled security or retention option cannot silently fall back to a default.

Disabling `Tickets.Enabled` stops new ticket intake and panel publication. Authorized close, reopen, rename, export, and hold/release controls remain available for tickets the bot already owns, alongside maintenance and retention. Discord-dependent reconciliation and channel cleanup wait until the Gateway is ready; a readiness transition immediately requests a serialized maintenance sweep to resume interrupted ticket states without waiting for the configured cleanup interval; validated local archive expiration continues while Discord is offline. Startup reads persisted records before enabling the Gateway: existing channels still require valid panel, support-role, and cleanup settings even when new ticketing is disabled. Retained archives require their original archive root; deleted tickets with only retained archives do not require panel or support-role configuration. Ready/resume validation also checks Discord capabilities for existing channels. Missing maintenance settings fail startup rather than repeatedly failing cleanup.

### Discord application and Linux startup

Create a dedicated Discord application and bot, then install it with the bot and application-command scopes. Enable Server Members Intent for welcomes and Message Content Intent for prefix commands and ticket transcript content. Grant the required channel/message and role-management permissions, and place the bot role above the allowlisted faction roles. Startup verifies required access against the configured guild; Administrator is not required. Discord administrators and the guild owner retain their platform access to private tickets.

Create `SWLOR.DiscordBot/secrets/secrets.json` from `secrets/secrets.json.example`, then replace both placeholder values with the Discord bot token and a strong database password. Add `databaseConnectionString` only when overriding the non-secret PostgreSQL host, port, database, user, or TLS settings; never put a password in that connection string. The actual JSON file and `bot.json` are ignored by Git and excluded from the worker image. The worker mounts that full JSON read-only as `discord_bot_secrets` at `/run/secrets/discord_bot_secrets`. Run every Compose command through `sh ./compose.sh`: the wrapper validates the single JSON object and uses host `jq` to project only `databasePassword` into the environment-backed Compose secret `database_secrets`. PostgreSQL mounts only that projection at `/run/secrets/database_secrets`; its container never receives the Discord token or optional worker connection settings. The database entrypoint extracts the password internally and starts the official PostgreSQL entrypoint. The projection lives only in the Compose process environment and mounted Docker secret, without a second operator-managed file or secret-bearing command argument. Host root and Docker administrators remain trusted as described above. Use Docker Compose with environment-backed secret support; this packaging does not support `docker stack deploy`. Install `jq` on the Linux host before using the wrapper ([Compose secrets](https://docs.docker.com/reference/compose-file/secrets/)). On Linux, protect the host secrets directory and make the mounted JSON readable by worker UID/GID 1654. Do not assume a host file with mode 0600 owned by another UID is readable inside the worker container.

Run from `SWLOR.DiscordBot/` on the Linux host:

```sh
sh ./compose.sh config --quiet
sh ./compose.sh build bot database
# Local configuration validation runs without secrets, database access, or Discord login.
sh ./compose.sh run --rm --no-deps bot --validate --config /config/bot.json
sh ./compose.sh up -d
sh ./compose.sh ps
sh ./compose.sh logs --tail 100 bot
```

The official PostgreSQL image uses `POSTGRES_PASSWORD` only when initializing an empty data directory; editing the JSON alone does not rotate a persisted role ([official image documentation](https://github.com/docker-library/docs/blob/master/postgres/README.md)). For the bundled Compose database, stop the worker before editing `databasePassword` in the existing `secrets/secrets.json`, then run the coordinated helper:

```sh
sh ./compose.sh stop bot
# Edit only databasePassword in secrets/secrets.json with a trusted editor.
sh ./rotate-database-password.sh
```

The helper projects the edited JSON once and sends only the database password projection through stdin to the running database container, uses local administrative access and psql's encrypted `\password` command, then recreates the database and worker to refresh their respective mounted secrets. It waits for authenticated database health and worker readiness; a failed step leaves the worker stopped. No password appears in command arguments, shell history, SQL text, or a second secrets file. A plain restart with a mismatched persisted password now fails database authentication health before Compose starts a replacement worker. Custom external databases require their own administrator-approved rotation, followed by refreshing the worker's JSON mount.

Once the worker passes startup validation, a configured bot administrator can explicitly publish the new button with `/ticket-panel publish panel:support`. Panels are not posted automatically. Staff use `/ticket rename`, `/ticket reopen`, `/ticket transcript`, `/ticket hold`, and `/ticket release` in managed tickets. The requester can use the close button or `/ticket close` in their own ticket.

Use `sh ./compose.sh down` to stop the deployment while retaining its named volumes. Preserve and back up both volumes before replacing the host. A normal restart preserves ticket state, due times, holds, audit records, and archived transcripts. Do not run a second worker against a copied database while the first is still active. The worker lease coordinates instances sharing the same database, not separate restored copies. Schema version 5 atomically binds each bot database/schema to its configured Discord guild before reading or changing retained state. A different guild ID fails startup and all store operations remain unavailable; give each guild a separate database/schema. When upgrading an older database without an ownership marker, use its original guild configuration for the first upgraded startup so the existing tickets, delivery intents, cooldowns, and deletion jobs are bound to that guild. Changing bot.json alone cannot reassign a retained database.

### Limits and remaining acceptance work

The initial worker provides ports between domain services, persistence, and Discord; no AI provider, general-chat index, or game account linking is enabled. Those remain future upgrades. Full claims/participant-management workflows and arbitrary Dyno macros are outside the initial requested workflows unless the remaining parity audit establishes a requirement.

Ticket reconciliation, scheduled channel/archive cleanup, and optional quick-answer timed response deletion are persisted in PostgreSQL and survive worker restarts. Each export commits its managed archive directory before publishing files and records completion separately; partial or fully written archives remain subject to retention after an interrupted save or external channel removal, while intentional holds still apply. Download staging stays in that owned directory and cleanup waits for active delivery. Copied attachments obey both the per-file limit and tickets.maxTicketAttachmentBytes (1 GiB by default), including cached files from earlier exports; an over-budget snapshot is rejected before downloads, and streaming is capped before writes. Retained Open and Closed ticket permissions are reconciled before Gateway readiness and during maintenance, including held/not-yet-due tickets and when new ticketing is disabled. Failed response deletions remain pending and retry with bounded backoff. Community event retries are bounded, and pending welcomes, answers, and faction actions are recovered from persisted actor/routing/cleanup context after restart with fresh authorization checks. Legacy pending entries without enough context are recorded as skipped without guessing a destination. Schema version 4 preserves the newest faction choice and cooldown-bearing answer action per user across restart and completed-intent retention; older intents complete without replaying obsolete mutations or extending newer cooldowns. Recovery yields after four minutes without completed steps while allowing long plans to keep progressing. Recovery uses persisted intents plus Discord's recent-message nonce deduplication; this does not guarantee exactly-once message delivery after an arbitrary long crash. Discord may not replay join or command events lost during an outage.

No live Discord token was available during implementation. Permission/interaction acceptance, exact legacy response comparison, application installation, Linux-host backup restoration, and per-feature production cutover still require the deployment configuration and a dedicated test guild. Leave existing Ticket Tool tickets with Ticket Tool and disable old handlers only after verifying their configured replacements.

### Local verification (2026-10-03)

- Release build completed with zero warnings and errors.
- All 243 relevant cases passed across focused Community, Configuration, TicketService, PostgreSQL, GatewayConfiguration, Compose secret projection, and password-rotation runs, including 15 real PostgreSQL store cases, four real PostgreSQL entrypoint cases, and 11 Linux shell-helper cases. The actual Compose database passed authenticated health and received only its password projection. The preceding review batch also passed 40 Linux archive/delivery cases. Isolated containers, volumes, and networks were removed; no game-server tests or live deployment were run.
- The disabled sample validates successfully; enabling its unresolved features rejects all missing inputs before credential loading.
- Linux container publication and credential-free validation passed; the runtime image runs as UID 1654. Compose configuration validation passed.
- This Windows machine's normal Docker NuGet restore failed with an SSL PartialChain error. Linux publication was verified with an ignored, temporary offline feed containing the packages already restored successfully on Windows. TLS validation remains enabled; the tracked Dockerfile retains its normal NuGet restore. Verify the normal network build on the actual Linux host.

Build once, then run the focused tests with a disposable PostgreSQL database configured through `SWLOR_BOT_TEST_DATABASE`:

```sh
dotnet build SWLOR.DiscordBot.Tests/SWLOR.DiscordBot.Tests.csproj --configuration Release -p:RunPostBuildEvent=Never
dotnet test SWLOR.DiscordBot.Tests/SWLOR.DiscordBot.Tests.csproj --configuration Release --no-build --filter 'FullyQualifiedName~CommunityTests|FullyQualifiedName~ConfigurationTests|FullyQualifiedName~TicketServiceTests|FullyQualifiedName~PostgresStoreTests|FullyQualifiedName~GatewayConfigurationTests'
```

The PostgreSQL tests require this explicitly supplied test connection; they never discover or use the game database. Stop and remove any disposable test container when verification is complete. The local verification container was stopped and removed after the final tests.
## Reference documentation

- [Discord Gateway and intents](https://docs.discord.com/developers/events/gateway)
- [Discord interactions and response deadlines](https://docs.discord.com/developers/interactions/receiving-and-responding)
- [Discord permissions and role hierarchy](https://docs.discord.com/developers/topics/permissions)
- [Discord channels and category capacity](https://docs.discord.com/developers/resources/channel)
- [Discord.Net documentation](https://docs.discordnet.dev/guides/introduction/intro.html)
- [Ticket Tool official links](https://docs.tickettool.xyz/)
- [Ticket Tool general panel options](https://docs.tickettool.xyz/dashboard/panel-configs/general-options)
- [Ticket Tool open/closed permissions](https://docs.tickettool.xyz/dashboard/panel-configs/permission-options)
- [Ticket Tool automation options](https://docs.tickettool.xyz/dashboard/panel-configs/automation-options)
- [Dyno welcome configuration](https://docs.dyno.gg/en/modules/welcome)
- [Dyno joinable roles](https://docs.dyno.gg/en/modules/autoroles)
- [Dyno custom commands](https://docs.dyno.gg/en/modules/customcommands)
- [Dyno custom-command inspection](https://docs.dyno.gg/en/commands/customs)
- [Ticket Tool configuration backup boundaries](https://docs.tickettool.xyz/dashboard/server-configs.md)
