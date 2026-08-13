# Combat Upgrade Active Perk Budget Review

Last reviewed: 2026-08-13 (Force Burst parity correction; see entry below)

## Recommended Rule

Each weapon combat style should land at 4-6 distinct active buttons. Count rank chains as one button because the current-rank feat replacement path removes lower-rank active feats. Count `Combat`, `Stance`, `Toggle`, and `Aura` rows as active buttons.

Use 6 as the normal ceiling, 5 as acceptable for tighter trees, and 4 only for narrow support trees. With a player expected to level 2-3 weapon skills and 3-5 combat skills total, this produces about 25-30 combat buttons instead of asking players to hotbar every tactical variant from every tree.

This 4-6 target is binding for weapon skills only. Devices and Force are explicit exceptions because their tabs are broader utility/power catalogs rather than two-style weapon progressions. Support and companion tabs can still use the count as a review signal, but they should not be failed against the weapon-style button ceiling unless the design intent says those rows are ordinary player weapon buttons.

Armor is not a combat-upgrade perk tree and should stay out of this button budget. Espionage and Mimicry remain fully included in the implementation review, but the weapon-style 4-6-button ceiling is not their governing structure: Espionage is a utility system, while Mimicry is limited by the documented 10-slot loadout cap and 1-3 slot costs.

## Conversion Principles

- Convert all ranks of a selected base perk together. Do not leave `Foo I` active while `Foo II` is a `Trait`.
- Converted rows should become `Trait` rows and lose active-only fields: FP/STM cost, casting time, cooldown, active feat grants, recast groups, spell links, and active ability definitions.
- Preserve gameplay identity by turning removed buttons into always-on modifiers, proc riders, retained-active upgrades, or stat/status adjustments.
- If a converted row only modifies one named perk, fold it into that perk line as a later rank instead of leaving it as a standalone trait. If it modifies a broader role pattern, write the description against the tree/role category rather than against a shopping list of specific active perks.
- Preserve stance, toggle, and aura identity as active buttons when the perk is a mode choice, maintenance state, or command surface. Do not convert those rows merely to hit the count target.
- Trait descriptions should not say `Passive`; trait rows are already passive by type.
- Trait descriptions must name resistance types clearly, such as `Trauma Resistance`, instead of using shorthand like `+2 Trauma`. Use `rating` for flat resistance-stat values and `%` only for true percent effects.
- Do not add equipment requirements to converted traits or description cleanup rows.
- Shield Deflection should remain the higher raw deflection package. Lightsaber Defense Attack Deflection should stay below comparable shield values and carry its identity through FP recovery, enmity, counters, and ally support.
- Avoid undefined tactical shorthand such as `control abilities`, `area controls`, or `protection effects`. Describe the trigger by status, buff type, or named perk line so the affected abilities are clear from the row.
- Shared combat, ability, and status-effect infrastructure should continue to read `StatType` adjustments where possible rather than special-casing perk types.
- Leadership aura rows remain active aura choices. Converted Leadership rows should become upgrades to retained commands or auras, not free global aura effects.

## Active Count Plan

The table below records the historical active-to-trait conversion pass. Treat weapon rows as the binding budget surface; treat Devices, Force, support, and companion rows as context only.

| Skill | Tree | Current active bases | Proposed active bases | Convert these bases to traits |
| --- | --- | ---: | ---: | --- |
| Beast Mastery | Balanced | 7 | 6 | Pack Recovery |
| Beast Mastery | Bruiser | 5 | 5 | None |
| Beast Mastery | Damage | 8 | 6 | Predator's Mark; Predator's Mark II (formerly Predator Rush) |
| Beast Mastery | Evasion | 5 | 5 | None |
| Beast Mastery | Force | 5 | 5 | None |
| Beast Mastery | Tank | 6 | 6 | None |
| Beast Mastery | Training | 6 | 6 | None |
| Devices | Assault Gadgets | 6 | 6 | None |
| Devices | Field Engineer | 6 | 6 | None |
| Devices | Field Support | 7 | 5 | Rayshield Screen; Dampening Field |
| Devices | Grenadier | 7 | 6 | Cluster Grenade |
| First Aid | Combat Pharmacology | 7 | 6 | Coagulant |
| First Aid | Trauma Medic | 7 | 6 | Emergency Sealant |
| Force | Universal | 3 | 4 | Restore Throw Lightsaber; classify Force Push, Force Leap, and Mind Trick as Universal |
| Force | Dark Manipulator | 10 | 6 | Mind Shroud moved to Universal; Fracture Focus; Force Grip III (formerly Force Choke); Collapse Will |
| Force | Dark Ravager | 9 | 6 | Saber Rend moved to Universal; Devouring Strike; Force Body |
| Force | Light Consular | 9 | 5 | Mind Trick moved to Universal; Clarity; Comprehend Speech; Force Mend |
| Force | Light Guardian | 10 | 4 | Force Push and Force Leap moved to Universal; Courageous Resolve; Soothing Guard; Reflective Barrier; Bastion of Light |
| Heavy Vibroblade | Defense | 11 | 6 | Anger Strike; Blood Weapon; Crushing Blow; Earthshatter II; Guardian's Resolve |
| Heavy Vibroblade | Offense | 10 | 6 | Bloodlust; Essence Hunter; Soul Ascension; Soul Sacrifice |
| Katar | Iron Guard | 9 | 6 | Breaker Reversal; Covering Claws; Iron Elbows |
| Katar | Venom Current | 9 | 6 | Venom Splash; Twin Fang Flurry; Toxic Rush |
| Leadership | Field Steward | 7 | 6 | Bolster Resolve |
| Leadership | Vanguard Command | 7 | 6 | Mark Target |
| Lightsaber | Defense | 7 | 5 | Guardian's Influence; Thunderous Challenge folded into Guardian's Challenge II. (Superseded 2026-07-11: Lightsaber Defense was redesigned into the Severance tree — see redesign entry below.) |
| Lightsaber | Offense | 13 | 6 | Arc Strike; Centering; Overwhelming Strike; Purify; Ripple Slash; Second Wind; Surge Strike. (Superseded 2026-07-11: Lightsaber Offense was redesigned into the Ward tree — see redesign entry below.) |
| Pistol | Gunslinger | 6 | 6 | None |
| Pistol | Skirmisher | 9 | 6 | Low Shot; Ricochet Shot; Snap Roll |
| Rifle | Marksman | 9 | 6 | Breach Round; Expose Weak Point; Kill Zone |
| Rifle | Pacification | 9 | 6 | Neutralizing Shot; Overwatch; Pinning Fire |
| Saberstaff | Conduit | 8 | 6 | Force Lens; Conduit Flare |
| Saberstaff | Tempest | 7 | 6 | Force Gyre |
| Spear | Damage | 9 | 6 | Breach Strike; Crippling Defense; Improved Attentiveness |
| Spear | Disabler | 9 | 6 | Force Nullification; Forcebane; Fracture Strike |
| Staff | Crusher | 7 | 6 | Skull Rattle |
| Staff | Sentinel | 8 | 6 | Guarding Step; Sentinel Guard |
| Throwing | Bombardier | 8 | 6 | Cluster Storm; Saturation Toss |
| Throwing | Deadeye | 8 | 6 | Marking Toss; Ricochet Toss |
| Twin Blade | Cyclone | 7 | 6 | Sweeping Advance |
| Twin Blade | Duelist | 7 | 6 | Reversal Cut |
| Vibroblade | Defense | 5 | 5 | None |
| Vibroblade | Offense | 7 | 6 | Riot Blade II and Savage Cleave II (formerly Whirlwind Assault) |
| Vibroknife | Saboteur | 9 | 6 | Cascade Failure; Sap Vitality; Toxic Coating |
| Vibroknife | Shadow | 9 | 6 | Smoke Bomb II (formerly Decoy); Evasive Combat; Marked for Death |

## Trait Redesign Notes

The conversion list favors buttons that are redundant with a retained active or can naturally become a proc/upgrade:

- Grenade, toss, and shot variants should become trait riders on the retained core projectile ability when they mostly add area shape, secondary targets, or repeated-hit pressure.
- Defensive conversions should trigger off existing defensive outcomes such as shield deflection, attack deflection, or guard, or become perk-owned stat packages without adding gear checks.
- Force conversions should fold secondary buffs and debuffs into retained signature powers, preserving Light/Dark identity without giving each narrow effect its own hotbar slot.
- First Aid conversions should augment retained heals/cleanses rather than compete with them as separate emergency buttons.
- Heavy Vibroblade conversions should keep the high-risk resource identity but reduce the number of separate self-buff and self-damage buttons.
- Leadership conversions should enrich retained commands/auras so commanders choose between fewer stronger orders.
- Stances, toggles, and auras such as Berserker Stance, Blazing Spikes, Sniper Stance, Tempest Stance, Deadly Precision, Rallying Standard, and Steady Formation remain active buttons.
- Converted traits should not be useless unless another unrelated perk is also purchased. The 2026-06-08 follow-up folds single-line riders such as Earthshatter II, Force Grip III, Smoke Bomb II, Predator's Mark II, Riot Blade II, and Savage Cleave II into their target perk lines, and broadens the remaining multi-ability riders to tree/role categories.
- Thunderous Challenge was folded into Guardian's Challenge II because the old Lightsaber Defense version duplicated Guardian's Challenge as a damage-plus-enmity challenge button with only line geometry and a longer cooldown separating it.
- The 2026-06-11 Field Support follow-up converts `Rayshield Screen I-II` and `Dampening Field I-II` into mitigation traits attached to Field Support ally buffs. This removes the placed Rayshield screen area and turns Dampening into a lower-potency support rider instead of another active buff button.

## Balance Review Notes

The 2026-06-09 balance pass uses these cross-tree constraints:

- Shield Deflection keeps the strongest raw deflection package at +35 total. Attack Deflection packages should stay below that ceiling; lightsaber remains lower and leans on FP recovery/enmity, staff and saberstaff cap at +30, and temporary Attack Deflection bonuses stay smaller.
- Recurring sustain traits should not outpace active healer/support tools. Heavy Vibroblade Offense keeps its high-risk HP-spend identity, but Bloodlust, Vampiric Fury, and Soul Strike III are tuned down and throttled so they do not combine into near-permanent self-healing and stamina recovery.
- Weapon-tree passive party support should not eclipse Leadership. Spear's stance-linked party hit bonus is reduced to a small passive value, leaving larger group throughput packages to Leadership commands and capstones.
- Light Force should not be reduced to healing and defense only. Light-side damage should read as controlled pressure, kinetic impact, judgment, or subdual paired with restraint/protection riders, while Dark remains the stronger raw damage, drain, and execute identity. Do not make a plainly nonviolent name such as `Pacify` deal damage; keep `Pacify` as mitigation/control or rename the line if it becomes offensive.
- The 2026-06-11 additive Light DPS pass restores `Throw Rock I-III` as Light-aligned Alter kinetic damage without replacing `Throw Lightsaber`, `Force Spark`, or any other existing Force line. This raises Force from 220 SP to 229 SP, so `Arc Projector I-III` was added to Devices Assault Gadgets for a matching +9 SP and keeps Devices at 229 SP.
- The 2026-06-11 follow-up replaces the mistaken fourth-rank idea with `Radiant Lance I-III` as a new Light Sense 8m line DPS ability. `Ion Lance I-III` was added as the matching Devices Assault Gadgets 8m line DPS ability. A later 2026-06-11 SP rebalance lowered `Radiant Lance I`, shifted Devices category costs to `59/59/59/60`, and left Force and Devices equal at 237 SP; the same-day tier-consistency pass below supersedes those row prices.
- Devices category SP parity is a binding constraint: each of the four Devices categories must cost the same total SP to complete, so a Grenadier build, Field Engineer build, Field Support build, and Assault Gadgets build all pay the same price. When categories carry different row counts (Assault Gadgets has 21 rows versus 15-16 elsewhere), per-row prices flex within the category to hold the shared total; Assault Gadgets rows price lower on average by design. Force categories are not held to per-category parity because Alter/Control/Sense are content-proportional sections of one tree (`97/87/56`); only the Force sheet total must equal the Devices sheet total.
- Within a category, SP prices should still respect relative ordering: ranks within a line never decrease in price, requirement-50 capstones cost 5, and the strongest unique traits and party tools (such as `Diagnostic Sweep`, `Beacon Targeting II`, `Overclock Routine`, `Rayshield Screen II`, and `Group Deflector`) carry premium 5 SP pricing. Never price a row purely to back into a target total without a power justification.
- Force/Devices twin lines must carry identical prices and base damage: `Throw Rock` and `Arc Projector` (Devices 12/30/45) are both 2/3/4 SP; `Radiant Lance` and `Ion Lance` (Devices 15/32/48) are both 3/4/4 SP. Cooldowns may diverge when Force alignment feel requires it; `Throw Rock` uses a 6-second cooldown so Light Alter has the same spammable opener cadence as Dark Alter's `Force Spark`, while `Arc Projector` remains on the Devices 18-second direct-damage cadence. Twin skill requirements may diverge per sheet (after the requirement-spread pass, `Throw Rock` sits at Force 0/18/40 and `Radiant Lance` at Force 8/35/48) because each sheet balances its own progression ladder.
- The 2026-06-11 tier-consistency pass repriced the additive twin lines up from their placeholder 1-2 SP values and moved Force from 237 SP to 240 SP, but left Devices category totals lopsided at `58/55/56/71`. The same-day follow-up restored category parity at `60/60/60/60`: Assault Gadgets non-twin lines were reshaped down (`Flamethrower` 2/2/3, `Wrist Rocket` 2/3/3, `Sonic Burst` 2/3/3, `Rail Dart` 2/3, `Gadget Harness` 2, `Tactical Uplink` 2, `Cryo Sprayer` 3), Grenadier raised `Concussion Grenade II` and `Ion Grenade II` to 4, Field Engineer raised `Blaster Beacon I` to 3, `Incendiary Field II` to 4, and `Shock Beacon II`, `Diagnostic Sweep`, and `Beacon Targeting II` to premium 5, and Field Support raised `Deflector Shield I` to 3 and `Overclock Routine`, `Rayshield Screen II`, and `Group Deflector` to premium 5. Force and Devices remain equal at 240 SP and the twin-line prices are unchanged.
- After the tier-consistency pass, Light and Dark core damage actives cost the same SP: Dark (`Force Spark` 5, `Force Lightning` 11, `Force Drain` 10, `Creeping Terror` 9) and Light (`Throw Rock` 9, `Radiant Lance` 11, `Force Judgment` 11, `Purifying Wave` 4) both total 35 SP. Dark keeps the raw damage/drain/execute identity (including the `Devouring Strike` and `Unstable Pressure` execute traits and the `Hunger of the Dark` capstone); Light keeps controlled pressure plus restraint/protection riders and the `Last Stand of the Light` capstone, with the Sense capstone `Eclipse of Resolve` staying alignment-neutral.
- The 2026-08-13 feedback pass restores `Force Burst` as one Force-30, 3-SP Light area-damage power instead of an additive three-rank line. `Force Judgment I-III` move from `3/4/4` to `2/3/3` SP, so the combined Light damage budget remains 35 SP. Force and Devices remain equal at 240 total SP, 68 perk rows, and 54 `Combat` rows.
- Force skill requirements must be balanced across Light, Dark, and Universal lines, and each alignment needs a rank-0 entry point: `Force Spark I` (Dark DPS), `Throw Rock I` (Light DPS, moved from Force 12), `Benevolence I` (Light support), and `Weaken Resolve I` (Dark debuff, moved from Force 8) all have no skill requirement, so both Light and Dark players can deal damage and progress from rank 0.
- Force requirements should be spread evenly across the 0-50 grid with no rank step carrying more than two rows in a section and no large dead zones. The 2026-06-11 requirement-spread pass fixed Alter's triples at 28/38/48 and its 40-42 gap, Control's 20-to-30 gap and 45 triple, and Sense's quadruple at 25 (Sense's 16 perks now land on 16 distinct steps). Line rank ordering is preserved (`Force Spark` 0/18, `Force Lightning` 10/22/42, `Throw Rock` 0/18/40, `Force Choke` 8/20/30/48, `Guardian Ward` 2/15/35/45, `Force Judgment` 5/25/45), alignment access alternates inside each band (Dark gets a new tool roughly every 2-5 ranks in Alter while Light's `Throw Rock`/`Purifying Wave` ranks land alongside them; `Fury Stance` and `Cruel Momentum` spread Dark presence across Control at 12/28/42; Sense alternates Light/Dark/Universal across its ladder), and every row stays inside its SP price band. The 2026-08-13 Force Burst parity correction retains the 240-SP total while shifting the content-proportional section totals to `97/87/56`. Requirement parity with the Devices twin lines is not required because Devices has its own rank-0 DPS openers in every category.
- The 2026-07-04 weapon-skill cross-check found all twelve weapon skills balanced at the structural level: every skill totals 120 SP with 60 SP per style, every style capstone costs 6 SP, active capstones use the shared 90-second capstone cooldown with a 45-second active duration where applicable, single-target strike lines scale on the same curve once cooldown and rider differences are priced in, and stance trade-offs stay within the established plus/minus 15-25% envelope. No weapon-tree total changes were needed.
- Universal Force riders should not be narrowed to Dark-only triggers when their notes or role say they are universal. Use `damaging Force power` for universal triggers and reserve `damaging Dark Force power` for traits that intentionally require Dark affinity or Dark tree attacks.
- Restore `Throw Lightsaber` as a Force Universal active line. Despite the legacy name, its design should work with any equipped weapon type and should not add a lightsaber-only equipment requirement.
- Damage and pulse descriptions should avoid vague terms such as `light damage`, `increased DMG`, or `high DMG`. Rows should state the base damage, damage type, scaling source where applicable, target count/area, and status duration.
- Temporary HP, absorption shields, and damage-reduction traits should state the magnitude and trigger cadence. Traits that can be refreshed by ordinary rotational abilities need lower values than long-cooldown active saves.

## Implementation Follow-Up Status

The local Bible workbook was updated on 2026-06-08 with a temporary active-budget helper, and `CombatUpgradeBiblePerkManifest.csv` was refreshed from the workbook. The pass converted 97 active-budget workbook rows to `Trait` rows and preserved stance, toggle, and aura rows as active buttons. On 2026-06-09, a follow-up workbook pass restored the Force Universal classification and the Throw Lightsaber line. On 2026-06-11, another follow-up restored Throw Rock as an additive Light Alter DPS line, added Radiant Lance as a new Light Sense 8m line DPS ability, and added matching Arc Projector and Ion Lance rows to Devices. On 2026-06-11, Field Support Rayshield and Dampening mitigation riders were converted into Field Support mitigation traits without changing Devices SP. On 2026-06-11, Devices was redistributed to `59/59/59/60` across its four categories and Force and Devices stayed equal at 237 SP. Later on 2026-06-11, the placeholder twin-line prices were repriced, moving Force and Devices to an equal 240 SP, and Devices category totals were restored at `60/60/60/60` without touching the Force sheet. Cached section Total values were recomputed after price edits before the workbook total values were repaired. Later on 2026-06-11, Force skill requirements were rebalanced across Light/Dark/Universal, each alignment received a rank-0 entry (`Force Spark I`, `Throw Rock I`, `Benevolence I`, `Weaken Resolve I`), requirements were spread evenly across the 0-50 grid, and every row stayed inside its SP price band. On 2026-08-13, Force Burst was restored as a single Force-30 power, Force Judgment prices were rebalanced to retain the 35-SP Light damage budget, and workbook formula caches were repaired while retaining 240-SP Force/Devices parity.

As of 2026-06-14, the implementation follow-up is largely closed in code:

1. `CombatUpgradeBibleSyncTests` fails if a Bible `Trait` row still grants active feats or if a live ability remains tied to a Bible-scoped perk without an implemented active Bible row granting that feat.
2. `Throw Lightsaber`, `Throw Rock`, `Radiant Lance`, `Arc Projector`, and `Ion Lance` have perk definitions, ability definitions, recast wiring, feat/spell links, and targeted tests.
3. Field Support Rayshield and Dampening mitigation riders have code and test coverage; final confidence still needs live support-playtest coverage because the behavior is group-buff math rather than just static row sync.
4. The legacy combat Bible review gate has been retired. Use the C# sync tests plus `tools\UpdateCombatUpgradeAudit.ps1` as the current static gates.
5. Remaining work from this review is release validation: run the focused audit/sync tests, the full server test project, and live-module smoke tests before handoff.

## 2026-07-11 Lightsaber Ward/Severance Redesign

The Lightsaber Defense and Lightsaber Offense rows in the Active Count Plan table above are superseded. Both trees were fully replaced with new perk lines, renamed Severance (`PerkCategoryType.LightsaberDefense`) and Ward (`PerkCategoryType.LightsaberOffense`):

- Ward: Saber Ward, Mental Fortress, Deflecting Return, a retained/reworked 2-rank Guardian's Challenge, Surrounded Not Outmatched, Force Link, Immovable Stance, Reprisal, Center of the Storm, and the capstone Aegis Eternal.
- Severance: Force Sheath, Overpower, Fast Strikes, Shattering Strike, Sundering Sweep, Weak Points, Imbuement Stance, High Ground, Focus Shift, and the capstone Epicenter.

The new trees were designed against the same 4-6 distinct active button rule and the 60 SP per-style/6 SP capstone structural constraints documented above; this entry does not restate specific active-button counts or SP prices because the local Bible workbook and `CombatUpgradeBiblePerkManifest.csv` are the source of truth for those numbers going forward. The old B-001 Attack Deflection accounting for `Deflection Training` no longer applies — Deflecting Return instead reflects a bounded amount of weapon damage back at the attacker off an Attack Deflect of a directly targeted ranged attack, rather than adding another raw deflection-chance source. The redesign also introduced Embattled stacking (Surrounded Not Outmatched / Aegis Eternal) and physical-to-Force damage conversion (Saber Ward) as net-new shared engine systems. The capstone mastery quests were reused unchanged: "Saber Storm" now gates Epicenter, and "Guardian Master" now gates Aegis Eternal.
