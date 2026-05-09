# Combat Upgrade Migration Notes

This note tracks player migration work for `feature/combat-upgrade`. Keep it current when carrying over more experimental branch work so future agents can reason about player impact without re-discovering it from diffs.

## Current Migration Hook

- Server migration: `SWLOR.Game.Server/Feature/MigrationDefinition/ServerMigration/_21_CombatSystemReplacement.cs`
- Execution type: `PostDatabaseLoad`
- Current behavior:
  - Refunds removed or materially changed combat perks through `RefundPerksByMapping`.
  - Removes refunded legacy perk keys before the forced rebuild refund path can process them again.
  - Preserves legacy numeric `FlurryStyle` saves as the current `FlurryStyle` perk so that investment is not silently dropped.
  - Forces every player through a full rebuild by setting `Player.RebuildComplete = false` via `RequireFullRebuildForAllPlayers()`.

## Player-Facing Migration Goals

- Force all players to perform a full rebuild for the combat upgrade.
- Do not grant or require rebuild tokens for this forced rebuild path.
- Refund SP for removed attack-count/mastery perks so players are not stranded with deleted perk investments.
- Remove deleted perks from persisted `Player.Perks` data.
- Keep existing player migration versions intact. If more player-specific data cleanup is needed, add the next numbered player migration instead of editing old shipped migrations.

## Forced Rebuild Flow

- `_21_CombatSystemReplacement` sets `Player.RebuildComplete = false` for every stored player.
- `PersistentLocation` detects `RebuildComplete == false` on login and sends the player to waypoint `REBUILD_LANDING`.
- `CharacterFullRebuildViewModel` prevents players from leaving the rebuild area until the rebuild is completed.
- Completing the rebuild sets `Player.RebuildComplete = true`.

## Reusing `RebuildComplete`

`RebuildComplete` was introduced for an earlier legacy rebuild, but its current usage is generic: it gates whether a player is allowed to leave the rebuild flow. It is safe to reuse for the combat upgrade as long as the intended behavior is a forced full rebuild.

Important nuance: setting the flag to `false` does not itself reset the character. It redirects and locks the player into the rebuild area. The actual full reset happens when the player uses the rebuild UI's reset action, which refunds all remaining perks/skills, resets stats, and keeps `RebuildComplete = false` until the rebuilt character is saved.

## Perks Currently Refunded By `_21_CombatSystemReplacement`

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

- Confirm `_21_CombatSystemReplacement` is still the next valid server migration version. If another migration is added first, renumber this file/class/version.
- Confirm perk refund totals against the final perk prices in `PerkDefinition` files.
- Confirm removed perks no longer appear in perk builders, droid default perk maps, or any player migration re-application logic.
- Confirm stale BAB/attacks-per-round logic remains removed:
  - `Stat.ApplyAttacksPerRound`
  - calls from player initialization/login temporary effects
  - beast/droid setup
  - equip/purchase/refund triggers
- Confirm player-owned weapons/items do not need a one-time item-property migration for Delay or DMG after the hak and item generation changes settle.
- Confirm active status effects from the old status effect service do not need cleanup for players who were logged out with `AdrenalStim*` or `Hasten*` effects active.
- Add release notes telling players they must perform a forced full rebuild and that removed combat perks were refunded.

## Useful Patterns

- Forced full rebuild flag: `Player.RebuildComplete = false`
- Forced full rebuild helper: `ServerMigrationBase.RequireFullRebuildForAllPlayers()`
- Rebuild landing redirect: `PersistentLocation`
- Rebuild completion UI: `CharacterFullRebuildViewModel`
- Perk refund and removal: `ServerMigrationBase.RefundPerksByMapping(...)`
