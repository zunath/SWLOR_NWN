# Fabrication perk line: Builder (implemented)

The line is implemented on `claude/crafting-revamp-perks-fabrication`. This file started as the draft the owner approved; it now records what was built, the owner's decisions of 22 October 2026 that amended the draft, and the balance evidence. Rules followed: `CraftingRevampPlan.md` (Line schedule, the Builder row under Lines) and `SkillPerkStandards.md`.

- Code: `Feature/PerkDefinition/FabricationBuilderPerkDefinition.cs` (nine perks), `FabricatorPerkStats` (the one place stats become engine numbers), `FabricatorSetup`, `FabricatorEngine`, `FabricatorCraftViewModel`.
- IDs: PerkType 864-872, StatType 1188-1197 (inside the reserved ranges 864-883 and 1188-1207). `iprp_resperk.2da` needs no rows (no perk here restricts an item), and no TLK strings are new (perk names and descriptions are C# text).
- Bible: the Fabrication tab, rows 20-31, written by `tools/UpdateFabricationPerkBible.py` from the perk definitions.
- Evidence: `fabrication-builder-perks.md` (`SWLOR.CraftSim builder-perks`), `fabricator-perks-gates.md` (gates 2 and 3 on the unperked scenario), `fabricator-production-run-gates.md` and `fabricator-fine-tolerance-gates.md` (the other gates for a Builder).

## Structure

- Builder is one of Fabrication's two independent, optional lines, 25 SP each. Neither requires the other, and no part of it is needed to reach maximum quality (gate 3: an unperked rank-50 crafter with the best gear reaches full quality on at least 50% of at-level crafts; perks change how, and how efficiently).
- Ten purchases at Fabrication ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP.
- **Rank 20 is the style, a choice of two.** *Production Run* and *Fine Tolerance* use `RequirementCannotHavePerk` against each other, and switching costs a Perk Refund Token like any perk. The owned style is always active in the Fabricator: there is no toggle in the window (owner decision).
- Rank 50 is the capstone, with no quest requirement.
- A ranked perk occupies two purchases (Quick Change at 5 and 35, Field Service at 15 and 40); each rank restates the one before inside its own total, because only the purchased rank's stat bonuses apply.
- Every effect acts only on the Fabricator. Perks grant `StatType` adjustments; the window sums them once into a `FabricatorPerkStats` record when the craft starts and passes it to `FabricatorSetup`, so previews and results stay pure and the engine never tests a perk by name.
- Every action a perk improves or adds is unlocked at or before the perk's own rank: Run, Precision Pass, Retool and Assemble are available from rank 0, Service from rank 10.

## The base minigame these perks build on

The Fabricator (`Service/CraftService/Minigame/Fabricator/`; every value is a starting value in `FabricatorRules` that CraftSim tuned in phase 2, `fabricator-gates.md`): a piece is three sub-assemblies, the Frame (Cutter head), the Shell (Extruder head) and the Fittings (Bonder head). One head is loaded at a time. Machine Condition is the integrity meter (100 for a plain piece; the piece breaks at 0).

| Action | Unlocks at Fabrication rank | Cost | Result |
| --- | --- | --- | --- |
| Run (Frame, Shell, Fittings) | 0 | Wear: 2-9 Machine Condition by the recipe's materials | Progress on the chosen sub-assembly: 28% of the recipe's work, scaled by Craftsmanship (+1% a point) and rank, at full effect with its own head loaded and half effect with another head |
| Precision Pass | 0 | Wear, as Run | The loaded head's sub-assembly gains 46 quality points (of 100), scaled by Control (+1% a point) and rank. It needs some progress first |
| Retool (Cutter, Extruder, Bonder) | 0 | CP: 1 + recipe level / 6; 1 Machine Condition | Loads the head |
| Service | 10 | CP: 2 + recipe level / 3 | Restores 40 Machine Condition |
| Assemble | 0 | none | Finishes the item once all three sub-assemblies are complete. The quality is the average of the three, weighted by the recipe's quality shares, and no higher than the weakest sub-assembly plus 20 |

**Prime Feedstock** (telegraphed): each action with none in play, each head that still has work has a 12% chance to be named. The named head's next action gets +50%, and the prime waits for it for 2 actions. It only ever helps. CP is gear CP plus 0.75 per rank, plus 31 from rank 25.

## Builder (production and service)

| Rank | SP | Perk | Type | Effect | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Quick Change I | trait | Retool costs 25% less CP, to the nearest CP and a minimum of 1. | `FabricatorRetoolCpReductionPercent` +25 |
| 10 | 1 | Hardened Heads | trait | Run and Precision Pass wear the machine 1 less Machine Condition, to a minimum of 1. Retool's wear is unchanged. | `FabricatorWearReduction` +1 |
| 15 | 2 | Field Service I | trait | Service restores 10 more Machine Condition and costs 2 less CP, to a minimum of 1. | `FabricatorServiceRestore` +10, `FabricatorServiceCpReduction` +2 |
| 20 | 2 | Production Run | style (exclusive with Fine Tolerance) | Run is 25% stronger and Precision Pass 25% weaker. | `FabricatorRunPowerPercent` +25, `FabricatorPrecisionPowerPercent` -25 |
| 20 | 2 | Fine Tolerance | style (exclusive with Production Run) | Precision Pass is 25% stronger and Run 25% weaker. | `FabricatorPrecisionPowerPercent` +25, `FabricatorRunPowerPercent` -25 |
| 25 | 3 | Dual Feed | active | Two new actions, `Dual Feed + <sub-assembly>`: one action builds the loaded head's sub-assembly and one other at 60% of a full Run each (120% in all), for the CP of a Retool and one Run's wear. The loaded head does not change. | `FabricatorDualFeedUnlocked` (flag) |
| 30 | 3 | Feedstock Reserve | trait | Prime Feedstock waits 1 more action (2 becomes 3). | `FabricatorPrimeWindowActions` +1 |
| 35 | 3 | Quick Change II | trait | Retool costs 50% less CP in total (replaces Quick Change I's 25%). | `FabricatorRetoolCpReductionPercent` +50 |
| 40 | 3 | Field Service II | trait | Service restores 20 more Machine Condition and costs 4 less CP in total (replaces Field Service I's +10 and 2). | `FabricatorServiceRestore` +20, `FabricatorServiceCpReduction` +4 |
| 45 | 3 | Feedstock Contract | trait | Prime Feedstock gives +75% instead of +50%. | `FabricatorPrimeBonusPercent` +25 |
| 50 | 4 | Master Assembler | capstone, trait | The Assemble cap rises from the weakest sub-assembly + 20 to the weakest + 35. | `FabricatorAssembleCapPoints` +15 |

SP check: 1 + 1 + 2 + 2 + 3 + 3 + 3 + 3 + 3 + 4 = 25. Only one of the two rank-20 perks can be bought.

Owner decisions applied (22 October 2026): no toggle (the owned style is always active); Dual Feed is two actions, the loaded head's sub-assembly with each of the other two (`dual_feed_first` and `dual_feed_second`, named for the other sub-assembly: with the Extruder loaded they are `Dual Feed + Frame` and `Dual Feed + Fittings`); Master Assembler raises the cap to the weakest + 35 as a starting value.

Engine details: a cost reduction rounds to the nearest CP and never goes below 1 CP; wear never goes below 1; Dual Feed is disabled when either of its sub-assemblies is complete (the preview says which), uses the loaded head's Prime Feedstock if it is primed (the bonus goes to both feeds and the prime is spent), and ignores a prime on another head. The window shows a line of the numbers the crafter's perks change, the action list holds the two extra rows, and the help page has a Builder paragraph for the styles and Dual Feed.

## Balance evidence

`fabrication-builder-perks.md` (seed 1, 50 attempts per cell, 6 recipes per band; rank 50 plays recipes of levels 41-53) and the gate reports (100 attempts, 6 recipes per band). The adaptive player is the model of skilled play throughout.

- **Gate 2 (style balance): passes.** `fabricator-perks-gates.md`: each style, alone and with the rest of the line, is the better choice on 34-66% of the 19 sampled recipes of rank 20 or more at every gear tier, and the mean gap is at most 0.5 quality points. Nearly every craft reaches full quality, so the gate mostly sees ties; see the actions below.
- **Gate 3 (perks optional): unchanged.** 97%, 99% and 99% of unperked rank-50 best-gear crafts reach full quality in the 41-45, 46-50 and 51-53 bands, as before. A test pins that a crafter without perks plays exactly the Fabricator as tuned.
- **A perked Builder against gates 1 and 4-7** (`fabricator-production-run-gates.md` and `fabricator-fine-tolerance-gates.md`: the style and the whole line each rank allows, every recipe at its own level's rank, 15 band and gear cells):
  - Gate 4 (length) passes in every cell (longest 18.7 actions of 20), gate 5 (economy parity) passes, gate 7 (enhancement parity) passes (lowest 1.07 times today's).
  - Gate 1 fails exactly as the unperked Fabricator does (smallest margin -5.0 in 1-10 / no gear), a base-game result the coordinator handles.
  - Gate 6 (XP rate): Fine Tolerance passes (lowest 1.00 times today's at 11-20 / no gear). Production Run misses one of the 60 cells, 11-20 / no gear, at 0.99 (41,921 against 42,142 XP per hour; the unperked crafter passes it at 1.01). The cause is Quick Change, not the style: a Retool a point cheaper lets a crafter with 8-15 CP afford full quality (48% to 90% of crafts) for 1.1 more actions and 0.7% more XP per craft. Raising the 11-20 band's XP multiplier from 1.45 to 1.50 restores it; the base is not tuned here.
- **Each purchase (rank 50, mid gear, against the unperked craft of 15.0 actions and 20.1 CP):** Quick Change halves the CP spent (10.5) with no change in actions; Hardened Heads, Field Service, Feedstock Reserve and Master Assembler change nothing for this player at mid gear (Master Assembler only lifts a lopsided piece); at no gear Hardened Heads turns saved wear into quality (full quality 52% to 79%, +0.6 actions); Feedstock Contract saves 1.0 action at best gear; Dual Feed saves 1.2 to 1.8 actions at every gear but spends 37 to 50 CP (the unperked craft spends 20 to 32), which is its decision against Retool.
- **CP.** The whole line spends 21 to 25 CP of the 65 to 105 available at rank 50, but so does the unperked crafter (20 to 32): at rank 50 CP is not the limit either way. It binds below rank 30 and for crafters without gear, where Quick Change and Dual Feed move it.
- **The two styles differ in actions, which gate 2 cannot see.** Fine Tolerance is the shorter craft on every sampled recipe at every gear tier (Production Run needs 2.2 to 2.8 more actions alone and 2.1 to 3.6 with the line), and Production Run is slower than the unperked crafter alone (+1.3, +3.0 and +1.8 actions at no, mid and best gear) and with the line at mid and best gear (+0.4 and +1.5). A rank-50 crafter at mid gear needs two Precision Passes per sub-assembly, and a pass 25% weaker needs three, while a Run 25% stronger saves at most one Run. The magnitudes cannot be fixed by sizing the mirror: the report's `The styles' magnitudes` table plays four other pairs. The closest is Production Run at +60% Run and -10% Precision Pass with Fine Tolerance left at +25% and -25%: Fine Tolerance is then 0.0 and 0.6 actions shorter at no and mid gear and 2.1 longer at best gear, so neither style leads everywhere.

## Open items

1. **Production Run's cost.** Production Run is never the shorter craft and is slower than the unperked crafter at every gear tier. Gate 2 cannot see it (quality saturates), so it passes with the approved +25% and -25%, which are implemented as approved. Recommended retune, for the owner to approve: Production Run +60% Run and -10% Precision Pass, Fine Tolerance unchanged (it breaks the "mirror" wording of the plan).
2. **Gate 6 at levels 11-20.** One cell of 60 misses by 0.006 (above): raise the Fabricator's XP multiplier for levels 11-20 to 1.50, or accept.
3. **Icons.** The nine perks reuse existing icons (`ife_reassemble`, `ife_gearhead`, `ife_repair`, `ife_mastercraft`, `ife_exmachine`, `ife_crafting_log`, `ife_steam-powere`, `ife_treasure_box`, `ife_buildrobot`) set as explicit perk icons, because new art could not be produced. Passive-gold framed custom art per `IconStandards.md` is a follow-up.
4. **Client evidence.** None yet: the owner deferred it to the Closeout joint pass. The window's 11-row action list and the perk line are unverified in the client.
