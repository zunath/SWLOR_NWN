# Twin Pistol Implementation Plan

## Design

Twin Pistol is a separate combat skill for fighting with a normal main-hand pistol and a simulated companion pistol in the left hand. **Barrage** rewards rapid sustained pressure on one enemy. **Crossfire** uses deliberate repositioning to build Momentum for stronger attacks and a stationary finisher. It accepts lower uninterrupted damage in exchange for movement. Single Pistol keeps its harder individual hits, critical-hit specialization, passive mobility/control choices, and shield option.

Twin Pistol has ranks 0 through 50, contributes to the existing 400-rank skill cap, and uses the Weapon combat-point category. It does not require Pistol ranks. Barrage and Crossfire each contain ten perks with eighteen purchasable ranks costing 60 SP. Both complete lines cost 120 SP. There is no shared perk line or shared purchase. Armor's existing Dual Wield ranks remain optional and cost another 9 SP. There are 20 distinct perks and 36 purchasable ranks across the full skill.

The skill name is **Twin Pistol** throughout player-facing text. Akimbo Stance belongs to Barrage; Crossfire Stance belongs to Crossfire. Add new identifiers and resource rows; do not reuse retired skill, perk, base-item, or TLK definitions.

This is a revised proposal for feedback, not shipped functionality. Values have undergone a reproducible formula/resource-budget pass; capstones, leveling, PvP, and engine timing still need validation. See the completed analysis and release criteria below. The companion [Discord feedback draft](TwinPistolDiscordFeedback.md) presents the same values for discussion.

## Skill and equipment progression

### Training from rank zero

Provide an obtainable rank-zero main-hand pistol and companion pistol so ordinary attacks can train Twin Pistol before its first ability unlocks at rank 2. There is no skill-rank-2 equip gate and no mandatory training perk. Use the existing basic pistol budget for the starter pair, then mirror existing pistol tiers, damage types, delays, upgrade limits, and crafting progression.

Add companion recipes to the appropriate existing crafting progression and expose the starter through an existing starter or training vendor. Inspect current sources before choosing the vendor and recipe requirements. Do not distribute obsolete off-hand pistols or make an otherwise unobtainable test item searchable in the economy.

Each gun remains a separately owned item with its own damage, accuracy, delay, upgrades, and appearance. A companion costs a comparable amount to a pistol of the same tier. The second item and its enhancement capacity are included in balance comparisons.

### Requirements and equip order

A normal pistol retains its Pistol requirement in its stored item properties. A valid Twin Pistol pair interprets only that main-hand pistol's weapon-skill requirement against Twin Pistol at the same rank. Its other restrictions still apply. Companion items explicitly require Twin Pistol at their tier's rank.

Validation must use the prospective equipment arrangement, not just the arrangement before an equip event:

- A companion may be equipped first as an inactive item. It cannot attack, grant equipment bonuses, or qualify for weapon defenses until paired with an eligible pistol. This allows a character with Twin Pistol ranks and no Pistol ranks to equip the main hand afterward.
- Equipping the main hand then validates both weapons against Twin Pistol. An ordinary pistol with a shield or empty left hand continues to require Pistol.
- Removing or replacing the companion cancels pending shots and revalidates the main hand against Pistol. If the character cannot use it, unequip it safely into their inventory. Do not leave a usable high-tier pistol behind.
- Removing the main hand makes the companion inactive. A rifle, melee weapon, shield, or second companion never activates it.
- Login, respawn, item transfer, equipment restoration, and failed equip attempts run the same validation. No temporary reinterpretation is saved into the item's properties or base-item ID.

The inactive companion must not leak native equipped bonuses. Prefer authored properties whose effects are read from a validated weapon profile; suppress or reject unsupported native properties. If the engine cannot safely support this equip sequence, solve the pair-equip operation before content production rather than adding a hidden Pistol prerequisite.

### Skill selection

Add a context-aware skill resolver for ordinary weapon attacks. Both hands of a valid pair use Twin Pistol for attack, accuracy, critical skill contribution, damage rank limits, timing effects, character-sheet estimates, and combat progression. A lone ordinary pistol still resolves to Pistol. Preserve the base-item-only lookup for callers that actually need item classification.

Use the current pistol attributes: Perception for damage and Agility for accuracy, preserving supported item/stat overrides. Add the companion to the appropriate weapon classification and attribute mappings without classifying it as a shield or a native melee attacker.

Ordinary paired attacks contribute to Twin Pistol only. Route their contribution through the existing combat XP accounting, enemy eligibility, and award caps; do not grant a second full XP award for the simulated hand. An explicitly Pistol-scoped cross-trained ability retains its Pistol skill and progression behavior. Capture that identity when the action starts so changing equipment before impact cannot redirect XP.

Initialize the new skill through normal missing-skill handling for existing characters. Preserve SP/AP accounting and decay rules. Do not refund, convert, or reset existing Pistol ranks.

## Attack behavior

### Ordinary attacks

The native main-hand pistol remains the real ranged launcher. SWLOR schedules and resolves the companion shot. The companion's presentation carrier and its gameplay identity are separate: an attachment technique must not confer shield AC, Shield Deflection, native melee attacks, or attacks of opportunity.

One paired cycle budgets one main-hand shot and one companion shot. Both weapon delays contribute to the cycle. Reuse the existing paired-delay calculation, including the single payment of the engine baseline, ordinary Haste/Slow behavior, fractional batching, and bounded attack budgets. Armor's Dual Wield stat reduces only the companion delay by 10/20/30 percent; do not add a second stacking Twin Pistol delay-reduction trait.

Resolve each shot from its own captured weapon profile and independently determine hit, critical, ranged deflection, and damage. The companion still fires if the main hand misses. Attribute damage, threat, kill credit, combat feedback, and on-hit effects to the original attacker and correct weapon.

The attack resolver must use the same rules as other ranged auto-attacks: concealment and other avoidance, valid targets, ranged deflection, Guard at the damage stage, defenses, immunity/resistance/reduction, and damage-triggered effects. Apply each adjustment once. Do not implement the companion as a hostile ability that bypasses auto-attack defenses or as an unqualified damage copy of the main-hand result.

Both shots use the normal pistol auto-attack range and line-of-fire rules. Main-hand and companion may have different damage types and accuracy. A companion does not copy the main hand's enhancements or inherit its successful hit result.

Any equipped companion disables Single Weapon and Doublehand, just as any occupied off hand does today. Existing weapon deflection remains subject to its normal rules; Shield Deflection is unavailable without a real shield. Inactive companions provide no weapon defenses.

### Captured actions and interruption

Keep an action/cycle identity, target ID, weapon IDs, validated skill, weapon profiles, ammunition source, pending hand, and consumed-roll budget. Do not hold native object pointers across delayed callbacks.

Before launching a pending shot, verify that the action is still current, both actors are valid, the weapons are unchanged, the target is in the same area and legal to attack, and range/line of fire remain valid. Stop, death, incapacitation, target changes, unequip, area transitions, and invalid action state cancel unlaunched shots. A missed main-hand shot is not an interruption.

Define the launch boundary explicitly. Once a shot has launched, retain its original target and profile and use normal ranged impact semantics; it cannot become a shot from replacement equipment. Removed targets safely discard the impact. Presentation updates and observers never cause damage or consume charges.

Consume finite ammunition once per launched logical shot. Reuse unlimited-ammunition behavior without duplicating generated stacks. If ammunition runs out between hands, cancel the unlaunched hand. Ensure the native launcher and simulated resolver cannot both debit or refund the same shot.

### Abilities and cross-training

Twin Pistol actives require an active pair and use Twin Pistol for their skill-based calculations. Capture both weapons once per activation. Barrage attacks and Final Salvo use a 25m range. Blaster Slide selects an enemy within 15m; Grapple Shot selects one within 7.5m and closes at most 6m; Pistol Rush selects a ground destination 2-6m away. These are ordinary activated weapon abilities using established action/impact timing, not free auto-attacks added by a hit callback.

Each ability pays STM and starts its cooldown once. Its impacts may critically hit independently, but activation-level bonuses and resource restoration run once according to the ability's result summary. Follow existing activated-ability avoidance rules; auto-attack deflection does not silently become a new rule for existing activated abilities.

Purchased Pistol abilities remain usable with an eligible main-hand pistol in a pair and retain their original Pistol skill, impact count, and main-hand damage source. They receive no Single Weapon bonus while paired. Double Shot stays at two impacts and never receives an automatic companion copy. Pistol-specific passive selectors continue matching Pistol; generic ranged and unrestricted effects may still apply to Twin Pistol according to their existing declarations. Do not broaden all Pistol selectors to include Twin Pistol.

Selective shared cooldowns limit additional burst while leaving a useful basic-attack option for Pistol cross-training:

| Twin Pistol abilities | Shared recast group | Base cooldown |
|---|---|---:|
| Paired Volley / Blaster Slide | New: Twin Volley | 18 seconds |
| Focused Burst / Grapple Shot | Existing: Double Shot | 24 seconds |
| Controlled Burst / Pistol Rush | Existing: Fan the Hammer | 30 seconds |
| Twin Suns / Final Salvo | Existing: Capstone | 90 seconds |

Quick Draw retains its independent cooldown. Buying it adds a basic attack at the cost of Pistol ranks, SP, and STM; it never gains an extra companion impact. Dead Man's Hand already uses the global Capstone group, so both new finishers share that group with all existing capstones. Use these existing identities and explain sharing in descriptions; do not rename existing Pistol cooldowns. Each new stance has a 30-second recast and participates in existing stance exclusivity, including Gambler Stance. Standard attacks use existing weapon ability timing; both capstones have a two-second activation.

Queued abilities reserve and replace exactly one eligible landed ordinary roll. Other budgeted rolls remain ordinary attacks. Count limited haste/no-delay charges per actual attempted roll, including misses. Grant effects earned during a budgeted cycle only when they can no longer be spent retroactively by that cycle.

### Repositioning rules

Blaster Slide, moving Grapple Shot, and Pistol Rush share a two-second movement lockout in addition to their attack cooldowns. Validate the destination and complete travel path before charging costs: same area, reachable walkmesh, legal elevation, normal collision, no passage through walls/closed doors or prohibited transitions. Roots block relocation; ordinary incapacitation rules block activation. A stationary Grapple Shot against an enemy already within 1.5m remains usable while rooted if normal attack requirements pass. No automatic cleanse, invulnerability, untargetability, or crowd control accompanies movement.

Capture origin, destination, targets, charges, and both gun profiles. Resolve shooting only after confirmed arrival; a late failure cancels the impacts and generates no Momentum. Validation failure before payment consumes nothing; once launched, use normal failed-impact cost/recast behavior without a second refund path. Presentation callbacks cannot move or damage twice. Any replacement for unsafe engine jumps must be implemented and tested explicitly.

Pistol Rush is freely ground-targeted and can reposition without enemies. Its aimed attack line follows the actual origin-to-destination segment, four meters wide, with at most three distinct hostile targets captured at activation. Travel without a landed hostile hit generates no Momentum. Allow normal stationary attacks between movement abilities; Final Salvo never moves the caster.

## Perk progression

Each rank requires the listed Twin Pistol rank and the preceding rank of that perk. SP prices are per purchase. Ranked values replace earlier ranks. All new passive bonuses require an active pair; all direct damage retains each firing gun's damage type.

`D` means the firing gun's item weapon DMG with normal eligible adjustments, without Single Weapon or Doublehand. Add listed flat power, including spent Momentum power, before the normal damage formula. Controlled Burst uses `floor(0.75 * D)`; Pistol Rush uses `floor(0.60 * D)`, separately per gun. Percentage damage traits apply at the shared outgoing-damage adjustment stage. Stance penalties multiply the final ordinary-shot outgoing amount after additive bonuses; they never reduce activated ability power. Slash-separated values below follow perk-rank order.

### Barrage: 60 SP

| Perk | Twin Pistol rank | SP per purchase | Effect | STM | Cooldown |
|---|---|---|---|---|---|
| Paired Volley I-IV | 2 / 12 / 28 / 40 | 2 / 2 / 3 / 5 | One shot per gun at one enemy; each D + 4 / 9 / 14 / 19. | 4 / 6 / 9 / 12 | 18s |
| Alternating Fire | 5 | 2 | Every three landed ordinary Twin shots restore 1 STM; at most once per 6s. | - | - |
| Matched Rhythm I-III | 8 / 22 / 45 | 2 / 4 / 4 | Ordinary Twin Pistol damage +10 / 20 / 25% total. Uses the higher bonus of this perk and Paired Handling. | - | - |
| Focused Burst I-III | 10 / 30 / 38 | 2 / 4 / 4 | One shot per gun at one enemy; each D + 10 / 22 / 34. | 4 / 8 / 10 | 24s |
| Balanced Grip | 15 | 4 | +5 ACC to the companion only. | - | - |
| Controlled Burst I-II | 18 / 32 | 3 / 3 | One shot per gun at one enemy; each floor(0.75 x D) + 7 / 13. Restore 2 / 3 STM once if either shot lands. | 6 / 9 | 30s |
| Akimbo Stance | 20 | 4 | +10% Haste. Landed ordinary Twin shots against the same enemy add +2% Haste per stack, maximum five (+20% total); at most one stack per 2s. Stacks clear on target change or 6s without a landed ordinary shot. Ordinary Twin shots deal 15% less damage. | 0 | 30s |
| Converging Fire | 25 | 4 | After three landed ordinary shots at the same enemy, +5% ordinary Twin damage against it for 6s; further landed ordinary shots refresh. | - | - |
| Cool Under Fire | 35 | 2 | Taking direct hostile damage makes the next Twin ability cost 2 less STM; one charge, 10s duration, 10s trigger cooldown. | - | - |
| Twin Suns | 50 + mastery | 6 | One shot per gun at one enemy, each D + 20; next six ordinary Twin shots gain +25% damage, expiring after 12s. | 15 | 90s |

### Crossfire: 60 SP

| Perk | Twin Pistol rank | SP per purchase | Effect | STM | Cooldown |
|---|---|---|---|---|---|
| Blaster Slide I-IV | 2 / 12 / 28 / 40 | 2 / 2 / 3 / 5 | Retreat 4m from an enemy, then one shot per gun at it; each D + 4 / 10 / 18 / 26. Requires at least 2m safe travel. | 3 / 5 / 8 / 10 | 18s |
| Momentum | 5 | 2 | A Slide or Rush moving at least 2m and landing any hit grants one charge, maximum three, lasting 12s. Each charge gives +2 ordinary-shot ACC. Grapple Shot and Final Salvo spend all charges. | - | - |
| Paired Handling I-III | 8 / 22 / 45 | 2 / 4 / 4 | Ordinary Twin Pistol damage +10 / 20 / 25% total. Uses the higher bonus of this perk and Matched Rhythm. | - | - |
| Grapple Shot I-III | 10 / 30 / 38 | 2 / 4 / 4 | Close up to 6m, stopping 1.5m from an enemy. One shot per gun, each D + 6 / 13 / 20, plus 4 / 5 / 6 power per Momentum spent. Can fire without moving when already close. | 4 / 6 / 8 | 24s |
| Changing Angles | 15 | 4 | A landed Crossfire attack after moving at least 2m gives the next Crossfire ability +5 ACC; one charge, 8s duration, 10s trigger cooldown. | - | - |
| Pistol Rush I-II | 18 / 32 | 3 / 3 | Dash to a freely selected point 2-6m away. Aimed 4m-wide travel line hits up to three enemies, one shot per gun per enemy, each floor(0.60 x D) + 6 / 12. | 8 / 10 | 30s |
| Crossfire Stance | 20 | 4 | +10% Haste, +10% Movement Speed, Momentum lasts 18s; ordinary Twin shots deal 10% less damage. | 0 | 30s |
| Follow Through | 25 | 4 | A Momentum-spending ability landing any hit restores 1 STM per charge spent, maximum 3 STM; once per activation, 12s trigger cooldown. | - | - |
| Second Wind | 35 | 2 | A direct hostile attack missing its accuracy-versus-evasion roll grants one Momentum, at most once per 10s. Requires Momentum. | - | - |
| Final Salvo | 50 + mastery | 6 | Remain stationary and fire one shot per gun at one enemy, each D + 20. Spend all Momentum for +6 power per charge on each shot, maximum +18 per shot. | 15 | 90s |

### Effect details

- **Alternating Fire:** either hand can advance the three-hit counter; misses do not reset it. On the third hit, reset the counter and restore 1 STM if the cooldown is ready. Hits during the cooldown cannot bank another refund. Casts, queued replacements, procs, reflected hits, and duplicate observers do not count. Maximum recovery is 10 STM per minute. Pair removal clears the counter but never resets the cooldown.
- **Matched Rhythm / Paired Handling:** distinct three-rank perks belonging to Barrage and Crossfire respectively. Each line purchases its own ranks. Use the larger ordinary-damage bonus, never their sum; declare a common exclusive modifier group through stat metadata rather than hardcoded perk checks. Affect ordinary shots only, including limited-haste rolls. Exclude queued replacements, activated abilities, reflected damage, DoTs, and proc damage. Converging Fire adds five percentage points for +30% total; Twin Suns adds another 25 points during its six-attempt window. These modifiers add rather than multiplying one another. The overlap gives hybrid builds an investment choice; buying every perk is not required to access a line's later perks.
- **Blaster Slide:** move directly away from the selected enemy, preferably 4m, stopping at the last valid reachable point if it still permits at least 2m of travel. Target must remain legal and in range after arrival. Both shots use the same captured enemy; no damage or charge is earned from merely playing the slide animation.
- **Balanced Grip:** a flat five ACC, not five percentage points of hit chance. It enters only the companion's weapon profile once.
- **Controlled Burst / Pistol Rush:** Controlled Burst has two impacts at one target and one successful-hit refund per cast. Rush has at most three distinct targets and six impacts, with no missing-target redistribution. Capture its aimed line, actual destination and both weapon profiles once. Every Rush rank needs its own valid aimed-line spell row. Refunds cannot exceed STM actually paid.
- **Stances:** existing activation timing, exclusivity and global Haste cap apply. Akimbo applies an ordinary-shot multiplier of 0.85; Crossfire applies 0.90. Apply after additive bonuses, including Twin Suns. Activated damage is unchanged. Invalidating the pair ends both benefits and penalties. Crossfire extends only newly earned/refreshed Momentum to 18s; leaving caps remaining time at 12s without refreshing it. Switching stance cannot create charges or repeatedly extend a saved stack. Akimbo may be disabled when external Haste already supplies its speed benefit.
- **Akimbo ramp:** start at +10% Haste with zero stacks. Either hand's landed ordinary shot against the current hostile target can grant one stack, at most once per two seconds per attacker. Each stack adds two percentage points, capped at five for +20% total stance Haste. Every qualifying landed ordinary shot refreshes the six-second expiry, including hits during the stack cooldown or at five stacks. Misses neither add stacks nor refresh expiry. Abilities, queued replacements, proc damage and duplicate presentation callbacks cannot grant or refresh stacks. Apply gains after the triggering shot without retroactively changing its captured cycle budget. Target change, expiry, combat ending, death, leaving stance or invalid pair state clears all stacks; these transitions never reset the two-second grant cooldown. Maintain five while qualifying fire continues; reaching five does not consume or automatically clear the ramp. Model the bonus through declarative ordinary-hit, target, stack and expiry metadata read by the shared stat system, separately from Crossfire Momentum.
- **Converging Fire:** one tracked target per attacker. Three landed ordinary shots establish the bonus, starting after the triggering shot. Misses do not reset progress, but changing targets, six seconds without a landed ordinary shot, or invalidating the pair clears it. Subsequent landed ordinary shots refresh, never stack. It does not reward switching between area impacts.
- **Cool Under Fire:** requires positive direct damage from a hostile source; excludes self-damage, friendly damage, periodic damage, and reflected damage. It does not stack charges. Consume only when an eligible activation successfully pays its cost; failed validation does not spend it. Pair removal clears its pending discount but does not reset its trigger cooldown.
- **Momentum:** the perk must be owned to earn charges. Slide/Rush must achieve at least 2m actual movement and one landed hostile hit; one charge maximum per activation, regardless of hands or targets. Empty-space dashes, auto-attacks, procs, friendly targets and cosmetic callbacks do not count. Maximum three; each grants +2 ACC to ordinary Twin shots, not activated abilities. New qualifying gains refresh all live charges to 12s or 18s with Crossfire Stance. Clear on expiry, combat ending, death or invalid pair state. Stance toggling never extends charges by itself.
- **Grapple Shot / Final Salvo:** can activate at zero Momentum. Capture and consume every live charge once when a valid activation pays its cost; subsequent misses or target loss do not refund charges. Add rank-specific power per spent charge to each gun's initial attack power, not as extra impacts. Neither generates Momentum. Grapple stops 1.5m from its selected enemy, and may fire stationary when already close. Final Salvo is stationary at normal 25m range and has a two-second activation.
- **Changing Angles:** a Crossfire attack must actually move at least 2m and land a hostile hit. One charge affects one later Crossfire-line activation, including all its captured impacts. The source activation cannot consume its own reward. Stationary Grapple/Final Salvo cannot generate it. Invalid pair state clears the charge without resetting the ten-second trigger cooldown.
- **Follow Through:** only Grapple Shot and Final Salvo can qualify. Read the activation's captured spent charges and successful-hit summary; restore one STM per charge, at most three and no more than STM paid. Once per activation and once per twelve seconds. It never restores Momentum and misses grant no refund.
- **Second Wind:** bonus fuel when a direct hostile attack fails its accuracy-versus-evasion roll, once per ten seconds. The native hook currently combines misses, deflections and concealment in `TrackAvoidedAttack`; publish the specific miss cause through generic attack/stat metadata rather than subscribing to that unfiltered notification. Deflection, concealment, Guard, invalid/out-of-range attempts, self/friendly attacks, periodic effects and secondary procs do not qualify. Requires Momentum ownership and respects its cap/expiry. Equipment swaps cannot reset its cooldown. Slide/Rush remain the reliable charge sources when no attack is evaded.
- **Twin Suns:** resolve the initial two impacts before granting the six-shot benefit. Grant after a valid completed cast even if avoided. Each ordinary attempted roll consumes one charge, including misses and deflections. Do not count pairs as one attack, refresh charges from criticals, or grant extra Haste, critical rate, or no-delay attacks. Pair removal clears it.
- **Final Salvo:** at three charges, each shot has D+38 total power. Its two impacts are the complete attack; no additional free casts, cooldown resets, Haste, critical chance or follow-up shot buff. Uses the same global Capstone cooldown as Twin Suns and existing finishers.

### Mastery quest

Use existing weapon mastery quest conventions and the conversation system. Barrage's mastery tests sustained two-hand attacks against one enemy; Crossfire's tests actual repositioning, earned Momentum and a successful spender. Credit only valid movement and hostile hits, not an empty dash or equipment at enemy death. Each path unlocks its own rank-50 capstone for 6 SP, with no free perk rank or extra damage item. Completing one does not unlock the other automatically.

Select the trainer, encounter, objective count, and any new placement after inspecting existing Pistol mastery content. Those content details do not block the combat prototype. Authored dialogue belongs in conversation JSON; any added NPC follows the NPC HP normalization and visual-verification rules.

## Proc and resource accounting

Represent ordinary attacks, activated impacts, queued replacements, and secondary procs explicitly in the action context. This gives shared rules a reliable way to decide whether an event counts toward a trait.

- Ordinary shots independently use their own weapon's on-hit properties. Shared internal cooldowns, healing caps, and resource limits remain per attacker where they are today.
- An activation has one identity across all targets and hands. New Twin multi-shot abilities dispatch at most one generic weapon on-hit proc per target per activation, using the first eligible landed hand's properties. If the main hand misses, a landed companion may qualify. Do not reroll a failed proc on the next hand. New Twin abilities notify generic critical-trigger effects once per activation if any impact critically hits; individual impacts still calculate their own critical damage. Once-per-ability refunds consume the activation identity once. These caps apply to new Twin abilities and do not rewrite existing Pistol abilities.
- Secondary damage and triggered attacks never recursively schedule a companion shot.
- Native and simulated damage paths must not notify the same hit twice. Ammo on-hit forwarding retains the launcher's captured identity.
- Skill-scoped effects match the captured skill. Generic ranged effects may apply to both Pistol and Twin Pistol; unrelated skill selectors do not change.
- Weapon skill, accuracy, and enhancement properties are not also added to creature-wide totals. Limit any necessary shared-code changes to the context and calculations needed for the new attack path.

## Balance targets and checks

### Current equipment baseline

The baseline below uses current `Combat` and `WeaponDamage` formulas at equal Attack/Defense 100, equal damage attributes 26, a fixed 10% critical chance, StandardCriticalRating 2, DMG 22 and Delay 250 per item. It excludes misses, mitigation, other item bonuses, and perk-line bonuses. A single weapon rounds its adjusted item DMG up once; the damage formula rounds its final range endpoints down.

| Equipment | Item DMG per shot | Cycle interval | Mean damage per second |
|---|---:|---:|---:|
| Single pistol with natural +20% | 27 | 2.416s | 9.62 |
| Single pistol with Doublehand III, +60% total | 36 | 2.416s | 13.04 |
| Pair without Dual Wield | 22 each | 4.833s for two shots | 7.90 |
| Pair with Dual Wield III | 22 each | 3.583s for two shots | 10.66 |

These are arithmetic baselines, not full-build measurements. They show why granting a free full-power companion hit at single-pistol cadence is inappropriate and why the slower paired cycle needs specialist damage bonuses. They do not establish full-build balance.

### Comparable builds

Compare Twin Pistol 50 with either full 60-SP line plus Dual Wield III against Pistol 50 with its full 60-SP critical line plus Doublehand III: 69 SP each, equal Armor investment, attributes, equipment tier, and external buffs. Include the pistol/shield defensive alternative and existing Skirmisher, Rifle, and melee paired-weapon builds. Test equal-tier gear first, then repeat at equal acquisition/upgrade cost because the pair provides another item and enhancement capacity.

Run hybrids at equal total SP and skill-rank investment. A character buying both Pistol and Twin Pistol lines is not comparable to a 69-SP single-line build. Include generic ranged/critical effects, Armor traits, buffs from allies, weapon enhancements, suppression, healing-on-hit, and limited haste/no-delay effects. Do not assume Pistol-specific traits apply merely because the models are pistols.

### Sustained ceiling budget analysis

Run `node tools/AnalyzeTwinPistolBalance.mjs` from the repository root. The script checks the delay/damage baseline above, then estimates expected damage and finite STM budgets. Akimbo uses its full five-stack Haste ceiling in this calculation: ramp startup and resets are excluded. These are ceiling estimates for the ramp design, not measured cold-start encounters or execution of the C# combat engine.

The representative 120-second case uses DMG 22 / Delay 250 guns, equal damage attributes and Attack/Defense, 85% hit chance, MGT 26, +12 maximum STM, +5 STM regeneration per 30 seconds, and no external Haste. Maximum STM is 61; natural recovery is 42 STM/minute. Abilities spend from that pool with a four-STM reserve; unfunded scheduled opportunities are skipped. Each line uses its three regular attacks at 18/24/30-second cooldowns. All builds include the appropriate stance. The Pistol comparator includes Gambler, average Lucky Chamber contribution, Rapid Shot, approximate Reload Tempo uptime, High Noon for 30% of the encounter, and a 30% effective ability critical chance before Quick Draw/Fan bonuses. Pistol-specific ability critical assumptions are varied separately below. Barrage's ordinary-hit recovery realizes 75% of its ceiling; Cool Under Fire contributes no refunds in this comparison.

| Build | SP including Armor | Single-target DPS | Three-enemy total DPS | Funded casts, solo / group |
|---|---:|---:|---:|---|
| Existing Pistol critical line | 69 | 22.05 | 25.18 | 11 / 11 |
| Barrage | 69 | 22.71 | 22.71 | 14 / 14 |
| Crossfire | 69 | 19.95 | 21.79 | 14 / 14 |
| Both complete Twin lines | 129 | 22.71 | 26.20 | 14 / 16 |

At its five-stack ceiling, Barrage reaches 103% of the Pistol reference's single-target output. Crossfire gives up 12% against that Barrage ceiling solo and 4% in three-enemy aggregate output, buying movement instead of the old area-damage specialization. Ramp startup and resets can reduce Barrage output below this ceiling. The 129-SP row uses Barrage's stance/sustain with Crossfire attacks against groups; it is not an equal-SP comparison. A hybrid may skip one redundant handling perk for 119 SP with the same modeled output. The lines remain independently priced and owned.

With this pair and no external Haste, Akimbo starts at a 2.966s cycle and reaches 2.348s at five stacks, versus 3.583s with no stance. That is about 21% more shots initially and 53% more at the ceiling, before the 15% ordinary-damage penalty. Five stacks take at least eight seconds from the first qualifying hit because of the two-second grant limit; misses and actual paired-shot timing may lengthen this. The peak budget is redistributed rather than increased. Extra critical/on-hit opportunities still need hybrid tests. No independent delay-reduction perks are added. The ceiling model selects Akimbo on/off for better ordinary output; cold-start stance choices need separate validation.

Sensitivity results matter more than a single favorable row:

| Sweep | Cases | Barrage / Pistol single-target | Crossfire / Pistol three-target |
|---|---:|---|---|
| Hit 65/85/95%, external Haste 0/15/30/50%, reference ability critical 22/30/37% | 36 | 99-116% | 80-96% |
| Hit 65/85/95%, Attack/Defense 0.5/1/2/3.625, damage-formula stat delta -7/0/+7, low/geared/supported STM | 108 | 82-118% | 65-103% |

Crossfire solo is 78-89% of Barrage throughout the first sweep. The wider sweep varies offensive and defensive conditions independently; low resources and unfavorable damage-stat matchups remain weak points. Supported STM adds 2 STM every four seconds. Barrage's 116-118% upper cases need investigation with actual animation lockout and optimized Pistol rotations before release. These ranges do not establish universal +/-5% equivalence.

The model tracks expected Slide/Rush charge generation, three-charge caps, 12/18-second expiry, Grapple spending and Follow Through refunds. Movement casts are spaced at least two seconds apart and are assumed legal. All builds still fire ordinary shots without interruption. Fractional charges approximate hit uncertainty, not sampled gameplay. Akimbo ramp startup/resets, Second Wind, Momentum's ordinary ACC, Changing Angles, capstones, actual travel/animation lockout, collision/roots, Guard/deflection, resistances, enhancement procs, Balanced Grip and cross-trained Pistol purchases are excluded. Rotations are fixed rather than optimized. Mobility's defensive value and its lost firing time are not quantified; both need engine comparisons.

The capstones have explicit bounds pending engine tests: Twin Suns adds two D+20 impacts and at most six +25% ordinary-damage attempts, still subject to Akimbo's penalty. Final Salvo adds two D+20 impacts plus at most 18 power per shot from three spent Momentum. Neither grants extra attacks, Haste, critical chance, or cooldown resets. Both compete with existing finishers through global Capstone recast. Earlier results for the area-focused Crossfire design are superseded by this budget pass.

### Existing-ability interactions

| Existing mechanic | Proposed interaction and tradeoff |
|---|---|
| Armor Dual Wield | Existing companion delay reduction; no second stacking delay-reduction perk. |
| Single Weapon / Doublehand | Disabled by the occupied off hand; neither gun receives their damage bonus. |
| Quick Draw | Optional extra basic attack after buying Pistol ranks/perks; main hand only, own STM cost. |
| Double Shot / Fan the Hammer | Share cooldowns with the corresponding Twin attacks, preventing duplicate burst rotations. |
| Dead Man's Hand and other capstones | Global Capstone cooldown; no back-to-back finisher stacking. |
| Gambler and other stances | Mutually exclusive with the two Twin stances. |
| Lucky Chamber / Rapid Shot / Reload Tempo / High Noon | Existing unrestricted triggers can work if purchased. Ordinary shots are actual rolls; new ability critical events are capped per activation. Extra Haste still respects the global cap. |
| Deadeye Reload and other Pistol selectors | Retain Pistol scope; Twin abilities do not gain those benefits by weapon appearance. |
| Suppression, debuffs and healing support | Normal shared rules; Crossfire adds no Evasion shred or crowd control and leaves control-focused weapon roles intact. |
| Single Pistol Skirmisher | Keeps its passive evasion/deflection/control identity. Crossfire's main benefits require actual relocation; Second Wind is optional bonus fuel. |
| Roots, collision and terrain | Block relocation normally. No automatic cleanse or teleport through a blocked path. Final Salvo and already-close Grapple still offer stationary attacks. |

Cross-training can therefore add options, but its extra SP, skill ranks, and trigger interactions must be priced in matched hybrid tests. No claim is made that every possible 410-SP build has been validated by this proposal.

### Release criteria

- At rank 50, target Barrage at 95-105% of the equally invested single-pistol critical build's sustained single-target damage in representative 120-second encounters. Include STM depletion, critical/haste traits, stance risk, and meaningful target movement. Investigate outliers separately rather than claiming parity in every matchup.
- Target Crossfire's uninterrupted solo damage around 80-90% of Barrage, accepting lower aggregate damage than area specialists to pay for movement. Include encounters where repositioning avoids danger or preserves target access, and measure damage uptime, distance gained, survival and resource use as well as stationary DPS. Never use missing target slots to duplicate Rush hits.
- Compare 6-second and 12-second openings with capstones and cross-training. Twin Pistol's single-target maximum burst should not exceed the matched single-pistol build by more than 10%; its sustained role does not justify a larger opening kill window. Check Crossfire's multi-target burst separately.
- Test ranks 0, 10, 25, and 50 using weapons actually available at each rank. No inaccessible rank-zero training path or mandatory Pistol investment is acceptable. Investigate Barrage damage gaps outside 90-110% against a comparably invested offensive pistol build; apply Crossfire's explicit solo tradeoff rather than that same target to its solo output.
- Check one, three, and excess area targets; high defense; high evasion; ranged/shield deflection; immunity; Guard; moving targets; PvP; and unequal weapon tiers/delays. No extra target, proc, or refund may be created by animation batching.
- Sweep ordinary Haste/Slow from -50% to +50%, then limited effects at and near the animation floor. Observe actual roll counts and damage, not just displayed delay.
- Test Akimbo from zero stacks, every intermediate stack and maintained five stacks. Verify the two-second per-attacker grant limit, six-second expiry refreshed by ordinary landed shots at the cap, misses, target changes, interruption, death and stance/equipment swaps. Abilities and queued/proc impacts cannot accelerate the ramp. Include 6/12-second openings and repeated short targets; use full-ramp calculations only as upper bounds.
- Sustain should have a resource cost. Alternating Fire, Controlled Burst, Cool Under Fire and Follow Through must respect their caps and cannot create indefinite refund loops. Momentum never refunds itself. Test zero/one/three charges, two independent misses, blocked/empty dashes, expiry during a cast, stance toggles, accuracy/evasion misses versus other avoidance causes, and equipment swaps. Test both Twin lines together and equal-SP Pistol/Twin hybrids with unrestricted critical triggers.

Use fixed-seed simulations and enough samples to report uncertainty, then verify representative rotations in engine. Report actual gear, hit rates, resources, DPS, burst, target assumptions and movement opportunities. Tune Matched Rhythm / Paired Handling and stance values for ordinary output, then ability power, spent-charge power and resource costs. Reprice movement utility within Crossfire rather than raising its damage to match a stationary specialist. Do not compensate by changing existing Pistol perks or other weapon systems.

## Implementation sequence

### 1 Establish the engine prototype

Create one new companion resource and one repeatable paired attack fixture. Confirm model attachment, native ranged classification, main-hand ammunition, companion damage attribution, independent misses/criticals/deflection, and exactly two scheduled ordinary rolls at normal cadence.

Prototype the left-hand fire pose, muzzle/projectile origin, hit/miss presentation, and sound with an observing client. Current canonical pistols use native sling behavior; the former empty-offhand firing remap uses both hands. Do not assume it works with a companion or that increasing native `ResolveAttack` count produces a valid off-hand ranged attack.

If a custom attachment or projectile presentation is required, implement and verify it here. Treat left-hand presentation as a feasibility dependency: do not generate all tiers or promise full visual support before this fixture passes. Use a new base-item ID determined from the current HAK data; no reserved numeric ID is assigned by this plan.

Prototype free-ground Rush and enemy-relative Slide/Grapple before producing progression assets. Confirm validated travel, obstacle/root behavior, actual arrival, shooting after movement, cancellation and two-second movement lockout. Existing jump abilities establish precedent, not proof that a freely selected destination is safe.

### 2 Add the skill and validated weapon context

Add Twin Pistol metadata, missing-skill initialization, skill/requirement labels, contextual equip requirements, both-hand profiles, ranged/weapon classification, and character-sheet support. Keep `GetSkillTypeByBaseItem` compatible; pass a resolved context or the existing skill override where the caller needs equipped behavior. Audit native attack hooks as well as managed services so the main hand cannot accidentally use Pistol rank.

Integrate the simulated hand with the scheduler, damage rules, XP accounting, ammo, effects, pending-action cancellation, and limited-roll budgets. Model perk-driven behavior as attributed stats and declarative ability/impact metadata. New helpers named during implementation must be introduced explicitly rather than assumed to exist.

### 3 Author progression and presentation

The Twin Pistol Bible sheet and workbook layout/audit registration contain all 36 proposed ranks, eighteen per 60-SP line. At implementation time, register the tab with the weapon generators and implement the exclusive handling-bonus group. Include costs, timings, attributes, target shapes, descriptions, and mastery requirements. Preserve cached formulas and unrelated ZIP entries through targeted XML edits, then run the required refresh.

Generate the perk definitions and one class/file per distinct active ability. Add all needed feat, spell, effect-icon, skill/item-property labels, status effects, and stat definitions. Choose available IDs from the current source data. New custom TLK entries use the first gaps; custom references use the required offset. Recast labels must fit the existing 14-character limit.

Create tiered companion blueprints, crafting recipes, starter acquisition, upgrades, icons, and appearance registrations. Retired off-hand pistols remain unchanged and disabled. No conversion of existing Pistol weapons or broad character-build migration is needed. Add the mastery quest and its dialogue after the mechanics work.

Choose VFX from the repository's effect-selection reference. Author paired attack and ability clips compatible with both weapons, interruption, and supported character models. Regenerate cooldown icons and run the gameplay icon audit. Player-facing combat feedback and UI must use PlayerName when displaying characters.

### 4 Validate and prepare delivery

Run the checks below, tune both lines within their budgets, and update the Bible and generated outputs together. Present actual NWN evidence of both weapons and shots. The complete feature requires a parent server/module PR and, when the submodule pointer changes, a pushed HAK companion PR linked in both directions. Resolve review threads on both before final handoff.

Deploy the matching C# server build, rebuilt HAK/TLK resources, and repacked module together. Repack/rebuild/deploy requirements are deployment steps, not reasons to alter gameplay code.

## Integration map

| Existing location | Required work |
|---|---|
| `Service/SkillService/SkillType.cs`, `Service/Skill.Mapping.cs`, `Service/Skill.cs` | Skill metadata, context-sensitive attack selection, initialization and progression |
| `Feature/EquipmentRestrictions.cs`, `Feature/PistolBaseItemCompatibility.cs`, `Service/Item.cs` | Prospective pair validation, tier requirements, compatibility, item classification and attributes |
| `Service/PerkService/EquipmentPredicates.cs`, `Service/Stat.cs` | Active-pair predicate, Armor Dual Wield eligibility, per-hand stats and overrides |
| `Native/OnAIActionAttackObject.cs`, `Native/WeaponAttackCycle.cs`, `Native/ResolveAttackRoll.cs`, `Native/GetDamageRoll.cs` | Paired ranged scheduling and explicit attack context; preserve existing melee/native behavior |
| `Service/Combat.cs`, `Service/CombatService/WeaponDamage.cs` | Shared timing, shot calculations, skill/proc accounting and Single Weapon exclusion |
| `Feature/PistolAnimationRemap.cs`, `Native/WeaponAttackAnimation.cs`, `design/animations/` | Paired ranged presentation without regressing empty-offhand or shielded pistols |
| `Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs`, perk/ability/status definitions | Hand-specific ability profiles, traits, activation summaries and shared recasts |
| Character sheet, equipment examination, skills/perks UI, player guide | Twin Pistol rank, both weapons' values, requirements, costs, and shared cooldown descriptions |
| `tools/GenerateWeaponArchetypeImplementation.py`, Bible and audit/layout inputs | New source sheet, reproducible generated output and balance data |
| `Module/uti`, recipes, starter acquisition, quests and `ConversationData` | Obtainable companions and mastery progression |
| `SWLOR_Haks` and `SWLOR.NWN.API` enums | New base item, skill labels, feats/spells/TLK, icons, models and animations |

Paths in this table are relative to `SWLOR.Game.Server` unless they explicitly name another repository directory. Add new types in matching domain folders. This map identifies integration boundaries; it does not authorize unrelated refactoring.

## Verification

### Logic and content

Add focused tests for the new skill, rank-zero access, equip transitions, per-hand profiles, conditional stats, ability metadata, costs, targets, cooldown sharing, XP attribution, and balance comparisons. Extend existing regression coverage where the feature changes shared behavior:

- `SkillMappingTests`, `CharacterRebuildSkillAvailabilityTests`, `EquipmentRestrictionsTests`, `AttackWeaponClassificationTests`.
- `CombatAttackDelayTests`, `PairedWeaponTimingTests`, `SingleWeaponDamageTests`, `WeaponLoadoutBalanceTests`, `WeaponDeflectionTests`.
- `AreaAbilityTargetingTests`, `StaminaSustainTests`, `CrossSkillPerkInteractionSafetyTests`, `SkillSelectorStackingTests`, `CombatReleaseBalanceAuditTests`.
- `PistolAnimationRemapTests` plus the relevant economy, acquisition, generator, and icon audits.

Build once with `-p:RunPostBuildEvent=Never`, then run relevant tests with `--no-build`. The final implementation changes shared combat infrastructure and adds enum/generator output, so a final full server test pass is justified after focused failures are resolved. Check submodule initialization before diagnosing asset tests; review test-generated tracked diffs before committing.

### Engine and persistence

Add a Twin Pistol engine filter for normal and hasted attacks, both independent misses, criticals, distinct weapon properties, deflection, Guard, immunity, ammunition, once-only effects, queued ability consumption, and all interruption boundaries. Include zero/one/multiple finite ammunition cases and unlimited stacks.

Run existing `DualWield`, `PistolPresentation`, and `SingleWeapon` engine coverage. Verify empty-offhand pistols, pistol/shield, and ordinary melee pairs remain correct. Check weapon swaps before the companion launches and after a projectile launches, target changes, area transitions, death, and disconnect cleanup.

Equip, attack, upgrade/change state, save, and reload through normal gameplay at least twice. Assert ownership, stable item identities, rank requirements, equipment state, ammo quantities, and absence of duplicate items. Include a Twin Pistol character with Pistol rank zero. Any later migration must start with legacy fixtures and run the actual conversion first.

Player-controlled combat droids need an explicit companion-equipment compatibility check before release. If the item is supported for droids, include skill/module and AI-slot behavior and run `DroidInventory` lifecycle coverage; if their current equipment system cannot support it, reject the companion clearly until that integration is complete rather than allowing a partially functional item.

### Client evidence

Follow `InEngineVisualVerification.md`: run the owned NWN client on the isolated private desktop, set every volume channel to zero, capture its real rendered output, and stop it afterward. Do not move the user's focus or desktop.

Capture both hands firing, independent hit/miss presentation, both capstones, Slide, Grapple, freely aimed Rush, maximum haste, equipment transitions, and another client's observation. Include blocked paths, roots, empty destinations, narrow doors and attempted terrain shortcuts. Check representative male/female and supported body/appearance variants, weapon alignment, muzzle origin, recoil, ready poses, impact timing, and duplicate/missing sounds. Record any unsupported appearance explicitly. Any new trainer or quest placement also needs evidence from a public approach with normal collision.

## Repository references

The starting mechanics and baseline calculations are based on commit `62364f27c`:

- [Dual wield combat](DualWieldCombat.md) and [weapon loadout balance tests](../../SWLOR.Game.Server.Tests/Service/WeaponLoadoutBalanceTests.cs).
- [Pistol perks](../Feature/PerkDefinition/PistolPerkDefinition.cs) and [Armor traits](../Feature/PerkDefinition/ArmorPerkDefinition.cs).
- [Budget analysis script](../../tools/AnalyzeTwinPistolBalance.mjs), [Pistol ability definitions](../Feature/AbilityDefinition/Pistol), [combat formulas](../Service/Combat.cs), and [weapon damage](../Service/CombatService/WeaponDamage.cs).
- [Equipment restrictions](../Feature/EquipmentRestrictions.cs), [pistol compatibility](../Feature/PistolBaseItemCompatibility.cs), and [pistol animation remap](../Feature/PistolAnimationRemap.cs).
- [Ability targeting](AbilityTargeting.md), [Design Bible workbook rules](DesignBibleWorkbookRules.md), [economy restrictions](EconomyRestrictedItems.md), [icon standards](IconStandards.md), [VFX selection](VisualEffectSelection.md), and [player identity](PlayerIdentity.md).
- [Engine testing](EngineTesting.md) and [in-engine visual verification](InEngineVisualVerification.md).
