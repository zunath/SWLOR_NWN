# Coding Standards

Read before writing or changing code, generators, or hak data.

Sections: Code · Renames · Module JSON · Stat-driven gameplay · Haks, TLK, and 2DA

## Code

- Before writing code in an unfamiliar area, find two or three existing examples of the same kind of thing and follow them. Never call a method you have not found.
- New code puts one type per file, in a folder matching its namespace and feature. When you substantially change a file that holds several types, split it.
- Each distinct ability gets its own `*AbilityDefinition.cs` and matching `IAbilityListDefinition` class named for it. Ranks of one ability share its file; unrelated abilities never share one.
- Match the surrounding comment density: at most a one-line comment, only for a non-obvious constraint. No dates, incident history, investigation notes, or "verified in the client" claims in code; those go in the pull request or the topic guide.
- `[NWNEventHandler]` and `ExecuteScript` script names are constants in `SWLOR.Game.Server/Core/ScriptName.cs`, never string literals.
- `DB.Search` returns at most 50 rows when the query has no `AddPaging`. A query that must load a complete set calls `AddPaging(<cap>, 0)`.
- No internal initiative, milestone, or phase labels (such as `CombatUpgrade`) in new identifiers, filenames, namespaces, or comments. Use gameplay terms.
- When an ability applies `EffectDamage` with `ApplyEffectToObject`, wrap it in `AssignCommand(source, () => ApplyEffectToObject(...))` so the damage appears in the player's combat log.
- Player chat commands use `.Permissions(AuthorizationLevel.All)`. `AuthorizationLevel.Player` alone silently rejects DM and admin accounts with "Invalid chat command".

## Renames

- When a rename affects something identified by an enum member (perk, skill, ability, item, key item, recipe, quest, NPC group, status effect, and similar), rename the enum member too, keeping its numeric value.
- Every enum rename needs migration code that moves old data to the new name. Persisted data stores enum names, not just numbers: Newtonsoft writes enum dictionary keys by name (for example `Player.Perks`, key items, and quest kill progress), so an unmigrated rename silently drops those entries on load. See `Migrations.md`, "Enum renames".
- A rename keeps the thing's identity. Replacing it with something different is a new definition instead (see "Haks, TLK, and 2DA").

## Module JSON

- Never read a whole area file (`Module/git/*.git.json` reaches 25 MB). Search for the object and read only its entry.
- Edit surgically. Never round-trip a file through `json.dump` or another serializer: it rewrites float formatting (`3.36e-043`) and CRLF line endings across the whole file. Prove a writer by round-tripping an untouched file byte-for-byte first.
- Some files carry raw non-UTF-8 bytes inside NWN color tokens (for example `Module/itp/placeablepalcus.itp.json`). That is valid data; read and write those files as bytes.

## Stat-driven gameplay

- Shared combat, ability, and status-effect code never special-cases specific perks or perk-specific status-effect classes. Model perk behavior as `StatType` adjustments that shared systems read. Direct perk checks are only for ownership, unlock, purchase, UI, or progression gates.
- Declare `StatType` classification, polarity, and category with `StatTypeAttribute` on the enum entry, not with `if`/`switch` lists elsewhere.
- Attack Deflection, Shield Deflection, and Guard are separate mechanics. Attack and Shield Deflection are attack-roll outcomes that negate the hit and do not stack with each other. Guard is a damage-stage outcome that reduces damage and raises enmity. Never implement one with another's state, stats, logs, or triggers.

## Haks, TLK, and 2DA

- Model compilation uses CleanModels v4.4.2 bundled with cleanmodels-qt v1.1.7, installed by `SWLOR_Haks/tools/SetupModelCompiler.ps1`. Use the shared compile helpers; never run `nwnmdlcomp.exe` or the NWN game to compile models.
- Replace by adding: a replaced stat, item property, 2DA row, TLK string, or enum value gets a new definition, and the old one stays untouched (hidden from builders only if asked).
- New custom TLK strings take the first placeholder slot in `SWLOR_Haks/sw_tlk/sw_tlk.tlk.json` before appending at the end. Never set an entry's text to empty: `nwn_tlk` drops it and every later strref shifts. Regenerate `sw_tlk.tlk` after editing the JSON and round-trip it: the entry count stays the same when you filled placeholders and grows by exactly the number of entries you appended, and every other entry's text is unchanged.
- Never delete a 2DA row; retire it in place. Row indices are referenced by enums and other data.
- 2DA references to custom TLK entries use `16777216 + tlkId` (entry `50003` is `16827219`). Raw IDs are only valid for base-game `dialog.tlk`. When moving or adding an entry, update every reference.
- `RecastGroup` short names are player-facing and at most 14 characters. Choose a meaningful label; never auto-truncate, and make generators fail when one is missing.
- Hak files live in the `SWLOR_Haks` submodule: commit there first, then the pointer bump (see `PullRequests.md`).
