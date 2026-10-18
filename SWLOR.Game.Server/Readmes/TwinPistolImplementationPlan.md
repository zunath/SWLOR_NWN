# Twin Pistol Implementation Plan

## Design

Twin Pistol is a separate combat skill for fighting with one owned Twin Pistols item. Equipping it automatically equips a temporary simulated companion pistol in the left hand. Removing either gun unequips the owned pair and destroys the companion. **Barrage** rewards rapid sustained pressure on one enemy. **Crossfire** uses deliberate repositioning to build Flow for stronger attacks and a stationary finisher. It accepts lower uninterrupted damage in exchange for movement. Single Pistol keeps its harder individual hits, critical-hit specialization, passive mobility/control choices, and shield option.

Twin Pistol has ranks 0 through 50, contributes to the existing 400-rank skill cap, and uses the Weapon combat-point category. It does not require Pistol ranks. Barrage and Crossfire each contain ten perks with eighteen purchasable ranks costing 60 SP. The handling perks are mutually exclusive purchases at every rank, so the maximum legal investment across both lines is 110 SP (119 SP with optional Armor Dual Wield). There is no shared perk line or shared purchase. Armor's existing Dual Wield ranks remain optional and cost another 9 SP. There are 20 distinct perks and 36 purchasable ranks across the full skill.

The skill name is **Twin Pistol** throughout player-facing text. Akimbo Stance belongs to Barrage; Crossfire Stance belongs to Crossfire. Add new identifiers and resource rows; do not reuse retired skill, perk, base-item, or TLK definitions.

The approved design is implemented on the feature branch and has not been deployed. The cross-skill revision, handling choice and Paired Volley changes supersede the earlier balance results. The companion [Discord feedback draft](TwinPistolDiscordFeedback.md) presents the same values for discussion; the validation record below distinguishes implemented behavior from release balance.

## Historical implementation validation

The execution results below predate the latest master reconciliation. Current simulator and native results, their exact source revisions, and remaining checks are recorded in [the Twin Pistol pull request](https://github.com/zunath/SWLOR_NWN/pull/2613). These earlier measurements do not establish balance against a later master revision.

The single-item equipment contract supersedes the earlier independently owned companion design. The earlier validation below is historical evidence for unchanged attack/perk behavior; equipment ownership, automatic equip/cleanup and repeated save/reload require coverage of the revised pair lifecycle. Earlier screenshots showing the resource name Momentum predate its player-facing rename to Flow and do not verify the current label.

The server build passes with deployment disabled. The initial full unit run passed 5,005 of 5,082 tests. Feature integration failures were repaired and verified with targeted reruns: 223 integration checks, 92 related equipment/animation/Force/gathering/store checks, 112 casting/stamina regressions, 11 guide/icon checks and 32 Bible/recipe/ability/stamina checks. These historical filters overlap; they are not a combined unique-test count. After the final numerical tuning, all 69 focused perk/state/stamina/cross-skill/Bible/targeting checks passed. The full suite has not been repeated. Eight failures still reproduce in unchanged content or tests: old apartment jukebox/placeable data, helmet assignments, double-weapon expectations, a droid serialization source assertion, Concussive Toss target-budget parsing and old `sw_openarc` walkmesh use-node names. Those failures are outside this feature. No baseline execution is claimed; the affected source/data were compared with the base branch.

All 22 active ability ranks passed native behavior coverage, including actual movement, immediate movement lockout and longer recast rejection. Other native engine checks verified cross-skill saber/rifle damage penalties and Haste, ten weapon loadouts, queued ability replacement, equipment recovery when inventory is full, movement/root/collision rules, actual Canyon Range mastery objectives and repeated save/reload. The existing DualWield, SingleWeapon and PistolPresentation cases passed. Canonical and converted legacy pistols passed real-player finite ammunition cases at 0/1/2 rounds and hasted 2/4/5 rounds. Launched shots retain their original damage and ammunition through stop, ammunition swaps and weapon swaps. Both capstone weapon-swap cancellations and a stale-capture/unrelated-cast control passed. Invalid earlier benchmark runs are excluded from balance evidence.

Real NWN client captures show paired ready/hit/miss presentation, representative bodies and robes, neutral cross-skill gestures, status icons and both mastery-record icons. The six HAK integrity guards and full gameplay icon audit (1,867 entries) pass. Verification used silent owned clients on an isolated private desktop; every owned client was stopped and its isolated profile removed. A second connected observer has not verified dynamic muzzle-anchor replication: the required zero-capability client isolation blocks local-server networking. No network exception or OS networking change was applied. Keep both PRs in draft pending that check. The controlled comparisons below also leave broader builds, incoming pressure and PvP for playtesting.

## Skill and equipment progression

### Training from rank zero

Provide an obtainable rank-zero Basic Twin Pistols item so ordinary attacks can train Twin Pistol before its first ability unlocks at rank 2. There is no skill-rank-2 equip gate and no mandatory training perk. Mirror the existing pistol tiers, per-gun damage types, delays, upgrade-slot limits, and crafting progression.

Add pair recipes to Smithery and expose Basic Twin Pistols through the seven existing general merchants that carry Basic Pistols. One crafted item represents both guns, so its materials and base price are twice the matching pistol's. Crafting level, unlock requirements, research modifier and one enhancement-slot budget match that pistol. Retain independent companion recipe identities but deactivate them and hide their category. Remove their store stock and mark their blueprints, along with the internal temporary companion template, NO_ECONOMY.

Only the main-hand pair item is owned, crafted, upgraded, sold or traded. It stores the shared damage, accuracy, delay, damage type and upgrades, along with the owner's appearance. The generated companion copies supported weapon-profile properties from that owner and retains its verified companion carrier appearance (parts 11/201/11); pistol and companion model prefixes differ, so arbitrary appearance IDs are not copied. Native and creature-wide equipment bonuses remain on the owner and apply once. The companion has no independent enhancement capacity or value.

### Requirements and equip order

A Twin Pistols item has the persistent TWIN_PISTOL_PAIR=1 marker and directly requires Twin Pistol at the matching pistol tier's rank. Other restrictions still apply. Ordinary pistols retain their Pistol requirements and never create a companion. The internal twp_companion template has TWIN_PISTOL_SPARE=1 and NO_ECONOMY=1; it is generated and linked to the owned pair only while equipped.

Validation must use the prospective equipment arrangement, not just the arrangement before an equip event:

- Equipping the owned pair in the right hand automatically creates and equips exactly one linked temporary companion in the left hand. It works with Twin Pistol ranks and Pistol rank zero.
- Removing either hand cancels unlaunched shots, unequips the owned pair and destroys the temporary companion. The owner remains a single inventory item; no spare is retained in inventory, dropped, sold or transferred. Existing occupied-hand replacement rules remain: remove the pair before equipping another weapon or shield.
- A shield or another off-hand item cannot coexist with an active pair. Failed companion creation/equip safely rolls back the whole pair rather than leaving a usable single gun.
- An unlinked companion, retired independent companion, ordinary pistol, rifle or melee weapon cannot activate a pair.
- Login and equipment restoration discard saved temporary companions and reconstruct only a valid equipped pair. Item loss and failed generation clean up the pair. Save/reload preserves the owner without duplicate or orphan spares.

The temporary companion must not leak native equipped bonuses. Copy only supported properties read from a validated weapon profile; native and creature-wide effects remain on the owner. Pair linkage must be validated before either gun can attack or supply equipment benefits. Do not add a hidden Pistol prerequisite.

Register the pair's removal guard with the shared EquipmentUnequip dispatcher during module-cache initialization. The common pre-unequip bridge supports multiple equipment owners and reports whether a guard handled the operation, so whole-pair removal skips the ordinary single-item path and removes the owner's equipment stats exactly once. Twin Pistols and other paired equipment use the same bridge.

### Skill selection

Add a context-aware skill resolver for ordinary weapon attacks. Both hands of a valid pair use Twin Pistol for attack, accuracy, critical skill contribution, damage rank limits, timing effects, character-sheet estimates, and combat progression. A lone ordinary pistol still resolves to Pistol. Preserve the base-item-only lookup for callers that actually need item classification.

Use the current pistol attributes: Perception for damage and Agility for accuracy, preserving supported item/stat overrides. Add the companion to the appropriate weapon classification and attribute mappings without classifying it as a shield or a native melee attacker.

Ordinary paired attacks contribute to Twin Pistol only. Route their contribution through the existing combat XP accounting, enemy eligibility, and award caps; do not grant a second full XP award for the simulated hand. An explicitly Pistol-scoped cross-trained ability retains its Pistol skill and progression behavior. Capture that identity when the action starts so changing equipment before impact cannot redirect XP.

Initialize the new skill through normal missing-skill handling for existing characters. Preserve SP/AP accounting and decay rules. Do not refund, convert, or reset existing Pistol ranks.

## Attack behavior

### Ordinary attacks

The native main-hand pistol remains the real ranged launcher. SWLOR schedules and resolves the companion shot. The companion's presentation carrier and its gameplay identity are separate: an attachment technique must not confer shield AC, Shield Deflection, native melee attacks, or attacks of opportunity.

Ordinary attacks retain the native main-hand projectile and add an explicit MIRV projectile only for the companion. Do not add a second explicit main-hand bolt to a native ordinary shot.

One paired cycle budgets one main-hand shot and one companion shot. Both weapon delays contribute to the cycle. Reuse the existing paired-delay calculation, including the single payment of the engine baseline, ordinary Haste/Slow behavior, fractional batching, and bounded attack budgets. Armor's Dual Wield stat reduces only the companion delay by 10/20/30 percent; do not add a second stacking Twin Pistol delay-reduction trait.

Resolve each shot from its own captured weapon profile and independently determine hit, critical, ranged deflection, and damage. The companion still fires if the main hand misses. Attribute damage, threat, kill credit, combat feedback, and on-hit effects to the original attacker and correct weapon.

The attack resolver must use the same rules as other ranged auto-attacks: concealment and other avoidance, valid targets, ranged deflection, Guard at the damage stage, defenses, immunity/resistance/reduction, and damage-triggered effects. Apply each adjustment once. Do not implement the companion as a hostile ability that bypasses auto-attack defenses or as an unqualified damage copy of the main-hand result.

Both shots use the normal pistol auto-attack range and line-of-fire rules. They share the owned pair's weapon profile and enhancements, but independently roll hits, criticals and deflection. A companion never inherits the main hand's successful hit result.

Any equipped companion disables Single Weapon and Doublehand, just as any occupied off hand does today. Existing weapon deflection remains subject to its normal rules; Shield Deflection is unavailable without a real shield. Inactive companions provide no weapon defenses.

### Captured actions and interruption

Keep an action/cycle identity, target ID, weapon IDs, validated skill, weapon profiles, ammunition source, pending hand, and consumed-roll budget. Do not hold native object pointers across delayed callbacks.

Before launching a pending shot, verify that the action is still current, both actors are valid, the weapons are unchanged, the target is in the same area and legal to attack, and range/line of fire remain valid. Stop, death, incapacitation, target changes, unequip, area transitions, and invalid action state cancel unlaunched shots. A missed main-hand shot is not an interruption.

Define the launch boundary explicitly. Once a shot has launched, retain its original target and profile and use normal ranged impact semantics; it cannot become a shot from replacement equipment. Removed targets safely discard the impact. Presentation updates and observers never cause damage or consume charges.

Consume finite ammunition once per launched logical shot. Reuse unlimited-ammunition behavior without duplicating generated stacks. If ammunition runs out between hands, cancel the unlaunched hand. Ensure the native launcher and simulated resolver cannot both debit or refund the same shot.

### Abilities and cross-training

All 36 Twin Pistol perk ranks support cross-skilling. Purchased perks have no blanket pistol/companion requirement. Active attacks capture one impact per eligible equipped attack weapon, maximum two: a single or two-handed weapon contributes one impact, and a shield contributes none. Each impact keeps its source weapon DMG/type. Ability scaling remains Twin Pistol with Perception for damage and Agility for accuracy, preserving supported item/stat overrides. Ordinary Twin Pistol attacks and training still require the valid pistol/companion pair. Capture the eligible weapon profiles once per activation. Barrage attacks and Final Salvo use a 25m range. Blaster Slide selects an enemy within 15m; Grapple Shot selects one within 7.5m and closes at most 6m; Pistol Rush selects a ground destination 2-6m away. These are ordinary activated weapon abilities using established action/impact timing, not free auto-attacks added by a hit callback.

Each ability pays STM and starts its cooldown once. Its impacts may critically hit independently, but activation-level bonuses and resource restoration run once according to the ability's result summary. Follow existing activated-ability avoidance rules; auto-attack deflection does not silently become a new rule for existing activated abilities.

With an active Twin Pistols pair, each captured main-hand and companion impact explicitly launches its own MIRV blaster bolt from that hand's muzzle position. Each bolt uses that hand's hit/miss result; a missed hand aims at a separate miss endpoint. Every projectile leases a distinct source anchor until its flight completes, so simultaneous hands and Pistol Rush targets cannot move another in-flight projectile's source. Presentation never resolves damage or consumes ammunition. Melee and other cross-skill loadouts receive their normal weapon presentation and never acquire gun bolts merely by using a Twin Pistol perk.

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

Capture origin, destination, targets, charge identities, and eligible weapon profiles. Resolve shooting only after confirmed arrival; a late failure cancels the impacts and generates no Flow. Validation failure before payment consumes nothing; once launched, use normal failed-impact cost/recast behavior without a second refund path. Presentation callbacks cannot move or damage twice. Any replacement for unsafe engine jumps must be implemented and tested explicitly.

Pistol Rush is freely ground-targeted and can reposition without enemies. Its aimed attack line follows the actual origin-to-destination segment, four meters wide, with at most three distinct hostile targets captured at activation. Travel without a landed hostile hit generates no Flow. Allow normal stationary attacks between movement abilities; Final Salvo never moves the caster.

## Perk progression

Each rank requires the listed Twin Pistol rank and the preceding rank of that perk. SP prices are per purchase. Ranked values replace earlier ranks. Every rank is cross-skill. Matched Rhythm and Paired Handling cannot be purchased together at any rank; this does not block buying later unrelated perks in either line. Both stance benefits and their ordinary-damage penalties apply with any weapon. Weapon swaps preserve buffs, counters, charge expiry and trigger cooldowns.

`D` means each impact source weapon's item DMG with its normal eligible adjustments. A valid pistol/companion pair receives neither Single Weapon nor Doublehand. Other cross-skill loadouts retain their normal eligibility; do not duplicate their modifiers. Add listed flat DMG, including spent Flow DMG, before the normal damage formula. Controlled Burst uses `floor(0.75 * D)`; Pistol Rush uses `floor(0.60 * D)`, separately per weapon. Percentage damage traits apply at the shared outgoing-damage adjustment stage. Stance penalties multiply the final ordinary-shot outgoing amount after additive bonuses; they never reduce activated ability DMG. Slash-separated values below follow perk-rank order.

### Barrage: 60 SP

All effects support eligible weapons from other skills. Active attacks produce one impact per equipped attack weapon, maximum two.

| Perk | Twin Pistol rank | SP per purchase | Effect | STM | Cooldown |
|---|---|---|---|---|---|
| Paired Volley I-IV | 2 / 12 / 28 / 40 | 2 / 2 / 3 / 5 | One enemy; each weapon D + 4 / 8 / 12 / 16. | 3 / 5 / 8 / 10 | 18s |
| Alternating Fire | 5 | 2 | Every three landed weapon auto-attacks restore 1 STM; at most once per 6s. | - | - |
| Matched Rhythm I-III | 8 / 22 / 45 | 2 / 4 / 4 | +3 / 6 / 8% Haste with any weapon. Cannot purchase Paired Handling. | - | - |
| Focused Burst I-III | 10 / 30 / 38 | 2 / 4 / 4 | One enemy; each weapon D + 8 / 16 / 24. | 4 / 8 / 10 | 24s |
| Balanced Grip | 15 | 4 | +5 ACC to any eligible off-hand attack weapon; shields receive none. | - | - |
| Controlled Burst I-II | 18 / 32 | 3 / 3 | One enemy; each weapon floor(0.75 x D) + 7 / 13. Restore 2 / 3 STM once if any impact lands. | 6 / 9 | 30s |
| Akimbo Stance | 20 | 4 | +10% Haste; hostile weapon auto-attack hits on one enemy ramp +2% per stack, max five (+20% stance total), one stack per 2s. Target change or 6s without a weapon auto-attack hit clears stacks. ALL weapon auto-attack damage x0.70. | 0 | 30s |
| Converging Fire | 25 | 4 | After three weapon auto-attack hits on one enemy, +5% weapon auto-attack damage against it for 6s. Hits refresh; changing targets clears. | - | - |
| Cool Under Fire | 35 | 2 | Direct hostile damage makes the next Twin Pistol-skill ability cost 2 less STM. One charge, 10s duration, 10s trigger cooldown. | - | - |
| Twin Suns | 50 + mastery | 6 | One enemy; each weapon D + 20. Next six weapon auto-attack attempts with any weapon gain +25% damage, expiring after 12s. Misses consume charges. | 15 | 90s |

### Crossfire: 60 SP

| Perk | Twin Pistol rank | SP per purchase | Effect | STM | Cooldown |
|---|---|---|---|---|---|
| Blaster Slide I-IV | 2 / 12 / 28 / 40 | 2 / 2 / 3 / 5 | Retreat 4m from an enemy, then each weapon D + 4 / 8 / 12 / 16. Requires at least 2m safe travel. | 3 / 5 / 8 / 10 | 18s |
| Flow | 5 | 2 | Slide/Rush moving 2m+ and landing any hostile hit grants one charge, max three, lasting 12s. Each gives +2 weapon auto-attack ACC with any weapon. Grapple/Final Salvo spend all captured charges. | - | - |
| Paired Handling I-III | 8 / 22 / 45 | 2 / 4 / 4 | +5 / 8 / 10% weapon auto-attack damage with any weapon. Cannot purchase Matched Rhythm. | - | - |
| Grapple Shot I-III | 10 / 30 / 38 | 2 / 4 / 4 | Close up to 6m, stopping 1.5m from a foe; each weapon D + 6 / 10 / 14, plus 4 DMG per captured Flow. Works stationary when already close. | 4 / 7 / 10 | 24s |
| Changing Angles | 15 | 4 | A Crossfire attack moving 2m+ and landing a hostile hit gives the next Crossfire ability +5 ACC. One charge, 8s duration, 10s trigger cooldown. | - | - |
| Pistol Rush I-II | 18 / 32 | 3 / 3 | Freely aimed ground dash, 2-6m. Up to three enemies in the 4m-wide travel line; each weapon floor(0.60 x D) + 6 / 12. | 8 / 10 | 30s |
| Crossfire Stance | 20 | 4 | +10% Haste, +10% Movement Speed, 18s Flow. ALL weapon auto-attack damage x0.80. Activated damage unchanged. | 0 | 30s |
| Follow Through | 25 | 4 | A Flow spender landing any hit restores 1 STM per captured charge spent, max three; once per activation, 12s trigger cooldown. | - | - |
| Second Wind | 35 | 2 | A direct hostile accuracy-versus-evasion miss grants one Flow; at most once per 10s. Requires Flow. | - | - |
| Final Salvo | 50 + mastery | 6 | Stationary attack on one enemy; each weapon D + 20, plus 6 DMG per captured Flow spent, maximum D+38. | 15 | 90s |

### Effect details

- **Cross-skill scope:** one eligible equipped attack weapon means one impact; two mean two. Two-handed weapons count once; shields never add an impact. Each impact keeps its source DMG/type and Twin Pistol PER/AGI scaling. Purchased perks are usable across weapon skills. Only weapon auto-attacks using the Twin Pistol skill and Twin Pistol training require the companion.
- **Matched Rhythm / Paired Handling:** mutually exclusive purchases at every rank, declared in purchase requirements. Matched Rhythm grants 3/6/8% Haste through weapon auto-attack timing and the global Haste cap; it does not shorten ability cooldowns. Paired Handling grants 5/8/10% additive weapon auto-attack damage. Their roles are speed versus damage. Each costs 10 SP at full rank; the combined legal maximum is 110 SP, or 119 SP with Armor Dual Wield. Native batching and the Haste cap affect which is stronger for a particular weapon.
- **Paired Volley / Focused Burst:** maximum rank is D+16 per weapon for 10 STM every 18s versus D+24 for 10 STM every 24s. Volley supplies more frequent pressure; Focused supplies more damage per cast and per STM. They do not share a recast group with each other.
- **Alternating Fire:** every three landed weapon auto-attacks restore 1 STM at most once per 6s. Misses do not reset progress; cooldown hits cannot bank refunds. Activated abilities, queued replacements, procs, reflected hits and duplicate observers do not count. Swaps preserve counter and cooldown.
- **Balanced Grip:** five Accuracy rating on an eligible off-hand attack weapon, including the companion. No main-hand or shield bonus; not five percentage points of hit chance.
- **Controlled Burst / Pistol Rush:** Controlled has at most two impacts at one enemy and one successful-hit refund. Rush has at most three distinct enemies and six impacts, without missing-target redistribution. Every Rush rank needs its own aimed-line spell row. Refunds remain bounded by paid STM.
- **Stances:** exclusive with other stances, including Gambler; global Haste/movement caps apply. Akimbo multiplies ALL weapon auto-attack damage by 0.70; Crossfire by 0.80, after additive outgoing bonuses and before mitigation. Activated damage is unchanged. Both benefits and penalties persist with every weapon loadout and through swaps. Crossfire extends only newly earned/refreshed Flow; leaving caps remaining duration at 12s without refreshing. Stance toggles never create charges.
- **Akimbo ramp:** starts at +10% Haste. Hostile weapon auto-attack hits on the tracked enemy grant +2% per stack, max five (+20% from stance alone), one stack per 2s per attacker. Every qualifying hit refreshes its 6s expiry, including at cap/during the grant cooldown. Misses, abilities, queued replacements and procs cannot build/refresh. Apply gains after the triggering hit. Target change, expiry, combat ending, death or stance exit clears stacks. Weapon swaps preserve stacks, stance penalty and grant cooldown. Additional Haste, including Matched Rhythm, respects the global cap.
- **Converging Fire:** three weapon auto-attack hits on one enemy establish +5% weapon auto-attack damage after the triggering hit. Further hits refresh 6s; misses do not reset progress. Changing targets or 6s without a weapon auto-attack hit clears it. Swaps preserve same-target progress. Additive with Paired Handling and Twin Suns, before stance multiplication.
- **Cool Under Fire:** positive direct hostile damage grants one next-Twin-Pistol-ability discount of 2 STM for 10s, once per 10s. Self/friendly, periodic and reflected damage do not count. The ability may use any eligible weapon. Successful payment consumes the charge; failed validation does not. Swaps preserve the discount and cooldown.
- **Flow:** requires perk ownership. Slide/Rush must move 2m+ and land a hostile hit; one charge per activation regardless of targets/weapons. Empty dashes, weapon auto-attacks, procs and cosmetic callbacks earn none. Maximum three, lasting 12s or 18s in Crossfire stance; qualifying gains refresh live charges. Each gives +2 ACC to weapon auto-attacks with any weapon. Swaps preserve charges and expiry; expiry/combat end/death clears them.
- **Grapple Shot / Final Salvo:** capture live charge identities at cast start and spend those captured charges once on valid paid completion, even if every impact misses. Expiry during casting does not reduce captured attack DMG; charges gained later are not spent. Validation failure before payment spends none. Both work at zero Flow and generate none. Grapple stops 1.5m from its enemy and may fire without moving when already close. Final Salvo is stationary with a 2s activation and 25m range.
- **Changing Angles:** actual 2m+ Crossfire movement plus a hostile landed hit grants one next-Crossfire-ability +5 ACC charge for 8s, once per 10s. Works with any eligible weapon and all captured impacts. Source activation cannot consume its own reward. Stationary Grapple/Final cannot generate it. Swaps preserve charge/cooldown.
- **Follow Through:** Grapple/Final landing any hit restore 1 STM per captured spent charge, max three, once per activation and once per 12s. No Flow refund; misses restore nothing. Combined discounts/refunds retain at least 1 STM paid. Works with any eligible weapon; swaps cannot reset cooldowns.
- **Second Wind:** direct hostile accuracy-versus-evasion misses grant one Flow at most once per 10s. Deflection, concealment, Guard, invalid/out-of-range attempts, friendly actions, periodic effects and procs do not qualify. Publish precise miss causes through generic attack/stat metadata; the existing unfiltered avoided-attack notification is insufficient. Any equipped weapon qualifies; swaps preserve cooldown.
- **Twin Suns:** resolve its initial captured impacts, then grant six weapon auto-attack attempts with +25% additive damage for at most 12s, even if the initial attack was avoided. Any weapon can consume the buff; misses/deflections consume charges. Swaps preserve charges. No extra attacks, critical chance, Haste or cooldown resets.
- **Final Salvo:** maximum three charges produce D+38 per source weapon, at most two impacts. No additional free cast or follow-up buff. Both finishers share the global Capstone cooldown.

### Mastery quest

Use existing weapon mastery quest conventions and the conversation system. Barrage's mastery tests sustained two-hand attacks against one enemy; Crossfire's tests actual repositioning, earned Flow and a successful spender. Credit only valid movement and hostile hits, not an empty dash or equipment at enemy death. Each path unlocks its own rank-50 capstone for 6 SP, with no free perk rank or extra damage item. Completing one does not unlock the other automatically.

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

The baseline below uses current `Combat` and `WeaponDamage` formulas at equal Attack/Defense 100, equal damage attributes 26, a fixed 10% critical chance, StandardCriticalRating 2, DMG 22 and Delay 250 per item. It excludes misses, mitigation, other item bonuses, and perk-line bonuses. A single weapon rounds its adjusted item DMG up once; the damage formula rounds its final range endpoints down. Damage casts a random float between those endpoints to an integer, so the upper integer endpoint is effectively excluded when endpoints differ.

| Equipment | Item DMG per shot | Cycle interval | Mean damage per second |
|---|---:|---:|---:|
| Single pistol with natural +20% | 27 | 2.416s | 9.42 |
| Single pistol with Doublehand III, +60% total | 36 | 2.416s | 12.83 |
| Pair without Dual Wield | 22 each | 4.833s for two shots | 7.70 |
| Pair with Dual Wield III | 22 each | 3.583s for two shots | 10.38 |

These are arithmetic baselines, not full-build measurements. They show why granting a free companion hit with full weapon DMG at single-pistol cadence is inappropriate and why the slower paired cycle needs specialist damage bonuses. They do not establish full-build balance.

### Comparable builds

Compare Twin Pistol 50 with either full 60-SP line plus Dual Wield III against Pistol 50 with its full 60-SP critical line plus Doublehand III: 69 SP each, equal Armor investment, attributes, equipment tier, and external buffs. Include the pistol/shield defensive alternative and existing Skirmisher, Rifle, and melee paired-weapon builds. Test equal-tier gear first, then repeat at equal acquisition/upgrade cost. The pair costs two pistols' materials but owns one enhancement-slot budget whose profile upgrades feed both guns.

Run hybrids at equal total SP and skill-rank investment. A character buying both Pistol and Twin Pistol lines is not comparable to a 69-SP single-line build. Include generic ranged/critical effects, Armor traits, buffs from allies, weapon enhancements, suppression, healing-on-hit, and limited haste/no-delay effects. Do not assume Pistol-specific traits apply merely because the models are pistols.

### Native rotation observations before tuning

Five equal 69-SP builds ran against the same stationary 30,000-HP NPC with measured DEF 94 and EVA 134. Attackers had rank 50, attributes 26, DMG 22 and 250 delay units per weapon. Single Pistol bought Doublehand III; paired builds bought Dual Wield III and exactly one handling trait. Actual feats, finite 61-STM pools, production regeneration every six seconds, native ordinary rolls and purchased capstones were used. Damage attribution equaled target HP loss in every completed measurement.

| Build | Handling | Damage near 6s | Damage near 12s | 120s DPS | Final STM |
|---|---|---:|---:|---:|---:|
| Pistol Gunslinger | Doublehand | 408 | 551 | 23.00 | 11 |
| Barrage | Matched Rhythm | 418 | 622 | 28.34 | 8 |
| Barrage | Paired Handling | 435 | 590 | 30.67 | 8 |
| Crossfire | Matched Rhythm | 175 | 418 | 20.60 | 30 |
| Crossfire | Paired Handling | 369 | 547 | 22.44 | 30 |

These are one seeded encounter per build before the October 14 tuning, not a statistical balance certification. Snapshots occurred at 6.29-6.61s, 12.30-12.49s and 120.43-120.80s. Barrage measured 123-133% of the Pistol reference, outside the 95-105% sustained release target. Its largest observed opening also exceeded the proposed 10% burst allowance. That prompted lower Volley and Focused DMG, a 30% Akimbo ordinary-damage penalty, and 5/8/10% Paired Handling. This first Barrage pass preserved costs and cooldowns. The later open-corridor comparison also prompted the Crossfire DMG/cost changes shown in the current tables. Haste, Flow and movement remain unchanged. Conditional repricing selected candidates for repeated engine comparison; it cannot recover exact sampled damage by category from these older records.

Crossfire completed Slide, Rush, Grapple and both paid finishers, but its controller accumulated 47-48 blocked Slide attempts and 42-43 unavailable Rush lanes. Its measured damage includes arena geometry and controller decisions; it is not an unobstructed rotation ceiling or a measure of movement's survival benefit. Target positions were not logged, so the records do not establish same-floor geometry throughout. The NPC remained above 70% HP, making High Noon's target-health condition eligible throughout. Incoming damage, Second Wind opportunities, Cool Under Fire, allied buffs, per-hit enhancements, equal acquisition cost, PvP and multiple seeds remain outside this comparison.

### Historical repeated native comparison

Three fixed seeds (20261013/14/15) per build ran in a verified flat open corridor at the same 69-SP investment, rank 50, attributes 26 and DMG 22 / Delay 250. Actual player attacks, native rolls/batching, finite 61 STM, production regeneration every six seconds, paid capstones and real movement/cast interruption are retained. Ordinary and activated resolved damage reconcile exactly with delivered HP after an eight-second projectile drain. The unchanged Pistol/Barrage samples and revised Crossfire samples use identical fixture conditions.

| Build | Handling | Mean 120s DPS | Observed DPS range | Comparison |
|---|---|---:|---:|---|
| Pistol Gunslinger | Doublehand | 21.61 | 17.98-25.01 | Reference |
| Barrage | Matched Rhythm | 21.33 | 19.36-24.84 | 98.7% of Pistol |
| Barrage | Paired Handling | 21.78 | 20.36-23.58 | 100.8% of Pistol |
| Crossfire | Matched Rhythm | 17.83 | 16.49-19.44 | 83.6% of matching Barrage |
| Crossfire | Paired Handling | 18.71 | 17.20-20.95 | 85.9% of matching Barrage |

These means meet the controlled sustained targets. The ranges show meaningful encounter variance; three seeds do not establish statistical equivalence or certify every build. Windows were observed at 6.270-6.328s, 12.287-12.537s and 120.313-120.953s; DPS uses actual elapsed time. The sustained controller requests capstones at eight seconds, so its six-second window excludes them.

The first open-corridor Crossfire candidate averaged 22.23/21.84 DPS, revealing that the old blocked lanes had understated its output. Matching Slide to Volley, reducing uncharged Grapple DMG, increasing its stamina cost and using a 20% global ordinary-damage penalty brought it into its mobility budget. A larger toggleable stance penalty alone would have obscured stance-off output. The final six Crossfire runs recorded all 77 accepted movement abilities ending within 0.35m of their authored destinations on Z=0; 66 traveled at least 2m, totaling 302.38m. Short already-close Grapples are counted separately. This verifies travel and endpoint agreement, not survival value or second-client rendering.

Incoming pressure, optimized rotations, PvP, other weapon/attribute tiers, external buffs, per-hit enhancements and equal acquisition-cost builds remain outside this bounded comparison. All five native cross-skill controls pass with the final costs, including real saber/rifle ordinary damage and Haste. Reports and source/resource hashes are retained in the local engine-test artifacts.

### Historical capstone-first opening comparison

All five opening cases passed with three fixed seeds each, using the same equal-69-SP builds, gear, corridor and finite resources. The controller requests the purchased capstone at zero seconds and core feats at two, three and four seconds, subject to real cast timing, readiness and the movement lockout. Final Salvo starts at zero Flow. Every sample reconciled ordinary plus activated damage to delivered HP after the bounded drain.

| Build | Handling | Mean damage near 6s [range] | Mean damage near 12s [range] | Mean damage versus Pistol, 6s / 12s |
|---|---|---:|---:|---:|
| Pistol Gunslinger | Doublehand | 369.67 [288-519] | 435.00 [359-567] | Reference |
| Barrage | Matched Rhythm | 313.00 [222-381] | 390.33 [319-466] | 84.7% / 89.7% |
| Barrage | Paired Handling | 270.33 [164-362] | 364.67 [272-435] | 73.1% / 83.8% |
| Crossfire | Matched Rhythm | 133.67 [41-201] | 374.33 [331-442] | 36.2% / 86.1% |
| Crossfire | Paired Handling | 158.00 [45-263] | 406.67 [345-510] | 42.7% / 93.5% |

Actual snapshots occurred at 6.260-6.614s and 12.261-12.613s. Comparing mean DPS calculated from each sample's elapsed time gives Barrage 72.2-84.9% of Pistol near six seconds and 83.6-89.1% near twelve; Crossfire gives 37.0-43.4% and 85.9-93.1%. Thus neither line's mean exceeds the proposed 10% opening allowance in this rotation. Individual rolls vary substantially; this is not a maximum-burst guarantee, an optimized or precharged Final Salvo test, or a multi-target/PvP comparison.

Every sample accepted one capstone and one of each core feat by twelve seconds. Crossfire's lower six-second result reflects this particular capstone-first schedule and actual movement lockouts. Final STM was 29 for Barrage, 25-26 for Crossfire and 15-17 for Pistol, from the initial 61 and two production regeneration ticks. All owned test containers/helpers were stopped after the runs; isolated Redis cache data and reports were preserved outside the repository.

### Model estimates and remaining balance validation

Earlier DPS tables model the superseded duplicate-damage handling perks and older Paired Volley costs/DMG. Do not present the old 103% result or the former 129-SP hybrid as current evidence. `tools/AnalyzeTwinPistolBalance.mjs` now models the current mutually exclusive handling choices, updated Volley, and ramp startup using 64 mixed-seed ordinary-hit samples, alongside expected activated damage and finite STM. It applies integer per-hit damage adjustments, clears Alternating Fire progress during its cooldown, and opens Crossfire with Slide, Rush, then Grapple. Quick Draw receives no repeated new-target critical bonus against the continuously damaged reference target.

Before tuning, this approximation produced 21.72 DPS for the 69-SP Pistol reference, 23.93 for Barrage and 19.36 for Crossfire. Those numbers are historical estimates, not the current build's result. The model lacks cast/travel interruption, native swing batching, per-hit enhancements, capstones and engine execution. STM refunds and Flow use fractional expectations, rotations retain a four-STM reserve, and Pistol trigger uptime is approximated. It does not establish release balance. Current values and assumptions are recorded in its JSON output, including handling comparisons and marginal single/paired weapon tradeoffs; those ratios do not represent complete saber/rifle builds.

Extend the matched-investment 6s/12s/120s comparisons to wider single/two-handed and saber/rifle hybrid builds, external Haste, the global Haste cap, per-hit effects and incoming pressure. Preserve finite STM, paid attacks and movement interruption. Continue checking weapon swaps without resetting stance penalties, counters or proc cooldowns. All legal combined-line purchases top out at 110 SP, or 119 including optional Armor Dual Wield; standalone lines remain 60 SP each.

At maximum rank Volley is D+16/10 STM/18s and Focused is D+24/10 STM/24s. Keep the frequency-versus-efficiency distinction through resource-constrained rotations, rather than treating ability DMG alone as a balance comparison. Matched Rhythm remains implemented at 3/6/8% Haste. Retain the release targets below; the repeated controlled observations establish results for their stated fixture and rotations.

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
- Sustain should have a resource cost. Alternating Fire, Controlled Burst, Cool Under Fire and Follow Through must respect their caps and cannot create indefinite refund loops. Flow never refunds itself. Test zero/one/three charges, two independent misses, blocked/empty dashes, expiry during a cast, stance toggles, accuracy/evasion misses versus other avoidance causes, and equipment swaps. Test both Twin lines together and equal-SP Pistol/Twin hybrids with unrestricted critical triggers.

Use fixed-seed simulations and enough samples to report uncertainty, then verify representative rotations in engine. Report actual gear, hit rates, resources, DPS, burst, target assumptions and movement opportunities. Tune Matched Rhythm / Paired Handling and stance values for ordinary output, then ability DMG, spent-charge DMG and resource costs. Reprice movement utility within Crossfire rather than raising its damage to match a stationary specialist. Do not compensate by changing existing Pistol perks or other weapon systems.

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

The Twin Pistol Bible sheet and workbook layout/audit registration contain all 36 proposed ranks, eighteen per 60-SP line. At implementation time, register the tab with the weapon generators and implement mutually exclusive handling purchases and distinct Haste/ordinary-damage stats. Include costs, timings, attributes, target shapes, descriptions, and mastery requirements. Preserve cached formulas and unrelated ZIP entries through targeted XML edits, then run the required refresh.

Generate the perk definitions and one class/file per distinct active ability. Add all needed feat, spell, effect-icon, skill/item-property labels, status effects, and stat definitions. Choose available IDs from the current source data. New custom TLK entries use the first gaps; custom references use the required offset. Recast labels must fit the existing 14-character limit.

Create tiered owned-pair blueprints, one internal temporary companion template, crafting recipes and starter acquisition. Pair upgrades are stored once on the owner; the temporary carrier retains its verified appearance. Retired off-hand pistols remain unchanged and disabled; independent companion recipes are inactive and their items are excluded from the economy. No conversion of existing Pistol weapons or broad character-build migration is needed. Add the mastery quest and its dialogue after the mechanics work.

Choose VFX from the repository's effect-selection reference. Author paired attack and ability clips compatible with both weapons, interruption, and supported character models. Regenerate cooldown icons and run the gameplay icon audit. Player-facing combat feedback and UI must use PlayerName when displaying characters.

### 4 Validate and prepare delivery

Run the checks below, tune both lines within their budgets, and update the Bible and generated outputs together. Present actual NWN evidence of both weapons and shots. The complete feature requires a parent server/module PR and, when the submodule pointer changes, a pushed HAK companion PR linked in both directions. Resolve review threads on both before final handoff.

Deploy the matching C# server build, rebuilt HAK/TLK resources, and repacked module together. Repack/rebuild/deploy requirements are deployment steps, not reasons to alter gameplay code.

## Integration map

| Existing location | Required work |
|---|---|
| `Service/SkillService/SkillType.cs`, `Service/Skill.Mapping.cs`, `Service/Skill.cs` | Skill metadata, context-sensitive attack selection, initialization and progression |
| `Feature/EquipmentRestrictions.cs`, `Feature/PistolBaseItemCompatibility.cs`, `Service/Item.cs` | Prospective pair validation, tier requirements, compatibility, item classification and attributes |
| `Service/PerkService/EquipmentPredicates.cs`, `Service/Stat.cs` | Active-pair predicate for ordinary attacks/equipment, Armor Dual Wield eligibility, cross-skill perk stats and per-hand overrides |
| `Native/OnAIActionAttackObject.cs`, `Native/WeaponAttackCycle.cs`, `Native/ResolveAttackRoll.cs`, `Native/GetDamageRoll.cs` | Paired ranged scheduling and explicit attack context; preserve existing melee/native behavior |
| `Service/Combat.cs`, `Service/CombatService/WeaponDamage.cs` | Shared timing, shot calculations, skill/proc accounting and Single Weapon exclusion |
| `Feature/PistolAnimationRemap.cs`, `Native/WeaponAttackAnimation.cs`, `design/animations/` | Paired ranged presentation without regressing empty-offhand or shielded pistols |
| `Feature/AbilityDefinition/WeaponActiveAbilityDefinitionBase.cs`, perk/ability/status definitions | Hand-specific ability profiles, traits, activation summaries and shared recasts |
| Character sheet, equipment examination, skills/perks UI, player guide | Twin Pistol rank, both weapons' values, requirements, costs, and shared cooldown descriptions |
| `tools/GenerateWeaponArchetypeImplementation.py`, Bible and audit/layout inputs | New source sheet, reproducible generated output and balance data |
| `Module/uti`, recipes, starter acquisition, quests and `ConversationData` | Obtainable owned pairs, internal temporary companion and mastery progression |
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

Equip, attack, upgrade/change state, save, and reload through normal gameplay at least twice. Assert one owned pair, stable owner identity, rank requirements, equipment state and ammo quantities. Verify automatic companion equip; removal of either hand unequips the owner and destroys the spare; replacement, failed equip, death, disconnect and repeated reload never duplicate or orphan a spare. Include a Twin Pistol character with Pistol rank zero. Any later migration must start with legacy fixtures and run the actual conversion first.

Player-controlled combat droids need an explicit companion-equipment compatibility check before release. If the item is supported for droids, include skill/module and AI-slot behavior and run `DroidInventory` lifecycle coverage; if their current equipment system cannot support it, reject the companion clearly until that integration is complete rather than allowing a partially functional item.

### Client evidence

Follow `InEngineVisualVerification.md`: run the owned NWN client on the isolated private desktop, set every volume channel to zero, capture its real rendered output, and stop it afterward. Do not move the user's focus or desktop.

Capture both hands firing, independent hit/miss presentation, both capstones, Slide, Grapple, freely aimed Rush, maximum haste, equipment transitions, and another client's observation. Include blocked paths, roots, empty destinations, narrow doors and attempted terrain shortcuts. Check representative male/female and supported body/appearance variants, weapon alignment, muzzle origin, recoil, ready poses, impact timing, and duplicate/missing sounds. Record any unsupported appearance explicitly. Any new trainer or quest placement also needs evidence from a public approach with normal collision.

## Repository references

The starting mechanics and baseline calculations are based on commit `62364f27c`:

- [Dual wield combat](DualWieldCombat.md) and [weapon loadout balance tests](../../SWLOR.Game.Server.Tests/Service/WeaponLoadoutBalanceTests.cs).
- [Pistol perks](../Feature/PerkDefinition/PistolPerkDefinition.cs) and [Armor traits](../Feature/PerkDefinition/ArmorPerkDefinition.cs).
- [Resource budget analysis script](../../tools/AnalyzeTwinPistolBalance.mjs), [Pistol ability definitions](../Feature/AbilityDefinition/Pistol), [combat formulas](../Service/Combat.cs), and [weapon damage](../Service/CombatService/WeaponDamage.cs).
- [Equipment restrictions](../Feature/EquipmentRestrictions.cs), [pistol compatibility](../Feature/PistolBaseItemCompatibility.cs), and [pistol animation remap](../Feature/PistolAnimationRemap.cs).
- [Ability targeting](AbilityTargeting.md), [Design Bible workbook rules](DesignBibleWorkbookRules.md), [economy restrictions](EconomyRestrictedItems.md), [icon standards](IconStandards.md), [VFX selection](VisualEffectSelection.md), and [player identity](PlayerIdentity.md).
- [Engine testing](EngineTesting.md) and [in-engine visual verification](InEngineVisualVerification.md).
