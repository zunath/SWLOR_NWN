# Crafting revamp: the research Experiment

Command: `SWLOR.CraftSim research --runs 400 --adaptive-runs 40 --seed 1 --out design/testing/crafting-revamp/research-experiment-report.md`

## Inputs

- Seed 1; 400 experiments per case for the focused player and 40 for the adaptive player. Each experiment's dice come from its own stream of the seed.
- Starting values (`ExperimentRules`): 6 trials; Test chance 60% plus one point per Fabrication rank past the recipe tier's required rank, up to 15; Refine Method +15 points for the next Test at 30 CP; Analyze 5% of the job's time per piece of spare evidence; one more point of Test chance per 2 Fabrication Control and one more trial per 120 Craftsmanship, and 5 fewer points for each research tier above tier 2; capped at 25%; evidence pays today's dice minimum at 1 and maximum at 6.
- CP is gear CP plus 0.75 per rank with no rank passive (the Experiment's only CP sink is Refine Method). Gear: no gear = 0 CP; mid gear = 26 CP; best gear = 37 CP; fully modded gear = 37 CP.
- Focused player: at a random level every Test goes to one chosen candidate; at a fixed level every Test banks evidence and the last trial analyzes it; it refines its method before every Test it can afford.
- Adaptive player: `AdaptivePolicy` over the previews (depth 3), valuing evidence only.
- Time: 6.0 s per action (an estimate until the client evidence measures it) plus 30.0 s per job for choosing the recipe and completing it.

## Length

Target: 5-8 actions (research experiment). Actions are the player's turns: Tests, Analyzes and Refine Methods (Conclude would only shorten it). Research level 1 stands for the fixed levels and 3 for the random levels (3, 5 and 8), which are played both as a reduction decided by evidence and with licensed runs rolled (a fixed-style experiment).

| Level kind | Tier | Rank | Gear | CP | Focused: mean actions | Focused: Refines | Adaptive: mean actions | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Fixed (1) | 1 | 10 | no gear | 7 | 6.0 | 0.00 | 6.0 | Passed |
| Fixed (1) | 1 | 10 | mid gear | 33 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 1 | 10 | best gear | 44 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 1 | 10 | fully modded gear | 44 | 7.0 | 0.00 | 7.0 | Passed |
| Fixed (1) | 2 | 20 | no gear | 15 | 6.0 | 0.00 | 6.0 | Passed |
| Fixed (1) | 2 | 20 | mid gear | 41 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 2 | 20 | best gear | 52 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 2 | 20 | fully modded gear | 52 | 7.0 | 0.00 | 7.0 | Passed |
| Fixed (1) | 3 | 30 | no gear | 22 | 6.0 | 0.00 | 6.0 | Passed |
| Fixed (1) | 3 | 30 | mid gear | 48 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 3 | 30 | best gear | 59 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 3 | 30 | fully modded gear | 59 | 8.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | no gear | 30 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | mid gear | 56 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | best gear | 67 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 4 | 40 | fully modded gear | 67 | 9.0 | 2.00 | 8.0 | Outside (9.0) |
| Fixed (1) | 5 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 5 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 5 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 5 | 50 | fully modded gear | 74 | 9.0 | 2.00 | 8.0 | Outside (9.0) |
| Fixed (1) | 1 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 1 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 1 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 1 | 50 | fully modded gear | 74 | 7.0 | 0.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Fixed (1) | 3 | 50 | fully modded gear | 74 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), reductions | 1 | 10 | no gear | 7 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3), reductions | 1 | 10 | mid gear | 33 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 1 | 10 | best gear | 44 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 1 | 10 | fully modded gear | 44 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), reductions | 2 | 20 | no gear | 15 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3), reductions | 2 | 20 | mid gear | 41 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 2 | 20 | best gear | 52 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 2 | 20 | fully modded gear | 52 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 30 | no gear | 22 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3), reductions | 3 | 30 | mid gear | 48 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 30 | best gear | 59 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 30 | fully modded gear | 59 | 8.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 4 | 40 | no gear | 30 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 4 | 40 | mid gear | 56 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 4 | 40 | best gear | 67 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 4 | 40 | fully modded gear | 67 | 9.0 | 2.00 | 8.0 | Outside (9.0) |
| Random (3), reductions | 5 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 5 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 5 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 5 | 50 | fully modded gear | 74 | 9.0 | 2.00 | 8.0 | Outside (9.0) |
| Random (3), reductions | 1 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 1 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 1 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 1 | 50 | fully modded gear | 74 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), reductions | 3 | 50 | fully modded gear | 74 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), runs rolled | 1 | 10 | no gear | 7 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3), runs rolled | 1 | 10 | mid gear | 33 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 1 | 10 | best gear | 44 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 1 | 10 | fully modded gear | 44 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), runs rolled | 2 | 20 | no gear | 15 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3), runs rolled | 2 | 20 | mid gear | 41 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 2 | 20 | best gear | 52 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 2 | 20 | fully modded gear | 52 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 30 | no gear | 22 | 6.0 | 0.00 | 6.0 | Passed |
| Random (3), runs rolled | 3 | 30 | mid gear | 48 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 30 | best gear | 59 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 30 | fully modded gear | 59 | 8.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 4 | 40 | no gear | 30 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 4 | 40 | mid gear | 56 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 4 | 40 | best gear | 67 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 4 | 40 | fully modded gear | 67 | 9.0 | 2.00 | 8.0 | Outside (9.0) |
| Random (3), runs rolled | 5 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 5 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 5 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 5 | 50 | fully modded gear | 74 | 9.0 | 2.00 | 8.0 | Outside (9.0) |
| Random (3), runs rolled | 1 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 1 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 1 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 1 | 50 | fully modded gear | 74 | 7.0 | 0.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 50 | no gear | 37 | 7.0 | 1.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 50 | mid gear | 63 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 50 | best gear | 74 | 8.0 | 2.00 | 7.0 | Passed |
| Random (3), runs rolled | 3 | 50 | fully modded gear | 74 | 7.0 | 0.00 | 7.0 | Passed |

Verdict: outside 5-8 actions (research experiment): Fixed (1) tier 4 rank 40 fully modded gear (9.0 / 8.0); Fixed (1) tier 5 rank 50 fully modded gear (9.0 / 8.0); Random (3), reductions tier 4 rank 40 fully modded gear (9.0 / 8.0); Random (3), reductions tier 5 rank 50 fully modded gear (9.0 / 8.0); Random (3), runs rolled tier 4 rank 40 fully modded gear (9.0 / 8.0); Random (3), runs rolled tier 5 rank 50 fully modded gear (9.0 / 8.0).

## Reduction size against today's dice (random levels 3, 5 and 8)

Owner decision (October 2026): licensed runs stay random. When the experiment opens, the level rolls whether it grants licensed runs at today's weight (300 of 1100, or 310 of 1150 with an enhancement slot) and today's d3; the experiment then earns only the time cut, XP and the extra-bonus chance. Otherwise the level grants a reduction and evidence decides between credit reduction and time reduction and sizes the winner, never above today's d10. "Mean size" counts an experiment with no evidence as 0 (it falls back to today's roll between the two); "Mean size with evidence" leaves those out.

| Candidate | Tier | Rank | Gear | Refine | Test chance | Mean evidence | No evidence | Mean size | Mean size with evidence | Today: dice average | Experiment / dice average | Reaches the maximum |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Credit Reduction | 1 | 10 | no gear | yes | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Credit Reduction | 1 | 10 | no gear | no | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Credit Reduction | 1 | 10 | mid gear | yes | 66% | 4.06 | 0.2% | 6.41 | 6.43 | 5.5 | 1.17x | 8% |
| Credit Reduction | 1 | 10 | best gear | yes | 74% | 4.59 | 0.0% | 7.34 | 7.34 | 5.5 | 1.34x | 22% |
| Credit Reduction | 1 | 10 | fully modded gear | yes | 95% | 6.64 | 0.0% | 9.90 | 9.90 | 5.5 | 1.80x | 96% |
| Credit Reduction | 1 | 10 | fully modded gear | no | 95% | 6.64 | 0.0% | 9.90 | 9.90 | 5.5 | 1.80x | 96% |
| Credit Reduction | 3 | 30 | no gear | yes | 55% | 3.33 | 1.5% | 5.21 | 5.29 | 5.5 | 0.96x | 2% |
| Credit Reduction | 3 | 30 | no gear | no | 55% | 3.33 | 1.5% | 5.21 | 5.29 | 5.5 | 0.96x | 2% |
| Credit Reduction | 3 | 30 | mid gear | yes | 61% | 3.79 | 0.2% | 5.97 | 5.98 | 5.5 | 1.09x | 6% |
| Credit Reduction | 3 | 30 | best gear | yes | 69% | 4.24 | 0.0% | 6.74 | 6.74 | 5.5 | 1.22x | 15% |
| Credit Reduction | 3 | 30 | fully modded gear | yes | 92% | 6.50 | 0.0% | 9.82 | 9.82 | 5.5 | 1.79x | 92% |
| Credit Reduction | 3 | 30 | fully modded gear | no | 92% | 6.47 | 0.0% | 9.80 | 9.80 | 5.5 | 1.78x | 91% |
| Credit Reduction | 5 | 50 | no gear | yes | 45% | 2.96 | 1.5% | 4.61 | 4.68 | 5.5 | 0.85x | 2% |
| Credit Reduction | 5 | 50 | no gear | no | 45% | 2.83 | 2.2% | 4.38 | 4.48 | 5.5 | 0.81x | 1% |
| Credit Reduction | 5 | 50 | mid gear | yes | 51% | 3.30 | 0.0% | 5.16 | 5.16 | 5.5 | 0.94x | 2% |
| Credit Reduction | 5 | 50 | best gear | yes | 59% | 3.82 | 0.2% | 6.00 | 6.02 | 5.5 | 1.09x | 6% |
| Credit Reduction | 5 | 50 | fully modded gear | yes | 82% | 6.08 | 0.0% | 9.40 | 9.40 | 5.5 | 1.71x | 78% |
| Credit Reduction | 5 | 50 | fully modded gear | no | 82% | 5.80 | 0.0% | 9.05 | 9.05 | 5.5 | 1.65x | 66% |
| Credit Reduction | 1 | 50 | no gear | yes | 75% | 4.64 | 0.0% | 7.40 | 7.40 | 5.5 | 1.35x | 22% |
| Credit Reduction | 1 | 50 | no gear | no | 75% | 4.47 | 0.0% | 7.09 | 7.09 | 5.5 | 1.29x | 17% |
| Credit Reduction | 1 | 50 | mid gear | yes | 81% | 5.09 | 0.0% | 8.21 | 8.21 | 5.5 | 1.49x | 36% |
| Credit Reduction | 1 | 50 | best gear | yes | 89% | 5.48 | 0.0% | 8.96 | 8.96 | 5.5 | 1.63x | 56% |
| Credit Reduction | 1 | 50 | fully modded gear | yes | 95% | 6.61 | 0.0% | 9.87 | 9.87 | 5.5 | 1.79x | 94% |
| Credit Reduction | 1 | 50 | fully modded gear | no | 95% | 6.61 | 0.0% | 9.87 | 9.87 | 5.5 | 1.79x | 94% |
| Time Reduction | 1 | 10 | no gear | yes | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Time Reduction | 1 | 10 | no gear | no | 60% | 3.60 | 0.2% | 5.65 | 5.66 | 5.5 | 1.03x | 3% |
| Time Reduction | 1 | 10 | mid gear | yes | 66% | 4.06 | 0.2% | 6.41 | 6.43 | 5.5 | 1.17x | 8% |
| Time Reduction | 1 | 10 | best gear | yes | 74% | 4.59 | 0.0% | 7.34 | 7.34 | 5.5 | 1.34x | 22% |
| Time Reduction | 1 | 10 | fully modded gear | yes | 95% | 6.64 | 0.0% | 9.90 | 9.90 | 5.5 | 1.80x | 96% |
| Time Reduction | 1 | 10 | fully modded gear | no | 95% | 6.64 | 0.0% | 9.90 | 9.90 | 5.5 | 1.80x | 96% |
| Time Reduction | 3 | 30 | no gear | yes | 55% | 3.33 | 1.5% | 5.21 | 5.29 | 5.5 | 0.96x | 2% |
| Time Reduction | 3 | 30 | no gear | no | 55% | 3.33 | 1.5% | 5.21 | 5.29 | 5.5 | 0.96x | 2% |
| Time Reduction | 3 | 30 | mid gear | yes | 61% | 3.79 | 0.2% | 5.97 | 5.98 | 5.5 | 1.09x | 6% |
| Time Reduction | 3 | 30 | best gear | yes | 69% | 4.24 | 0.0% | 6.74 | 6.74 | 5.5 | 1.22x | 15% |
| Time Reduction | 3 | 30 | fully modded gear | yes | 92% | 6.50 | 0.0% | 9.82 | 9.82 | 5.5 | 1.79x | 92% |
| Time Reduction | 3 | 30 | fully modded gear | no | 92% | 6.47 | 0.0% | 9.80 | 9.80 | 5.5 | 1.78x | 91% |
| Time Reduction | 5 | 50 | no gear | yes | 45% | 2.96 | 1.5% | 4.61 | 4.68 | 5.5 | 0.85x | 2% |
| Time Reduction | 5 | 50 | no gear | no | 45% | 2.83 | 2.2% | 4.38 | 4.48 | 5.5 | 0.81x | 1% |
| Time Reduction | 5 | 50 | mid gear | yes | 51% | 3.30 | 0.0% | 5.16 | 5.16 | 5.5 | 0.94x | 2% |
| Time Reduction | 5 | 50 | best gear | yes | 59% | 3.82 | 0.2% | 6.00 | 6.02 | 5.5 | 1.09x | 6% |
| Time Reduction | 5 | 50 | fully modded gear | yes | 82% | 6.08 | 0.0% | 9.40 | 9.40 | 5.5 | 1.71x | 78% |
| Time Reduction | 5 | 50 | fully modded gear | no | 82% | 5.80 | 0.0% | 9.05 | 9.05 | 5.5 | 1.65x | 66% |
| Time Reduction | 1 | 50 | no gear | yes | 75% | 4.64 | 0.0% | 7.40 | 7.40 | 5.5 | 1.35x | 22% |
| Time Reduction | 1 | 50 | no gear | no | 75% | 4.47 | 0.0% | 7.09 | 7.09 | 5.5 | 1.29x | 17% |
| Time Reduction | 1 | 50 | mid gear | yes | 81% | 5.09 | 0.0% | 8.21 | 8.21 | 5.5 | 1.49x | 36% |
| Time Reduction | 1 | 50 | best gear | yes | 89% | 5.48 | 0.0% | 8.96 | 8.96 | 5.5 | 1.63x | 56% |
| Time Reduction | 1 | 50 | fully modded gear | yes | 95% | 6.61 | 0.0% | 9.87 | 9.87 | 5.5 | 1.79x | 94% |
| Time Reduction | 1 | 50 | fully modded gear | no | 95% | 6.61 | 0.0% | 9.87 | 9.87 | 5.5 | 1.79x | 94% |

### Expected supply per random level

Today one random level adds, on average, the weighted mix below. Under the Experiment the licensed runs are still rolled (same odds, same d3), so their supply is exactly today's. The rest of the time the level grants a reduction, and the player picks the kind with their evidence: a player who always wants one kind gets that kind's size whenever the level is a reduction, and none of the other kind. The "Experiment" column is the chance the level is a reduction times that kind's mean size across the focused cases above (with Refine Method, evidence gathered).

| Kind | Today: expected per random level | Experiment: expected per random level | Change in that kind's supply | Dice maximum |
| --- | --- | --- | --- | --- |
| Licensed Runs (rolled) | 0.545 runs | 0.545 runs | 1.0x (unchanged) | 3 runs |
| Credit Reduction (if always chosen) | 2.00% | 5.31% | 2.7x (+166%) | 10% |
| Time Reduction (if always chosen) | 2.00% | 5.31% | 2.7x (+166%) | 10% |

Licensed runs: today a random level rolls runs 27.3% of the time for 1-3 (mean 2), so 0.545 runs per random level on average; the Experiment rolls the same chance and die when it opens, so the expected licensed runs per blueprint are identical (a test enumerates every roll of both). The power ceiling is unchanged for every kind: one bonus per random level and never above the dice maximum. The reduction supply rises because the player chooses the kind and the evidence sizes it near the die's average instead of a one-in-two draw; it falls back toward today's if the player mixes kinds across levels 3, 5 and 8.

## Time cut (fixed levels)

All of a fixed level's evidence is spare. The focused player Tests and Analyzes with the last trial. The cap is 25% of the job.

| Tier | Rank | Gear | Test chance | Mean evidence analyzed | Mean time cut | Reaches the cap |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | no gear | 60% | 2.99 | 15.0% | 8% |
| 1 | 10 | mid gear | 66% | 3.50 | 17.5% | 16% |
| 1 | 10 | best gear | 74% | 3.77 | 18.9% | 24% |
| 1 | 10 | fully modded gear | 95% | 4.96 | 24.8% | 98% |
| 2 | 20 | no gear | 60% | 3.01 | 15.1% | 8% |
| 2 | 20 | mid gear | 66% | 3.34 | 16.7% | 14% |
| 2 | 20 | best gear | 74% | 3.85 | 19.2% | 27% |
| 2 | 20 | fully modded gear | 95% | 4.95 | 24.8% | 96% |
| 3 | 30 | no gear | 55% | 2.79 | 14.0% | 7% |
| 3 | 30 | mid gear | 61% | 3.19 | 15.9% | 10% |
| 3 | 30 | best gear | 69% | 3.57 | 17.9% | 22% |
| 3 | 30 | fully modded gear | 92% | 4.95 | 24.8% | 95% |
| 4 | 40 | no gear | 50% | 2.66 | 13.3% | 4% |
| 4 | 40 | mid gear | 56% | 2.89 | 14.4% | 7% |
| 4 | 40 | best gear | 64% | 3.47 | 17.4% | 16% |
| 4 | 40 | fully modded gear | 87% | 4.82 | 24.1% | 85% |
| 5 | 50 | no gear | 45% | 2.43 | 12.2% | 3% |
| 5 | 50 | mid gear | 51% | 2.81 | 14.1% | 6% |
| 5 | 50 | best gear | 59% | 3.23 | 16.1% | 13% |
| 5 | 50 | fully modded gear | 82% | 4.77 | 23.9% | 79% |
| 1 | 50 | no gear | 75% | 3.93 | 19.6% | 32% |
| 1 | 50 | mid gear | 81% | 4.33 | 21.6% | 45% |
| 1 | 50 | best gear | 89% | 4.58 | 22.9% | 65% |
| 1 | 50 | fully modded gear | 95% | 4.97 | 24.9% | 98% |
| 3 | 50 | no gear | 70% | 3.63 | 18.1% | 17% |
| 3 | 50 | mid gear | 76% | 4.08 | 20.4% | 36% |
| 3 | 50 | best gear | 84% | 4.38 | 21.9% | 53% |
| 3 | 50 | fully modded gear | 95% | 4.97 | 24.9% | 98% |

## Fabrication XP per real hour

XP is the experiment (62% of the chart XP at full effort) plus completion (31%), each raised 10% per research level reached; the chart XP is `Skill.GetDeltaXP(recipe level - Fabrication rank)`, 0 once the recipe is 5 or more ranks below the researcher. The values are named constants in `ResearchXpRules`, in one place, so a ceiling can be applied there.

Each row is the best case for a tier: the researcher is at the tier's first rank and researches the highest-level recipe of the tier, which is where the chart pays most. XP per hour assumes every job slot is kept busy (one at ranks 10-24, two from 25, three from 50), each job restarted the moment it completes, with the experiment's mean time cut. A job's timer starts before its experiment opens, so the experiment runs inside the job's length; the researcher still plays only one experiment at a time, so a round takes at least the active time per job (the experiment's actions plus the overhead) times the concurrent jobs.

| Tier | Rank | Recipe level | Research level | Concurrent jobs | Chart XP | Experiment XP | Completion XP | Job length (after cut) | Active time per job | XP per real hour |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | 10 | 9 | 1 | 1 | 450 | 306 | 153 | 2.8 min | 1.2 min | 10012 |
| 1 | 10 | 9 | 5 | 1 | 450 | 418 | 209 | 1.4 h | 1.2 min | 451 |
| 1 | 10 | 9 | 10 | 1 | 450 | 558 | 279 | 4.6 h | 1.2 min | 182 |
| 2 | 20 | 19 | 1 | 1 | 450 | 306 | 153 | 12.7 min | 1.2 min | 2161 |
| 2 | 20 | 19 | 5 | 1 | 450 | 418 | 209 | 6.4 h | 1.2 min | 98 |
| 2 | 20 | 19 | 10 | 1 | 450 | 558 | 279 | 21.1 h | 1.2 min | 40 |
| 3 | 30 | 29 | 1 | 2 | 450 | 306 | 153 | 31.4 min | 1.2 min | 1754 |
| 3 | 30 | 29 | 5 | 2 | 450 | 418 | 209 | 15.6 h | 1.2 min | 81 |
| 3 | 30 | 29 | 10 | 2 | 450 | 558 | 279 | 52.6 h | 1.2 min | 32 |
| 4 | 40 | 39 | 1 | 2 | 450 | 306 | 153 | 1.0 h | 1.2 min | 915 |
| 4 | 40 | 39 | 5 | 2 | 450 | 418 | 209 | 29.3 h | 1.2 min | 43 |
| 4 | 40 | 39 | 10 | 2 | 450 | 558 | 279 | 99.8 h | 1.2 min | 17 |
| 5 | 50 | 52 | 1 | 3 | 750 | 511 | 255 | 1.6 h | 1.3 min | 1395 |
| 5 | 50 | 52 | 5 | 3 | 750 | 697 | 348 | 47.9 h | 1.3 min | 65 |
| 5 | 50 | 52 | 10 | 3 | 750 | 930 | 465 | 163.7 h | 1.3 min | 26 |

### Against at-level Fabrication crafting

At level (recipe level equal to rank), chart XP is 600. A craft of 15 actions (the middle of the 10-20 target) at 6.0 s each takes 90.0 s and pays 24000 XP per hour at 0% quality, 36000 at 50% and 48000 at 100% (`CraftOutcomeRules.CalculateXp`, no blueprint, not a first craft).

The highest research row above is 10012 XP per hour (tier 1, rank 10, recipe level 9, research level 1, 1 concurrent job(s)), 42% of the 0%-quality at-level crafting rate and 28% of the 50%-quality rate. Research XP falls with research level because the job length grows with the square of the level while the XP grows by 10% per level.

## Limitations

- Seconds per action and the per-job overhead are estimates; the client evidence replaces them. XP per hour scales directly with them.
- The focused player is a stated strategy, not a search for the best play. The adaptive player values evidence only. A real player who mixes candidates or skips Refine Method will land between the rows.
- Tier 1-5 rank cases use the lowest rank that can research the tier and rank 50. Ranks between those add up to 15 points of Test chance (one per rank past the tier's required rank), which raises evidence a little and leaves the length unchanged.
- The recipe cost modifier (0.2 on some food) lengthens a job by up to that share and is not modeled; blueprint time reductions (up to 30%) shorten it and are not modeled.
- Perks are not modeled; the Researcher line is a draft (`fabrication-researcher-perk-line-draft.md`).
- No client run: the window is untested in the client.
