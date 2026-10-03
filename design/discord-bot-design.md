# SWLOR Discord Bot Design

Status: draft; live-server parity audit partially observed and blocked. Updated 2026-10-03.

## Confirmed scope

- Deploy to the owner's existing Linux machine using Docker.
- Replace Ticket Tool's ticket workflow: a button in a dedicated channel creates a private text channel in the staff section; configured staff roles and the requester have access; staff can rename it; the requester can close it; closed channels are cleaned up periodically.
- Replace the Dyno features currently used for welcome messages, faction-channel joining, and quick answers to common questions.
- Leave room for conversation summaries, staff searches using AI, and game integration.
- Inspect the existing Discord server and bot dashboards to discover configuration rather than asking the owner to transcribe every setting.

The current request covers design and discovery. Runtime implementation, installation of a new Discord application, and production cutover are subsequent work. Existing bots remain operational during discovery.

## Discovery status and evidence

| Item | Status | Evidence or next action |
| --- | --- | --- |
| Hosting | Confirmed | Linux; Docker preferred. Existing database, capacity, backup location, and deployment access remain to be inspected. |
| Existing ticket bot | Confirmed | Owner identified Ticket Tool. Use its canonical dashboard at https://tickettool.xyz/manage-servers. |
| Discord guild, channels, roles, and permissions | Partially observed | Dyno identifies SWLOR as guild 484936923341651971. Dashboard counts were observed; individual channel/role IDs and permissions remain unverified. |
| Ticket Tool panels and behavior | Access confirmed; panels not observed | Signed-in Manage Servers page lists SWLOR under Servers with Ticket Tool. Inspect every panel and its open/closed permissions, messages, limits, and automation. |
| Dyno configuration | Partially observed | Bot nickname Yoda; Custom Commands module enabled with 24/25 configured command slots. Prefix ?, full 24-command name/description inventory, Announcements welcome text, and 12 joinable faction role names observed. Exact command responses, switch states, and stable role/channel IDs remain unverified. |
| Live inspection access | Blocked after partial Chrome audit | Owner signed into both dashboards in Chrome. Read-only inspection reached the Dyno general dashboard, Welcome, Announcements, Autoroles, Joinable Ranks, the full custom-command listing, and Ticket Tool Manage Servers in a fresh chat. Computer Use then explicitly stopped because it could not determine the current Chrome URL confidently enough to enforce policy. No bot settings were saved or changed, and no tickets or messages were created. Remaining configuration requires supported browser access or owner-provided configuration evidence. |
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
| Ticket Tool access | Authenticated Manage Servers page at `https://tickettool.xyz/dashboard` lists SWLOR under Servers with Ticket Tool, with a Manage button | Existing Chrome tab. Panel configuration is not yet observed. |

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

### Remaining live-audit gaps

| Gap | What is still needed |
| --- | --- |
| Ticket panels | Panel/message IDs, button labels, opening content, prompts, categories, limits, and eligibility. |
| Ticket access and operations | Stable staff/observer role IDs; opened/closed requester permissions; rename, close, claim, participant, reopen, and delete behavior. |
| Ticket cleanup and archives | Cleanup timing, inactivity rules, export prerequisites, transcript/log destinations, retention and attachments, existing-ticket inventory. |
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

The source chat had a missing-path runtime initialization failure. Only its two narrowly identified helpers were stopped; no global runtime/security configuration, browser sessions, Codex host, or unrelated helpers were changed. The owner authorized this fresh chat, where initialization and native Chrome accessibility reading succeeded. Chrome was unavailable to the browser-provider API; native screenshots and clicks remained unusable, and the native URL safety check eventually stopped Computer Use. Honor that stop: do not extract session credentials, change browser security, invent a helper protocol, or repeatedly fork tasks to evade the URL check. Further live inspection requires a functional supported browser connection.

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

1. Renew the failed browser-tool runtime, finish the partially observed parity inventory using the existing authenticated Chrome session and published Wiki baseline, and identify only decisions the configuration does not answer.
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
