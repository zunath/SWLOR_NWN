# Combat Upgrade Implementation Status

Last updated: 2026-05-13

## Source Of Truth

The upstream combat upgrade source is the live Bible spreadsheet:

`https://docs.google.com/spreadsheets/d/1rppEkwp2dX0oGKY1ftSbDTcg7GhopODseqbDb4cpNSU/edit?gid=207006097#gid=207006097`

The current local workbook snapshot is:

`design\bible\SWLOR Design Bible - Combat Upgrade.xlsx`

The current local manifest is `CombatUpgradeBiblePerkManifest.csv`. Regenerate the manifest from the local workbook and run the gap audit with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\UpdateCombatUpgradeAudit.ps1 -RefreshLocalBible
```

Regenerate the gap audit from the existing manifest with:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File tools\UpdateCombatUpgradeAudit.ps1
```

When network access is available, use `-RefreshBible` to pull fresh per-tab CSV exports before auditing. Prefer `-RefreshLocalBible` when working from the checked-in workbook snapshot.

Espionage and Farming are out of scope. The audit also excludes crafting, research, and gathering tabs because they are not part of the combat-upgrade implementation surface.

## Current Snapshot

`SWLOR.Game.Server.sln` was last recorded as building successfully on 2026-05-13 after the stat-scaling normalization pass.

Combat-upgrade stat scaling is balanced around the practical player stat band: a focused character is expected to reach 26 in one ability stat, with rare 27 cases when a racial stat point is used. Food and other temporary item effects can push a stat a little higher for short windows, but baseline perk and ability values should not be tuned around those temporary overcap states. Scaling formulas should stay bounded above the normal band by using a documented cap or explicit soft-overcap rule.

The first implementation pass closed a few narrow, concrete gaps:

- Added Bible status IDs for Sunder, Force Disruption, Blind, Toxin, Disoriented, Weakened, Dazed, Stunned, Exposed, Hemorrhage, Knockdown, Vital Strike, Hamstring, Exhausted, Taunting Deflection, Deflective Presence, Deflecting Aura, Guardian's Wrath, Hobble, Brutal Assault, Essence Drain, Soul Ascension, Flash, Rampart, Absolute Defense, Force Erosion, Fractured Focus, Force Warding, and Foggy Mind.
- Added combat-upgrade status definitions/icons for those status IDs so combat-upgrade ability application has registered status metadata.
- Updated Bleed to use max-HP Bible scaling and added Toxin max-HP ticking.
- Added Exposed defense reduction and Hemorrhage damage-taken scaling to the current combat calculations.
- Wired current Vibroblade ability recasts and applied Exposed/Hemorrhage for Rending Strike and Carve.
- Fixed Kolto Recovery spell icon rows in `spells.2da` to use the existing `kolto_rec` resource.
- Added `tools\UpdateCombatUpgradeAudit.ps1` and regenerated `CombatUpgradePerkAudit.csv`.
- Added `tools\LinkCombatUpgradeFeatSpells.ps1`, generated spell rows for generated combat feats, updated `feat.2da` `SPELLID` links, and added matching `spell.cs` enum entries.
- Added `tools\ApplyCombatUpgradeRecasts.ps1` and wired Bible cooldowns into existing active ability definitions that were missing `HasRecastDelay`.
- Added shape-aware telegraphed combat impact support and moved representative line/cone abilities onto it: Soul Burst, Earthshatter, Covering Strike, Savage Cleave, Arc Strike, Overwhelming Strike, Line Breaker, and Suppressive Line.
- Fixed telegraph facing math so helper-created cone/line telegraphs convert NWN facing degrees to the radians expected by the shader/shape calculations.
- Added runtime stat modifiers for Sunder, Disoriented, Weakened, Force Erosion, Exhausted, and Vital Strike in the combat-upgrade stat path; Exposed, Hemorrhage, and Toxin are implemented in combat-upgrade damage/status hooks.
- Extended combat impact handling so a single resisted hit can apply additional tracked statuses after the same saving throw.
- Wired the audited Bible-mentioned status applications into existing combat ability files. This includes the next telegraph/status batches for Spear, Saberstaff, Rifle, Pistol, Staff, Throwing, Katar, Lightsaber, Twin Blade, Vibroknife, Devices, and Force Push.
- Added runtime hooks for Fractured Focus FP cost increases and Foggy Mind activation delay increases.
- Tightened the audit status matcher so damage-type wording such as "poison DMG" and conditional wording such as "if the target is Disoriented" are not counted as missing status applications.
- Implemented the missing Heavy Vibroblade active ability slice: feat grants, recast groups, ability definitions, queued strikes, self/party buffs, and runtime hooks for Life Siphon, Soul Ascension, Soul Devourer recoil, Blazing Spikes reflection, Flash, Rampart, Absolute Defense, Bastion Stance, Soul Storm, Soul Sacrifice, and Essence Drain.
- Added the remaining audited active combat ability definitions for Katar, Lightsaber, Pistol, Rifle, Saberstaff, Spear, Staff, Throwing, Twin Blade, Vibroblade, and Vibroknife.
- Integrated the active combat perk bases for Backstab, Circle Slash, Crippling Shot, Cross Cut, Double Shot, Double Strike, Explosive Toss, Hacking Blade, Leg Sweep, Piercing Toss, Quick Draw, Riot Blade, Shield Bash, Slam, Spinning Whirl, Striking Cobra, and Tranquilizer Shot into their weapon-family perk definitions.
- Added generated feat/spell enum and 2DA rows through `FeatType.WatchfulPresence3 = 2558` / `Spell.WatchfulPresence3 = 1526`, and updated the link script range accordingly.
- Wired existing active combat perk levels to their generated feat grants so the new and existing abilities can be learned from the perk trees.
- Added status IDs/definitions for the remaining active combat stance/buff/debuff statuses, including the weapon-family stances, guard buffs, challenge/mark effects, incapacitation, and decoy effects.
- Added first-pass runtime hooks for the new active statuses in `Stat`, `Combat`, and `StatusEffect`: attack/defense/accuracy/evasion modifiers, Invincible damage prevention, Marked for Death damage bonus, Duelist's Challenge damage exposure, haste stance handling, and several on-hit stance effects.
- Added discovered status effect definitions and migrated combat ability/status application, combat stat hooks, damage hooks, and status data lookup to the status service.
- Added the remaining scoped passive combat perk definitions for Rapid Shot, Bulwark, Alacrity, and Crushing Style.
- Wired runtime hooks for Rapid Shot attack delay reduction, Bulwark shield deflection, Alacrity shield-deflect stamina recovery, and Crushing Style staff damage/critical bonuses.
- Tightened the audit so blank spreadsheet note rows and non-combat crafting/research/gathering tabs are excluded from combat-upgrade blocker counts.
- Added the missing Flurry Style staff perk and wired its staff attack-delay reduction behavior.
- Updated the audit refresh to parse the live Bible spreadsheet's pre-table metadata rows and fail loudly if no scoped manifest rows are available.
- Added local workbook refresh support to `tools\UpdateCombatUpgradeAudit.ps1`, regenerated `CombatUpgradeBiblePerkManifest.csv` from `design\bible\SWLOR Design Bible - Combat Upgrade.xlsx`, and regenerated `CombatUpgradePerkAudit.csv` from that manifest.
- Implemented the Leadership aura slice: Rallying Standard, Coordinated Focus, Charge Order, Watchful Presence, Steady Formation, and Field Recovery now use the existing aura toggle/range system with specific rank status effects and SOC-capped scaling.
- Refactored Rousing Shout away from revive/stabilize language and behavior. It now only affects living allies, grants short temporary HP, and applies low-HP damage reduction without competing with First Aid's revive and healing identity.
- Normalized combat-upgrade direct-effect scaling around the practical player stat band. Direct WIL/PER/MGT/SOC effect scaling treats 10 as the baseline and 26 as the cap, with 26 granting the full +25% direct-effect bonus. Explicit Leadership SOC caps now interpolate from the base value to the Bible-listed cap by SOC 26.
- Brought Leadership stat-scaling implementation in line with the Bible caps for Press the Attack, Mark Target, Break Morale, Decisive Command, Rousing Shout, Bolster Resolve, Cleanse Order, Hold the Line, Triage Protocol, and Command Radius duration/range support.
- Refreshed Dev Status and Scaling Source metadata in the local Bible workbook. Implemented rows now use `Combat Formula`, `Design Added`, `Explicit Code`, or `None`; `Design Only` remains only on Espionage rows that are still intentionally out of scope.

The latest local-workbook audit currently reports no scoped findings. `CombatUpgradePerkAudit.csv` contains only its header row after the Design-phase implementation pass for Beast Mastery, Devices, First Aid, Force, and Leadership.

The scoped combat-upgrade audit is clean against the 2026-05-10 local workbook snapshot. Espionage remains intentionally excluded by the current combat-upgrade implementation scope.

The combat-upgrade feat and spell icon resource checks pass for the scoped rows audited by the script. Generated combat feat `SPELLID` links are present, and active ability definitions have detected recast wiring for Bible cooldowns.

## Major Remaining Gaps

The telegraph service now has a combat ability integration point, and a broader set of line/cone/sphere abilities use it. Some bespoke channel, persistent field, chained, and conditional cases still need playtest review beyond static audit coverage.

The `StatusEffect` service is now adopted for status effects. Status definitions are discovered by class, so new effects are added by creating a definition rather than maintaining a separate enum list.

Perks and abilities are functional for the earlier active weapon audit surface and for the scoped Design-phase Beast Mastery, Devices, First Aid, Force, and Leadership rows. Active perk levels grant feats, feat/spell/icon links exist, and active Bible rows have ability definitions. Several bespoke mechanics are still first-pass approximations rather than exact simulations: channel ticks, persistent fields, target-count caps, positional bonuses, guarded-hit behavior, exact enmity math, and some critical-hit/offhand-delay effects.

`feat.2da` and `spells.2da` are now linked for generated combat feats. The remaining 2DA risk is target metadata quality: generated spell rows should be reviewed after ability-specific target shape/range behavior is confirmed in play.

The `experimental/combat-upgrade-status-effects` branch was checked as a reference. It contains older alternate combat work such as one-handed/two-handed ability structures and a broader recast enum. Use it as reference material only, not as a clean source to copy wholesale into this branch.

## Next Steps

1. Playtest conditional/channel/persistent-field abilities such as Current Overload, Serpent's Eclipse, Tempest Bloom, Pacification Field, Gas Bomb, Disruption Field, Shield Wall, Force Capacitor, and Infinite Conduit against the live module.
2. Decide when Espionage should enter scope; it remains Design-stage and intentionally excluded from the current audit.
3. Refine generated spell target metadata after any ability-specific target shape/range adjustments are confirmed in play.
4. Keep `tools\UpdateCombatUpgradeAudit.ps1`, the build, and this status file aligned when the Bible changes.
