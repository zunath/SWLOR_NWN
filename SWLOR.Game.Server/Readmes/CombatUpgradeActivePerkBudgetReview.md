# Combat Upgrade Active Perk Budget Review

Last reviewed: 2026-06-09

## Recommended Rule

Each combat perk tree should land at 4-6 distinct active buttons. Count rank chains as one button because the current-rank feat replacement path removes lower-rank active feats. Count `Combat`, `Stance`, `Toggle`, and `Aura` rows as active buttons.

Use 6 as the normal ceiling, 5 as acceptable for tighter trees, and 4 only for narrow support trees. With a player expected to level 2-3 weapon skills and 3-5 combat skills total, this produces about 25-30 combat buttons instead of asking players to hotbar every tactical variant from every tree.

Armor is not a combat-upgrade perk tree and should stay out of this budget. Espionage remains out of current combat-upgrade scope. Beast role abilities appear to be AI-usable beast feats rather than ordinary player weapon buttons, but they should still be kept within this shape unless playtest proves they do not create player-facing command pressure.

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
| Devices | Field Support | 7 | 6 | Rayshield Screen |
| Devices | Grenadier | 7 | 6 | Cluster Grenade |
| First Aid | Combat Pharmacology | 7 | 6 | Coagulant |
| First Aid | Trauma Medic | 7 | 6 | Emergency Sealant |
| Force | Universal | 3 | 4 | Restore Throw Lightsaber; classify Force Push, Force Leap, and Mind Trick as Universal |
| Force | Dark Manipulator | 10 | 6 | Mind Shroud moved to Universal; Fracture Focus; Force Grip III (formerly Force Choke); Collapse Will |
| Force | Dark Ravager | 9 | 6 | Saber Rend moved to Universal; Devouring Strike; Force Body |
| Force | Light Consular | 9 | 5 | Mind Trick moved to Universal; Clarity; Comprehend Speech; Force Mend |
| Force | Light Guardian | 10 | 4 | Force Push and Force Leap moved to Universal; Courageous Resolve; Soothing Guard; Reflective Barrier; Bastion of Light |
| Heavy Vibroblade | Defense | 11 | 6 | Anger Strike; Blood Weapon; Crushing Blow; Earthshatter II (formerly Edge of Darkness); Guardian's Resolve |
| Heavy Vibroblade | Offense | 10 | 6 | Bloodlust; Essence Hunter; Soul Ascension; Soul Sacrifice |
| Katar | Iron Guard | 9 | 6 | Breaker Reversal; Covering Claws; Iron Elbows |
| Katar | Venom Current | 9 | 6 | Venom Splash; Twin Fang Flurry; Toxic Rush |
| Leadership | Field Steward | 7 | 6 | Bolster Resolve |
| Leadership | Vanguard Command | 7 | 6 | Mark Target |
| Lightsaber | Defense | 7 | 5 | Guardian's Influence; Thunderous Challenge folded into Guardian's Challenge II |
| Lightsaber | Offense | 13 | 6 | Arc Strike; Centering; Overwhelming Strike; Purify; Ripple Slash; Second Wind; Surge Strike |
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

## Balance Review Notes

The 2026-06-09 balance pass uses these cross-tree constraints:

- Shield Deflection keeps the strongest raw deflection package at +35 total. Attack Deflection packages should stay below that ceiling; lightsaber remains lower and leans on FP recovery/enmity, staff and saberstaff cap at +30, and temporary Attack Deflection bonuses stay smaller.
- Recurring sustain traits should not outpace active healer/support tools. Heavy Vibroblade Offense keeps its high-risk HP-spend identity, but Bloodlust, Vampiric Fury, and Soul Strike III are tuned down and throttled so they do not combine into near-permanent self-healing and stamina recovery.
- Weapon-tree passive party support should not eclipse Leadership. Spear's stance-linked party hit bonus is reduced to a small passive value, leaving larger group throughput packages to Leadership commands and capstones.
- Light Force should not be reduced to healing and defense only. Light-side damage should read as controlled pressure, kinetic impact, judgment, or subdual paired with restraint/protection riders, while Dark remains the stronger raw damage, drain, and execute identity. Do not make a plainly nonviolent name such as `Pacify` deal damage; keep `Pacify` as mitigation/control or rename the line if it becomes offensive.
- Universal Force riders should not be narrowed to Dark-only triggers when their notes or role say they are universal. Use `damaging Force power` for universal triggers and reserve `damaging Dark Force power` for traits that intentionally require Dark affinity or Dark tree attacks.
- Restore `Throw Lightsaber` as a Force Universal active line. Despite the legacy name, its design should work with any equipped weapon type and should not add a lightsaber-only equipment requirement.
- Damage and pulse descriptions should avoid vague terms such as `light damage`, `increased DMG`, or `high DMG`. Rows should state the base damage, damage type, scaling source where applicable, target count/area, and status duration.
- Temporary HP, absorption shields, and damage-reduction traits should state the magnitude and trigger cadence. Traits that can be refreshed by ordinary rotational abilities need lower values than long-cooldown active saves.

## Implementation Follow-Up

The local Bible workbook was updated on 2026-06-08 with `tools/ApplyCombatUpgradeActivePerkBudget.ps1`, and `CombatUpgradeBiblePerkManifest.csv` was refreshed from the workbook. The pass converted 97 active-budget workbook rows to `Trait` rows and preserved stance, toggle, and aura rows as active buttons. On 2026-06-09, `tools/ApplyCombatUpgradeForceUniversal.ps1` restored the Force Universal classification and the Throw Lightsaber line. This is a design-only review; the remaining implementation cleanup should happen in a separate code pass:

1. Update code for each converted base: remove active feat grants, remove or retire ability definitions, remove active-only recast/status wiring that no retained active uses, and preserve behavior through `StatType` or local status-effect hooks.
2. Extend the audit to flag stale active surfaces for manifest rows that are now `Trait`, because the current audit only checks that active rows have ability definitions.
3. Implement the restored `Throw Lightsaber` active line as Force Universal and preserve all-weapon compatibility. Until that code pass lands, `CombatUpgradePerkAudit.csv` should report three `MissingAbilityDefinition` rows for Throw Lightsaber; unrelated audit rows should be handled separately.
3. Run the focused perk review/audit scripts and the server test project before handoff.
