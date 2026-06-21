# Player Recognition And Disguises Plan

## Purpose

This plan introduces player-specific character recognition for SWLOR, then builds an optional disguise system on top of it.

The core problem is that the NWN client exposes a character's true name in places where the character may not be known in-character. SWLOR will launch the system with all other player characters unknown by default, leaving players to re-establish remembered names through the new tools.

The implementation should be light-touch, player-friendly, and auditable. It should support roleplay, aliases, hidden identities, and double lives without turning recognition into homework or a hard stealth-vs-detection subsystem.

## Design Principles

- Player trust comes first. The system supports RP presentation and memory; it is not a replacement for consent, judgment, or staff moderation.
- Other player characters are unknown by default until the observer names them.
- Recognition is personal. One player naming another character does not rename that character globally.
- Disguises are presentation tools. They obscure IC recognition, but they do not erase staff visibility or server accountability.
- Staff, audit, abuse-report, and diagnostic records must always retain true character identity.
- Player-facing combat, chat, target, and inspect surfaces should not leak true identity when the observer should see an unknown or disguised identity.
- The first release should avoid automatic detection rolls, passive identification, and Espionage requirements.

## Reference Behavior

NWN.Xenomech uses NWNX Rename to apply per-observer PC name overrides:

- `/name` targets another PC and stores the observer's personal name for that target. When used on yourself, it sets the gray unknown description shown to players who have not personally named you.
- Stored names are keyed by observer player id and target player id.
- Stored names and unknown descriptions are limited to 64 characters and reject player-entered color tokens. The service owns gray unknown-name styling.
- On player enter, the service applies default unknown names, self true-name visibility, and saved name overrides between online players.
- NWNX Rename settings are enabled for module character list and player list behavior.

SWLOR should use the same broad pattern, adapted for its older live-world history and broader set of custom UI and message surfaces.

## Step 1: Known Names Foundation

Goal: introduce player-specific recognition without disguises.

This step creates the core infrastructure needed for both ordinary known names and later disguise identities. Players can record what their character knows another PC as, but characters cannot yet present separate disguise identities.

### Scope

- Add an NWNX Rename wrapper to `SWLOR.NWN.API`.
- Add Rename plugin environment settings to the SWLOR Docker configuration.
- Add persistent per-player known-name storage.
- Add a player-facing command for targeted naming, such as `/name <name>`. Self-targeting this command updates the character's unknown display description, not the character's true name.
- Validate `/name` input before persistence: reject empty input, reject values longer than 64 characters, and reject player-entered color tokens instead of silently stripping them.
- Add a separate forget command, such as `/forgetname`, to remove a personal override without reserving words that could be valid character names.
- Apply name overrides when players enter, relog, and encounter other online PCs.
- Preserve true-name display for self and DMs.
- Show unnamed players as the gray unknown-facing descriptor only. Once an observer names a target, show the observer's assigned name plus the gray descriptor in brackets, for example `Joe Blow [A Seedy Individual]`.
- Show staff the true character name plus the unknown-facing descriptor, with the descriptor still wrapped as unknown/gray text. If the player has not self-assigned a description, staff see the default unknown label, for example `Joe Smith [Someone]`.
- Preserve true names in staff tools, audit logs, abuse reports, crash logs, diagnostics, and database records.
- Add a central display-name resolver for SWLOR-authored UI and messages.
- Update high-value player-facing surfaces to use observer-specific display names.
- Verify engine-generated name display and combat-log behavior under NWNX Rename.

### Persistence

Add a dedicated persistence entity instead of expanding the existing player entity.

Suggested shape:

- Observer player id.
- Dictionary keyed by target player id.
- Stored display name.
- Created and updated timestamps if the existing persistence style supports them cleanly.

The first version only needs one remembered name per observer-target pair. Separately, each player can store one self-assigned unknown display description for use when an observer has not named them.

### Name Application

Create a service responsible for all player-name override behavior.

Responsibilities:

- Determine the display name one player should see for another.
- Apply default unknown names when appropriate.
- Apply true names for self and DMs.
- Apply stored known-name overrides.
- Reapply overrides on player enter and when online players become relevant to one another.
- Provide a single helper for SWLOR-authored messages and UI, for example `GetDisplayName(observer, target)`.

### Migration

SWLOR will launch with a clean recognition state.

Recommended rollout behavior:

- Other player characters display as `Someone` by default.
- Players use `/name <name>` to record who their character recognizes.
- Players can target themselves with `/name <description>` to replace their default unknown `Someone` text while remaining visually marked as unknown.
- Players use `/forgetname` to remove a remembered name.
- No legacy visibility cutoff is used.
- No large pairwise migration is required to seed existing relationships.

### Player-Facing Surfaces

Update SWLOR-authored surfaces where identity leaks would undermine the feature:

- Target status UI.
- Character sheet or inspect-style UI.
- Custom chat presentation where SWLOR writes sender or target names directly.
- Custom combat, death, healing, status-effect, and ability messages.
- Party, group, or nearby-player summaries where non-DM players see PC names.

Keep canonical names in:

- DM tools.
- Admin tooling.
- Server logs.
- Audit logs.
- Abuse reports.
- Database records.
- Diagnostics and exception details.

### Combat Logs

Combat feedback must be part of Step 1, not a later cleanup item.

Player-facing combat messages should use the observer-specific display name when SWLOR authors the text. Engine-generated combat logs need explicit verification because NWNX Rename may already affect some client-side display while custom `SendMessageToPC` flows will not.

Testing should cover:

- Basic attacks.
- Ability damage.
- Healing.
- Death messages.
- Status-effect application and removal.
- PvP interactions.
- Group combat where multiple observers know the same target by different names.
- DM observation.

### Exclusions

Step 1 does not include:

- Disguises.
- Aliases as active presented identities.
- Espionage integration.
- Detection rolls.
- Passive identification.
- Appearance, helmet, armor, species, cybernetic, or body-part enforcement.

### Acceptance Criteria

- A player can target another PC and set a personal name for them.
- The name is observer-specific and does not affect what other players see.
- The name persists across relog.
- Forgetting the name restores default behavior.
- Self and DM views retain true identity.
- Other player characters default to unknown until personally named.
- Key player-facing UI and custom messages use the display-name resolver.
- Staff and audit records retain true identity.
- Combat-log behavior is verified and documented.

## Step 2: Disguise System

Goal: let players temporarily present a separate identity on top of the known-name foundation.

This step adds opt-in disguise identities. A disguise changes the identity a character presents to other players, but it does not conceal true identity from staff tools or logs.

### Scope

- Add active disguise state to a character.
- Allow every character to use at least one basic disguise.
- Add a player-facing management flow for setting, activating, and removing a disguise.
- Allow a disguise display label such as `Masked Courier`, `Sith Trooper`, `Dockworker in a gray coat`, or `Helmeted Mandalorian`.
- Make the active disguise label the presented name for other players.
- Allow players to remember or name a disguise separately from the real character.
- Restore normal known-name behavior when the disguise is removed.
- Add staff-visible audit history for disguise changes.
- Add cooldown or conflict restrictions to prevent rapid identity swapping.

### Player Interaction

Prefer a targeted feat, dialog, or NUI flow over making slash commands the primary interface.

The first version can still include slash commands as backup controls for power users, staff, or troubleshooting, but routine use should be simple in game.

Suggested actions:

- Set active disguise label.
- Activate disguise.
- Remove disguise.
- Rename a targeted PC or disguise identity.
- Forget a remembered name.

### Disguise Rules

The system should not try to prove whether a disguise is plausible.

Players are expected to make disguises make sense through RP, outfit, helmet, voice, mannerisms, faction uniform, and context. Obvious identifying features, such as cybernetics or species traits, should remain an RP and moderation concern rather than hardcoded appearance logic.

Guardrails should include:

- Cooldown on changing disguise labels.
- Restriction on changing disguises during active conflict.
- Staff-visible true identity.
- Staff-visible disguise history.
- Clear player rules that disguises do not erase consequences.

### Disguise Recognition

Disguise identities should be remembered separately from the real character.

Example:

- A player knows a character as `Vessa Tal`.
- The same character activates a disguise labeled `Masked Courier`.
- Other players see the presented identity and can remember that disguise separately.
- Removing the disguise restores normal known-name behavior.

The system should not automatically link the disguise identity to the true identity for observers.

### Exclusions

Step 2 should not include:

- Automatic detection rolls.
- Passive proximity-based identity reveals.
- Espionage requirements.
- True-name reveal mechanics.
- Hardcoded appearance validation.
- Separate detection skill checks.
- Random mechanics that can forcibly ruin an ongoing disguise RP plan.

### Acceptance Criteria

- A player can set and activate a disguise label.
- Other players see the disguise label instead of the normal known-name result.
- The disguise does not change staff true-identity visibility.
- The disguise has an audit trail.
- Removing the disguise returns the character to normal known-name display.
- Players can remember the disguise identity separately from the real character.
- Cooldowns or conflict restrictions prevent rapid abuse.

## Future Expansion

Future work should be based on actual player usage after Steps 1 and 2 are live.

Possible expansions:

- Saved disguise profiles.
- Additional disguise slots.
- Espionage perks that improve disguise convenience rather than forcing identity reveals.
- Faction uniform support.
- Forged credentials.
- Scanning tools.
- DM-assisted bounty or investigation flows.
- Suspicion prompts that suggest familiarity without revealing true identity.

Avoid adding passive detection or automatic true-name reveals until the basic system has been used in production and the real abuse patterns are understood.

## Implementation Notes

Likely code areas:

- `SWLOR.NWN.API` for the NWNX Rename wrapper.
- `SWLOR.Game.Server/Entity` for known-name and disguise persistence.
- `SWLOR.Game.Server/Service` for player recognition and display-name resolution.
- `SWLOR.Game.Server/Feature/ChatCommandDefinition` for `/name`.
- `SWLOR.Game.Server/Feature/GuiDefinition` for target, inspect, and management UI surfaces.
- `SWLOR.Game.Server/Docker/swlor.env` for Rename plugin settings. The running server image must include `NWNX_Rename`; this feature should fail deployment verification if that plugin is unavailable.

Use a central display-name helper wherever SWLOR writes player-facing text. Avoid spreading raw `GetName(target)` calls into UI or message code where the observer matters.

## Rollout Messaging

The player-facing proposal should emphasize:

- This is a recognition and RP support tool.
- Other player characters begin unknown by default, and players can rebuild recognition with `/name`.
- Players are not expected to mechanically rename every person they meet.
- Disguises are planned as a second step, after the name foundation is stable.
- Disguises will support aliases and hidden identities, but not remove accountability.
- Staff logs and tools always retain true character identity.
