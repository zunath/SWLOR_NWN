# Combat Upgrade Implementation Status

Last updated: 2026-08-17

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

Espionage and Mimicry are fully in scope, including all Tradecraft/Infiltrator/Saboteur rows and all Mimicry techniques. The only intentional skips in the current workbook are five unimplemented Agriculture rows, which are explicitly out of scope for the first iteration.

## Current Snapshot

The static Bible implementation review currently reports 999 PASS, 5 SKIP (unimplemented, first-iteration-out-of-scope Agriculture), and 0 FAIL rows. Espionage is 41/41 PASS and Mimicry is 98/98 PASS, including all 88 techniques.

Static cross-skill validation is also covered. The release audit hard-gates every active weapon package against the legal Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage support frontier under 400 SP, plus curated high-risk profiles. The interaction audit guards recursive damage/reflection/status chains, transferred-damage loops, aggregate damage-derived healing, cross-resource conversion, and cooldown-reset cycles.

Combat-upgrade stat scaling is balanced around the practical player stat band: a focused character is expected to reach 26 in one ability stat, with rare 27 cases when a racial stat point is used. Food and other temporary item effects can push a stat a little higher for short windows, but baseline perk and ability values should not be tuned around those temporary overcap states. Scaling formulas should stay bounded above the normal band by using a documented cap or explicit soft-overcap rule.

The combat-upgrade skill cap is 400. Armor remains an active skill for equipment requirements and normal SP progression. Current Bible `General` perks use Armor skill requirements because Armor is the closest thing SWLOR has to a general character-level proxy. These General perks are in scope when present in the current Bible, but Armor is not a weapon-style combat-upgrade perk tree and stale Heavy/Light/older Armor perk-tree rows should be ignored in combat-upgrade active-button counts.

The local Bible workbook now applies the active-button budget pass from `CombatUpgradeActivePerkBudgetReview.md`. Combat perk trees target 4-6 distinct active buttons, counting rank-replacement chains as one button. After the latest local-workbook refresh, all reviewed in-scope combat trees land at 5-6 distinct active bases.

The first implementation pass closed a few narrow, concrete gaps:

- Added Bible status IDs for Sunder, Force Disruption, Blind, Toxin, Disoriented, Weakened, Dazed, Stunned, Exposed, Hemorrhage, Knockdown, Vital Strike, Hamstring, Exhausted, Taunting Deflection, Deflective Presence, Deflecting Aura, Guardian Master, Hobble, Brutal Assault, Essence Drain, Soul Ascension, Flash, Rampart, Absolute Defense, Force Erosion, Fractured Focus, Force Warding, and Foggy Mind.
- Added combat-upgrade status definitions/icons for those status IDs so combat-upgrade ability application has registered status metadata.
- Updated Bleed to use max-HP Bible scaling and added Toxin max-HP ticking.
- Added Exposed defense reduction and Hemorrhage damage-taken scaling to the current combat calculations.
- Wired current Vibroblade ability recasts and applied Exposed/Hemorrhage for Rending Strike and Carve.
- Fixed Kolto Recovery spell icon rows in `spells.2da` to use the existing `kolto_rec` resource.
- Added `tools\UpdateCombatUpgradeAudit.ps1` and regenerated `CombatUpgradePerkAudit.csv`.
- Added `tools\LinkCombatUpgradeFeatSpells.ps1`, generated spell rows for generated combat feats, updated `feat.2da` `SPELLID` links, and added matching `spell.cs` enum entries.
- Wired Bible cooldowns into existing active ability definitions that were missing `HasRecastDelay`.
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
- Wired runtime hooks for Rapid Shot attack delay reduction, Bulwark shield deflection, Alacrity shield-deflect stamina recovery, and Crushing Style weapon damage/critical bonuses.
- Tightened the audit so blank spreadsheet note rows are excluded while implementation-facing crafting, research, and gathering perk rows remain reviewable.
- Added the missing Flurry Style perk and wired its Haste behavior.
- Updated the audit refresh to parse the live Bible spreadsheet's pre-table metadata rows and fail loudly if no scoped manifest rows are available.
- Added local workbook refresh support to `tools\UpdateCombatUpgradeAudit.ps1`, regenerated `CombatUpgradeBiblePerkManifest.csv` from `design\bible\SWLOR Design Bible - Combat Upgrade.xlsx`, and regenerated `CombatUpgradePerkAudit.csv` from that manifest.
- Implemented the Leadership aura slice: Rallying Standard, Coordinated Focus, Charge Order, Watchful Presence, Steady Formation, and Field Recovery now use the existing aura toggle/range system with specific rank status effects and SOC-capped scaling.
- Refactored Rousing Shout away from revive/stabilize language and behavior. It now only affects living allies, grants short temporary HP, and applies low-HP damage reduction without competing with First Aid's revive and healing identity.
- Normalized combat-upgrade direct-effect scaling around the practical player stat band. Direct WIL/PER/MGT/SOC effect scaling treats 10 as the baseline and 26 as the cap, with 26 granting the full +25% direct-effect bonus. Explicit Leadership SOC caps now interpolate from the base value to the Bible-listed cap by SOC 26.
- Brought Leadership stat-scaling implementation in line with the Bible caps for Press the Attack, Mark Target, Break Morale, Decisive Command, Rousing Shout, Bolster Resolve, Cleanse Order, Hold the Line, Triage Protocol, and Command Radius duration/range support.
- Refreshed Dev Status and Scaling Source metadata in the local Bible workbook. Implemented rows use `Combat Formula`, `Design Added`, `Explicit Code`, or `None`; every Espionage and Mimicry row is included in the implementation review.
- Updated active ability feat syncing so higher perk ranks replace lower-rank active ability feats instead of stacking redundant granted feats or stale hotbar entries. Ability use now rejects superseded active feat ranks, and droids use the same current-rank granted-feat synchronization.
- Added per-player hotbar cooldown readiness feedback through `AbilityCooldownVisual`, generated `pr0_` through `pr5_` icon variants, recast/login/reset integration, and focused service and gameplay-icon coverage.
- Normalized weapon DMG item properties so `DMG` is the untyped amount and `WeaponDamageType` selects the whole weapon damage type. The native damage roll hook, crafting/enhancement application, enhancement generation, live module items, and serialized item migrations now follow that shape.
- Rebalanced armor, food, and droid resistance enhancement amounts for the direct -100 to 100 resistance scale. Player and server migrations update live player inventories plus stored serialized records in inventories, markets, world properties, research jobs, outfits, DM creatures, and ships.
- Added migration coverage for obsolete Bible perks and obsolete combat instruction discs. `CombatUpgradeMigrationCoverageTests` now also guards the release-critical migration surfaces: forced rebuild flagging, player migration entry points, stored item requirement migration, weapon Delay/DMG migration, resistance migration, obsolete instruction cleanup, droid instruction normalization, and ship/module serialized items.
- Clamped effective auto-attack delay to the engine's 1.75s practical floor after baseline subtraction and delay reductions. Weapon delay values were raised across the table so the fastest normal weapons sit above that floor and can benefit from haste, natural creature weapons use the same fastest-category delay, training weapons use slower Bible-listed values, module templates plus embedded area/store item instances use the updated values, all Bible `World NPCs` rows now resolve to equipped weapon/natural delay sources, and player/server migrations normalize existing weapon `Delay` item properties.
- Replaced the flat 1.75s effective-delay clamp with engine-native multi-attack swings. Swing animations still fire no faster than the 1.75s engine floor, but effective delays below it now resolve additional attack rolls within a single swing (up to 3 per swing, matching the stock engine's flurry behavior for high attacks-per-round builds), lowering the minimum effective attack delay to 0.584s. Fractional attacks are carried between swings so long-run attack rates match the delay, extra swing rolls widen the swing's main-hand/offhand attack-count region before `ResolveAttack` so weapon typing stays correct, and swing-debt state clears on combat end, leash evade, and weapon swaps. Natural creature weapon swings intentionally stay at one attack per swing, so their cadence still floors at 1.75s under heavy haste. Live playtest should confirm multi-roll swings read correctly for ranged weapons and that per-swing consumables (queued weapon abilities, next-attack bonuses) consume as intended.
- Converted 97 local Bible rows from active abilities to traits for the active-button budget pass, cleared their active-only cost/timing/cooldown fields, preserved stance/toggle/aura rows as active buttons, folded single-perk riders and redundant active variants into their target perk lines, broadened multi-ability riders to tree/role categories, removed redundant `Passive` wording, clarified resistance names, removed equipment-gate wording, refreshed `CombatUpgradeBiblePerkManifest.csv`, and kept the static audit header-only against the refreshed workbook.
- Cleaned the character sheet's combat-upgrade defense/resistance display model so Physical Defense and Force Defense are presented separately from typed elemental/status Resistances.
- Updated resistance behavior and Bible documentation to use direct -100 to 100 percentage scaling, with vulnerabilities below 0 and temporary immunity at 100. Player resistance totals from gear, food, perks, auras, and smaller stacked buffs now cap below the immunity threshold unless an active finite-duration status explicitly grants 100 resistance.
- Corrected Resistance item-property storage for the NWN cost-table model. Custom Resistance now points at SWLOR's `iprp_swlrescost.2da` through cost table `54`; positive resistance amounts use rows `0` through `100`, and vulnerability amounts use rows `101` through `200` so persisted `CostValue` entries remain non-negative while runtime code decodes them back to `-1` through `-100`.
- Kept Trauma and Disruption as status-family resistances: they reduce matching hostile status duration and matching status-effect damage ticks, but do not reduce generic Physical, Force, or Sonic direct damage.
- Added distinct resistance-pressure NPC abilities for Hutlar Ice, CZ220 Electrical, and Korriban Disruption enemies, with Bible NPC ability/package/world NPC coverage and AI/2DA validation.
- Added conservative enemy resistance vulnerabilities capped at -20, synchronized the Bible enemy resistance packages and World NPC skin properties, and introduced Coolant-Scarred Mynock, Byysk Cryo Adept, and Sith Frostbinder as spawned Ice-pressure variants.
- Confirmed logged-out status-effect state is process-local runtime cache and does not survive the fresh boot migration path.
- Routed Marked for Death bonus damage through the shared triggered-damage path so it applies resistance, damage-dealt hooks, and status-effect damage notifications while preserving recursion protection.
- Added cross-skill interaction regression coverage for secondary-damage proc termination, reflection termination, transferred-damage termination, one-shot redirects, aggregate per-hit damage-healing caps, sub-100% paid-cost resource conversion, and bounded cooldown reduction.
- Added an active-context 400 SP frontier and curated danger profiles for poison/trap/Mimicry, stealth burst, cross-resource sustain, damage-healing sustain, deflection/reflection, and layered control.
- Fixed Deflecting Return's missing Center of the Storm high-Embattled bonus consumer and changed Perfect Aegis to override reflection at documented final values instead of adding to the permanent trait values.
- **2026-07-11: Completed a full redesign of the two Lightsaber perk trees.** Ward (`PerkCategoryType.LightsaberOffense`) replaced its old perk set (Ward Bond, Guardian's Oath, Reactive Ward, Guardian's Challenge, Deflective Presence, Punishing Guard, Impenetrable Guard, Guardian's Influence, Overwhelming Defense, Guardian Master) with Saber Ward, Mental Fortress, Deflecting Return, a retained/reworked Guardian's Challenge (2 ranks, "damaged you" trigger), Surrounded Not Outmatched, Force Link (reuses the old Ward Bond ally damage-redirect behavior), Immovable Stance, Reprisal, Center of the Storm, and the capstone Aegis Eternal. Severance (`PerkCategoryType.LightsaberDefense`) replaced its old perk set (Severing Strike, Deflection Training, Severance Riposte, Leg Slash, Severance Flow, Surge Strike, Focused Stance, Blade Blitz, Purify, Saber Storm) with Force Sheath, Overpower, Fast Strikes, Shattering Strike, Sundering Sweep, Weak Points, Imbuement Stance, High Ground, Focus Shift, and the capstone Epicenter. Three net-new shared engine systems were added to support the redesign: physical-to-Force damage conversion, Embattled stacking, and bounded Deflecting-Return weapon reflection triggered off Attack Deflect. The existing capstone mastery quests were reused rather than renamed: the "Saber Storm" mastery quest now gates Epicenter, and the "Guardian Master" mastery quest now gates Aegis Eternal (quest IDs/definitions unchanged). 29 new gameplay icons were produced through Claude's illustrated-SVG icon pipeline, and `IconStandards.md` gained an agent-specific icon rule (GPT Image 2 for Codex, illustrated SVG for Claude). This redesign supersedes every prior Lightsaber Offense/Defense entry in this file, in `CombatUpgradeCurrentStateAudit.md`, and in the Lightsaber rows of `CombatUpgradeActivePerkBudgetReview.md`; the checked-in Bible workbook and regenerated `CombatUpgradeBiblePerkManifest.csv` are the implementation/audit snapshots for the new rows.

The latest checked-in local-workbook audit reports no scoped findings. After refreshing from the checked-in workbook, `CombatUpgradeBiblePerkManifest.csv` contains 1004 manifest rows: 999 scoped implemented rows and 5 intentionally skipped, unimplemented Agriculture rows that are out of scope for the first iteration. `CombatUpgradePerkAudit.csv` contains only its header row. Current Bible General rows that use Armor requirements are in scope, but Armor rows should not be counted as weapon-tree active-button work.

The scoped combat-upgrade audit is clean against the current local workbook snapshot. `CombatUpgradeBibleSyncTests` includes Espionage and both Mimicry core perks and techniques; it also guards the active-to-trait cleanup by failing if a Bible `Trait` row still grants active feats or if a live ability remains tied to a Bible-scoped perk without an implemented active Bible row granting that feat.

The combat-upgrade feat and spell icon resource checks pass for the scoped rows audited by the script. Generated combat feat `SPELLID` links are present, and active ability definitions have detected recast wiring for Bible cooldowns.

The legacy combat Bible review gate has been retired. Use `CombatUpgradeBibleSyncTests` and `tools\UpdateCombatUpgradeAudit.ps1` as the current static release gates.

## Static Implementation Status And Validation Risks

The scoped perk, ability, stat-consumer, feat, spell, icon, recast, and status-effect implementation audit is complete with no known static production gaps. Remaining mechanic work in this section is release validation, not another row-alignment or foundational-system implementation pass. Capstone world construction is tracked separately in `CapstoneQuestLinePlan.md`; eight content packages comprising sixteen physical areas remain there.

Active-to-trait code cleanup is no longer a known outstanding implementation slice. The current C# sync tests cover trait rows, active feat grants, and extra live ability surfaces for Bible-scoped perks.

Armor-specific specialization implementation is not remaining work. Armor release validation covers equipment requirements, the skill-cap/SP path at the 400 cap, current Bible General perk requirements, and confirmation that stale Heavy/Light/older Armor perk-tree entries are absent from player-facing data.

The telegraph service is implemented and integrated across line, cone, and sphere abilities through shared and ability-specific paths. Some bespoke channel, persistent-field, chained, and conditional cases still need playtest review beyond static audit coverage.

The `StatusEffect` service is now adopted for status effects. Status definitions are discovered by class, so new effects are added by creating a definition rather than maintaining a separate enum list.

Cached logged-out status effects are process-local runtime state and are not persisted. They do not survive the fresh boot migration path, so pre-upgrade active effect instances such as `AdrenalStim*` or `Hasten*` are not carried across the forced rebuild.

Perks and abilities are statically accounted for across the active weapon surface and the scoped Beast Mastery, Devices, First Aid, Force, Leadership, Mimicry, and Espionage rows. Active perk levels grant feats, feat/spell/icon links exist, active Bible rows have ability definitions, every granted Bible stat has a production consumer, and Mimicry/Espionage have dedicated parity and behavior checks. Several bespoke mechanics still need live behavior confirmation: channel ticks, persistent fields, target-count caps, positional bonuses, guarded-hit behavior, exact enmity math, and some critical-hit/offhand-delay effects.

`feat.2da` and `spells.2da` are now linked for generated combat feats. `AbilityTargetingMetadataTests` covers positive-delay casted area ability targeting metadata against 2DA shape and size data. The remaining 2DA risk is playtest-driven: generated spell rows should be reviewed again after any ability-specific target shape/range behavior is changed or confirmed in play.

The `experimental/combat-upgrade-status-effects` branch was checked as a reference. It contains older alternate combat work such as one-handed/two-handed ability structures and a broader recast enum. Use it as reference material only, not as a clean source to copy wholesale into this branch.

## Release Validation

1. Playtest conditional/channel/persistent-field abilities such as Current Overload, Serpent's Eclipse, Tempest Bloom, Pacification Field, Disruption Field, Shield Wall, Force Capacitor, and Infinite Conduit against the live module.
2. Smoke-test forced rebuild, equipment skill requirements, weapon Delay/DMG, resistance enhancement behavior, and serialized item migrations against representative live data. Static coverage now confirms the migration entry points and storage surfaces, but it does not replace a run over representative player/world records.
3. Verify Armor uses the 400 skill cap/SP path, current Bible General perks use Armor requirements as intended, and stale Heavy/Light/older Armor perk-tree rows, feats, instruction discs, and UI entries are not exposed.
4. Refine generated spell target metadata only after any ability-specific target shape/range adjustments are confirmed in play.
5. Live-play the curated cross-skill danger profiles, especially poison/trap/Mimicry, stealth burst, cross-resource sustain, damage-derived healing, deflection/reflection, and layered control.
6. Keep Espionage and Mimicry in the mandatory row-by-row review and release gates as their mechanics and content evolve.
7. Keep `tools\UpdateCombatUpgradeAudit.ps1`, the C# sync tests, the build/tests, and this status file aligned when the Bible changes.
8. Playtest the redesigned Ward and Severance Lightsaber trees added 2026-07-11: physical-to-Force conversion feel, Embattled stacking pacing, Deflecting Return's bounded reflection uptime, and the reused Saber Storm/Guardian Master mastery-quest gates against Epicenter and Aegis Eternal.
