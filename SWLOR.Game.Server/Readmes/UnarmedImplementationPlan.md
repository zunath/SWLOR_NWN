# Unarmed skill implementation

## Identity and equipment

Unarmed is an equipped melee weapon skill. **Power Glove** is a new one-handed weapon class: powered striking gloves, shock boxing gloves and compact fist weapons. These weapons occupy the normal weapon hand slots and carry their own DMG, Delay, ACC, damage type, rank requirement, enhancement slots and appearance. Ordinary armor-slot gloves remain armor and never supply the Unarmed weapon profile. Bare hands without an equipped weapon do not qualify for the new perks or train the new skill.

The two lines have different jobs:

| Line | Role | Cost of its strengths |
|---|---|---|
| Martial Artist | Disciplined combinations: a jab sets up palms/kicks and an optional ordinary weave. Flow Stance rewards a short defensive ending or a complete offensive ending. | A missed bridge loses the full finisher. Stance rewards require landed, sequenced finishers. No area damage or automatic dodges. |
| Brawler | Dirty tricks and committed heavy hits. Distract an opponent, then trade the distraction for an uppercut or haymaker. | A longer attack commitment; spending the setup removes its short defensive benefit. No stun chain, forced movement, Guard or tank threat. |

This leaves Katar's Guard tank and Scrapper control, knife stealth/poison, Staff control/area damage and Spear evasive utility intact. Both Unarmed lines remain usable when their condition fails: their attacks still have base damage.

Unarmed has ranks 0-50, counts toward the existing 400-rank cap, uses Weapon combat points, and uses **Might for damage / Perception for accuracy**, subject to normal item overrides. No Katar prerequisite, conversion, refund or free initial perk. Each line has ten distinct perks, eighteen purchasable ranks and a 60-SP budget. Both complete lines cost 120 SP; they share no purchase. Optional Armor Doublehand III or Dual Wield III adds 9 SP for a 69-SP single-line comparison.

### Weapon and grip rules

- One Power Glove with an empty off hand uses one ordinary weapon roll. It receives the normal +20% Single Weapon item-DMG bonus and any purchased Doublehand adjustment, rounded once by the existing formula.
- Two Power Gloves use the existing melee two-weapon scheduler. Both delays count; each weapon retains its own profile. Dual Wield reduces the offhand delay. Single Weapon and Doublehand do not apply.
- Full new perk effects and authored Unarmed abilities require a main-hand Power Glove with an empty off hand or another Power Glove. Shield and mixed-weapon arrangements retain ordinary weapon behavior but fail this perk/ability grip predicate. This follows the existing authored Unarmed animation compatibility boundary.
- Ordinary strikes from a Power Glove use Unarmed for their captured weapon skill. A different weapon in a mixed pair keeps its own skill. No armor glove/bracer fallback may contribute damage, requirements, XP or Unarmed perks to a real Power Glove attack.
- Punches, elbows and kicks are presentation. One visible flurry never implies extra mechanical attacks. No monk flurry, native unarmed damage progression or automatic second fist is added.

Start with the current **Katar item budget** rather than a faster or stronger new weapon economy. Examples from the existing Equipment - Weapons sheet:

| Power Gloves tier | Required Unarmed | DMG | Delay | Reference |
|---|---:|---:|---:|---|
| Training | 0 | 6 | 220 | Training Katar; training vendor |
| Basic | 0 | 7 | 220 | Basic Katar; ordinary vendor and Smithery |
| Field | 5 | 8 | 220 | Field Katar |
| Titan | 10 | 9 | 220 | Titan Katar |
| Veteran | 15 | 10 | 220 | Veteran Katar |
| Delta | 20 | 11 | 220 | Delta Katar |
| Prime | 25 | 12 | 220 | Prime Katar |
| Proto | 30 | 13 | 220 | Proto Katar |
| Ascendant | 35 | 15 | 220 | Ascendant Katar |
| Ophidian equivalent | 40 | 16 | 220 | Ophidian Katar |
| Charmbreaker equivalent | 45 | 18 | 220 | Charmbreaker Katar |
| Chiro equivalent | 50 | 19 | 220 | Chiro Katar |

The twelve weapon tiers mirror the corresponding Katar material costs, enhancement capacity, credit value and acquisition routes. Ten Smithery recipes, training and ordinary vendor stock, named loot and the locked Chiro recipe book provide acquisition. Basic Power Gloves deal Physical damage. The shock boxing, vibroknuckler, blastknuckle and power fist appearances are cosmetics with identical mechanical budgets. Elemental damage still requires the normal weapon-property budget.

Both lines are available to **Force-sensitive and Standard characters**. This is a physical weapon skill using MGT/PER and Stamina. There is no Force skill prerequisite, FP cost, character-type restriction or Force-sensitive quota.

### Visible and invisible appearances

Six appearances are available: Power Glove, Vibroknuckler, Blastknuckle, Shock Boxing Glove, Power Fist and invisible bare fist. All remain real equipped weapons with identical stats, costs, requirements and attack scheduling. The invisible appearance uses present, valid composite weapon parts with no rendered geometry and retains a visible inventory icon.

The 2026-10-13 native prototype verified the model approach in two fresh, silent NWN clients on isolated private desktops. A missing-resource control reproduced a bag in the equipped hand. Three present composite weapon parts, each containing a matching root plus a child dummy and no mesh, produced empty-looking hands while the real weapon objects remained equipped. Single and paired grips, visible/invisible swaps, a client restart, human male/female and elf male samples passed. The technique also has an authored reference in [Merricksdad's null weapon-part tutorial](https://neverwintervault.org/article/tutorial/merricksdads-gmax-tutorials-create-weapon-model-2d-texture).

The production pass adds 32 native rest/combat/body views using base item 541 and the final models, plus 20 views covering all ten authored abilities on human male and female rigs. Single and paired invisible grips do not show the unwanted held bag. Animation inheritance resolves through 27 body/robe routes, with those routes checked statically. Pose captures replay at quarter speed for inspection after normal-speed playback; they do not measure gameplay timing. Other body families' active poses, multiplayer observers and ground-drop appearance still need player testing. Native captures, source hashes and isolation/cleanup records remain in the task's visual artifacts.

## Progression and perk tables

Each opener introduces its own working mechanic at rank 2. Rank 5 improves that existing mechanic, rank 8 adds its first spender, and rank 10 improves the already-unlocked spender. No earlier purchase needs a later perk to perform its advertised core function. Individual prerequisite purchases are listed in the Bible; higher ranks require the previous rank of the same perk. All 36 purchases are implemented. Balance remains subject to player feedback.

**Martial Artist:** Jab already grants Opening and its ordinary accuracy benefit at rank 2. Flowing Form at rank 5 extends the existing windows and rewards one ordinary weave. Palm Strike arrives at rank 8; Practiced Form improves it at rank 10. Finishing Combination later offers either a direct Jab follow-up or the stronger complete sequence. Palms, kicks and sweeps belong to its physical animation style; they create only the declared impacts and no automatic knockdown.

**Brawler:** Dirty Jab already applies its short accuracy debuff at rank 2. Dirty Fighting at rank 5 extends it and improves ordinary accuracy against that opponent. Uppercut arrives at rank 8; Heavy Blows improves it at rank 10. The brawler can cash in immediately instead of waiting to be attacked.

`D` is the captured main-hand weapon DMG after eligible Single Weapon/Doublehand adjustments. The normal ability skill bonus, floor(0.15 x rank), is additional to listed weapon/flat power and applies per impact: +7 at rank 50, including each half of Finishing Combination. Only Practiced Form's new budget is split between those impacts; existing generic flat modifiers retain their declared engine behavior and require hybrid tests. The ordinary weave adds its stated power to one real ordinary roll, not to a new ability impact, and receives no activated-ability rank bonus.

Each line has ten distinct perks, eighteen purchases and a 60-SP total. Both lines cost 120 SP. Rank-50 capstones require separate mastery quests and still cost 6 SP each. Both stances and capstones have two-second activations; Uppercut commits for one second. Other listed attacks are instant.

### Martial Artist: 60 SP

| Perk | Skill | SP | Effect | STM | Activation | Recast |
|---|---:|---:|---|---|---|---|
| Jab I | 2 | 2 | Deals weapon DMG + 8. On hit, grants Opening against this enemy for 4 seconds. Opening gives +3 ordinary Unarmed ACC and sets up either follow-up attack. | 3 | Instant | 18 seconds |
| Flowing Form | 5 | 2 | Extends Opening and Finish from 4 to 6 seconds. During Opening, your first ordinary Unarmed attempt against that enemy gains +4/8/12/16 power by Jab rank. One bonus per opener, consumed even on a miss. | - | - | - |
| Palm Strike I | 8 | 2 | Deals weapon DMG + 12. Spends Opening at activation. If the sequenced strike lands, grants Finish for 4 seconds; Finish gives +3 ordinary Unarmed ACC. | 4 | Instant | 24 seconds |
| Practiced Form I | 10 | 2 | Adds +4 total power to Palm Strike spending Opening, or a finisher spending Opening/Finish. Applied once per cast; split evenly between Finishing Combination's two impacts. | - | - | - |
| Jab II | 12 | 2 | Deals weapon DMG + 18. On hit, grants Opening against this enemy for 4 seconds. Opening gives +3 ordinary Unarmed ACC and sets up either follow-up attack. | 5 | Instant | 18 seconds |
| Measured Technique | 15 | 4 | Gain +5 ACC when Palm Strike spends Opening, Finishing Combination spends Opening/Finish, or Perfect Form spends Finish. | - | - | - |
| Finishing Combination I | 18 | 3 | Two strikes at one enemy, each floor(0.50 x weapon DMG) + 8. Spend Opening for +3 power per strike, or Finish for +6 per strike. | 6 | Instant | 30 seconds |
| Flow Stance | 20 | 4 | A landed Finishing Combination spending Opening grants Slip Away: +20% Evasion and +15% movement speed for 6 seconds. A landed finisher spending Finish grants Press Forward: +15% Haste for 6 seconds. These windows are exclusive and share a 12-second trigger gate. | - | 2 seconds | 30 seconds |
| Palm Strike II | 22 | 4 | Deals weapon DMG + 24. Spends Opening at activation. If the sequenced strike lands, grants Finish for 4 seconds; Finish gives +3 ordinary Unarmed ACC. | 8 | Instant | 24 seconds |
| Controlled Breathing | 25 | 4 | Finishing Combination spending Opening/Finish, or Perfect Form spending Finish, restores 2 STM if any hit lands. Once per cast, no more than STM paid; shares a 12-second refund cooldown with Catch Your Breath. | - | - | - |
| Jab III | 28 | 3 | Deals weapon DMG + 28. On hit, grants Opening against this enemy for 4 seconds. Opening gives +3 ordinary Unarmed ACC and sets up either follow-up attack. | 8 | Instant | 18 seconds |
| Practiced Form II | 30 | 4 | Adds +8 total power to Palm Strike spending Opening, or a finisher spending Opening/Finish. Applied once per cast; split evenly between Finishing Combination's two impacts. | - | - | - |
| Finishing Combination II | 32 | 3 | Two strikes at one enemy, each floor(0.50 x weapon DMG) + 14. Spend Opening for +4 power per strike, or Finish for +8 per strike. | 9 | Instant | 30 seconds |
| Rooted Technique | 35 | 2 | Gain +15 Mobility Resistance while using the valid Power Gloves grip. | - | - | - |
| Practiced Form III | 38 | 4 | Adds +12 total power to Palm Strike spending Opening, or a finisher spending Opening/Finish. Applied once per cast; split evenly between Finishing Combination's two impacts. | - | - | - |
| Jab IV | 40 | 5 | Deals weapon DMG + 38. On hit, grants Opening against this enemy for 4 seconds. Opening gives +3 ordinary Unarmed ACC and sets up either follow-up attack. | 12 | Instant | 18 seconds |
| Palm Strike III | 45 | 4 | Deals weapon DMG + 36. Spends Opening at activation. If the sequenced strike lands, grants Finish for 4 seconds; Finish gives +3 ordinary Unarmed ACC. | 10 | Instant | 24 seconds |
| Perfect Form | 50 | 6 | Deals weapon DMG + 45 to one enemy. Spending Finish adds +15 power. Practiced Form and Measured Technique apply only with Finish. | 15 | 2 seconds | 90 seconds |

### Brawler: 60 SP

| Perk | Skill | SP | Effect | STM | Activation | Recast |
|---|---:|---:|---|---|---|---|
| Dirty Jab I | 2 | 2 | Deals weapon DMG + 8. On hit, applies your Rattled for 6 seconds, reducing this enemy's ordinary weapon ACC by 10. | 3 | Instant | 18 seconds |
| Dirty Fighting | 5 | 2 | Extends your Rattled from 6 to 8 seconds. Gain +3 ordinary Unarmed ACC against the enemy affected by your Rattled. | - | - | - |
| Uppercut I | 8 | 2 | One committed strike, weapon DMG + 12. Spending your Rattled against its enemy adds +8 power. | 4 | 1 second | 24 seconds |
| Heavy Blows I | 10 | 2 | Uppercut or Haymaker spending your Rattled gains +4 total power for that activation. | - | - | - |
| Dirty Jab II | 12 | 2 | Deals weapon DMG + 18. On hit, applies your Rattled for 6 seconds, reducing this enemy's ordinary weapon ACC by 10. | 5 | Instant | 18 seconds |
| Hard Knocks | 15 | 4 | Gain +15 Trauma Resistance while using the valid Power Gloves grip. | - | - | - |
| Low Kick I | 18 | 3 | Deals weapon DMG + 16. On hit, this enemy deals 10% less ordinary physical damage for 6 seconds. | 6 | Instant | 30 seconds |
| Bruiser Stance | 20 | 4 | Gain +10% Physical Defense. Dirty Jab opens a 4-second brace against its Rattled opponent. A direct hostile hit dealing actual HP damage from that opponent arms +8 power for your next Uppercut/Haymaker spending that Rattled before it expires. Once per opener, with an 18-second grant gate. | - | 2 seconds | 30 seconds |
| Uppercut II | 22 | 4 | One committed strike, weapon DMG + 24. Spending your Rattled against its enemy adds +12 power. | 8 | 1 second | 24 seconds |
| Catch Your Breath | 25 | 4 | A Rattled spender landing its hit restores 2 STM once per cast, no more than STM paid. Shares a 12-second refund cooldown with Controlled Breathing. | - | - | - |
| Dirty Jab III | 28 | 3 | Deals weapon DMG + 28. On hit, applies your Rattled for 6 seconds, reducing this enemy's ordinary weapon ACC by 10. | 8 | Instant | 18 seconds |
| Heavy Blows II | 30 | 4 | Uppercut or Haymaker spending your Rattled gains +8 total power for that activation. | - | - | - |
| Low Kick II | 32 | 3 | Deals weapon DMG + 30. On hit, this enemy deals 10% less ordinary physical damage for 8 seconds. | 9 | Instant | 30 seconds |
| Payback | 35 | 2 | Positive direct hostile HP damage makes your next positive-cost Brawler activation cost 2 less STM. One charge, lasts 8 seconds, triggers at most once per 18 seconds. | - | - | - |
| Heavy Blows III | 38 | 4 | Uppercut or Haymaker spending your Rattled gains +12 total power for that activation. | - | - | - |
| Dirty Jab IV | 40 | 5 | Deals weapon DMG + 38. On hit, applies your Rattled for 6 seconds, reducing this enemy's ordinary weapon ACC by 10. | 12 | Instant | 18 seconds |
| Uppercut III | 45 | 4 | One committed strike, weapon DMG + 36. Spending your Rattled against its enemy adds +16 power. | 10 | 1 second | 24 seconds |
| Haymaker | 50 | 6 | Deals weapon DMG + 40 to one enemy. Spending your Rattled against its enemy adds +20 power; Heavy Blows also applies. | 15 | 2 seconds | 90 seconds |

## Ordinary attacks and technique states

Jab directly sets up either follow-up: **Jab -> Palm Strike** or **Jab -> Finishing Combination**. The complete route is **Jab -> Palm Strike -> Finishing Combination or Perfect Form**. All stages belong to the same enemy. Jab's landed impact starts Opening for four seconds, or six with Flowing Form. Opening and Finish give +3 ACC to ordinary Unarmed rolls.

Palm Strike captures and consumes Opening at paid activation. If that sequenced strike lands, grant Finish for four seconds, or six with Flowing Form. Finishing Combination can spend Opening directly for +3/4 power per impact, or spend Finish for the stronger +6/8 per impact. Perfect Form uses Finish only for its conditional bonus. Without matching setup, these abilities still deal base damage but receive no conditional power, accuracy or refund. The first spenders arrive after their setup exists, and their enhancement traits arrive after the spenders.

Flowing Form rewards the first ordinary Unarmed attempt against the same enemy during Opening with +4/8/12/16 power according to the purchased Jab rank. Consume this one-use weave bonus even on a miss; it never consumes Opening or creates a new stage. The player can immediately cast a follow-up, or wait for an ordinary attack to collect the bonus. Spending/expiring Opening removes any unused weave bonus. The core combo never requires an ordinary attack or this passive.

Only an actual ordinary weapon roll can weave. Queued replacements, activated abilities, enhancement/proc damage, animation callbacks and duplicate damage queries do not qualify. One opener pays out once, regardless of hands or haste. A visible elbow/kick/flurry never creates another attack. Existing two-weapon timing and per-hand profiles remain; the eligible real roll gets its own weapon damage/type plus the single weave bonus. Sample and consume its eligibility once per attempted roll. Timing buffs cannot retroactively change an already-budgeted cycle.

The Brawler sequence is **Dirty Jab -> Uppercut or Haymaker**, with ordinary attacks or Low Kick available before the spender. A landed Dirty Jab applies your Rattled for six seconds, or eight with Dirty Fighting, using normal eligible status-application rules. Rattled reduces the opponent's ordinary weapon ACC by ten; it does not lower ability accuracy, stop actions, stun, interrupt, slow or lower Evasion. Dirty Fighting also gives +3 ordinary Unarmed ACC against the enemy carrying your effect.

Rattled is caster-owned and limited to one enemy per brawler. Uppercut/Haymaker captures and removes only your Rattled at paid activation for its conditional power. A miss still spends it. Another player's Rattled cannot power your hit; multiple live sources apply only the strongest accuracy penalty. Spending one source must leave other valid sources intact. This is a new modest debuff, not a repurposed Blind/Disoriented effect with hidden control or Evasion riders.

A character can hold only one line's offensive setup: Opening, Finish or their Rattled. A valid paid opener clears earlier setup before its impact; it creates new setup only on a landed, valid impact/application. Ordinary attacks against the same enemy preserve setup without refreshing its timer; the one-use weave is separate eligibility attached to Opening. Low Kick against the same opponent preserves Rattled. Other-line or other-skill hostile abilities, another target, stance/equipment changes, death, area change, expiry or combat end clear the owner's setup and remove their Rattled. Self buffs/healing do not advance it and timers continue. No out-of-combat banking or weak-minion-to-boss transfer. Preserve trigger and ability cooldowns when invalidating benefits.

## Activation, resource and hybrid safeguards

All hostile Unarmed activations share a one-second action lockout in addition to their recasts. Uppercut's one-second activation is a committed attack, not an instant doubled hit. Capstones activate in two seconds. Normal activation interruption, range, legal-target and line-of-attack checks apply. Use the existing 7m single-target melee ability limit as the initial consistency baseline; ordinary attacks retain normal melee reach. Capture stage, target and main-hand profile once at valid paid activation and revalidate before launch/impact. Later misses or target loss never refund setup.

| Shared recast | Members | Delay | Short label |
|---|---|---:|---|
| New Unarmed Opener | Jab / Dirty Jab | 18s | Fist Opener |
| New Unarmed Response | Palm Strike / Uppercut | 24s | Fist Response |
| New Unarmed Technique | Finishing Combination / Low Kick | 30s | Fist Technique |
| Existing global Capstone | Perfect Form / Haymaker / existing capstones | 90s | Existing label |

Flow Stance changes the choice of ending. Finishing Combination spending Opening grants Slip Away (+20% Evasion and +15% movement speed for six seconds). A finisher spending Finish grants Press Forward (+15% Haste for six seconds). Any landed impact qualifies, once per completed cast, and both rewards share a twelve-second trigger gate. They cannot coexist, stack or refresh through stance toggles. Palm Strike is a bridge and grants neither reward. Normal Haste/Evasion caps and existing ordinary attack scheduling apply. There are no permanent speed bonuses or ordinary-damage penalties.

Bruiser Stance retains +10% Physical Defense and gives Dirty Jab an optional reaction. Its landed, caster-owned Rattled opens a four-second brace against that enemy. The first direct hostile hit from that same enemy that deals actual HP damage arms Retaliation, adding eight total power to the next Uppercut or Haymaker spending that exact Rattled before it expires. The bonus is captured and consumed at paid activation, including later misses or interruption. It grants no free attack, STM refund, Guard or threat. It triggers once per opener and at most once per eighteen seconds. Self/friendly, periodic, reflected, temporary-HP-only and other-enemy damage cannot arm it. Immediate spenders retain their normal damage; waiting is optional.

Stance changes, invalid equipment, target transfer and combat end clear earned stance benefits. Clearing benefits never resets trigger cooldowns. Global stance exclusivity remains. Neither stance reduces ordinary damage, and neither requires another perk to supply its advertised effect.

Controlled Breathing and Catch Your Breath share one twelve-second refund gate, checked once from the completed activation summary. Restore two STM only when the correct spender lands, never more than STM actually paid after discounts. Controlled Breathing qualifies Finishing Combination spending Opening/Finish or Perfect Form spending Finish; Catch Your Breath qualifies Uppercut/Haymaker spending the owner's Rattled. A two-hit finisher pays out once if either impact lands. Payback stores one discount after positive direct hostile HP damage while in hostile combat, lasts eight seconds and triggers at most once per eighteen seconds. It applies to the next positive-cost Brawler activation, cannot reduce cost below zero, and excludes self/friendly, periodic and reflected damage. Clear it on invalid grip/combat end without resetting its cooldown. It grants no damage, setup or automatic retaliation.

All activated damage uses the captured main hand. A second Power Glove does not add an ability copy. Finishing Combination has two half-D impacts with independent hit/crit rolls, one split Form budget, at most one generic weapon proc per target/cast and one generic critical-trigger notification per cast. Ordinary paired rolls retain normal native per-hand properties and triggers. Cross-trained abilities keep their original skill and impact count; their hostile activation breaks Unarmed setup.

Low Kick reduces ordinary Physical output by ten percent for six/eight seconds, with strongest-live reapplication rather than stacking copies. It does not change attack cadence, Force/ability damage or control immunity. Rattled and Low Kick are short soft disruption, leaving Katar's sustained control and Guard roles distinct. Both openers work without the rank-5 window upgrades; both first spenders work without the rank-10 power upgrades.

## Implementation approach

The implementation adds skill 51, base item 541, two perk categories, twenty perks and twenty-two active ranks. Existing glove armor and Katar definitions retain their original behavior. Power Gloves use normal hand-slot weapon properties, skill mappings, enhancements, market categories and appearance selection.

Constructed-droid instruction items are outside this implementation. Unarmed perks leave droid AI slot metadata unset so incomplete instruction support is not exposed.

Shared combat reads generic technique metadata and attributed stats. It captures paid setup/main-hand profiles, consumes optional weaves on ordinary attempts, keeps source-owned Rattled separate between brawlers and gates refunds, reactive windows and retaliation once per activation. Native animation playback never creates damage. The original animation registrations and existing source clips are preserved.

The Unarmed tab is registered as a bespoke weapon implementation so generic regeneration cannot overwrite its mechanics. Separate files define each active ability. New feats, spells, TLK entries, thirty distinct icon families and native animations accompany the server code.

Tressa Kade in Offices & Labs offers the Perfect Form trial. Borrik Sen in the CZ-220 Hangar offers the Haymaker trial. These independent rank-50 quests count actual paid, landed techniques against eligible level-45-or-higher hostile NPCs. Counters belong to the accepting player and persist through save/reload. Trainer turn-in awards the relevant mastery achievement and unlocks its purchasable capstone. No character-build migration or refund is added.

## Balance and release validation

Run node tools/AnalyzeUnarmedBalance.mjs for reproducible finite-STM comparisons with Katar Scrapper. Match skill, attributes, equipment tier, acquisition/upgrade costs and external support. The rank-50 weapon reference remains DMG19 / Delay220; Doublehand III gives effective single D31, while Dual Wield III uses two ordinary D19 profiles. Either full line plus the Armor investment costs 69 SP. Both lines alone cost 120 SP and do not double the shared rotation; evaluate the higher-cost hybrid separately.

The seeded model uses 128 trials per build/case and includes the reactive stance timing. At reference settings, Martial Artist produces 98.5% of Katar Scrapper's sustained damage with one weapon and 99.2% with two; Brawler produces 92.4% and 91.7%. Across 24 core cases, Martial Artist ranges from 93.1% to 107.3% and Brawler from 90.2% to 98.0%. These comparisons exclude defensive value and capstones, so they support the initial damage budget rather than certify overall balance. Brawler trades sustained output for personal resilience and larger individual hits. The 216 resource/rotation stress cases, 24 incoming-damage cases and immediate-versus-counter policies are sensitivity checks; actual execution and hybrids still require engine coverage and player testing.

The model must sample the ordinary attempt that weaves, including its miss chance and timing. It must not grant a free hit or assume every opener produces a full combination. Dirty Jab's setup is more reliable than the former incoming-attack window, so its spender budget remains restrained. Include finite/capped STM, expiring state, one-second action spacing, global Haste cap, stance-off choices and independent finisher misses. Report animation/cast suppression, geometry, mitigation, enhancement procs, defensive value, capstone DPS and optimized hybrids as unverified rather than silently assigning them zero gameplay cost.

The normal rank-50 ability bonus is +7 per activated impact. At effective D31, a fully sequenced Palm Strike has power86, Finishing Combination has two power50 impacts, Uppercut has power102, and either setup-enhanced capstone has power110 before the normal damage formula/mitigation. An earned Retaliation adds eight more total power to its qualifying Uppercut/Haymaker. The ordinary weave has one real D31+16 power roll and its ordinary critical rules; it is not another activated hit. Ordinary rank-50 critical baseline is ten percent at equal relevant stats; activated baseline is five percent. These figures are attack power, not final damage or proof of burst balance.

Before release, validate levels 2/5/8/10/18/20/22/30/32/38/40/45/50, cold openings, short-lived targets, six/twelve-second burst, low STM, slow/high-Haste conditions, allied buffs, single/paired gear and equal-investment cross-training. Check that both lines provide their core payoff at the first purchase, and that every enhancement has an already-unlocked consumer.

Required engine coverage: native per-hand profiles/misses/criticals/deflection/Guard/mitigation; queued and limited-roll budgets; weave miss/expiry/duplicate-callback behavior; caster-owned Rattled with multiple brawlers, spending/expiry/source cleanup and immunity; correct paid-cost refunds and Payback exclusions; target/equipment/stance/area/death/combat-end invalidation; global/shared cooldowns; starter acquisition, skill-zero requirements, XP and enhancement costs; repeated owned-item equip/attack/upgrade/save/reload cycles. Include real private-desktop screenshots of ordinary and activated poses, body families and glove/weapon combinations. Deploy matching server, HAK/TLK and repacked module together after validation.

The Bible is the source of truth for all 36 implemented ranks. Other sheets retain their existing content apart from the added Power Glove equipment/recipe rows and required workbook formatting. The implementation passes focused unit/content and live engine checks, including actual paid sequences, missed spenders/weaves, source ownership, reactive stances and repeated mastery persistence. Defensive value, optimized hybrids and PvP remain player-testing work. Deploy matching server, HAK/TLK and repacked module together.
