# World NPC Balance Audit

Last full audit: 2026-08-15 (feature/combat-upgrade)

## What the audit verifies

Every `World NPCs` Bible row must match its module assets exactly:

- UTC `Str/Dex/Wis/Con/Int` = Bible `MGT/PER/WIL/VIT/AGI` (runtime mapping:
  Might=STR, Perception=DEX, Vitality=CON, Agility=INT, Willpower=WIS).
- The stat skin's `NPCHP`, UTC `CurrentHitPoints`, and UTC `MaxHitPoints` = Bible
  `HP`, which is the final combat budget. UTC `HitPoints` is deliberately lower:
  NWN treats it as base HP and adds the Constitution modifier (SWLOR Vitality) once
  per class level, one HP per level for Toughness, and 20 HP per Epic Toughness feat.
  Its required value is `NPCHP - native HP bonuses`. `LoadNPCStats` applies the same
  final budget through `Stat.SetNPCMaxHitPoints`, which compensates the engine-derived
  bonuses instead of passing the final budget to NWNX as though it were base HP.
- Stat skin item properties (`NPCLevel`, `NPCHP`, `STM`, `FP`, `Attack`,
  `Force Attack`, `Evasion`, `Defense` physical/force, all 8 `Resistance`
  subtypes) = Bible columns. Negative resistances encode as `100 + abs(value)`.
- Equipped weapon `DMG` totals = Bible `DMG`. Conventions: a single weapon
  carries the full value; dual-wield hands split it (28/27 style); the same
  creature-weapon uti equipped in two claw slots counts once; shields
  (base items 14/56/57) carry no `DMG`/`Delay`.
- Weapon `Delay` (cost value x10) = Bible `Delay`, which reads the
  `World NPC Weapon Delays` tab (regenerated from module weapons) and falls
  back to the preset formula for rows without a weapon source. Floor is 220
  for weapon-sourced delays, 240 for preset delays.
- UTC ability feats = Bible `Existing Abilities` (AQ). AQ documents what the
  creature actually has: the difficulty|role package from
  `Enemy Ability Packages` plus any authored resistance-pressure or signature
  additions. Capstone bosses carry their quest line's `BossFeat`, whose enum
  name can differ from the line display name (e.g. "Vital Rupture" line ->
  `FeatType.ViralCascade1`).
- Every Droid-type row has a hand-entered Trauma Res adjustment (`AK`) that
  pins final Trauma resistance at 100 so Bleed fails against droids.

Regression coverage lives in `NPCEnemyBalanceAuditTests`,
`CombatAttackDelayTests`, and `CapstoneQuestDefinitionTests`.

`NPCEnemyBalanceAuditTests` also audits every NPCHP-backed creature in the module,
not only the documented World NPC rows, against the native-adjusted UTC base and
final HP budget. Focused effective-Evasion coverage includes UTC `NaturalAC`, which
contributes five Evasion rating per point through the native AC term and must not
silently inflate reviewed NPC profiles.

## Intentionally out of scope for the World NPCs tab

- Starship combat NPCs (`t1bomber`..`t6platform`, `mandocap1-3`, `sithcap1-7`):
  balanced by the `Starships` tab, not the enemy preset system.
- Faction-2 (non-hostile) ambient variants placed in areas for set dressing:
  `valnpcjwar4`, `valnpcswar4nh`, `vnpcjedicons3`, `vnpcjedisage`,
  `vnpcrepelite`, `vnpcsinvadenh`, `vnpcstroop5nh`, `vnpcswar3nh`,
  `vrepnpctroop1`. They reuse hostile siblings' stat skins but never fight.
- DM-event/orphaned templates with no spawn or placement: `dmnpcbrokisee`,
  `vnpcjediguard1`, `vnpcjediknight`, `vnpcsi3nh`, `vnpcssorc3nh`,
  `vnpcssorc4nh`.
- Retired legacy skins left unused after the Nar Shaddaa migration:
  `nar_t3`, `nar_t4`, `nar_t5`, `nar_boss`, `nar_droid_sk`,
  `cz220_droid_hide` (nar copy), `ww_outlaw_skin`, `chirodactyl_skin`.

## 2026-07 audit outcomes

- 120 NPCs had Perception/Agility (DEX/INT) transposed on their UTCs.
- HP serialization was later strengthened module-wide: every NPCHP-backed UTC now
  stores native-adjusted base `HitPoints`, while `CurrentHitPoints`/`MaxHitPoints`
  match the final NPCHP budget. This prevents Vitality and Toughness from being
  counted a second time by NWN and is enforced by the normalization tool and corpus test.
- ~50 NPCs carried delay-pressure-nerfed weapon damage below preset; restored
  to preset values (matching the earlier fast-cadence restoration pass).
- Five named rares (`reefmaw`, `sable_quarr`, `kael_drox`, `inkveil`,
  `glassjaw`) were authored from stale preset math; fully restatted to their
  Bible rows.
- `byysk_chieftain` dual-carried a full-damage weapon plus a shield with a
  damage property (~180% budget); `sanddemon` double-carried full-damage
  creature weapons. Both normalized to the split convention.
- The `World NPC Weapon Delays` tab was missing ~200 rows; regenerated for
  every row from actual module weapons.
- The `Existing Abilities` column still documented pre-revert ability kits
  (35dc7027e reverted the rebalance patch); regenerated from actual UTC feats.
- All Droid rows (not just the original 15) now have the Trauma=100 override.
- Nar Shaddaa's 14 enemies were never migrated to the combat-upgrade stat
  system (legacy skins with only `NPCLevel`, shared player weapon templates,
  no ability packages). Fully migrated with dedicated skins/weapons and Bible
  rows: levels 33 (streets), 41 (leaders/rares), Great Arkanian Dragon as a
  41 Boss.
- Seven preset-built alternate/variant enemies (`man_ranger_2`,
  `man_warrior_2`, `v_raivor2`, `v_flesheater2`, `s_app_m`, `ecoterr_2`,
  `byysk_guard002`) had no Bible rows; rows added.
- Capstone quest-line boss review: `cp_untinst_wd` (50 Boss Swarm, Hard to
  Hit, 2075 HP) and `cp_untinst_ms` (50 Boss Swarm, Glass Cannon, 1587 HP)
  were the only capstone wardens/masters below the group-boss bar — a final
  capstone boss weaker than its own line's Tough adepts. Both retuned to the
  50 Boss Melee preset with the Hard to Hit modifier (4611 HP, DMG 92,
  Evasion 19), preserving the line's evasive identity while matching the
  other capstone bosses; Bible rows, UTCs, skins, and claw weapons updated
  together. Trash ranks (`_ad`/`_sp`/`_ic`) intentionally stay Swarm-role.
