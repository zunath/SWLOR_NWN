# Combat Upgrade Current-State Balance Audit

Last reviewed: 2026-07-11 (Lightsaber Ward/Severance redesign; see addendum below)

## Scope

This audit captures the current pre-release tuning state of the combat upgrade after the release-blocker implementation pass. It records the player-feedback risks, the mitigations now in code/Bible/TLK, and the post-fix playtest queue using the current Bible, perk definitions, status effects, and shared combat/stat systems.

The audit includes weapons, Force, Devices, Leadership, First Aid, Beast Mastery, Armor, and companion contribution. Force, Devices, Leadership, First Aid, and Beast Mastery remain balance-audit surfaces, not broad thematic redesign targets.

## Key Mechanical Facts

- `Stat.GetStatAdjustment` combines persistent perk bonuses, status-effect bonuses, and temporary stat modifiers. Always-on passives, stances, support statuses, and temporary windows can stack through the same stat path.
- Perk stat bonuses use the current/effective perk level, not the sum of all prior levels. Max-rank audit values should use each perk's final active level.
- Shield Deflection is checked before Attack Deflection. If a shield deflect chance exists, it is used and Attack Deflection is skipped for that attack.
- Attack Deflection requires a valid weapon and no equipped shield. Its default cap is 50, and `AttackDeflectionChanceCap` can raise that cap.
- Guard is a damage-stage mitigation mechanic. It is separate from both deflection mechanics and currently has a base 20 percent reduction with a 40 percent maximum reduction.

## Release Blockers

### B-001 Permanent Attack Deflection Cap Access - Implemented, Automated Coverage Added

Permanent Attack Deflection sources were reduced so the all-in permanent weapon stack stays below the 50 percent default cap:

- Staff Sentinel `Staff Parry`: +12 Attack Deflection at max rank.
- Lightsaber Defense `Deflection Training`: +14 Attack Deflection at max rank. (Superseded 2026-07-11: `Deflection Training` no longer exists — Lightsaber Defense was redesigned into the Severance tree. See the 2026-07-11 addendum below for current Attack Deflection sourcing.)
- Saberstaff Tempest `Spinning Deflection`: +10 Attack Deflection at max rank.
- Twin Blade Duelist `Centerline Guard`: +5 Attack Deflection.
- Heavy Vibroblade Defense `Unbreakable Will`: +4 to +8 Attack Deflection, scaling from MGT.

The all-in permanent stack across the remaining four weapon lines (Staff Sentinel, Saberstaff Tempest, Twin Blade Duelist, Heavy Vibroblade Defense) is now 35 Attack Deflection (12 + 10 + 5 + 8) before temporary buffs or ally support; Lightsaber Defense no longer contributes after the 2026-07-11 redesign. Cap access should come from temporary, active, support, or capstone windows.

### B-002 Staff Crusher Mandatory Cross-Tree Damage Risk - Implemented, Automated Coverage Added

Staff Crusher's all-weapon MGT damage payload was removed and its universal crit package was reduced:

- `Crushing Style`: `StaffMightModifierDamageMultiplier +1` and `CriticalRatePercentAdjustment +5`.
- `Crushing Mastery III`: `StaffMightModifierDamageMultiplier +2`, `CriticalRatePercentAdjustment +5`, `CriticalDamagePercentAdjustment +10`, and 3 STM on crit with a 6 second cooldown.

The native damage roll applies `WeaponMightModifierDamageMultiplier` to all weapons, then separately adds `StaffMightModifierDamageMultiplier` only when a staff is equipped. Crusher now preserves staff identity without becoming a mandatory all-melee damage pickup.

### B-003 Damage Plus Sustain Compound Risk - Implemented, Automated Coverage Added

Heavy Vibroblade Offense and Defense sustain loops were reduced so high damage no longer scales survivability as sharply:

- `Life Siphon`: below 40 percent HP, heals 8 percent of damage dealt.
- `Soul Strike`: rank heals reduced to 15/25/30 percent, with rank III scaling by 1 percentage point per 2 MGT to a 40 percent cap.
- `Vampiric Fury`: critical heals reduced to 12 percent plus MGT/2 scaling, capped at 25 percent, with an 8 second cooldown.
- `Soul Devourer`: +25 percent Attack, +10 percent critical chance, 45 percent self-damage reduced by MGT to a 20 percent floor.
- `Soul Sacrifice`, `Soul Amplification`, `Soul Reaping`, `Soul Ascension`, `Blood Weapon`, and `Guardian's Reaping` all received lower magnitude or shorter uptime.

### B-004 Positionals Still Need Baseline-Proofing - Implemented, Automated Coverage Added

Spear Damage and Vibroknife Shadow no longer require stable side/back uptime for core function:

- Spear `Lateral Strike` now restores STM from baseline Spear damage and grants additional STM from side attacks.
- Spear `Opportunist's Flow` now grants baseline attack-delay reduction from Spear damage, with side attacks adding more.
- Spear `Restoration Strike` now restores baseline STM on Spear crits, with side crits adding an extra chance.
- Spear `Flanking Barrage` now works from any facing and gets stronger from the side.
- Vibroknife `Backstab` now has a baseline hit, with behind-target positioning increasing damage and enabling the rank III knockdown.

Positionals are now upside, not the baseline requirement.

### B-005 Spear Disabler Is Still Force-Centric - Implemented, Automated Coverage Added

Spear Disabler was broadened into general shutdown and resource pressure:

- `Disabling Strike` and `Total Force Denial` now apply Force Disruption and Foggy Mind.
- `Disruption Field` drains both FP and STM.
- `Erosion Strike II` drains FP and STM through Force Erosion.
- `Force Piercing` critical hits reduce both FP and STM.
- `Force Suppression` reduces generic Attack and Force Attack.

Force-sensitive targets still suffer, but the tree now has value against ordinary dangerous enemies too.

### B-006 Lightsaber Offense Area-Rider Mismatch - Implemented, Automated Coverage Added

Lightsaber Offense riders were moved from mostly area-only payoff to the actual Offense ability cadence:

- `Overwhelming Strike` applies Sunder from hostile Lightsaber Offense abilities.
- `Purify` triggers from hostile Lightsaber Offense abilities.
- `Ripple Slash` applies Disoriented from hostile Lightsaber Offense abilities.
- `Arc Strike` keeps its area secondary-target bonus and adds single-target bonus damage against debuffed targets.
- Generic Sunder riders do not downgrade stronger existing Sunder effects.

(Superseded 2026-07-11: Lightsaber Offense was fully redesigned into the Ward tree, and none of `Overwhelming Strike`, `Purify`, `Ripple Slash`, or `Arc Strike` remain in it. See the 2026-07-11 addendum below.)

## High-Risk Warnings

### W-000 Release Validation Matrix - Automated Coverage Added

`CombatReleaseBalanceAuditTests` now checks curated archetype legality, permanent Attack Deflection cap access, and full 400 SP package-frontier outlier reporting. `CombatUpgradeReleaseValidationMatrix.md` defines the manual test set for real enemies, attack-delay feel, support-system interactions, weapon identity checks, peak-damage target bands, and mob-tuning decisions.

### W-001 Crit Cap Pressure - Automated Coverage Added

Permanent crit pressure is now explicitly budget-tested. Staff Crusher plus Spear always-on crit sources stay below the 50 percent crit cap before stances, support, or temporary buffs.

### W-002 Haste And Attack Delay Need Engine Validation - Timing Pass Implemented

The code pass extended weapon setup payoff windows that were most likely to expire before the player could benefit from them. Next auto/ability/attack-delay payoff windows below the 15-30 second review band were moved to 18 seconds across weapon lines such as Pistol, Rifle, Throwing, Staff, Spear, Katar, Twin Blade, Vibroblade, Vibroknife, Heavy Vibroblade, and Saberstaff.

Engine feel still needs playtest confirmation under real attack-delay conditions, but the short-window implementation issue is addressed.

### W-003 Shield Deflection Is Currently Cleaner Than Attack Deflection - Guarded By Coverage

Shield Deflection remains shield-gated and mechanically distinct from Attack Deflection. Regression coverage verifies that Shield Deflection, Attack Deflection, and Guard budgets do not pollute each other.

### W-004 Guard Is Distinct And Currently Less Dangerous Than Deflection - Guarded By Coverage

Katar Iron Guard's `Guard Training` remains a damage-stage tank identity rather than a deflection clone. Regression coverage verifies Guard stays separate from both attack-roll deflection mechanics.

### W-005 Dependency-Only Weapon Perks - Implemented, Automated Coverage Added

Weapon perks that only enhanced one named sibling perk were converted to broader stat-driven effects. The pass covers Cascade Failure, Essence Hunter, Spinning Deflection, Retaliatory Flow, Reload Tempo, Gunslinger Focus, Expose Weak Point, Dead Center, Steady Aim II, Shrapnel Casing, and Volatile Payload.

The removed behavior-specific hooks are no longer active in shared combat/stat code. Regression coverage now checks the replacement broad stat hooks and guards against reintroducing the old named dependency strings.

## Post-Fix Playtest Priorities

1. Attack Deflection stack with Staff Parry, Saberstaff Tempest, Twin Blade, and Heavy Vibroblade Defense. (Lightsaber Defense removed 2026-07-11 — the redesigned Deflecting Return reflects bounded weapon damage instead of granting permanent Attack Deflection.)
2. Staff Crusher with low-delay/high-hit-rate MGT builds.
3. Heavy Vibroblade sustain tank after reduced sustain values.
4. High-MGT damage stack with Leadership support.
5. High-PER crit stack with Leadership support.
6. Spear/Vibroknife low-positional-uptime solo baseline.
7. Spear/Vibroknife high-positional-uptime ceiling.
8. Spear Disabler against non-Force enemies.
9. Lightsaber Offense before and after Saber Storm availability. (Superseded 2026-07-11: Lightsaber Offense is now the Ward tree and the "Saber Storm" mastery quest gates the Severance capstone Epicenter, not a Lightsaber Offense unlock. See the 2026-07-11 addendum below for the current pre/post-capstone playtest pairing.)
10. Shield Deflection tank using Vibroblade Defense and shield item properties.
11. Katar Guard tank with and without support buffs.
12. Weapon plus Beast Mastery companion-pressure baseline.

## Implementation Queue Status

1. Permanent Attack Deflection cap access: complete.
2. Staff Crusher global passive payload: complete.
3. High damage plus sustain pass: complete.
4. Positional requirements converted into bonuses: complete.
5. Spear Disabler non-Force target value: complete.
6. Lightsaber Offense area-rider mismatch: complete.
7. Weapon setup payoff duration pass: complete.
8. Curated archetype and 400 SP package-frontier audit coverage: complete.
9. Staff Sentinel `Sentinel Stance` text/stat mismatch: complete.
10. Weapon capstone/SP cost normalization: complete for every weapon style; all weapon styles now use the 18-row/60 SP progression shape.

## 2026-07-11 Addendum: Lightsaber Ward/Severance Redesign

Both Lightsaber perk trees were fully replaced on 2026-07-11, superseding B-001's Lightsaber Defense line, B-006 in its entirety, and playtest priority 9 above.

- Ward (`PerkCategoryType.LightsaberOffense`) dropped its old perk set (Ward Bond, Guardian's Oath, Reactive Ward, Guardian's Challenge, Deflective Presence, Punishing Guard, Impenetrable Guard, Guardian's Influence, Overwhelming Defense, Guardian Master) for Saber Ward, Mental Fortress, Deflecting Return, a retained/reworked 2-rank Guardian's Challenge ("damaged you" trigger), Surrounded Not Outmatched, Force Link (reuses the old Ward Bond ally damage-redirect), Immovable Stance, Reprisal, Center of the Storm, and the capstone Aegis Eternal.
- Severance (`PerkCategoryType.LightsaberDefense`) dropped its old perk set (Severing Strike, Deflection Training, Severance Riposte, Leg Slash, Severance Flow, Surge Strike, Focused Stance, Blade Blitz, Purify, Saber Storm) for Force Sheath, Overpower, Fast Strikes, Shattering Strike, Sundering Sweep, Weak Points, Imbuement Stance, High Ground, Focus Shift, and the capstone Epicenter.
- Attack Deflection on the old Lightsaber Defense `Deflection Training` line no longer exists; Deflecting Return instead reflects a bounded amount of weapon damage back at the attacker specifically off an Attack Deflect of a directly targeted ranged attack, rather than adding another raw deflection-chance source. The B-001 permanent-stack accounting for the other four weapon lines (Staff Sentinel, Saberstaff Tempest, Twin Blade Duelist, Heavy Vibroblade Defense) is unaffected.
- Three net-new shared engine systems back the redesign: physical-to-Force damage conversion (Saber Ward), Embattled stacking (Surrounded Not Outmatched / Aegis Eternal), and bounded Deflecting-Return weapon reflection triggered off an Attack Deflect of a directly targeted ranged attack.
- The existing capstone mastery quests were reused, not renamed: the "Saber Storm" mastery quest now gates Epicenter, and the "Guardian Master" mastery quest now gates Aegis Eternal. Quest IDs and definitions are unchanged.
- Updated playtest priority 9: test Ward/Severance baseline play before Epicenter/Aegis Eternal unlock, then again after, rather than the retired "before/after Saber Storm" framing.

The Bible workbook and `CombatUpgradeBiblePerkManifest.csv` are the source of truth for the new Ward/Severance rows; this addendum is qualitative and does not restate specific numeric tuning values.

## Not Recommended

- Do not tune mobs around pre-fix damage screenshots.
- Do not add combo-specific hardcoded penalties.
- Do not weapon-lock broad perk lines unless softer budget, source-tier, uptime, or trigger changes fail.
- Do not perform broad thematic rewrites to Force, Devices, Leadership, First Aid, or Beast Mastery in this leg.
