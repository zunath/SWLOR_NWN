# Combat Upgrade Release Validation Matrix

Last reviewed: 2026-08-13

## Purpose

This matrix is the release-facing validation surface for the combat upgrade feedback pass. It translates the player feedback into repeatable checks, curated test builds, real enemy targets, and manual playtest prompts.

The code-side audit now covers weapons, Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, Espionage, Armor, and companion contribution. The live engine still needs manual confirmation for attack queue feel, facing, and real enemy behavior.

## Automated Release Gates

The automated balance suite adds these guardrails:

- Curated archetypes must be legal under the 400 SP cap and stay below hard release gates.
- Full package enumeration scans every selectable combat package through a capped frontier and independently hard-fails permanent Melee Deflection or Ranged Deflection reaching its default cap.
- A hard active-context frontier combines each weapon package with every retained legal Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage support frontier under 400 SP. These combinations must retain a meaningful offense/defense/sustain/control tradeoff.
- Enumeration compound outliers are reported for manual review instead of automatically failing, because many package-level totals include weapon-specific effects that cannot all be cashed out by one equipped weapon at the same time.
- `CrossSkillPerkInteractionSafetyTests` guards the shared trigger graph: triggered and periodic damage cannot re-enter direct-hit procs or reflection; transferred damage cannot reshare; one-shot redirects are consumed before dispatch; damage-derived healing shares the per-hit cap; cross-resource restores remain below paid cost; and cooldown reductions cannot reset capstones or run past ready.
- `CombatUpgradeBibleWorkbookFormattingTests.CharacterStats_DocumentsRuntimeCombatLimits` keeps the Bible `Character Stats` limits synchronized with the runtime caps that bound combined builds.

Hard automated blockers include permanent Melee Deflection or Ranged Deflection reaching its independent default 50 percent cap, a curated or active-context frontier profile crossing the compound release threshold, and any recursive or unbounded interaction edge. Melee and Ranged Deflection totals must never be combined into one budget. Coarse all-package compound totals remain diagnostic because they deliberately overcount mutually exclusive weapon, companion, poison, trap, Mimicry, and positional payloads.

## Second Targeted Pass Outcome

The evidence pass did not justify another broad numeric sweep. The targeted changes from this leg are consistency and build-freedom fixes:

- Staff Sentinel `Sentinel Stance` now says +8 Melee Deflection everywhere, matching the status effect and tests.
- Weapon perks that only improved one named sibling perk were converted into broader weapon-line, status-condition, or generic stat hooks.
- The removed named hooks include the old Cascade Failure/Incapacitate cone, Explosive Toss-only bleed and critical knockdown, Circle Slash-only deflection payoff, Aimed Shot-only mark/cooldown/payoff, Quick Draw kill-recast, Guard Counter-only guarded payoff, and Soul Strike-only Essence Hunter rider.

The remaining risks are validation risks, not known static blockers:

- Package enumeration can surface high theoretical stacks, but many include weapon-specific effects that cannot all apply to one equipped weapon at once.
- Attack-delay and next-hit feel still depends on the NWN action queue.
- Real enemies, not holograms, must decide whether any outlier becomes a blocker.

## Target Weapon Progression Pattern

Use this as the default cleanup pattern for weapon skill perk lines. Deviations are allowed only when the tree's identity genuinely needs them; document those exceptions instead of letting the progression drift silently.

| Slot | SP | Perk Slot | Type | Skill Req |
| --- | ---: | --- | --- | ---: |
| 1 | 2 | Ability 1 Rank I | Combat | 2 |
| 2 | 2 | Trait 1 | Trait | 5 |
| 3 | 2 | Cross-skill Trait 1 Rank I | Trait | 8 |
| 4 | 2 | Ability 2 Rank I | Combat | 10 |
| 5 | 2 | Ability 1 Rank II | Combat | 12 |
| 6 | 4 | Cross-skill Trait 2 | Trait | 15 |
| 7 | 3 | Ability 3 Rank I | Combat | 18 |
| 8 | 4 | Stance | Stance | 20 |
| 9 | 4 | Cross-skill Trait 1 Rank II | Trait | 22 |
| 10 | 4 | Cross-skill Trait 3 | Trait | 25 |
| 11 | 3 | Ability 1 Rank III | Combat | 28 |
| 12 | 4 | Ability 2 Rank II | Combat | 30 |
| 13 | 3 | Ability 3 Rank II | Combat | 32 |
| 14 | 2 | Trait 2 | Trait | 35 |
| 15 | 4 | Ability 2 Rank III | Combat | 38 |
| 16 | 5 | Ability 1 Rank IV | Combat | 40 |
| 17 | 4 | Cross-skill Trait 1 Rank III | Trait | 45 |
| 18 | 6 | Capstone | Capstone | 50 |

The local Bible and regenerated manifest now adopt this progression pattern for every weapon style: 18 rows, 60 SP, skill-rank 2 opens with an active `Combat` ability, skill-rank 50 is a 6 SP `Capstone`, and no adjacent ranked ability rows are intended. The code alignment pass is still pending, so audit rows that report missing or stale live perk definitions are expected until the C# definitions are updated to match the Bible.

Adoption rule: keep the normalized SP costs and active-first opener unless playtesting proves the new pricing or early-combat cadence creates a release blocker. Future cleanup should focus on code alignment, engine feel, and real-enemy validation rather than another blind cost pass.

## Curated Archetypes

These are the release gate builds. They represent different playstyles, not better or worse roles.

| Archetype | Packages | Validation Goal |
| --- | --- | --- |
| Single weapon specialist | Vibroblade Frenzy | Baseline kill time and simple loop feel. |
| Two weapon-line hybrid | Vibroblade Frenzy, Heavy Vibroblade Berserker | Cross-tree value without mandatory stacking. |
| Three weapon-line combat maximizer | Heavy Vibroblade Berserker, Spear Vigor, Staff Crusher | High-MGT damage pressure after Crusher and sustain reductions. |
| Weapon plus Leadership | Vibroblade Frenzy, Leadership Vanguard Command | Party damage amplification without runaway solo damage. |
| Weapon plus Force support | Lightsaber Ward, Force Control, Force Sense | Force utility and weapon pressure without Ranged Deflection cap access. |
| Weapon plus Devices support | Rifle Marksman, Devices Field Support | Device mitigation/support with ranged cadence. |
| Weapon plus First Aid sustain | Heavy Vibroblade Berserker, First Aid Trauma Medic | Sustain budget after Heavy healing reductions. |
| Weapon plus Beast pressure | Spear Vigor, Beast Damage | Companion pressure without hiding weak weapon baselines. |
| Weapon plus Mimicry | Vibroblade Frenzy, Mimicry | Analyzer payload remains additive without erasing the weapon baseline. |
| Weapon plus Espionage Infiltrator | Vibroknife Shadow, Espionage Infiltrator | Back-attack burst and stealth utility retain positional and setup tradeoffs. |
| Weapon plus Espionage Saboteur | Vibroknife Saboteur, Espionage Saboteur | Poison and trap bonuses remain distinct, bounded payloads. |
| Poison/trap/Mimicry payload stack | Vibroknife Saboteur, Espionage Saboteur, Mimicry, General | Multiple non-weapon damage systems do not become a single runaway proc chain. |
| Stealth burst cross-skill stack | Vibroknife Shadow, Espionage Infiltrator, Mimicry, Leadership Vanguard Command | Burst setup keeps meaningful uptime and package tradeoffs. |
| Cross-resource sustain engine | Saberstaff Conduit, Force Control, Force Sense, First Aid Trauma Medic | FP/STM conversion cannot create free casts or unlimited sustain. |
| Damage-healing sustain engine | Heavy Vibroblade Berserker, Heavy Vibroblade Immortal, Saberstaff Conduit, First Aid Trauma Medic, Leadership Field Steward | All damage-derived healing riders share the 50% per-hit cap. |
| Deflection/reflection support stack | Lightsaber Ward, Staff Sentinel, Devices Field Support, Leadership Field Steward | Reflection, Melee Deflection, Ranged Deflection, and Shield Deflection remain separate and bounded. |
| Cross-skill control stack | Spear Disabler, Rifle Suppression, Devices Grenadier, Espionage Saboteur, Mimicry | Layered status pressure retains an offense/defense tradeoff and cannot recursively proc. |
| High-MGT damage stack | Heavy Vibroblade Berserker, Spear Vigor, Staff Crusher, Leadership Vanguard Command | Recreate the scary high-MGT test without pre-fix Crusher payload. |
| High-PER crit stack | Pistol Gambler, Rifle Marksman, Throwing Flurry, Leadership Vanguard Command | Crit ceiling and Leadership crit amplification. |
| Melee Deflection stack | Staff Sentinel, Twin Blade Lacerator, Heavy Vibroblade Immortal | Permanent Melee Deflection stays below its cap; stance and temporary windows feel earned against melee weapon auto-attacks. |
| Ranged Deflection stack | Lightsaber Ward, Saberstaff Tempest | Permanent Ranged Deflection stays below its cap; ability windows and reflection remain bounded against ranged weapon auto-attacks. |
| Shield Deflection stack | Vibroblade Bulwark, Devices Field Support, Leadership Field Steward | Shield identity covers both melee and ranged weapon auto-attacks while remaining separate from both weapon-deflection budgets. |
| Guard tank stack | Katar Iron Guard, Heavy Vibroblade Immortal, Leadership Field Steward | Guard mitigation and enmity without becoming deflection. |
| Sustain tank | Heavy Vibroblade Immortal, Heavy Vibroblade Berserker, First Aid Trauma Medic, Leadership Field Steward | Damage plus sustain stays survivable but not unkillable. |
| High-control/debuff stack | Spear Disabler, Vibroknife Saboteur, Rifle Suppression, Devices Grenadier | Control pressure does not crowd out damage tradeoffs. |
| Positional low-uptime build | Spear Vigor, Vibroknife Shadow | Baseline works when facing is unreliable. |
| Positional high-uptime build | Spear Vigor, Vibroknife Shadow, Katar Scrapper | Positional upside is rewarding without becoming the only viable mode. |

## Enumeration Outliers

The current coarse package frontier reports high-offense legal profiles clustered around:

- Espionage Saboteur, General, Katar Iron Guard, Rifle Marksman, Saberstaff Conduit/Tempest, Staff Crusher, Throwing Flurry/Ordnance, and Vibroblade Frenzy.
- Sustain variants centered on Heavy Vibroblade Berserker/Immortal plus Saberstaff Conduit, Spear Vigor, and Vibroblade Frenzy.

These remain diagnostic outliers because the coarse scan intentionally adds unrelated payload channels. The hard active-context frontier separately validates one equipped weapon plus every legal support frontier. If live play proves that a multi-weapon build can cash out the coarse total while also keeping high sustain, deflection, guard, or control, tune shared values or triggers before considering weapon locks.

The current sustain outliers should be rechecked around Heavy Vibroblade Immortal plus Heavy Vibroblade Berserker, often with Pistol Gambler, Rifle Marksman, Vibroblade Frenzy, and Beast Tank or Vibroblade Bulwark. These are the highest-priority sustain tests because they map most closely to the player feedback about healing after large hits.

## Real Enemy Profiles

Do not tune mobs from hologram results. Use these real profiles first:

| Profile | Resref Examples | Why It Matters |
| --- | --- | --- |
| Starter ordinary | `mynock`, `czcryo_mynock` | Low-stat baseline, starter pacing, simple special ability check. |
| Ordinary humanoid ranged/melee | `man_ranger_2`, `man_warrior_2` | Real weapon delays and basic humanoid defenses. |
| Mid-tier Force/caster pressure | `s_app_m`, `korr_frostbind` | Force Attack/Defense interaction, shutdown and resource-pressure value. |
| Dathomir ordinary fauna | `vdathswampland`, `vdathshear`, `vdathsquell`, `vgapingspider` | Late-game solo baseline. A full Light Consular damage rotation should maintain at least 75% hit chance and defeat these ordinary enemies in roughly 20-30 seconds; they are not group-gated elites. |
| Elite/scary | `byysk_guard002` | High HP, shield/weapon profile, sustained incoming pressure. |
| Boss chain | `bf_butcher`, `bf_kess` | High HP boss pacing and capstone encounter pressure. |
| Optional stress boss | `frogboss` | Extreme boss profile only; do not balance baseline combat around it. |

Mob tuning is not recommended until these profiles show a specific enemy-side failure after player build validation.

## Attack Delay And Duration Feel

The current code pass extended short setup windows, but live engine validation is still required. Static tests cannot prove NWN action-queue timing, facing, dual-wield cadence, or action clearing/resume behavior.

Manual checks should focus on:

- Twin Blade Cyclone and Lacerator haste/bleed windows.
- Saberstaff Conduit and Tempest area-triggered haste, deflection, and resource returns.
- Katar Scrapper control windows and Opportunist critical payoff.
- Pistol Rapid Shot plus Gambler cadence.
- Rifle Marksman slow/idle payoff and casted Aimed Shot feel.
- Rifle Suppression sustained-fire cadence.
- Throwing Ordnance and Flurry target-density cadence.
- Lightsaber Severance and Ward before and after capstones.
- Heavy Vibroblade sustain under real incoming damage.
- Force Leap against large Dathomir fauna, confirming the player lands 1.5m from the target and both models remain selectable.
- Full Light Consular rotations against the four ordinary Dathomir fauna profiles, then the named Tuskens and Hutlar Byysks, confirming the ordinary baseline does not inherit elite pacing.

If a timed payoff still feels bad, tune in this order: extend the window, lower the spike if needed, then consider next-N-hit mechanics only if duration tuning cannot solve the engine feel.

## Weapon Identity Status

The screenshot identity list is now the Bible target for weapon styles. Code alignment is still pending, so this table describes the intended local Bible identity and the release check each style still needs.

## Status Glossary

`Controlled` is a category, not a single status effect. A target is controlled while affected by a control effect such as Blind, Confusion, Dazed, Disoriented, Foggy Mind, Force Disruption, Hamstring, Hobble, Immobilized, Knockdown, Shadow Strike, Stunned, Tranquilized, or Adhesive Grenade.

| Weapon Tree | Current Status | Release Check |
| --- | --- | --- |
| Vibroblade Frenzy | Haste, auto-attack, and dual-wield rhythm. | Compare kill time and haste uptime against other specialists. |
| Vibroblade Bulwark | Shield use, Shield Deflection, and physical resistance. | Confirm Shield Deflection remains shield-gated and separate. |
| Vibroknife Shadow | Bleeds, DoTs, combos, and rupture. | Confirm baseline bleed loop works without positional dependency. |
| Vibroknife Saboteur | Debuff/control identity is coherent. | Confirm control pressure trades off damage. |
| Heavy Vibroblade Berserker | HP costs and low-HP pressure. | High-MGT plus support sustain test. |
| Heavy Vibroblade Immortal | Drain-tank sustain and defensive recovery. | Test with Guard/Shield/Leadership support. |
| Spear Vigor | High-STM evasion bruiser with positional upside. | Low-uptime baseline must remain acceptable. |
| Spear Disabler | Broadened beyond anti-Force. | Test against non-Force elite and caster enemy. |
| Staff Crusher | Mandatory global MGT payload removed. | Confirm remaining universal crit/haste is useful but not mandatory. |
| Staff Sentinel | CC and temporary Melee Deflection identity. | Confirm stance text/stat consistency and Melee Deflection stacking. |
| Twin Blade Cyclone | AoE/haste identity remains engine-sensitive. | Target-density and missed-attack cadence test. |
| Twin Blade Lacerator | Bleed spread and nearby bleeding payoff. | Bleed cadence and spread density test. |
| Saberstaff Conduit | Resource-flow identity is distinct. | Test high-resource payoff and area density. |
| Saberstaff Tempest | Ranged Deflection/Force pressure identity is distinct. | Test haste plus Ranged Deflection snowball risk. |
| Katar Scrapper | Strong control with longer cooldowns. | Guard/control window and support stacking test. |
| Katar Opportunist | Melee crit DPS and opening exploitation. | Critical payoff against debuffed targets and targets affected by control effects. |
| Pistol Gambler | Crit/rate-of-fire identity is clear. | Haste cap and crit-chain feel. |
| Pistol Skirmisher | Evasion/mobile ranged identity is clear. | Confirm dodge tanking does not become sustain tanking. |
| Rifle Marksman | Slow, long-range payoff identity is clear. | Idle/slow payoff must feel worth the delay. |
| Rifle Suppression | Medium-range auto-attack pressure and stacking shot damage. | Sustained-fire cadence against ordinary and Force profiles. |
| Throwing Ordnance | AoE/control/on-hit payload identity is clear. | Multi-target density and cluster ceiling. |
| Throwing Flurry | Ranged bleed and DoT cadence. | Bleed cadence and crit ceiling. |

## Screenshot Identity Verification

The local Bible, regenerated manifest, generated perk/ability code, and automated tests now use the screenshot archetypes as the weapon style names and row-level design target. Engine validation and real-enemy balance tests still need to prove the implementation in live play.

| Screenshot Identity | Bible Style | Bible Status | Code Status |
| --- | --- | --- | --- |
| Vibroknife Shadow: bleeds, DoTs, combos, rupture | Vibroknife Shadow | Adopted | Implemented |
| Vibroknife Saboteur: attack/accuracy debuffs, STM drain | Vibroknife Saboteur | Adopted | Implemented |
| Vibroblade Frenzy: haste, auto-attack enhancement, dual wield | Vibroblade Frenzy | Adopted | Implemented |
| Vibroblade Bulwark: shield use, Shield Deflection, physical resistances | Vibroblade Bulwark | Adopted | Implemented |
| Twinblade Cyclone: AoE damage, buffs from hitting multiple enemies | Twin Blade Cyclone | Adopted | Implemented |
| Twinblade Lacerator: DoTs, bleed spread, attack from nearby bleeding enemies | Twin Blade Lacerator | Adopted | Implemented |
| Staff Crusher: next-hit DPS, bonus damage to CC'd enemies | Staff Crusher | Adopted | Implemented |
| Staff Sentinel: CC and Deflection | Staff Sentinel | Adopted | Implemented |
| Heavy Vibroblade Immortal: life stealing, drain tank | Heavy Vibroblade Immortal | Adopted | Implemented |
| Heavy Vibroblade Berserker: HP costs, stronger at lower HP | Heavy Vibroblade Berserker | Adopted | Implemented |
| Spear Disabler: FP drain, Force reflection, ability disablement | Spear Disabler | Adopted | Implemented |
| Spear Vigor: high STM cost abilities, evasion bruiser | Spear Vigor | Adopted | Implemented |
| Katar Opportunist: melee crit DPS | Katar Opportunist | Adopted | Implemented |
| Katar Scrapper: melee control, high-CD stronger CC | Katar Scrapper | Adopted | Implemented |
| Lightsaber Severance: deflection and empowered attacks after deflection | Lightsaber Severance | Adopted | Implemented |
| Lightsaber Ward: one-person defense guarding, damage sharing, enmity redirection | Lightsaber Ward | Adopted | Implemented |
| Saberstaff Conduit: FP/STM regen, resource synergy, higher damage if both resources are high | Saberstaff Conduit | Adopted | Implemented |
| Saberstaff Tempest: deflection with FP regen and Force Power enhancement | Saberstaff Tempest | Adopted | Implemented |
| Pistol Skirmisher: evasion buffs and dodge tanking | Pistol Skirmisher | Adopted | Implemented |
| Pistol Gambler: crit-focused ranged DPS | Pistol Gambler | Adopted | Implemented |
| Rifle Suppression: auto-attack DPS, medium-range faster fire, stacking damage per shot | Rifle Suppression | Adopted | Implemented |
| Rifle Marksman: long range, long delay, enhanced attacks, idle damage buff | Rifle Marksman | Adopted | Implemented |
| Throwing Ordnance: ranged on-hit effects, control, AoE | Throwing Ordnance | Adopted | Implemented |
| Throwing Flurry: ranged bleeds and DoTs | Throwing Flurry | Adopted | Implemented |

Pre-release stance: preserve build freedom and use weapon locks only as a last resort if value, trigger, uptime, SP, and shared-stat tuning cannot solve a real tested balance problem.

## Support-System Audit

Force, Devices, Leadership, First Aid, Beast Mastery, Mimicry, and Espionage are included in the audit but are not broad redesign targets in this leg.

Review these interactions first:

- Leadership Vanguard Command damage, crit, and mark amplification.
- Leadership Field Steward damage reduction and temporary HP.
- Devices Field Support temporary HP, ranged mitigation, and Power Cell accuracy.
- First Aid Trauma Medic healing amplification and recovery windows.
- First Aid Combat Pharmacology stim duration and combat sustain.
- Beast Damage pressure and Beast Tank mitigation alongside player sustain.
- Mimicry potency and passive-trait loadouts alongside weapon packages.
- Espionage Infiltrator back-attack pressure and Saboteur poison/trap amplification alongside weapon packages.

## Peak Damage Target

Do not set a hard damage cap for this release pass. Use target bands:

- Routine specialist hits should stay well below the old 600-1100 screenshot range.
- Around 300 damage should require situational setup, enemy state, crit, support, or a high-risk build.
- 600+ moments should be rare, heavily conditional, and should not also come with high passive sustain, high deflection, or high control.
- If a legal curated build repeatedly hits 600+ on real enemies while also sustaining through the fight, tune source values.

## Mob Tuning Decision

No broad mob tuning is recommended yet. First validate legal player builds against the real enemy profiles above.

Tune enemies only when evidence shows the enemy profile is the problem after player-build and cadence issues are ruled out. Do not raise enemy HP or defenses merely to survive pre-fix damage stacks, because that would punish ordinary builds.
