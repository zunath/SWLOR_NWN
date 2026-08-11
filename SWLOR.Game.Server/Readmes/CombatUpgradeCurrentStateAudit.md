# Combat Upgrade Current-State Balance Audit

Last reviewed: 2026-08-11 (split weapon-deflection review)

## Scope

This audit captures the current pre-release tuning state of the combat upgrade after the release-blocker implementation pass. It records the player-feedback risks, the mitigations now in code/Bible/TLK, and the post-fix playtest queue using the current Bible, perk definitions, status effects, and shared combat/stat systems.

The audit includes weapons, Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, Espionage, Armor, and companion contribution. Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage remain balance-audit surfaces, not broad thematic redesign targets.

## Key Mechanical Facts

- `Stat.GetStatAdjustment` combines persistent perk bonuses, status-effect bonuses, and temporary stat modifiers. Always-on passives, stances, support statuses, and temporary windows can stack through the same stat path.
- Perk stat bonuses use the current/effective perk level, not the sum of all prior levels. Max-rank audit values should use each perk's final active level.
- Shield Deflection is checked before weapon deflection and covers both melee and ranged weapon auto-attacks. If a shield deflect chance exists, the matching Melee or Ranged Deflection check is skipped for that attack.
- Melee Deflection and Ranged Deflection require a valid weapon and no equipped shield. They apply only to melee and ranged weapon auto-attacks respectively, each has an independent default cap of 50, and `MeleeDeflectionChanceCap` or `RangedDeflectionChanceCap` can independently raise that cap to the shared hard maximum of 100.
- Guard is a damage-stage mitigation mechanic. It is separate from both deflection mechanics and currently has a base 20 percent reduction with a 40 percent maximum reduction.
- Direct, triggered, periodic, and transferred damage are distinct delivery types. Only direct damage may run ordinary damage-dealt and status-effect procs; reflection and secondary riders use triggered delivery, while shared damage uses transferred delivery and cannot reshare.
- Damage-derived healing from all riders on one hit shares a 50 percent per-hit cap. Cross-resource conversion restores 35 percent of the resource actually spent and cannot call the inverse conversion. Cooldown reduction cannot affect capstones or reduce a timer past ready.

## Release Blockers

### B-001 Permanent Weapon-Deflection Cap Access - Implemented, Automated Coverage Added

The release gate now audits permanent Melee Deflection and Ranged Deflection independently against their 50 percent default caps. Current direct perk sources remain well below those limits:

- Heavy Vibroblade Immortal `Unbreakable Will`: +4 to +8 permanent Melee Deflection, scaling from MGT.
- Lightsaber Ward `Deflecting Return`: +4 to +8 permanent Ranged Deflection by rank.

Staff Sentinel, Twin Blade Duelist, and Saberstaff Tempest grants are stance- or ability-driven windows rather than permanent direct bonuses. Manual validation therefore uses separate Melee and Ranged Deflection archetypes and tests those active windows against the matching attack type. The two source totals must never be added together to assess either cap; cap access should come from temporary, active, support, or capstone windows within that same source budget.

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

`CombatReleaseBalanceAuditTests` now checks curated archetype legality, independent permanent Melee and Ranged Deflection cap access, full 400 SP package-frontier outlier reporting, and every active weapon package combined with the legal cross-skill support frontier. The scope includes Mimicry and all three Espionage categories. Curated danger profiles cover poison/trap/Mimicry, stealth burst, cross-resource sustain, damage-derived healing, source-specific deflection/reflection, and layered control. `CombatUpgradeReleaseValidationMatrix.md` defines the manual test set for real enemies, attack-delay feel, support-system interactions, weapon identity checks, peak-damage target bands, and mob-tuning decisions.

### W-000A Cross-Skill Feedback Loops - Automated Coverage Added

`CrossSkillPerkInteractionSafetyTests` proves the shared static termination rules: triggered and periodic damage exit before direct-hit perk/status procs; reflection and Marked for Death bonus damage use triggered delivery; transferred damage cannot reshare; one-shot redirects are consumed before damage dispatch; damage-derived healing aggregates under one per-hit cap; cross-resource conversion stays below 100 percent of paid cost and cannot call the inverse conversion; and cooldown reduction cannot reset capstones or run past ready.

### W-001 Crit Cap Pressure - Automated Coverage Added

Permanent crit pressure is now explicitly budget-tested. Staff Crusher plus Spear always-on crit sources stay below the 50 percent crit cap before stances, support, or temporary buffs.

### W-002 Haste And Attack Delay Need Engine Validation - Timing Pass Implemented

The code pass extended weapon setup payoff windows that were most likely to expire before the player could benefit from them. Next auto/ability/attack-delay payoff windows below the 15-30 second review band were moved to 18 seconds across weapon lines such as Pistol, Rifle, Throwing, Staff, Spear, Katar, Twin Blade, Vibroblade, Vibroknife, Heavy Vibroblade, and Saberstaff.

Engine feel still needs playtest confirmation under real attack-delay conditions, but the short-window implementation issue is addressed.

### W-003 Shield And Weapon Deflection Remain Separate - Guarded By Coverage

Shield Deflection remains shield-gated and mechanically distinct from both Melee and Ranged Deflection. Regression coverage verifies that Shield Deflection, both weapon-deflection budgets, and Guard do not pollute each other.

### W-004 Guard Is Distinct And Currently Less Dangerous Than Deflection - Guarded By Coverage

Katar Iron Guard's `Guard Training` remains a damage-stage tank identity rather than a deflection clone. Regression coverage verifies Guard stays separate from both attack-roll deflection mechanics.

### W-005 Dependency-Only Weapon Perks - Implemented, Automated Coverage Added

Weapon perks that only enhanced one named sibling perk were converted to broader stat-driven effects. The pass covers Cascade Failure, Essence Hunter, Spinning Deflection, Retaliatory Flow, Reload Tempo, Gunslinger Focus, Expose Weak Point, Dead Center, Steady Aim II, Shrapnel Casing, and Volatile Payload.

The removed behavior-specific hooks are no longer active in shared combat/stat code. Regression coverage now checks the replacement broad stat hooks and guards against reintroducing the old named dependency strings.

## Post-Fix Playtest Priorities

1. Melee Deflection stack with Staff Sentinel, Twin Blade Duelist, and Heavy Vibroblade Immortal against melee weapon auto-attacks.
2. Ranged Deflection stack with Lightsaber Ward and Saberstaff Tempest against ranged weapon auto-attacks, including Deflecting Return reflection.
3. Staff Crusher with low-delay/high-hit-rate MGT builds.
4. Heavy Vibroblade sustain tank after reduced sustain values.
5. High-MGT damage stack with Leadership support.
6. High-PER crit stack with Leadership support.
7. Spear/Vibroknife low-positional-uptime solo baseline.
8. Spear/Vibroknife high-positional-uptime ceiling.
9. Spear Disabler against non-Force enemies.
10. Lightsaber Offense before and after Saber Storm availability. (Superseded 2026-07-11: Lightsaber Offense is now the Ward tree and the "Saber Storm" mastery quest gates the Severance capstone Epicenter, not a Lightsaber Offense unlock. See the 2026-07-11 addendum below for the current pre/post-capstone playtest pairing.)
11. Shield Deflection tank using Vibroblade Defense and shield item properties against both melee and ranged weapon auto-attacks.
12. Katar Guard tank with and without support buffs.
13. Weapon plus Beast Mastery companion-pressure baseline.

## Implementation Queue Status

1. Independent permanent Melee and Ranged Deflection cap access: complete.
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
- The old Lightsaber Defense `Deflection Training` line no longer exists; Deflecting Return instead grants Ranged Deflection and reflects a bounded amount of weapon damage back at the attacker when that Ranged Deflection negates a directly targeted ranged weapon auto-attack. B-001 now accounts for its Ranged Deflection independently from Melee Deflection sources.
- Three net-new shared engine systems back the redesign: physical-to-Force damage conversion (Saber Ward), Embattled stacking (Surrounded Not Outmatched / Aegis Eternal), and bounded Deflecting-Return weapon reflection triggered by Ranged Deflection against a directly targeted ranged weapon auto-attack.
- The existing capstone mastery quests were reused, not renamed: the "Saber Storm" mastery quest now gates Epicenter, and the "Guardian Master" mastery quest now gates Aegis Eternal. Quest IDs and definitions are unchanged.
- Updated playtest priority 9: test Ward/Severance baseline play before Epicenter/Aegis Eternal unlock, then again after, rather than the retired "before/after Saber Storm" framing.

The Bible workbook and `CombatUpgradeBiblePerkManifest.csv` are the source of truth for the new Ward/Severance rows; this addendum is qualitative and does not restate specific numeric tuning values.

## 2026-07-19 Addendum: Full Static Review And Deflecting Return Correction

The refreshed workbook review contains 1,003 rows: 998 in-scope rows pass and only the five unimplemented, first-iteration-out-of-scope Agriculture rows are skipped. Mimicry is 98/98 pass, including all 88 techniques; Espionage is 41/41 pass.

The production-consumer audit found one real stat wiring defect: `EmbattledHighStackDeflectionReflectionBonusPercent` was granted by Center of the Storm but never consumed. It is now applied at the documented Embattled stack threshold. The same review found that Aegis Eternal's Perfect Aegis status added its 24% reflection and 75% cap on top of Deflecting Return instead of setting the documented final values. Perfect Aegis now uses stat-driven override values, so the capstone resolves to exactly 24% reflection with a 75% damage cap while normal Center of the Storm behavior resolves to 20%/50% at high Embattled stacks.

## Not Recommended

- Do not tune mobs around pre-fix damage screenshots.
- Do not add combo-specific hardcoded penalties.
- Do not weapon-lock broad perk lines unless softer budget, source-tier, uptime, or trigger changes fail.
- Do not perform broad thematic rewrites to Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, or Espionage in this leg.
