# Twin Pistol: Discord feedback draft

Each section below is one Discord message under 2,000 characters. Effects are proposed maximum-rank values; [the implementation plan](TwinPistolImplementationPlan.md) and Design Bible contain full progression.

## Message 1 of 5

**Twin Pistol proposal — updated for cross-skilling**

A separate 0-50 weapon skill within the normal 400-rank cap; no Pistol prerequisite. Ordinary Twin Pistol attacks/training use a real main-hand pistol and simulated companion, each with its own damage, accuracy, upgrades and appearance. Both weapons contribute to attack delay.

**Barrage:** sustained pressure and stamina management.
**Crossfire:** reposition, earn Momentum, then spend it on a stronger attack.

All perk ranks support cross-skilling. Active attacks produce one impact per eligible equipped attack weapon (maximum two); a single/two-handed weapon gives one, a shield gives none. Each impact keeps its weapon DMG/type; ability scaling remains Twin Pistol PER/AGI.

Each standalone line: 10 perks, 18 ranks, 60 SP. Handling perks are mutually exclusive purchases at every rank:
```text
Matched Rhythm I/II/III | +3/6/8% Haste with any weapon
Paired Handling I/II/III| +10/20/25% ordinary weapon damage
Both: rank 8/22/45; SP 2/4/4 per purchase
```
Maximum legal combined investment: 110 SP; optional Armor Dual Wield adds 9. Neither handling choice blocks later unrelated perks.

Tables use maximum rank. D = each impact source weapon's DMG before normal calculations. STM/CD = stamina/cooldown. Values remain proposed; implementation and validation are in progress.

## Message 2 of 5

**Barrage — attacks and handling**
```text
Perk / unlocks / total SP | Maximum-rank effect
-------------------------+----------------------------------------
Paired Volley            | One enemy, each weapon D+26.
2/12/28/40; 12 SP         | STM/CD: 10 / 18s.

Alternating Fire         | Every 3 ordinary landed weapon hits:
5; 2 SP                  | restore 1 STM, at most once per 6s.

Matched Rhythm           | +8% Haste with any weapon.
8/22/45; 10 SP            | Cannot purchase Paired Handling.

Focused Burst            | One enemy, each weapon D+34.
10/30/38; 10 SP           | STM/CD: 10 / 24s.

Balanced Grip            | +5 ACC to any eligible off-hand
15; 4 SP                 | attack weapon. No shield bonus.

Controlled Burst         | One enemy, each weapon floor(.75D)+13.
18/32; 6 SP              | Restore 3 STM once if any impact hits.
                         | STM/CD: 9 / 30s.
```
Volley ranks: D+4/10/18/26, costing 3/5/8/10 STM. Focused ranks: D+10/22/34, costing 4/8/10 STM. Volley attacks more frequently; Focused gives more damage per cast and per STM. Separate cooldown groups.

Matched Rhythm changes ordinary attack speed, not ability cooldowns, and respects the global Haste cap.

## Message 3 of 5

**Barrage — stance and finisher**
```text
Perk / unlock / SP | Maximum-rank effect
-------------------+---------------------------------------------
Akimbo Stance      | Any weapon: +10% Haste. Ordinary hostile
20; 4 SP           | hits on one foe add +2% per stack, max 5
                   | (+20% from stance); one stack per 2s.
                   | Clear on target change or 6s without a hit.
                   | ALL ordinary weapon damage x0.85.
                   | STM/CD: 0 / 30s.

Converging Fire    | After 3 ordinary weapon hits on one foe:
25; 4 SP           | +5% ordinary damage to it for 6s.
                   | Hits refresh; target changes clear.

Cool Under Fire   | Direct hostile damage: next Twin-skill
35; 2 SP           | ability costs 2 less STM. One charge,
                   | lasts 10s; 10s trigger cooldown.

Twin Suns         | One enemy, each weapon D+20. Next 6
50 + mastery; 6 SP | ordinary attacks with any weapon gain
                   | +25% damage, expiring after 12s.
                   | Misses use charges. STM/CD: 15 / 90s.
```
Stance damage penalties apply after additive ordinary-damage bonuses and before mitigation. Activated damage is unchanged. Weapon swaps preserve stance, stacks, buffs and trigger cooldowns. Other Haste sources are additional, subject to the global cap. Stances remain exclusive.

## Message 4 of 5

**Crossfire — movement and Momentum**
```text
Perk / unlocks / SP     | Maximum-rank effect
-----------------------+------------------------------------------
Blaster Slide          | Retreat 4m, then each weapon D+26 at
2/12/28/40; 12 SP       | selected foe. Needs 2m safe movement.
                       | STM/CD: 10 / 18s.

Momentum               | Slide/Rush moving 2m+ and landing a
5; 2 SP                | hostile hit: +1 charge, max 3, 12s.
                       | Each: +2 ordinary ACC with any weapon.

Paired Handling        | +25% ordinary weapon damage.
8/22/45; 10 SP         | Cannot purchase Matched Rhythm.

Grapple Shot           | Close up to 6m, stop 1.5m from foe.
10/30/38; 10 SP         | Each weapon D+20, +6 per Momentum.
                       | Works already close or at zero charges.
                       | STM/CD: 8 / 24s.

Changing Angles        | Crossfire move 2m+ and hostile hit:
15; 4 SP               | next Crossfire ability +5 ACC.
                       | One charge, 8s; 10s trigger cooldown.

Pistol Rush            | Free ground dash 2-6m; up to 3 foes
18/32; 6 SP            | in a 4m-wide travel line. Each weapon
                       | floor(.60D)+12. STM/CD: 10 / 30s.
```
Slide/Rush earn at most one Momentum per cast. Empty-space Rush is allowed but earns none. All movement respects roots, collision and terrain, with a shared 2s movement lockout. No invulnerability or cleanse.

## Message 5 of 5

**Crossfire — stance, sustain and finisher**
```text
Perk / unlock / SP | Maximum-rank effect
-------------------+---------------------------------------------
Crossfire Stance   | Any weapon: +10% Haste, +10% movement,
20; 4 SP           | 18s Momentum. ALL ordinary damage x0.90.
                   | Activated damage unchanged. STM/CD: 0 / 30s.

Follow Through    | Grapple/Final landing any hit restores
25; 4 SP           | 1 STM per captured Momentum spent, max 3.
                   | Once per cast; 12s trigger cooldown.

Second Wind       | Direct hostile accuracy-vs-evasion miss:
35; 2 SP           | +1 Momentum, at most once per 10s.
                   | Requires Momentum; other avoidance excluded.

Final Salvo       | Stationary, one foe, each weapon D+20;
50 + mastery; 6 SP | +6 per Momentum spent, max D+38.
                   | STM/CD: 15 / 90s.
```
Grapple/Final capture charges at cast start and spend those on valid paid completion, even on a miss. Later-earned charges are preserved. Swaps preserve charges/expiry; combat end clears them. Refunds/discounts retain at least 1 STM paid.

Shared cooldowns: Volley/Slide 18s; Focused/Grapple/Double Shot 24s; Controlled/Rush/Fan the Hammer 30s; both finishers share global Capstone 90s. Quick Draw stays independent. New multi-impact abilities cap generic weapon procs at one per target/cast and critical-trigger notifications at one per cast.

**Balance:** previous DPS tables are superseded. New handling values and cross-skill combinations need matched 6s/12s/120s checks, including saber/rifle builds, external Haste and per-hit effects. Feedback wanted on the speed/damage choice, stance tradeoffs and rotations.
