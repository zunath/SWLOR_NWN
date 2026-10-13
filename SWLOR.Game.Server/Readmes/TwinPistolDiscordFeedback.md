# Twin Pistol: Discord feedback draft

Each numbered section is one message, below 2,000 characters including its code fences. Copy a section's text and table, including the triple backticks used for [Discord code-block formatting](https://support.discord.com/hc/en-us/articles/210298617-Markdown-Text-101-Chat-Formatting-Bold-Italic-Underline). Full rank-by-rank values and implementation details are in [the implementation plan](TwinPistolImplementationPlan.md).

## Message 1 of 5

**Twin Pistol skill proposal — looking for feedback**

A separate 0–50 weapon skill, counting toward the normal 400-rank cap. No Pistol prerequisite. NWN cannot truly dual-wield ranged weapons, so the second blaster would be a simulated off-hand weapon with its own damage, accuracy, upgrades, and appearance. Both guns contribute to attack delay: equipping two does not simply double your firing rate.

Two perk lines:
- **Barrage:** sustained pressure against one enemy; steady fire and stamina management.
- **Crossfire:** repositioning attacks, Momentum, and a stationary finisher; lower uninterrupted damage buys mobility.

**Cost:** Each line has 10 perks with 18 purchasable ranks, costing 60 SP. Both complete lines cost 120 SP. Armor's existing Dual Wield costs another 9 SP if fully purchased. No Single Weapon/Doublehand damage bonuses while paired.

```text
Line              | Ranked ordinary-damage perk
------------------+---------------------------------------------
Barrage           | Matched Rhythm I-III
Crossfire         | Paired Handling I-III
Both progressions | Skill ranks 8/22/45; SP prices 2/4/4;
                  | +10/20/25% ordinary Twin shot damage.
```

These are separate purchases. If you own both, use the higher damage bonus; they do not stack. There is no shared perk line.

**Reading the tables:** effects below are at maximum perk rank. Rank lists show purchase unlocks; SP is the total for that perk. `D` is the firing gun's weapon damage rating, before normal combat calculations. `STM/CD` means stamina cost / cooldown. Each shot rolls separately.

## Message 2 of 5

**Barrage — sustained single-target damage**

```text
Perk / skill ranks / SP | Maximum-rank effect
-----------------------+----------------------------------------------
Paired Volley          | One shot per gun at one enemy: D+19 each.
2/12/28/40; 12 SP       | STM/CD: 12 / 18s.

Alternating Fire       | Every 3 landed ordinary Twin shots restores
5; 2 SP                | 1 STM, at most once per 6s. Misses don't reset.

Matched Rhythm I-III   | +25% ordinary Twin shot damage. Does not
8/22/45; 10 SP         | stack with Paired Handling.

Focused Burst          | One shot per gun at one enemy: D+34 each.
10/30/38; 10 SP         | STM/CD: 10 / 24s.

Balanced Grip          | +5 ACC to the off-hand blaster only.
15; 4 SP               |

Controlled Burst       | Two shots at one enemy: floor(0.75D)+13 each.
18/32; 6 SP             | Refund 3 STM if either hits. STM/CD: 9 / 30s.

Akimbo Stance          | +10% Haste. Ordinary hits on the same foe
                      | add +2% per stack, max 5 (+20% total).
                      | One stack per 2s; clear on target change or
                      | 6s without an ordinary hit. Maintain at 5.
                      | Ordinary Twin shots deal 15% less damage.
20; 4 SP               | STM/CD: 0 / 30s. One stance at a time.

Converging Fire        | After 3 ordinary hits on one enemy, +5%
25; 4 SP               | ordinary Twin damage to it for 6s. Hits
                       | refresh; switching enemies clears the bonus.

Cool Under Fire        | Taking direct hostile damage makes the next
35; 2 SP               | Twin ability cost 2 less STM. Lasts 10s;
                       | one charge; triggers at most once per 10s.

Twin Suns              | Two shots: D+20 each. Next 6 ordinary Twin
50 + mastery; 6 SP     | shots get +25% damage, expiring after 12s.
                       | Misses use charges. STM/CD: 15 / 90s.
```

## Message 3 of 5

**Crossfire — repositioning and Momentum**

```text
Perk / skill ranks / SP | Maximum-rank effect
-----------------------+----------------------------------------------
Blaster Slide          | Retreat 4m from an enemy, then fire both
2/12/28/40; 12 SP       | guns at it: D+26 each. Needs 2m safe travel.
                       | STM/CD: 10 / 18s.

Momentum               | Slide/Rush moving 2m+ and landing any hit
5; 2 SP                | grants 1 charge; max 3, lasts 12s. Each
                       | gives +2 ordinary-shot ACC. Grapple Shot
                       | and Final Salvo spend all charges.

Paired Handling I-III  | +25% ordinary Twin shot damage. Does not
8/22/45; 10 SP         | stack with Matched Rhythm.

Grapple Shot           | Close up to 6m, stopping 1.5m from a foe;
10/30/38; 10 SP         | fire both guns: D+20 each, plus 6 power per
                       | Momentum spent on each shot. Works without
                       | charges or when already close.
                       | STM/CD: 8 / 24s.

Pistol Rush            | Freely aimed ground dash, 2-6m. Fire both
18/32; 6 SP             | guns at up to 3 foes in its 4m-wide travel
                       | line: floor(0.60D)+12 per shot.
                       | STM/CD: 10 / 30s.
```

Slide/Rush grant at most one Momentum per cast, regardless of hits or targets. An empty-space dash is allowed but earns no charges. Charges expire and cannot be banked out of combat. Movement attacks share a 2s movement lockout and respect roots, terrain and blocked paths.

## Message 4 of 5

**Crossfire — follow-through perks and finisher**

```text
Perk / skill ranks / SP | Maximum-rank effect
-----------------------+----------------------------------------------
Changing Angles        | Landing a Crossfire attack after moving 2m+
15; 4 SP               | gives your next Crossfire ability +5 ACC.
                       | One charge, lasts 8s; 10s trigger cooldown.

Crossfire Stance       | +10% Haste, +10% Movement Speed; Momentum
20; 4 SP               | lasts 18s. Ordinary Twin shots deal 10%
                       | less damage. STM/CD: 0 / 30s.

Follow Through         | A Momentum spender landing any hit restores
25; 4 SP               | 1 STM per charge spent, max 3; once per cast,
                       | at most once per 12s. No Momentum refund.

Second Wind            | A hostile attack missing its accuracy vs.
                       | evasion roll grants
35; 2 SP               | 1 Momentum, at most once per 10s.

Final Salvo            | Stand still and fire both guns at one foe:
50 + mastery; 6 SP     | D+20 each, plus 6 power per Momentum spent
                       | on each shot (max D+38 each).
                       | STM/CD: 15 / 90s.
```

Evades are bonus fuel; Slide/Rush build Momentum reliably without them. Grapple/Final Salvo spend their captured charges even if the attacks miss. Final Salvo can fire with zero charges and never moves you. Existing stance exclusivity applies.

## Message 5 of 5

**Stacking rules and balance notes**

```text
Abilities                          | Shared cooldown
-----------------------------------+----------------------------
Paired Volley / Blaster Slide       | Twin Volley: 18s
Focused Burst / Grapple Shot        | Existing Double Shot: 24s
Controlled Burst / Pistol Rush      | Existing Fan the Hammer: 30s
Twin Suns / Final Salvo             | Global Capstone: 90s
```

- Quick Draw keeps its own cooldown if you also train Pistol. Existing Pistol abilities keep their skill scaling and normal shot count.
- Both Twin stances are exclusive with other stances, including Gambler. Haste respects the existing cap.
- The higher handling bonus adds to Converging Fire/Twin Suns. Stance ordinary-damage penalties apply afterward; activated damage is unchanged.
- Ordinary shots use their own gun's on-hit effects. New multi-shot abilities permit one weapon on-hit proc per enemy per cast and one notification to critical-trigger effects per cast. Refunds cannot multiply per impact.
- No Evasion reduction, crowd control, free extra attacks, or cooldown resets. Capstones take 2s to activate. Movement gives no invulnerability or automatic cleanse.

**Ceiling budget check:** at equal 69-SP investment, the 120s resource calculation with Akimbo already at five stacks puts Barrage at 103% of the Pistol critical build. Crossfire is 12% below that ceiling solo and 4% below it across three foes. Ramp startup and resets lower Barrage's actual output.

Those are formula/resource estimates, not playtest results. Wider stat/resource checks have larger gaps; capstone burst, low-level progression, PvP, movement, and cross-trained builds still need engine testing.

**Feedback wanted:** Do these roles feel distinct? Are the stance tradeoffs worthwhile? Does the build/spend movement loop sound useful? Which existing combinations would you try first?
