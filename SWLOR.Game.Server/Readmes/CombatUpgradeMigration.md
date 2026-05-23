# Combat Upgrade Migration Notes

This note tracks player migration work for `feature/combat-upgrade`. Keep it current when carrying over more experimental branch work so future agents can reason about player impact without re-discovering it from diffs.

## Current Migration Hook

- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_22_CombatSystemReplacement.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_23_UpdateSerializedItemRequirements.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_28_SplitDefensesAndResistances.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_29_AddBeastResistancePurities.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_30_RemoveBeastSavingThrowPurities.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_31_MigrateResistanceItemProperties.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_32_SpaceResistanceTypeIds.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_33_MoveBeastElementalPuritiesToResistances.cs`
- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_34_RemoveObsoleteBiblePerks.cs`
- Player migration: `SWLOR.Game.Server/Feature/MigrationDefinition/PlayerMigration/_14_MigrateResistanceItemProperties.cs`
- Player migration: `SWLOR.Game.Server/Feature/MigrationDefinition/PlayerMigration/_15_RemoveObsoleteCombatInstructionDiscs.cs`
- Server execution type: `PostDatabaseLoad`
- Current behavior:
  - Refunds removed or materially changed combat perks through `RefundPerksByMapping`.
  - Removes refunded legacy perk keys before the forced rebuild refund path can process them again.
  - Preserves legacy numeric `FlurryStyle` saves as the current `FlurryStyle` perk so that investment is not silently dropped.
  - Forces every player through a full rebuild by setting `Player.RebuildComplete = false` via `RequireFullRebuildForAllPlayers()`.
  - Updates stored item requirement properties to the combat-upgrade skill requirement model.
  - Splits persisted mitigation data so Physical/Force remain in `Player.Defenses` and elemental/status mitigation lives in `Player.Resistances`.
  - Moves legacy elemental defense entries into resistances, fills missing default keys, and removes Physical/Force from resistances.
  - Adds and normalizes beast resistance purities, removes legacy beast saving throw purities, and moves beast elemental purities out of defenses.
  - Migrates live and serialized item properties for the new resistance, weapon damage, and weapon delay property model, including untyped `DMG`, separate `WeaponDamageType`, and normalized weapon `Delay` for held weapons and natural creature weapons.
  - Removes obsolete Bible perks, stale recast entries, and obsolete combat instruction discs from players, beasts, stored items, markets, world properties, research jobs, outfits, DM creatures, and ships.

## Player-Facing Migration Goals

- Force all players to perform a full rebuild for the combat upgrade.
- Do not grant or require rebuild tokens for this forced rebuild path.
- Refund SP for removed attack-count/mastery perks so players are not stranded with deleted perk investments.
- Remove deleted perks from persisted `Player.Perks` data.
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

- Character sheet redesign remains outstanding: the first pass keeps the existing defense/resistance display shape for compatibility, but a later UI pass should rename the bound model properties and present baseline Defense separately from typed Resistances.
- Confirm `_22_CombatSystemReplacement` still runs immediately after master migration `_21_SetDefaultOutfitAndMarketLimits`. If another migration is added first, renumber the combat upgrade migration series.
- Confirm perk refund totals against the final perk prices in `PerkDefinition` files.
- Confirm removed perks no longer appear in perk builders, droid default perk maps, instruction discs, or any player migration re-application logic.
- Confirm stale BAB/attacks-per-round logic remains removed:
  - `Stat.ApplyAttacksPerRound`
  - calls from player initialization/login temporary effects
  - beast/droid setup
  - equip/purchase/refund triggers
- Dry-run the item-property migrations against representative live data for player inventories, markets, world property storage, research jobs, player outfits, DM creatures, ships, and nested serialized inventories.
- Confirm the weapon Delay migration updates old Throwing/Vibroknife/natural-weapon and Sling-based pistol values and preserves training-weapon and intentional short-sword delay exceptions in representative live data. Checked-in module templates and embedded `.git` area/store/NPC item instances have already been normalized to the updated delay table.
- Confirm active status effects from the old status effect service do not need cleanup for players who were logged out with `AdrenalStim*` or `Hasten*` effects active.
- Add release notes telling players they must perform a forced full rebuild and that removed combat perks were refunded.

## Useful Patterns

- Forced full rebuild flag: `Player.RebuildComplete = false`
- Forced full rebuild helper: `ServerMigrationBase.RequireFullRebuildForAllPlayers()`
- Rebuild landing redirect: `PersistentLocation`
- Rebuild completion UI: `CharacterFullRebuildViewModel`
- Perk refund and removal: `ServerMigrationBase.RefundPerksByMapping(...)`
