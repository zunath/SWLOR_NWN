# Combat Upgrade Migration Notes

This note tracks player migration work for `feature/combat-upgrade`. Keep it current when carrying over more experimental branch work so future agents can reason about player impact without re-discovering it from diffs.

## Current Migration Hook

- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_22_CombatSystemReplacement.cs`
- Stored item helper: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/StoredItemDataMigration.cs`
- Player migration: `SWLOR.Game.Server/Feature/MigrationDefinition/PlayerMigration/_14_MigrateResistanceItemProperties.cs`
- Player migration: `SWLOR.Game.Server/Feature/MigrationDefinition/PlayerMigration/_15_RemoveObsoleteCombatInstructionDiscs.cs`
- Server version 22 has two stages: `_22_CombatSystemReplacement` repairs raw database records at `PostDatabaseLoad`; `StoredItemSchemaMigration` (in the same file) migrates native items and bank storage at `PostCacheLoad`, after skill and recipe caches are available.
- The runner advances the server configuration's migration version only after both stages succeed. A failure blocks subsequent stages and version advancement. Server player-data conversion preserves `Player.Version`; refunds remove or trim their source perk entries in the same save, making retries safe without another version field on players.
- Login migrations use the existing `Player.Version` checkpoint. After each live-object migration succeeds, the runner refreshes the player record and applies `MigratePlayerData` before saving that record with its new version. Player migration 15 grants the bonus rebuild token in that save, so retries cannot repeat it. New characters start at the latest player version and receive their starting token through initialization.
- Nonempty serialized records that cannot be loaded or saved now fail the migration instead of silently being marked complete. Login migration failures disconnect the affected player for staff repair.
- `MigrationObject` applies item-property removals synchronously through the engine handler and verifies removal before subsequent migration steps or serialization. Replacements cannot retain both legacy and new properties. Nested item copies and removals resolve the actual bag with `GetItemPossessor(item, true)`.
- Current behavior:
  - Uses the combat-upgrade skill-cap model: 400 total skill ranks, with Armor contributing normally to the cap and SP progression.
  - Refunds removed or materially changed combat perks during `MigratePlayerData`, before obsolete keys are discarded. Numeric and named aliases count as one investment; legacy blueprint refunds use their original purchase prices.
  - Removes refunded legacy perk keys before the forced rebuild refund path can process them again.
  - Uses `LegacyPerkRefundMigration` to resolve all 265 historical player perk definitions from pre-upgrade master commit `ce4f91749c2e`. Retired proficiencies, styles, and weapon-focus perks receive their original SP investments, including numeric and named aliases. Historical perks whose names are reused at new `PerkType` IDs also refund their original purchase costs. New perk names never inherit refunds from a reused legacy numeric ID.
  - Forces every player through a full rebuild by setting `Player.RebuildComplete = false` in the same save as refunds.
  - Grants each existing player one `CurrencyType.RebuildToken` through player migration 15 on login, in addition to the forced rebuild, so players keep a spare respec for later use.
  - Updates stored item requirement properties to the combat-upgrade skill requirement model.
  - Applies droid CPU/weapon-skill and recipe transformations to carried items, equipped items, nested containers, and controller inventories. Blueprint expansion keeps every replacement recipe in those inventories. Database recipe variants use stable IDs and are written before replacing their source, so retries neither duplicate nor lose alternatives.
  - Preserves obsolete saber-kit stack quantities during conversion and refreshes the replacement name, resref, tag, and icon in storage metadata.
  - Decodes Resistance cost-table rows before merging properties, preserving the distinction between resistance and vulnerability. Numeric resistance dictionary keys are normalized using resistance IDs before legacy defense keys are moved.
  - Splits persisted mitigation data so Physical/Force remain in `Player.Defenses` and elemental/status mitigation lives in `Player.Resistances`.
  - Moves legacy elemental defense entries into resistances, fills missing default keys, and removes Physical/Force from resistances.
  - Adds and normalizes beast resistance purities, removes legacy beast saving throw purities, moves beast elemental purities out of defenses, clears learned beast perks, and returns beast SP to the level-based total.
  - Migrates live and serialized item properties for the new resistance, weapon damage, and weapon delay property model, including untyped `DMG`, separate `WeaponDamageType`, and normalized weapon `Delay` for held weapons and natural creature weapons.
  - Preserves Blueprint item elemental weapon-damage bonuses during item-property migration, but when a Blueprint has multiple elemental damage types, randomly keeps one elemental type and drops bonuses for the other elemental conflicts.
  - Stores Resistance item properties through SWLOR's `iprp_swlrescost.2da` cost table `54`. Negative gameplay vulnerability amounts are encoded as non-negative cost-table row ids `101` through `200` and decoded by runtime stat aggregation.
  - Reuses the legacy cooldown-reduction equipment, enhancement, and food property IDs as Combat Readiness, renames the legacy cooldown-reduction enhancement items in live and serialized storage, drops the old blanket cooldown-reduction player stat during player resaves, and recalculates Combat Readiness from equipped items during player migration.
  - Remaps renamed `RecipeType` dictionary keys (Combat Readiness enhancements, weapon damage enhancement wording, Cooked Sardine) in `Player.UnlockedRecipes`/`Player.CraftedRecipes` raw JSON before invalid-key cleanup would discard them. Recipe unlock data survives the forced rebuild, so renames must remap rather than drop.
  - Removes obsolete Bible perks, stale recast entries, and obsolete combat instruction discs from players, beasts, stored items, markets, world properties, research jobs, outfits, DM creatures, and ships.
  - Normalizes DM-built lightsabers/saberstaffs (any Lightsaber/Saberstaff base item outside the craftable training saber lines and workbench-built sabers) to the tier 5 baseline in place: DMG, weapon damage type, attack delay, enhancement/damage/accuracy bonuses, and the skill requirement are replaced with the tier 5 values and the weapon is stamped with `SABER_TIER = 5` so the tiered Engineering upgrade kits recognize it. Owners keep their weapons. The live login sweep (`_15` + `LegacySaberMigration.MigratePlayer`) covers equipped/carried/nested/droid-held sabers; stored surfaces run through `StoredItemDataMigration` + `LegacySaberMigration.MigrateStoredObject`. DM creature equipment is intentionally left untouched. The retired single-step upgrade kits (`saber_upg1`, `saberstaff_upg1`) become tier II kits with their stack quantities preserved; their obsolete recipe items are removed by the obsolete-item sweep; the tiered kits (II-V and the recipe-unlocked Chiro 5.5 kits) are crafted through Engineering. New sabers still come from the Lightsaber Workbench flow: a DM-issued Kyber Token item converts into one `CurrencyType.KyberToken`, and the workbench placeables (Sith Academy, Jedi Enclave, Dathomir Hidden Cave) consume one per constructed tier 1 saber.
  - Leaves logged-out status-effect runtime cache out of migration. That cache is process-local and empty on the fresh boot that runs server migrations.

## Player-Facing Migration Goals

- Force all players to perform a full rebuild for the combat upgrade.
- The forced rebuild itself is free and does not require spending a rebuild token.
- Additionally, grant every existing player one `CurrencyType.RebuildToken` on login so they have a spare respec banked after the forced rebuild. New characters likewise start with one free rebuild token (`PlayerInitialization.GiveStartingRebuildToken`).
- Use a 400 skill cap. Armor is not exempt from the cap and grants SP through the normal active-skill path.
- Current Bible General perks use Armor skill requirements because Armor is the closest thing SWLOR has to a general character-level proxy.
- Refund SP for removed attack-count/mastery perks so players are not stranded with deleted perk investments.
- Remove deleted perks from persisted `Player.Perks` data.
- Obsolete Heavy/Light Armor and stale Armor perk-tree data should be removed/refunded through the same obsolete-perk cleanup path when present in persisted data. Current Bible General perks that use Armor requirements remain valid.
- Do not add new one-off migrations solely for removed perks, blueprints, skills, or similar character-build data that is already covered by the planned full rebuild.
- Fold additional work into the existing unshipped migrations. Do not change shipped migrations or add a new version for work already covered by the pending rebuild.

## Forced Rebuild Flow

- `_22_CombatSystemReplacement` sets `Player.RebuildComplete = false` for every stored player.
- `PersistentLocation` detects `RebuildComplete == false` on login and sends the player to waypoint `REBUILD_LANDING`.
- `CharacterFullRebuildViewModel` prevents players from leaving the rebuild area until the rebuild is completed.
- Completing the rebuild sets `Player.RebuildComplete = true`.

## Reusing `RebuildComplete`

`RebuildComplete` was introduced for an earlier legacy rebuild, but its current usage is generic: it gates whether a player is allowed to leave the rebuild flow. It is safe to reuse for the combat upgrade as long as the intended behavior is a forced full rebuild.

Important nuance: setting the flag to `false` does not itself reset the character. It redirects and locks the player into the rebuild area. The actual full reset happens when the player uses the rebuild UI's reset action, which refunds all remaining perks/skills, resets stats, and keeps `RebuildComplete = false` until the rebuilt character is saved.

## Examples of Legacy Perk Refunds

- The complete historical ID/name/price table is in `ServerMigration/LegacyPerkRefundMigration.cs`. Current removed perks are listed in `_22_CombatSystemReplacement.PlayerRemovedPerks`.
- Weapon proficiencies: 2 SP per purchased rank; weapon-focus perks: 3 SP and 4 SP; legacy Flurry Style: 1 SP and 4 SP.
- `ImprovedTwoWeaponFightingBlade` level 1: 4 SP
- `ImprovedTwoWeaponFightingHeavyWeapon` level 1: 4 SP
- `Furor` level 1: 4 SP
- `ShieldMaster` level 1: 4 SP
- Weapon mastery perks level 1 and 2: 8 SP per level
  - `VibrobladeMastery`
  - `FinesseVibrobladeMastery`
  - `LightsaberMastery`
  - `HeavyVibrobladeMastery`
  - `PolearmMastery`
  - `TwinBladeMastery`
  - `SaberstaffMastery`
  - `KatarMastery`
  - `StaffMastery`
  - `PistolMastery`
  - `ThrowingWeaponMastery`
  - `RifleMastery`
- `RapidShot` level 1: 3 SP
- `RapidShot` level 2: 5 SP
- `RapidReload` level 1: 3 SP

## Follow-Up Checks Before Release

- Character sheet combat display cleanup is complete: Physical Defense and Force Defense use dedicated bindings, and typed elemental/status mitigation is presented through the Resistance table.
- Static coverage in `CombatUpgradeMigrationCoverageTests` confirms `_22_CombatSystemReplacement` still follows master migration `_21_SetDefaultOutfitAndMarketLimits` and forces `Player.RebuildComplete = false`. `PlayerMigrationTests` verifies the bonus rebuild token is saved with player migration 15's version. If another migration is added first, renumber the combat upgrade migration series and update those tests.
- Keep the removed-perk refund mappings in place. The forced rebuild uses `TotalSPAcquired` for skill redistribution, but deleted perk definitions cannot be refunded by the rebuild UI after their keys are removed; migration must refund those obsolete perk investments before cleanup. Recheck the hard-coded amounts only if the legacy final prices change.
- Static coverage in `CombatUpgradeMigrationCoverageTests` confirms removed-perk cleanup entry points for players, beasts, stale recasts, live player migration, stored item records, constructed droids, and ship/module serialized items. Still spot-check that obsolete Heavy/Light Armor and stale Armor perk-tree rows no longer appear in player-facing builders, default perk maps, instruction discs, or UI surfaces.
- Confirm Armor skill rank-ups count toward the 400 skill cap, grant SP normally, and gate current Bible General perks as intended.
- Confirm stale BAB/attacks-per-round logic remains removed:
  - `Stat.ApplyAttacksPerRound`
  - calls from player initialization/login temporary effects
  - beast/droid setup
  - equip/purchase/refund triggers
- `MigrationDataTests` exercises repeatable server refunds, trimmed ranks, refund limits, mixed resistance aliases, every droid recipe alias, recipe date preservation, beast point limits, and migration failure state. `PlayerMigrationTests` exercises login checkpoints, record refreshes, missing token entries, and retries after failed loads, data hooks, or writes (including lost write acknowledgements).
- `MigrationEngineTests` uses real NWN serialization for stored weapons, carried and serialized droid data, nested blueprints, obsolete containers, saber normalization, replacement stacks, and property replacement. Its storage tests read persisted Redis records directly, exercise partial blueprint expansion retries in banks, markets, and research jobs, preserve property storage identities, cover outfits, saved creatures, and all ship module collections, and verify corrupt records are preserved when migration fails. A staging run against a production database copy remains the release check for the actual saved corpus.
- Confirm the weapon Delay migration updates old Throwing/Vibroknife/natural-weapon and Sling-based pistol values and preserves training-weapon and intentional short-sword delay exceptions in representative live data. Checked-in module templates and embedded `.git` area/store/NPC item instances have already been normalized to the updated delay table.
- Logged-out active status effects are process-local runtime cache only. They are not persisted and do not survive the fresh boot migration path, so no migration cleanup is required.
- Add release notes telling players they must perform a forced full rebuild, that removed combat perks were refunded, and that they were granted a bonus rebuild token for later use.

## Useful Patterns

- Forced full rebuild flag: `Player.RebuildComplete = false`
- Forced full rebuild helper: `ServerMigrationBase.RequireFullRebuildForAllPlayers()`
- Rebuild landing redirect: `PersistentLocation`
- Rebuild completion UI: `CharacterFullRebuildViewModel`
- Historical player refunds: `LegacyPerkRefundMigration.Migrate(...)`; current removed-perk cleanup: `_22_CombatSystemReplacement.CleanPerks(...)`
- Bonus rebuild token grant (existing players): `_15_RemoveObsoleteCombatInstructionDiscs.MigratePlayerData(dbPlayer)`, saved with `Player.Version` by the login migration runner
- Starting rebuild token grant (new characters): `PlayerInitialization.GiveStartingRebuildToken(dbPlayer)`
