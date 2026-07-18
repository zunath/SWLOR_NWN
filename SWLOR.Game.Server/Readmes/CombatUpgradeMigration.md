# Combat Upgrade Migration Notes

This note tracks player migration work for `feature/combat-upgrade`. Keep it current when carrying over more experimental branch work so future agents can reason about player impact without re-discovering it from diffs.

## Current Migration Hook

- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_22_CombatSystemReplacement.cs`
- Stored item helper: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/StoredItemDataMigration.cs`
- Player migration: `SWLOR.Game.Server/Feature/MigrationDefinition/PlayerMigration/_14_MigrateResistanceItemProperties.cs`
- Player migration: `SWLOR.Game.Server/Feature/MigrationDefinition/PlayerMigration/_15_RemoveObsoleteCombatInstructionDiscs.cs`
- Server execution type: `PostDatabaseLoad`
- Current behavior:
  - Uses the combat-upgrade skill-cap model: 400 total skill ranks, with Armor contributing normally to the cap and SP progression.
  - Refunds removed or materially changed combat perks through `RefundPerksByMapping`.
  - Removes refunded legacy perk keys before the forced rebuild refund path can process them again.
  - Preserves legacy numeric `FlurryStyle` saves as the current `FlurryStyle` perk so that investment is not silently dropped.
  - Forces every player through a full rebuild by setting `Player.RebuildComplete = false` via `RequireFullRebuildForAllPlayers()`.
  - Grants every player one `CurrencyType.RebuildToken` via `GrantCombatUpgradeRebuildToken` in addition to the forced rebuild, so players keep a spare respec for later use.
  - Updates stored item requirement properties to the combat-upgrade skill requirement model.
  - Splits persisted mitigation data so Physical/Force remain in `Player.Defenses` and elemental/status mitigation lives in `Player.Resistances`.
  - Moves legacy elemental defense entries into resistances, fills missing default keys, and removes Physical/Force from resistances.
  - Adds and normalizes beast resistance purities, removes legacy beast saving throw purities, moves beast elemental purities out of defenses, clears learned beast perks, and returns beast SP to the level-based total.
  - Migrates live and serialized item properties for the new resistance, weapon damage, and weapon delay property model, including untyped `DMG`, separate `WeaponDamageType`, and normalized weapon `Delay` for held weapons and natural creature weapons.
  - Preserves Blueprint item elemental weapon-damage bonuses during item-property migration, but when a Blueprint has multiple elemental damage types, randomly keeps one elemental type and drops bonuses for the other elemental conflicts.
  - Stores Resistance item properties through SWLOR's `iprp_swlrescost.2da` cost table `54`. Negative gameplay vulnerability amounts are encoded as non-negative cost-table row ids `101` through `200` and decoded by runtime stat aggregation.
  - Reuses the legacy cooldown-reduction equipment, enhancement, and food property IDs as Combat Readiness, renames the legacy cooldown-reduction enhancement items in live and serialized storage, drops the old blanket cooldown-reduction player stat during player resaves, and recalculates Combat Readiness from equipped items during player migration.
  - Remaps renamed `RecipeType` dictionary keys (Combat Readiness enhancements, weapon damage enhancement wording, Cooked Sardine) in `Player.UnlockedRecipes`/`Player.CraftedRecipes` raw JSON before invalid-key cleanup would discard them. Recipe unlock data survives the forced rebuild, so renames must remap rather than drop.
  - Removes obsolete Bible perks, stale recast entries, and obsolete combat instruction discs from players, beasts, stored items, markets, world properties, research jobs, outfits, DM creatures, and ships.
  - Normalizes DM-built lightsabers/saberstaffs (any Lightsaber/Saberstaff base item outside the craftable training saber lines and workbench-built sabers) to the tier 5 baseline in place: DMG, weapon damage type, attack delay, enhancement/damage/accuracy bonuses, and the skill requirement are replaced with the tier 5 values and the weapon is stamped with `SABER_TIER = 5` so the tiered Engineering upgrade kits recognize it. Owners keep their weapons. The live login sweep (`_15` + `LegacySaberMigration.MigratePlayer`) covers equipped/carried/nested/droid-held sabers; stored surfaces run through `StoredItemDataMigration` + `LegacySaberMigration.MigrateStoredObject`. DM creature equipment is intentionally left untouched. The retired single-step upgrade kits (`saber_upg1`, `saberstaff_upg1`) and their recipe items are removed by the obsolete-item sweep; the tiered kits (II-V and the recipe-unlocked Chiro 5.5 kits) are crafted through Engineering. New sabers still come from the Lightsaber Workbench flow: a DM-issued Kyber Token item converts into one `CurrencyType.KyberToken`, and the workbench placeables (Sith Academy, Jedi Enclave, Dathomir Hidden Cave) consume one per constructed tier 1 saber.
  - Leaves logged-out status-effect runtime cache out of migration. That cache is process-local and empty on the fresh boot that runs server migrations.

## Player-Facing Migration Goals

- Force all players to perform a full rebuild for the combat upgrade.
- The forced rebuild itself is free and does not require spending a rebuild token.
- Additionally, grant every player one `CurrencyType.RebuildToken` so they have a spare respec banked after the forced rebuild. New characters likewise start with one free rebuild token (`PlayerInitialization.GiveStartingRebuildToken`).
- Use a 400 skill cap. Armor is not exempt from the cap and grants SP through the normal active-skill path.
- Current Bible General perks use Armor skill requirements because Armor is the closest thing SWLOR has to a general character-level proxy.
- Refund SP for removed attack-count/mastery perks so players are not stranded with deleted perk investments.
- Remove deleted perks from persisted `Player.Perks` data.
- Obsolete Heavy/Light Armor and stale Armor perk-tree data should be removed/refunded through the same obsolete-perk cleanup path when present in persisted data. Current Bible General perks that use Armor requirements remain valid.
- Do not add new one-off migrations solely for removed perks, blueprints, skills, or similar character-build data that is already covered by the planned full rebuild.
- Keep existing player migration versions intact. If more player-specific data cleanup is needed, add the next numbered player migration instead of editing old shipped migrations.

## Forced Rebuild Flow

- `_22_CombatSystemReplacement` sets `Player.RebuildComplete = false` for every stored player.
- `PersistentLocation` detects `RebuildComplete == false` on login and sends the player to waypoint `REBUILD_LANDING`.
- `CharacterFullRebuildViewModel` prevents players from leaving the rebuild area until the rebuild is completed.
- Completing the rebuild sets `Player.RebuildComplete = true`.

## Reusing `RebuildComplete`

`RebuildComplete` was introduced for an earlier legacy rebuild, but its current usage is generic: it gates whether a player is allowed to leave the rebuild flow. It is safe to reuse for the combat upgrade as long as the intended behavior is a forced full rebuild.

Important nuance: setting the flag to `false` does not itself reset the character. It redirects and locks the player into the rebuild area. The actual full reset happens when the player uses the rebuild UI's reset action, which refunds all remaining perks/skills, resets stats, and keeps `RebuildComplete = false` until the rebuilt character is saved.

## Perks Currently Refunded By `_22_CombatSystemReplacement`

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
- Static coverage in `CombatUpgradeMigrationCoverageTests` confirms `_22_CombatSystemReplacement` still follows master migration `_21_SetDefaultOutfitAndMarketLimits`, forces `Player.RebuildComplete = false`, and grants a rebuild token via `GrantCombatUpgradeRebuildToken`. If another migration is added first, renumber the combat upgrade migration series and update that test.
- Keep the removed-perk refund mappings in place. The forced rebuild uses `TotalSPAcquired` for skill redistribution, but deleted perk definitions cannot be refunded by the rebuild UI after their keys are removed; migration must refund those obsolete perk investments before cleanup. Recheck the hard-coded amounts only if the legacy final prices change.
- Static coverage in `CombatUpgradeMigrationCoverageTests` confirms removed-perk cleanup entry points for players, beasts, stale recasts, live player migration, stored item records, constructed droids, and ship/module serialized items. Still spot-check that obsolete Heavy/Light Armor and stale Armor perk-tree rows no longer appear in player-facing builders, default perk maps, instruction discs, or UI surfaces.
- Confirm Armor skill rank-ups count toward the 400 skill cap, grant SP normally, and gate current Bible General perks as intended.
- Confirm stale BAB/attacks-per-round logic remains removed:
  - `Stat.ApplyAttacksPerRound`
  - calls from player initialization/login temporary effects
  - beast/droid setup
  - equip/purchase/refund triggers
- Dry-run the item-property migrations against representative live data for player inventories, markets, world property storage, research jobs, player outfits, DM creatures, ships, and nested serialized inventories. Static coverage now guards those storage surfaces, recursive live-object entry points, and Combat Readiness enhancement item renames, but representative serialized records are still needed to prove live data shape compatibility.
- Confirm the weapon Delay migration updates old Throwing/Vibroknife/natural-weapon and Sling-based pistol values and preserves training-weapon and intentional short-sword delay exceptions in representative live data. Checked-in module templates and embedded `.git` area/store/NPC item instances have already been normalized to the updated delay table.
- Logged-out active status effects are process-local runtime cache only. They are not persisted and do not survive the fresh boot migration path, so no migration cleanup is required.
- Add release notes telling players they must perform a forced full rebuild, that removed combat perks were refunded, and that they were granted a bonus rebuild token for later use.

## Useful Patterns

- Forced full rebuild flag: `Player.RebuildComplete = false`
- Forced full rebuild helper: `ServerMigrationBase.RequireFullRebuildForAllPlayers()`
- Rebuild landing redirect: `PersistentLocation`
- Rebuild completion UI: `CharacterFullRebuildViewModel`
- Perk refund and removal: `ServerMigrationBase.RefundPerksByMapping(...)`
- Bonus rebuild token grant (existing players): `_22_CombatSystemReplacement.GrantCombatUpgradeRebuildToken(dbPlayer)`
- Starting rebuild token grant (new characters): `PlayerInitialization.GiveStartingRebuildToken(dbPlayer)`
