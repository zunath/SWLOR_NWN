# Crafting revamp: the research Experiment

Command: `SWLOR.CraftSim research --runs 400 --adaptive-runs 40 --seed 1 --out design/testing/crafting-revamp/research-experiment-report.md`

## Inputs

- Seed 1; 400 experiments per case for the focused player and 40 for the adaptive player. Each experiment's dice come from its own stream of the seed.
- Starting values (`ExperimentRules`): 6 trials; Test chance 60% plus one point per Fabrication rank past the recipe tier's required rank, up to 15; Refine Method +15 points for the next Test at 30 CP; Analyze 5% of the job's time per piece of spare evidence, capped at 25%; evidence pays today's dice minimum at 1 and maximum at 6.
- CP is gear CP plus 0.75 per rank with no rank passive (the Experiment's only CP sink is Refine Method). Gear: no gear = 0 CP; mid gear = 26 CP; best gear = 37 CP.
- Focused player: at a random level every Test goes to one chosen candidate; at a fixed level every Test banks evidence and the last trial analyzes it; it refines its method before every Test it can afford.
- Adaptive player: `AdaptivePolicy` over the previews (depth 3), valuing evidence only.
- Time: 6.0 s per action (an estimate until the client evidence measures it) plus 30.0 s per job for choosing the recipe and completing it.

## Length

Target: 5-8 actions (research experiment). Actions are the player's turns: Tests, Analyzes and Refine Methods (Conclude would only shorten it). Research level 1 stands for the fixed levels and 3 for the random levels (3, 5 and 8).

| Level kind | Tier | Rank | Gear | CP | Focused: mean actions | Focused: Refines | Adaptive: mean actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed (1) | 1 | 10 | no gear | 7 | 6.0 | 0.00 | 6.0 | Passed |
| Fixed (1) | 1 | 10 | mid gear | 33 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 1 | 10 | best gear | 44 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 2 | 20 | no gear | 15 | 6.0 | 0.00 | 6.0 | Passed |
| Fixed (1) | 2 | 20 | mid gear | 41 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 2 | 20 | best gear | 52 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 3 | 30 | no gear | 22 | 6.0 | 0.00 | 6.0 | Passed |
| Fixed (1) | 3 | 30 | mid gear | 48 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 3 | 30 | best gear | 59 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | no gear | 30 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | mid gear | 56 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | best gear | 67 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 5 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 5 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 5 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 1 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 1 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 1 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 1 | 10 | no gear | 7 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3) | 1 | 10 | mid gear | 33 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 1 | 10 | best gear | 44 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 2 | 20 | no gear | 15 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3) | 2 | 20 | mid gear | 41 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 2 | 20 | best gear | 52 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 3 | 30 | no gear | 22 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3) | 3 | 30 | mid gear | 48 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 3 | 30 | best gear | 59 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 4 | 40 | no gear | 30 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 4 | 40 | mid gear | 56 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 4 | 40 | best gear | 67 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 5 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 5 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 5 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 1 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 1 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 1 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 3 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3) | 3 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3) | 3 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |

Verdict: every case takes 5-8 actions (research experiment) for both players.

## Bonus size against today's dice (random levels 3, 5 and 8)

Today a random level rolls one of three bonuses by weight 300:400:400 and then its die. The Experiment lets the player choose the candidate and sizes it by evidence, never above today's dice maximum. "Mean size" counts an experiment with no evidence as 0 (it falls back to today's roll); "Mean size with evidence" leaves those out.

| Candidate | Tier | Rank | Gear | Refine | Test chance | Mean evidence | No evidence | Mean size | Mean size with evidence | Today: dice average | Experiment / dice average | Reaches the maximum |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Licensed Runs | 1 | 10 | no gear | yes | 60% | 3.60 | 0.2% | 2.06 | 2.07 | 2.0 | 1.03x | 22% |
| Licensed Runs | 1 | 10 | no gear | no | 60% | 3.60 | 0.2% | 2.06 | 2.07 | 2.0 | 1.03x | 22% |
| Licensed Runs | 1 | 10 | mid gear | yes | 60% | 3.73 | 0.2% | 2.12 | 2.13 | 2.0 | 1.06x | 28% |
| Licensed Runs | 1 | 10 | best gear | yes | 60% | 3.70 | 0.2% | 2.09 | 2.10 | 2.0 | 1.05x | 24% |
| Licensed Runs | 3 | 30 | no gear | yes | 60% | 3.57 | 0.8% | 2.06 | 2.08 | 2.0 | 1.04x | 24% |
| Licensed Runs | 3 | 30 | no gear | no | 60% | 3.57 | 0.8% | 2.06 | 2.08 | 2.0 | 1.04x | 24% |
| Licensed Runs | 3 | 30 | mid gear | yes | 60% | 3.73 | 0.2% | 2.11 | 2.12 | 2.0 | 1.06x | 28% |
| Licensed Runs | 3 | 30 | best gear | yes | 60% | 3.69 | 0.0% | 2.09 | 2.09 | 2.0 | 1.05x | 26% |
| Licensed Runs | 5 | 50 | no gear | yes | 60% | 3.85 | 0.5% | 2.18 | 2.19 | 2.0 | 1.10x | 35% |
| Licensed Runs | 5 | 50 | no gear | no | 60% | 3.69 | 1.0% | 2.10 | 2.12 | 2.0 | 1.06x | 30% |
| Licensed Runs | 5 | 50 | mid gear | yes | 60% | 3.82 | 0.0% | 2.16 | 2.16 | 2.0 | 1.08x | 27% |
| Licensed Runs | 5 | 50 | best gear | yes | 60% | 3.89 | 0.2% | 2.20 | 2.21 | 2.0 | 1.10x | 31% |
| Licensed Runs | 1 | 50 | no gear | yes | 75% | 4.64 | 0.0% | 2.54 | 2.54 | 2.0 | 1.27x | 57% |
| Licensed Runs | 1 | 50 | no gear | no | 75% | 4.47 | 0.0% | 2.46 | 2.46 | 2.0 | 1.23x | 50% |
| Licensed Runs | 1 | 50 | mid gear | yes | 75% | 4.75 | 0.0% | 2.60 | 2.60 | 2.0 | 1.30x | 62% |
| Licensed Runs | 1 | 50 | best gear | yes | 75% | 4.83 | 0.0% | 2.65 | 2.65 | 2.0 | 1.33x | 66% |
| Credit Reduction | 1 | 10 | no gear | yes | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Credit Reduction | 1 | 10 | no gear | no | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Credit Reduction | 1 | 10 | mid gear | yes | 60% | 3.73 | 0.2% | 5.86 | 5.87 | 5.5 | 1.07x | 5% |
| Credit Reduction | 1 | 10 | best gear | yes | 60% | 3.70 | 0.2% | 5.82 | 5.84 | 5.5 | 1.06x | 6% |
| Credit Reduction | 3 | 30 | no gear | yes | 60% | 3.57 | 0.8% | 5.62 | 5.66 | 5.5 | 1.03x | 4% |
| Credit Reduction | 3 | 30 | no gear | no | 60% | 3.57 | 0.8% | 5.62 | 5.66 | 5.5 | 1.03x | 4% |
| Credit Reduction | 3 | 30 | mid gear | yes | 60% | 3.73 | 0.2% | 5.87 | 5.88 | 5.5 | 1.07x | 5% |
| Credit Reduction | 3 | 30 | best gear | yes | 60% | 3.69 | 0.0% | 5.82 | 5.82 | 5.5 | 1.06x | 7% |
| Credit Reduction | 5 | 50 | no gear | yes | 60% | 3.85 | 0.5% | 6.08 | 6.11 | 5.5 | 1.11x | 9% |
| Credit Reduction | 5 | 50 | no gear | no | 60% | 3.69 | 1.0% | 5.83 | 5.89 | 5.5 | 1.07x | 8% |
| Credit Reduction | 5 | 50 | mid gear | yes | 60% | 3.82 | 0.0% | 6.03 | 6.03 | 5.5 | 1.10x | 7% |
| Credit Reduction | 5 | 50 | best gear | yes | 60% | 3.89 | 0.2% | 6.12 | 6.14 | 5.5 | 1.12x | 6% |
| Credit Reduction | 1 | 50 | no gear | yes | 75% | 4.64 | 0.0% | 7.40 | 7.40 | 5.5 | 1.35x | 22% |
| Credit Reduction | 1 | 50 | no gear | no | 75% | 4.47 | 0.0% | 7.09 | 7.09 | 5.5 | 1.29x | 17% |
| Credit Reduction | 1 | 50 | mid gear | yes | 75% | 4.75 | 0.0% | 7.61 | 7.61 | 5.5 | 1.38x | 25% |
| Credit Reduction | 1 | 50 | best gear | yes | 75% | 4.83 | 0.0% | 7.75 | 7.75 | 5.5 | 1.41x | 27% |
| Time Reduction | 1 | 10 | no gear | yes | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Time Reduction | 1 | 10 | no gear | no | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Time Reduction | 1 | 10 | mid gear | yes | 60% | 3.73 | 0.2% | 5.86 | 5.87 | 5.5 | 1.07x | 5% |
| Time Reduction | 1 | 10 | best gear | yes | 60% | 3.70 | 0.2% | 5.82 | 5.84 | 5.5 | 1.06x | 6% |
| Time Reduction | 3 | 30 | no gear | yes | 60% | 3.57 | 0.8% | 5.62 | 5.66 | 5.5 | 1.03x | 4% |
| Time Reduction | 3 | 30 | no gear | no | 60% | 3.57 | 0.8% | 5.62 | 5.66 | 5.5 | 1.03x | 4% |
| Time Reduction | 3 | 30 | mid gear | yes | 60% | 3.73 | 0.2% | 5.87 | 5.88 | 5.5 | 1.07x | 5% |
| Time Reduction | 3 | 30 | best gear | yes | 60% | 3.69 | 0.0% | 5.82 | 5.82 | 5.5 | 1.06x | 7% |
| Time Reduction | 5 | 50 | no gear | yes | 60% | 3.85 | 0.5% | 6.08 | 6.11 | 5.5 | 1.11x | 9% |
| Time Reduction | 5 | 50 | no gear | no | 60% | 3.69 | 1.0% | 5.83 | 5.89 | 5.5 | 1.07x | 8% |
| Time Reduction | 5 | 50 | mid gear | yes | 60% | 3.82 | 0.0% | 6.03 | 6.03 | 5.5 | 1.10x | 7% |
| Time Reduction | 5 | 50 | best gear | yes | 60% | 3.89 | 0.2% | 6.12 | 6.14 | 5.5 | 1.12x | 6% |
| Time Reduction | 1 | 50 | no gear | yes | 75% | 4.64 | 0.0% | 7.40 | 7.40 | 5.5 | 1.35x | 22% |
| Time Reduction | 1 | 50 | no gear | no | 75% | 4.47 | 0.0% | 7.09 | 7.09 | 5.5 | 1.29x | 17% |
| Time Reduction | 1 | 50 | mid gear | yes | 75% | 4.75 | 0.0% | 7.61 | 7.61 | 5.5 | 1.38x | 25% |
| Time Reduction | 1 | 50 | best gear | yes | 75% | 4.83 | 0.0% | 7.75 | 7.75 | 5.5 | 1.41x | 27% |

### Expected supply per random level

Today one random level adds, on average, the weighted mix below. Under the Experiment the player picks the bonus, so a player who always wants one kind gets that kind's size every time and none of the others. The columns give each choice's mean size across the focused cases above (with Refine Method, evidence gathered) and the change against today's expected amount of that kind per random level.

| If the player always chooses | Today: expected per random level | Experiment: mean size across the cases above | Change in that kind's supply | Dice maximum |
| --- | --- | --- | --- | --- |
| Licensed Runs | 0.55 runs | 2.24 runs | 4.1x (+311%) | 3 runs |
| Credit Reduction | 2.00% | 6.31% | 3.2x (+216%) | 10% |
| Time Reduction | 2.00% | 6.31% | 3.2x (+216%) | 10% |

The power ceiling is unchanged: one bonus per random level and never above the dice maximum. What changes is that the bonus is chosen and averages about the same as the die of the chosen kind, instead of being a one-in-three draw. Total supply across the three kinds rises when everyone picks the kind they value most, and falls back toward today's if the player mixes kinds across levels 3, 5 and 8.

## Time cut (fixed levels)

All of a fixed level's evidence is spare. The focused player Tests and Analyzes with the last trial. The cap is 25% of the job.

| Tier | Rank | Gear | Test chance | Mean evidence analyzed | Mean time cut | Reaches the cap |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | no gear | 60% | 2.99 | 15.0% | 8% |
| 1 | 10 | mid gear | 60% | 3.21 | 16.1% | 9% |
| 1 | 10 | best gear | 60% | 3.05 | 15.2% | 8% |
| 2 | 20 | no gear | 60% | 3.01 | 15.1% | 8% |
| 2 | 20 | mid gear | 60% | 3.04 | 15.2% | 8% |
| 2 | 20 | best gear | 60% | 3.13 | 15.7% | 10% |
| 3 | 30 | no gear | 60% | 3.03 | 15.2% | 10% |
| 3 | 30 | mid gear | 60% | 3.12 | 15.6% | 9% |
| 3 | 30 | best gear | 60% | 3.14 | 15.7% | 9% |
| 4 | 40 | no gear | 60% | 3.16 | 15.8% | 10% |
| 4 | 40 | mid gear | 60% | 3.07 | 15.4% | 9% |
| 4 | 40 | best gear | 60% | 3.25 | 16.2% | 13% |
| 5 | 50 | no gear | 60% | 3.18 | 15.9% | 12% |
| 5 | 50 | mid gear | 60% | 3.27 | 16.4% | 12% |
| 5 | 50 | best gear | 60% | 3.28 | 16.4% | 14% |
| 1 | 50 | no gear | 75% | 3.93 | 19.6% | 32% |
| 1 | 50 | mid gear | 75% | 4.00 | 20.0% | 31% |
| 1 | 50 | best gear | 75% | 4.07 | 20.4% | 33% |
| 3 | 50 | no gear | 75% | 3.90 | 19.5% | 26% |
| 3 | 50 | mid gear | 75% | 4.02 | 20.1% | 33% |
| 3 | 50 | best gear | 75% | 4.06 | 20.3% | 37% |

## Fabrication XP per real hour

XP is the experiment (100% of the chart XP at full effort) plus completion (50%), each raised 10% per research level reached; the chart XP is `Skill.GetDeltaXP(recipe level - Fabrication rank)`, 0 once the recipe is 5 or more ranks below the researcher. The values are named constants in `ResearchXpRules`, in one place, so a ceiling can be applied there.

Each row is the best case for a tier: the researcher is at the tier's first rank and researches the highest-level recipe of the tier, which is where the chart pays most. XP per hour assumes every job slot is kept busy (one at ranks 10-24, two from 25, three from 50), each job restarted the moment it completes, with the experiment's mean time cut. A job's timer starts before its experiment opens, so the experiment runs inside the job's length; the researcher still plays only one experiment at a time, so a round takes at least the active time per job (the experiment's actions plus the overhead) times the concurrent jobs.

| Tier | Rank | Recipe level | Research level | Concurrent jobs | Chart XP | Experiment XP | Completion XP | Job length (after cut) | Active time per job | XP per real hour |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 9 | 1 | 1 | 450 | 495 | 247 | 2.8 min | 1.2 min | 15909 |
| 1 | 10 | 9 | 5 | 1 | 450 | 675 | 337 | 1.4 h | 1.2 min | 729 |
| 1 | 10 | 9 | 10 | 1 | 450 | 900 | 450 | 4.7 h | 1.2 min | 288 |
| 2 | 20 | 19 | 1 | 1 | 450 | 495 | 247 | 13.0 min | 1.2 min | 3432 |
| 2 | 20 | 19 | 5 | 1 | 450 | 675 | 337 | 6.4 h | 1.2 min | 159 |
| 2 | 20 | 19 | 10 | 1 | 450 | 900 | 450 | 21.4 h | 1.2 min | 63 |
| 3 | 30 | 29 | 1 | 2 | 450 | 495 | 247 | 31.5 min | 1.2 min | 2823 |
| 3 | 30 | 29 | 5 | 2 | 450 | 675 | 337 | 15.6 h | 1.2 min | 130 |
| 3 | 30 | 29 | 10 | 2 | 450 | 900 | 450 | 52.7 h | 1.2 min | 51 |
| 4 | 40 | 39 | 1 | 2 | 450 | 495 | 247 | 59.6 min | 1.2 min | 1495 |
| 4 | 40 | 39 | 5 | 2 | 450 | 675 | 337 | 29.3 h | 1.2 min | 69 |
| 4 | 40 | 39 | 10 | 2 | 450 | 900 | 450 | 98.7 h | 1.2 min | 27 |
| 5 | 50 | 52 | 1 | 3 | 750 | 825 | 412 | 1.6 h | 1.3 min | 2316 |
| 5 | 50 | 52 | 5 | 3 | 750 | 1125 | 562 | 47.9 h | 1.3 min | 106 |
| 5 | 50 | 52 | 10 | 3 | 750 | 1500 | 750 | 159.1 h | 1.3 min | 42 |

### Against at-level Fabrication crafting

At level (recipe level equal to rank), chart XP is 600. A craft of 15 actions (the middle of the 10-20 target) at 6.0 s each takes 90.0 s and pays 24000 XP per hour at 0% quality, 36000 at 50% and 48000 at 100% (`CraftOutcomeRules.CalculateXp`, no blueprint, not a first craft).

The highest research row above is 15909 XP per hour (tier 1, rank 10, recipe level 9, research level 1, 1 concurrent job(s)), 66% of the 0%-quality at-level crafting rate and 44% of the 50%-quality rate. Research XP falls with research level because the job length grows with the square of the level while the XP grows by 10% per level.

## Limitations

- Seconds per action and the per-job overhead are estimates; the client evidence replaces them. XP per hour scales directly with them.
- The focused player is a stated strategy, not a search for the best play. The adaptive player values evidence only. A real player who mixes candidates or skips Refine Method will land between the rows.
- Tier 1-5 rank cases use the lowest rank that can research the tier and rank 50. Ranks between those add up to 15 points of Test chance (one per rank past the tier's required rank), which raises evidence a little and leaves the length unchanged.
- The recipe cost modifier (0.2 on some food) lengthens a job by up to that share and is not modeled; blueprint time reductions (up to 30%) shorten it and are not modeled.
- Perks are not modeled; the Researcher line is a draft (`fabrication-researcher-perk-line-draft.md`).
- No client run: the window is untested in the client.
