# Migrations

Read before adding or changing a server or player migration, or any change to persisted data.

Sections: How migrations run · One migration per release · One pass per data set · Server before player · Saved-data item sweeps · Character upgrade · Enum renames · Testing

## How migrations run

- Server migrations implement `IServerMigration` (`Feature/MigrationDefinition/ServerMigration/`). `PostDatabaseLoad` runs on database load; `PostCacheLoad` runs after the module cache loads (use it when you need item blueprints). Versions are keyed per execution type, so one version number can have one class in each phase.
- `ServerConfiguration.MigrationVersion` advances only after both phases succeed. A failure shuts the server down, and the next boot reruns every migration above the stored version, so every migration must be idempotent.
- Server migrations production has already run are deleted, not kept. `Migration.RetiredServerMigrationVersion` is the newest deleted version: a new database starts there, and a database below it refuses to boot (upgrade it with an older build first). When you retire more versions, raise the constant to the newest one production has run and delete their classes and tests.
- Player migrations implement `ICharacterMigration` (`Feature/MigrationDefinition/CharacterMigration/`) and run at boot, after the server migrations, over every character file in the server vault and its `Player` record, including characters whose players have not logged in since (see "Character upgrade"). `Migrate(SavedCharacter)` edits the saved character file; `MigratePlayerData(Player)` edits only the record. Login refuses a character the upgrade has not brought to the latest version.
- Load complete sets with `AddPaging(count, 0)` and log progress with `ServerMigrationProgress`.

## One migration per release

A release is a production deployment. Each release gets at most one server migration version and one player migration version.

1. Find the newest server or player migration of the kind you need.
2. Ask the user whether it has shipped to production, unless they already said so in this task. Nothing in the repository proves deployment: GitHub release tags do not (the `2026-10-16` release contains server migration 27 and player migration 22, which were still unreleased afterwards).
3. If it has not shipped, add your change to it (same version, same pass) instead of adding a version. If it has, add the next version; later changes before the next release go into that one.
4. Folding into a migration that already shipped means production never runs the change, so never guess.
5. An unreleased migration may already have run on the test server. If the test server needs your change, ask before relying on a rerun.

## One pass per data set

Large data sets are loaded and saved once per release, with every change for that release applied in the same loop.

- Stored items live in banks and storage (`InventoryItem`), the market (`MarketItem`), city and property storage (`WorldPropertyCategory`, `WorldProperty`), research jobs, outfits, DM creatures, player ships, and quest contracts. Droid contents are serialized inside the controller item's `CONSTRUCTED_DROID` local, so item traversals recurse into droids.
- Extend the release's existing sweep (see `StoredItemSweep` and `StoredItemConverters`) rather than writing another loop over the same records. Never add a version that reruns a full sweep another version already performs.
- On the player side, apply all of a release's item changes in one traversal of equipped, carried, and droid items.

## Server before player

Prefer server migrations for database records. Use a player migration only for data that lives in the character file (equipped and carried items, droid contents, feats, levels, the quickbar) and the record changes that follow from it, and keep it to that data. Both run at boot over every character, online or not.

## Saved-data item sweeps

The stored-item sweep (`StoredItemSweep`) edits saved GFF directly instead of loading each stored item into the engine. `ParallelStoredItemPass` converts each batch of records on worker threads with the rules in `StoredObjectData.ItemSweep.cs` and saves the changed records from the main thread, so a production sweep takes about a minute at boot. A release that changes stored items calls `StoredItemSweep.Migrate(Version)` from its server migration (currently `_29_CorrectStoredCarapaceBracers`).

- Conversion runs off the main thread: no NWScript, NWNX, engine or `DB` calls. The pass does the loading and saving.
- Each saved-data rule mirrors the engine rule for live items (`HeadgearSlotMigration`, `EquipmentTypeMigration`, `ItemIconMigration`, `ItemAppearanceCatalogMigration`, `LootStatRevisionMigration`). A change to either changes the other in the same commit and adds the legacy kind to the `SavedDataItemSweepMatchesEngine` engine test (filter `SavedDataItemSweep`), which runs both on the same saved data and compares what the engine loads.
- Write new GFF data the way the engine saves it: a property built with `ItemPropertyCustom` and -1 arguments saves Subtype 65535 and Param1Value 255, item properties are numbered by list index, and locals use struct type 0.
- For a new kind of rule, also check parity on a production copy: boot one copy with a local build whose `StoredItemSweep.Migrate` passes `EngineStoredItemConverter.Instance` (the engine baseline, hours on production data) and another with the normal build, then compare the decoded records. The engine also re-saves older items in the current format (new default fields, reset orientation, reversed locals, reordered creature inventories); those differences are expected.

## Character upgrade

`CharacterFileUpgrade.UpgradeServerVault` (called from `Migration.AfterCacheLoaded`) reads every `.bic` in the server vault, loads each record on the main thread, converts on worker threads, and saves from the main thread. Character migrations follow the saved-data rules above: no NWScript, NWNX, engine or `DB` calls inside `Migrate`; read game data from `SavedCharacterData`, which `Load()` fills on the main thread first.

- Checkpoints: the file's root `PlayerFileVer` field and `Player.Version`, as before. `Player.CharacterFileVersion` records which file migrations already changed the record, so a retry after a crash between the record save and the file write changes only the file. `ServerConfiguration.CharacterFileVersion` skips the pass once every character is current; a character that fails keeps its saved state, is logged, and is tried again at the next boot.
- Saving order: overflow items, a backup of the original file, the upgraded file written beside it as `<file>.upgrade`, the record, then a move of the upgraded file over the original. A boot that finds a leftover `.upgrade` file moves it into place when the record already holds that upgrade and deletes it otherwise. The backup goes to `servervault_backups/character-upgrade-<version>-<UTC time>/`, beside the real server vault folder (the server image links its run folder's `servervault` to the mounted home).
- Before the first migration, `SavedCharacterLoad` applies the item changes that loading a character into the engine used to make (legacy property conversion, retired basic weapons, pistol base items, missing Delay on worn weapons), so migrations see what they saw at login.
- Write character data the way the engine saves it. `SavedCharacter` keeps the inventory grid (10 x 36 in pages of 6 rows; an item never spans pages), retargets or clears quickbar item buttons when items move, and writes hit points at full: login restores current HP from the record.
- Items that do not fit go to `OverflowItem` records with ids derived from the upgrade, so a retry replaces them. Players claim them with `/overflow`; login tells them when any wait. Messages a migration adds to `SavedCharacter.Notices` are saved on the record (`Player.UpgradeNotices`) and shown at the next login.
- Test a character migration on saved data: run it with `CharacterFileUpgrade.Upgrade` or on a `SavedCharacter` built from a fixture, load the result in the engine, and check what the engine loads.

## Enum renames

Renaming an enum member (required by `CodingStandards.md`, "Renames") always comes with migration code that moves old data to the new member.

- Map each old name to its new member in a rename table, and rewrite raw records before they deserialize. The reference is `RelocatedDungeonEnumMigration`, which a `PostDatabaseLoad` server migration applies to raw Player JSON.
- Also add a read-time fallback on each affected property so records that skip the migration (restored from a backup or imported after the version was recorded) still load. The reference is `RelocatedKeyItemsJsonConverter` and `RelocatedKillProgressesJsonConverter`, attached in `Player.cs` with `[JsonConverter]`; test that a legacy record deserializes without the migration.
- Cover every place the old name can live: dictionaries keyed by the enum on `Player` and other entities, item and character locals, serialized item or object data, and stored strings such as search fields.
- Add the rewrite to the release's existing migration and pass ("One migration per release", "One pass per data set").
- Test from a legacy record that still uses the old name, and assert the entry survives under the new one.

## Testing

- Migration tests start from legacy data and run the actual conversion first, then repeat use, change, save, and reload at least twice with ownership and quantity checks (`EngineTesting.md`, "Persistence and migration coverage").
- Droid item changes run the `DroidInventory` engine filter.
- Do not write one-off migrations to remove or refund deleted perks, blueprints, or skills; the planned full character rebuild handles character-build data.
