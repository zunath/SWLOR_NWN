# Fabrication perk line: Researcher (implemented)

The line is implemented on `claude/crafting-revamp-perks-fabrication`. This file started as the draft the owner approved; it now records what was built, the owner's decisions of 22 October 2026 that amended the draft, the balance calls of 23 October 2026, and the balance evidence. Rules followed: `CraftingRevampPlan.md` (Line schedule, Lines, Fabrication research: the Experiment) and `SkillPerkStandards.md`.

- Code: `Feature/PerkDefinition/FabricationResearcherPerkDefinition.cs` (nine perks), `ExperimentTuning.FromStatTotals` (the one place stats become engine numbers), the Experiment engine, `ResearchExperimentViewModel` and `ResearchViewModel`.
- IDs: PerkType 873-881, StatType 1198-1204 (inside the reserved ranges 864-883 and 1188-1207). `iprp_resperk.2da` needs no rows (no perk here restricts an item), and no TLK strings are new (perk names and descriptions are C# text).
- Bible: the Fabrication tab, rows 33-44, written by `tools/UpdateFabricationPerkBible.py` from the perk definitions.
- Evidence: `fabrication-researcher-perks.md` (`SWLOR.CraftSim researcher-perks`) and `fabrication-researcher-style-search.md` (`researcher-perks --search`, the search for style numbers that pass both comparisons) and `fabrication-researcher-line-ablation.md` (`researcher-perks --ablate`, which shared purchase moves the style balance; played on the line as it stood before the 23 October rebalance).

## Structure

- One independent, optional line of 25 SP. It needs no other line and is not needed to run an experiment well: an unperked researcher gets the full experiment (six trials, every base action) and the same bonus ceiling.
- Ten purchases at Fabrication ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is a choice of two styles, `Methodical` and `Intuitive`, made exclusive through `RequirementCannotHavePerk`; switching costs a Perk Refund Token. The owned style is always active: there is no toggle. The rank-50 purchase is the capstone with no quest requirement.
- A ranked perk occupies two purchases (Field Notes at 5 and 40, Efficient Analysis at 30 and 45); each rank restates the one before inside its own total, because only the purchased rank's stat bonuses apply.
- Every effect acts only on the research Experiment. Perks grant `StatType` adjustments; the window reads them once through `ExperimentTuning.FromStatTotals` when it sets the experiment up, and the engine never tests a perk by name. A job's stored record keeps the trials it was opened with (`TrialsTotal`), so a perk change cannot alter a saved experiment.
- The Experiment has no rank-gated actions: Test, Refine Method, Analyze and Conclude work from the first experiment at any rank. Peer Review and Breakthrough are unlocked by buying the perk.

## Owner decisions applied (22 October 2026)

- Intuitive is +20% Test chance and -1 trial; Methodical is +2 trials and a Test chance cost. The 22 October decision kept the plan's -10; the balance study (below) found -10 out of balance, and on 23 October 2026 the cost became **-15 points** (the plan and the Bible row now say -15).
- Line rebalance (23 October 2026, delegated by the owner): Broader Hypotheses is the rank-35 purchase (3 SP) and Second Opinion the rank-15 one (2 SP), and Field Notes gives +6 and +12 points (was +3 and +6). The styles keep their numbers. See the balance evidence.
- A style may move the experiment's length off the 5-8 turn target by one or two turns.
- Breakthrough doubles what each piece of spare evidence cuts from the job's time when Analyze spends it, still inside the 25% cap.
- Breakthrough's guaranteed Test is an action the player chooses when to spend: it is free, uses no trial, and the next Test succeeds.
- Peer Review has two directions (credit to time, and time to credit), so the window shows two buttons, only where evidence decides between the two reductions.

## Researcher (Experiment)

| Rank | SP | Perk | Type | Effect | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Field Notes I | trait | Every Test has 6 more points of chance. | `ExperimentTestChancePercent` +6 |
| 10 | 1 | Steady Hands | trait | Refine Method costs 5 less CP (30 becomes 25), to a minimum of 1. | `ExperimentRefineCpReduction` +5 |
| 15 | 2 | Second Opinion | trait | Refine Method raises the next Test by 20 points instead of 15. | `ExperimentRefineChancePercent` +5 |
| 20 | 2 | Methodical | style (exclusive with Intuitive) | 2 more trials and every Test has 15 fewer points of chance. | `ExperimentTrials` +2, `ExperimentTestChancePercent` -15 |
| 20 | 2 | Intuitive | style (exclusive with Methodical) | Every Test has 20 more points of chance and the experiment has 1 fewer trial. | `ExperimentTestChancePercent` +20, `ExperimentTrials` -1 |
| 25 | 3 | Peer Review | active | Where evidence decides between a credit and a time reduction: once per experiment, for 25 CP and no trial, move half of one candidate's evidence (rounded up) to the other. Two actions, credit to time and time to credit. | `ExperimentPeerReviewUnlocked` (flag) |
| 30 | 3 | Efficient Analysis I | trait | Analyze cuts 6% of the job's time per piece of spare evidence instead of 5%. The 25% cap stays. | `ExperimentTimeCutPerEvidencePercent` +1 |
| 35 | 3 | Broader Hypotheses | trait | The experiment has 1 more trial (6 becomes 7). | `ExperimentTrials` +1 |
| 40 | 3 | Field Notes II | trait | Every Test has 12 more points of chance in total (replaces Field Notes I's 6). | `ExperimentTestChancePercent` +12 |
| 45 | 3 | Efficient Analysis II | trait | Analyze cuts 7% per piece of spare evidence (replaces Efficient Analysis I's 6%). The cap stays, so four pieces reach it. | `ExperimentTimeCutPerEvidencePercent` +2 |
| 50 | 4 | Breakthrough | capstone, active | A free action, once per experiment, no trial: the next Test succeeds, and for the rest of the experiment each piece of spare evidence cuts twice as much of the job's time, still inside the 25% cap. No extra bonuses. | `ExperimentBreakthroughUnlocked` (flag) |

SP check: 1 + 1 + 2 + 2 + 3 + 3 + 3 + 3 + 3 + 4 = 25. Only one of the two rank-20 perks can be bought.

Window and engine changes the perks needed: `ExperimentTuning` gained `PeerReview` and `Breakthrough`; the state gained three flags (Peer Review used, Breakthrough used, Breakthrough armed); the engine lists Peer Review's two actions only where evidence decides between the reductions and the researcher owns the perk, and Breakthrough wherever the researcher owns it; the window has eight action slots, with the two perk rows below the CP meter so a researcher without them sees blank space at the foot and not between buttons.

## Interactions and limits

- **No extra bonuses.** Nothing in the line changes the extra guaranteed bonus rolled when the experiment opens.
- **Power ceiling.** A bonus never exceeds today's dice (licensed runs 1-3, reductions 1-10 percent) and the time cut never exceeds 25% of the job. The line makes the maximum easier to reach, never larger.
- **XP.** The experiment's XP is the share of its trials used, so more trials pay the same XP. Research XP per real hour moves only through the job's time cut, which is capped at 25% whatever the perks: with the whole line it is at most 1.016 times the unperked peak in the simulation, and no build can pass 1.12 times it. The peak row (rank 10, tier 1, research level 1) stays at 26% of the at-level Fabricator crafting rate at 0% quality with the band's XP multiplier even at the cap, and every other rank is 5-9% (research XP is the scaled value of the progression-pace change).
- **Trials and evidence.** Evidence pays today's dice maximum at six pieces. Intuitive has five trials, so it cannot reach the maximum without Broader Hypotheses; that is part of its cost.
- **Stacking.** Methodical with Broader Hypotheses has nine trials (nine to twelve turns with Refine Method); Intuitive with Broader Hypotheses has six. Intuitive with Field Notes II reaches 92% at the tier's first rank, and the 95% cap applies to the sum with the rank bonus (up to 15 points) and Refine Method.
- **Peer Review and CP.** Its 25 CP competes with Refine Method (30, or 25 with Steady Hands). A researcher with no gear has 37 CP at rank 50 and cannot afford both.

## Balance evidence

`fabrication-researcher-perks.md`, seed 1, 400 experiments per case, every build on the same dice. The generic gates have no scoring rule for an experiment (it has no quality meter), so the style comparison applies gate 2's thresholds to a score from 0 to 100: the reduction won as a share of today's largest where evidence decides, the time cut as a share of the cap at a fixed level.

- **Gate 2 (style balance): passes alone and with the whole line, on three seeds.** Methodical against Intuitive passes all 12 comparisons at seeds 1, 2 and 3 (400 experiments per case).
  - Alone: Methodical leads on 33-42% of cases; the mean gap is +1.2 to +1.5 score points at fixed levels and -2.6 to -3.0 where evidence decides, at 3.4 more turns (9.3 against 5.7 on average). At -10 it failed four of the six.
  - With every other purchase added: Methodical leads on 33-42%; the gap is -0.9 to 0.0 at fixed levels and -2.1 to -1.0 where evidence decides.
  - **Before the 23 October rebalance** the full line failed all six comparisons (Intuitive ahead by 3.7 to 7.0 points, Methodical on 17-25% of cases), and the search over every Methodical (+2 or +3 trials, -5 to -20 points) and Intuitive (+10 to +25 points, -1 or -2 trials) pairing, 1024 pairs, found none that passed both: 60 passed alone, 40 passed with the line. No style cost between -10 and -15 passed both (alone / full-line failures of six: 4/3 at -10, 3/3 at -11 and -12, 3/4 at -13, 0/6 at -14 and -15).
  - **The hypothesis that Efficient Analysis drives Intuitive's full-line lead is refuted** (`fabrication-researcher-line-ablation.md`, the line before the rebalance). Taking Efficient Analysis out of the whole line moves the fixed-level gap from -3.7 to -4.0 and the evidence-decides gap not at all (-6.8): it only changes the time cut, and where evidence decides the score is the reduction, so it cannot touch half of the comparisons. **Broader Hypotheses is the driver**: without it the whole line passes (0 of 6, gaps -0.9 and +4.1); added alone to the styles it moves them from +1.3 and -2.8 to -8.8 and -12.0 (6 of 6 failing). A flat extra trial is worth the chance of a Test, about 0.85 evidence to Intuitive (80-90%) and 0.5 to Methodical (45-55%). Field Notes goes the other way (it favors Methodical: taking it out moves the gaps to -4.9 and -9.5; added alone, +5.3 and +1.4) probably because Intuitive's chance reaches the 95% cap and the points are wasted on it (not isolated). Breakthrough also leans slightly to Methodical; Steady Hands and Peer Review are neutral for the focused player and Second Opinion close to it.
  - **The rebalance** (Broader Hypotheses at rank 35 and Second Opinion at rank 15, Field Notes doubled to +6 and +12) was chosen from the grid in the ablation report: where Broader Hypotheses first applies (rank 15, 25, 35, 45, never) against Field Notes' size (x1 to x3), failed comparisons of six at seeds 1, 2 and 3. Field Notes x2 with Broader Hypotheses from rank 25, 35 or 45 (and x3 from rank 25) held 0 at all three seeds; rank 35 with x2 keeps both mean gaps inside 2.2 points; rank 35 with x1.5 (the first try) passed seed 1 and failed at seeds 2 (2 comparisons) and 3 (1). Broader Hypotheses at rank 35 costs 3 SP and Second Opinion at rank 15 costs 2, so the line still totals 25 SP.
  - **Search after the rebalance** (`fabrication-researcher-style-search.md`): 60 of 1024 pairs pass alone, 108 pass with the line, 49 pass both. The implemented pair (Methodical -15, Intuitive +20) passes both and is distance 5 from the approved numbers; -14 against +20 is the closest (distance 4). The approved -10 still fails (4 alone and 2 with the line), which is why -15 stays.
- **Gate 3 (perks optional): unchanged.** An unperked researcher gets the full experiment and the same ceiling.
- **Length.** Methodical 8-10 turns, Intuitive 5-7, as the owner's decision allows; Broader Hypotheses adds one more, and Breakthrough adds one turn.
- **Per purchase (rank 50, tier 5, mid gear).** Field Notes +1.2 points of reduction and +2.4 of time cut; Broader Hypotheses +1.0 and +2.7; Efficient Analysis +4.5 of time cut; Second Opinion and Steady Hands are small (Refine Method is CP-limited); Breakthrough lifts the mean time cut from 16.6% to 24.3% and reaches the cap in 89% of fixed-level experiments, which makes it the strongest single purchase, as a capstone should be; the whole line reaches the dice maximum in 62% (Methodical) and 69% (Intuitive) of reductions and the cap in every fixed-level experiment.
- **Peer Review.** The focused player never has evidence behind the leader, so it does nothing for them. For a player who tests both reductions first and then backs the leader, it raises the mean reduction from 5.0% to 5.8-5.9% and shortens the experiment by about half a turn.

## Open items

1. **Second Opinion at rank 15.** It is the cheap rank-15 purchase now, but Refine Method costs 30 CP (25 with Steady Hands) and a researcher at rank 15 with no gear has 11 CP, so it does nothing until the researcher has gear. Broader Hypotheses was the only purchase at rank 15 that helped without CP. If that matters more than the balance, swap another cheap, neutral purchase into the slot.
2. **Icons.** The nine perks reuse existing icons (`ife_rsrch`, `ife_observe`, `ife_rsrch_proj`, `ife_sci_net`, `ife_tiny_bulb`, `ife_copycat_bulb`, `ife_min_light`, `ife_prune`, `ife_spark`) set as explicit perk icons, because new art could not be produced. Passive-gold framed custom art per `IconStandards.md` is a follow-up.
3. **Client evidence.** None yet: the owner deferred it to the Closeout joint pass. The window's eight slots and the two Peer Review buttons are unverified in the client.
