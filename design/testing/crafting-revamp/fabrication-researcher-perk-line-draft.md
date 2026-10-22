# Fabrication perk line: Researcher (draft)

Draft for the owner's approval. No perk code, no StatType IDs and no Bible rows exist yet; this file is the only artifact. Every magnitude is a starting value that CraftSim tunes later. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Fabrication research: the Experiment) and `SkillPerkStandards.md`. Only the Researcher line is drafted here; the Builder line belongs to the Fabricator workstream.

## Structure

- One independent, optional line of 25 SP. It needs no other line and is not needed to run an experiment well: an unperked researcher gets the full experiment (six trials, every action) and the same bonus ceiling.
- Ten purchases at Fabrication ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style, a choice of two mutually exclusive perks through `PerkRequirementCannotHavePerk`; switching costs a Perk Refund Token. The rank-50 purchase is the capstone with no quest requirement.
- A ranked perk occupies two purchases; its second rank restates the first rank's benefit inside its own total (only the purchased rank's stat bonuses apply).
- Every effect acts only on the research Experiment (`ExperimentEngine`). Perks grant StatType adjustments, and the engine reads them; it never tests a perk by name.
- The Experiment has no rank-gated actions (owner rule, October 2026): Test, Refine Method, Analyze and Conclude work from the first experiment at any rank. Peer Review and Breakthrough are actions the perks add, so they are unlocked by buying the perk, not by rank.

## How the engine reads perks

The seam already exists. At the start of an experiment the window sums the researcher's stat totals into an `ExperimentTuning` record and passes it to `ExperimentSetup`, beside rank and gear. `ExperimentTuning` holds exactly the numbers a perk changes (`Trials`, `TestChanceBonusPercent`, `RefineChanceBonus`, `RefineCp`, `TimeCutPerEvidence`, `TimeCutCap`), so the traits below need no engine change. Peer Review and Breakthrough add one action each and one flag each to `ExperimentState`; previews stay pure.

| Tuning field | Starting value | Stats that change it |
| --- | --- | --- |
| `Trials` | 6 | `ExperimentTrials` |
| `TestChanceBonusPercent` | 0 | `ExperimentTestChancePercent` |
| `RefineCp` | 30 | `ExperimentRefineCpReduction` |
| `RefineChanceBonus` | 15 | `ExperimentRefineChancePercent` |
| `TimeCutPerEvidence` | 5 | `ExperimentTimeCutPerEvidencePercent` |
| `TimeCutCap` | 25 | none: the cap is a hard constraint of the plan and no perk raises it |

## Researcher (Experiment)

The line rewards a researcher who plans the experiment: how many trials, how reliable each is, how far Refine Method goes, how much of the job the leftover evidence buys back, and a capstone that guarantees one good result.

| Rank | SP | Perk | Type | Effect (starting values) | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Field Notes I | trait | Every Test has 3 more points of chance. | `ExperimentTestChancePercent` +3 |
| 10 | 1 | Steady Hands | trait | Refine Method costs 5 less CP (30 becomes 25). | `ExperimentRefineCpReduction` +5 |
| 15 | 2 | Broader Hypotheses | trait | The experiment has 1 more trial (6 becomes 7). | `ExperimentTrials` +1 |
| 20 | 2 | Methodical | style | The experiment has 2 more trials and every Test has 10 fewer points of chance. Cannot be owned with Intuitive. | `ExperimentTrials` +2, `ExperimentTestChancePercent` -10 (both carried by the perk) |
| 20 | 2 | Intuitive | style | Every Test has 15 more points of chance and the experiment has 2 fewer trials. Cannot be owned with Methodical. | `ExperimentTestChancePercent` +15, `ExperimentTrials` -2 (both carried by the perk) |
| 25 | 3 | Peer Review | active | A new action that costs 25 CP and no trial, once per experiment: it moves half of one candidate's evidence (rounded up) to another candidate you name. It does nothing at a fixed level, which has one candidate. | `ExperimentPeerReviewUnlocked` (flag) |
| 30 | 3 | Efficient Analysis I | trait | Analyze cuts 1 more percent of the job's time for each piece of spare evidence (5% becomes 6%). The 25% cap stays. | `ExperimentTimeCutPerEvidencePercent` +1 |
| 35 | 3 | Second Opinion | trait | Refine Method raises the next Test by 5 more points (15 becomes 20). | `ExperimentRefineChancePercent` +5 |
| 40 | 3 | Field Notes II | trait | Every Test has 6 more points of chance in total (this rank replaces Field Notes I's 3). | `ExperimentTestChancePercent` +6 |
| 45 | 3 | Efficient Analysis II | trait | Analyze cuts 2 more percent of the job's time in total for each piece of spare evidence (5% becomes 7%; this rank replaces Efficient Analysis I's 1). The 25% cap stays, so four pieces reach it. | `ExperimentTimeCutPerEvidencePercent` +2 |
| 50 | 4 | Breakthrough | capstone, active | A new action, free and without a trial, once per experiment: the next Test succeeds automatically, and for the rest of the experiment each piece of spare evidence cuts twice as much of the job's time as it otherwise would (still inside the 25% cap). It adds no evidence beyond that one Test and grants no extra bonuses. | `ExperimentBreakthroughUnlocked` (flag) |

SP check: 1 + 1 + 2 + 2 + 3 + 3 + 3 + 3 + 3 + 4 = 25. Only one of the two rank-20 perks can be bought, so the line holds ten purchases.

Decisions the line creates:

- Methodical against Intuitive (the plan's pair): more trials mean more evidence and more room for Refine Method, but a longer experiment and a lower chance per Test; fewer, surer Tests give a shorter experiment. See the balance notes: the plan's numbers do not make these equal.
- Steady Hands and Second Opinion against Efficient Analysis: Refine Method (CP and a better next Test) against turning leftover evidence into a shorter job.
- Peer Review at a random level: spread evidence while undecided, then pool it into the candidate you commit to. Evidence behind the leader can also be analyzed instead, so Peer Review is the choice to keep it as bonus size.
- Breakthrough: a guaranteed Test is worth most late, when the leader is one piece short of the next size, or early at a fixed level to bank a piece of evidence before Analyze.

## Interactions and limits

- **No extra bonuses.** Nothing in the line changes the extra guaranteed bonus rolled when the experiment opens; Breakthrough's own text says so. Evidence never raises it.
- **Power ceiling.** A bonus never exceeds today's dice (licensed runs 1-3, reductions 1-10 percent), and the time cut never exceeds 25% of the job. The line makes the maximum easier to reach (more evidence), never larger.
- **Trials and evidence.** Evidence pays today's dice maximum at 6 pieces (`EvidenceForMaximum`). Broader Hypotheses and Methodical let a researcher reach it more often; Intuitive caps evidence at 4 (5 with Broader Hypotheses), so an Intuitive researcher can never reach the dice maximum; that is part of the style's cost and another reason to review its numbers.
- **Stacking.** Methodical with Broader Hypotheses gives 9 trials; Intuitive with Broader Hypotheses gives 5. Field Notes II (+6) with Intuitive (+15) gives 81% at the tier's first rank, and the 95% cap applies to the sum with the rank bonus (up to 15 points) and Refine Method.
- **Refine Method.** Steady Hands and Second Opinion are the only ways to refine more often or harder. CP stays the limit: a researcher with 37 CP (rank 50, no gear) refines once at 30 CP, twice with Steady Hands only if CP is 50 or more.

## Proposed StatTypes

Names only; the IDs are reserved against current master and the HAK when the owner approves the rows (the next free StatType is 1168). Each declares its classification, polarity and category on its enum entry. All are beneficial when positive, except the negative values carried by the style perks, which are tradeoffs.

| Name | Aggregation | Used by |
| --- | --- | --- |
| `ExperimentTrials` | additive | Broader Hypotheses, Methodical, Intuitive |
| `ExperimentTestChancePercent` | additive | Field Notes, Methodical, Intuitive |
| `ExperimentRefineCpReduction` | additive | Steady Hands |
| `ExperimentRefineChancePercent` | additive | Second Opinion |
| `ExperimentTimeCutPerEvidencePercent` | additive | Efficient Analysis |
| `ExperimentPeerReviewUnlocked` | maximum | Peer Review |
| `ExperimentBreakthroughUnlocked` | maximum | Breakthrough |

## Engine changes the perks need (after approval)

- A builder that sums the seven stats into an `ExperimentTuning`, plus two optional flags on `ExperimentSetup`.
- Two actions (`peer_review`, `breakthrough`) with their previews, and the state they need: a Peer Review used flag, a Breakthrough armed-or-used flag and a time-cut multiplier. All of it lives in `ExperimentState`.
- Peer Review needs a source and a target: at three candidates that is six pairs. The window can show one button per candidate pair only when Peer Review is owned and the level is random, or use the leader as the source and offer two buttons for the target (proposed in the open questions).
- A job's stored record keeps the trials it was opened with (`TrialsTotal`), so a perk change between sittings cannot change a saved experiment.
- Unit tests for each stat at each rank, style exclusivity, the 95% and 25% caps with every perk owned, Peer Review's rounding and Breakthrough's once-per-experiment limit.

## Balance notes and evidence needed before coding

Expected evidence is trials times Test chance, before Refine Method (one Refine adds about 0.15). Base is 6 trials at 60%.

| Build | Trials | Test chance | Expected evidence | Mean actions (trials plus Refines) |
| --- | --- | --- | --- | --- |
| Unperked | 6 | 60% | 3.6 | 6-8 |
| Methodical | 8 | 50% | 4.0 | 8-10 |
| Intuitive | 4 | 75% | 3.0 | 4-6 |
| Intuitive, proposed alternative (+20 points, 1 fewer trial) | 5 | 80% | 4.0 | 5-7 |

- **The plan's style pair is not balanced.** Methodical gives 11% more evidence than the unperked researcher and Intuitive 17% less, so Intuitive is worse on evidence and only wins on time. Either accept that Intuitive trades power for a shorter sitting, or use the proposed alternative (+20 points, 1 fewer trial), which matches Methodical's evidence (4.0) in about three fewer turns (5 trials against 8). Decision for the owner.
- **Length.** The 5-8 turn target is for the unperked experiment. Methodical lengthens it to 8-10 and Intuitive shortens it to 4-6. The length gate cannot pass for a style that changes the trials by 2; the owner should decide whether the target applies per style.
- CraftSim (later): `ExperimentStudy.Play` already takes an `ExperimentTuning`, so each build above can be played with the focused player for evidence, magnitude and time cut. The perks-optional and style-balance gates need an Experiment scoring rule the generic gates do not have (an experiment has no quality meter).
- SP prices: the plan's schedule (1/1/2/2/3/3/3/3/3/4) and the other crafting lines; no combat comparator applies.

## Open questions for the owner

1. **Breakthrough's "winner's time cut".** The plan says the winner's time cut doubles. An experiment has one time cut (Analyze's, on the job), not one per candidate, and doubling the Time Reduction bonus would pass today's 10% dice maximum. This draft doubles what each piece of spare evidence cuts for the rest of the experiment, inside the 25% cap. If you mean that the cap itself doubles to 50%, or that a winning Time Reduction doubles, say so; both raise the plan's power ceiling.
2. **Style balance.** The plan's Methodical and Intuitive numbers, or the proposed alternative above?
3. **Length per style.** Does the 5-8 turn target apply to an experiment with a style, or only to an unperked one?
4. **Peer Review's targeting.** A source and a target are two choices in a window with no prompt. Proposed: the source is always the leader (the candidate with the most evidence, ties to the lowest in the list), and the player picks the target with one of two buttons. Or six pair buttons, shown only when Peer Review is owned at a random level.
5. **Breakthrough as an action.** It is drafted as a free action the player chooses to spend, which makes the guaranteed Test a decision. As a trait it would instead turn the first inconclusive Test of the experiment into a success.
