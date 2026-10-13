# Twin Pistol: Discord feedback draft

Each numbered section is one message, below 2,000 characters including its code fences. Copy a section's text and table, including the triple backticks used for [Discord code-block formatting](https://support.discord.com/hc/en-us/articles/210298617-Markdown-Text-101-Chat-Formatting-Bold-Italic-Underline). Full rank-by-rank values and implementation details are in [the implementation plan](TwinPistolImplementationPlan.md).

## Message 1 of 4

**Twin Pistol skill proposal — looking for feedback**

A separate 0–50 weapon skill, counting toward the normal 400-rank cap. No Pistol prerequisite. NWN cannot truly dual-wield ranged weapons, so the second blaster would be a simulated off-hand weapon with its own damage, accuracy, upgrades, and appearance. Both guns contribute to attack delay: equipping two does not simply double your firing rate.

Two perk lines:
- **Barrage:** sustained pressure against one enemy; steady fire and stamina management.
- **Crossfire:** spreading damage across two or three enemies; weaker against a lone target.

**Cost:** 10 SP shared training + 50 SP per line. One complete line costs 60 SP; both cost 110 SP. Armor's existing Dual Wield costs another 9 SP if fully purchased. No Single Weapon/Doublehand damage bonuses while paired.

```text
Shared perk       | Skill ranks | SP/rank | Effect
------------------+-------------+---------+----------------------
Paired Handling   | 8 / 22 / 45 | 2/4/4   | +10/20/25% ordinary
                  |             |         | Twin shot damage.
```

**Reading the tables:** effects below are at maximum perk rank. Rank lists show purchase unlocks; SP is the total for that perk. `D` is the firing gun's weapon damage rating, before normal combat calculations. `STM/CD` means stamina cost / cooldown. Each shot rolls separately.

## Message 2 of 4

**Barrage — sustained single-target damage**

```text
Perk / skill ranks / SP | Maximum-rank effect
-----------------------+----------------------------------------------
Paired Volley          | One shot per gun at one enemy: D+19 each.
2/12/28/40; 12 SP       | STM/CD: 12 / 18s.

Alternating Fire       | Every 3 landed ordinary Twin shots restores
5; 2 SP                | 1 STM, at most once per 6s. Misses don't reset.

Focused Burst          | One shot per gun at one enemy: D+34 each.
10/30/38; 10 SP         | STM/CD: 10 / 24s.

Balanced Grip          | +5 ACC to the off-hand blaster only.
15; 4 SP               |

Controlled Burst       | Two shots at one enemy: floor(0.75D)+13 each.
18/32; 6 SP             | Refund 3 STM if either hits. STM/CD: 9 / 30s.

Akimbo Stance          | +10% Haste; take 10% more damage.
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

## Message 3 of 4

**Crossfire — damage across small groups**

```text
Perk / skill ranks / SP | Maximum-rank effect
-----------------------+----------------------------------------------
Split Shot             | One shot per gun at 2 enemies: D+26 each.
2/12/28/40; 12 SP       | Solo: off-hand follow-up at 75% damage.
                       | STM/CD: 10 / 18s.

Efficient Fire         | A Crossfire-line ability landing any hit
5; 2 SP                | restores 1 STM, at most once per 8s.

Crossfire              | Up to 3 enemies, one shot each: D+20.
10/30/38; 10 SP         | Solo: add off-hand follow-up at 75% damage.
                       | At 2 foes, only 2 shots. STM/CD: 8 / 24s.

Changing Angles        | An ordinary Twin hit on a different enemy
15; 4 SP               | gives the next Crossfire ability +5 ACC.
                       | Lasts 8s; one charge; 10s trigger cooldown.

Sweeping Salvo         | 10m x 10m aimed cone, up to 3 enemies;
18/32; 6 SP             | 2 shots each: floor(0.75D)+13 per shot.
                       | STM/CD: 8 / 30s.

Crossfire Stance       | +10% Haste; deal 10% less damage to the
20; 4 SP               | primary enemy. STM/CD: 0 / 30s.

Wide Coverage          | +20% Crossfire-line ability damage when
25; 4 SP               | targeting at least 2 different enemies.

Clear Lanes            | Hitting 2 enemies with a Crossfire ability
35; 2 SP               | makes the next cost 2 less STM. One charge,
                       | lasts 10s; 12s trigger cooldown.

Two Fronts             | Two enemies: D+25 each; solo off-hand 75%.
50 + mastery; 6 SP     | Next 3 Crossfire abilities targeting 2+ foes
                       | gain +10% damage; expires after 18s.
                       | STM/CD: 15 / 90s.
```

## Message 4 of 4

**Stacking rules and balance notes**

```text
Abilities                          | Shared cooldown
-----------------------------------+----------------------------
Paired Volley / Split Shot          | Twin Volley: 18s
Focused Burst / Crossfire           | Existing Double Shot: 24s
Controlled Burst / Sweeping Salvo   | Existing Fan the Hammer: 30s
Twin Suns / Two Fronts              | Global Capstone: 90s
```

- Quick Draw keeps its own cooldown if you also train Pistol. Existing Pistol abilities keep their skill scaling and normal shot count.
- Both Twin stances are exclusive with other stances, including Gambler. Haste respects the existing cap.
- Shared training is purchased once. Its damage bonus adds to Converging Fire/Twin Suns. Wide Coverage and Two Fronts add to +30%, rather than multiplying.
- Ordinary shots use their own gun's on-hit effects. New multi-shot abilities permit one weapon on-hit proc per enemy per cast and one notification to critical-trigger effects per cast. Refunds cannot multiply per impact.
- No Evasion reduction, crowd control, free extra attacks, or cooldown resets. Capstones take 2s to activate; misses still consume their follow-up charges.

**Initial budget check:** at equal 69-SP investment, the representative 120s calculation puts Barrage at 98% of the existing Pistol critical build's single-target damage. Crossfire is 15% below Barrage solo, but 23% above it in total damage across three foes; about 5% above Pistol against those three foes.

Those are formula/resource estimates, not playtest results. Wider stat/resource checks have larger gaps; capstone burst, low-level progression, PvP, movement, and cross-trained builds still need engine testing.

**Feedback wanted:** Do the two roles feel useful? Are the stance drawbacks worth the benefits? Is Crossfire's solo fallback worthwhile without making Barrage redundant? Which existing perk combinations would you try first?
