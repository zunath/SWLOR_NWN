# Agriculture perk line: Angler (implemented)

The line is implemented on `claude/crafting-revamp-perks-agriculture`. This file started as the draft for the owner's approval; it now records what was built, the owner's decisions of October 2026 that shaped it, and the balance evidence. Rules followed: `CraftingRevampPlan.md` (Line schedule, the Angler row under Lines, Agriculture fishing) and `SkillPerkStandards.md`.

- Code: `Feature/PerkDefinition/AgricultureAnglerPerkDefinition.cs` (six perks, ten purchases), `LineAnglerEffects` (the one place stats become engine numbers: `FromStatTotals`), `LineSetup`, `LineEngine`, `FishingBite` (carries the angler's effects from the bite into the fight), `FishingFightLifecycle`, `LineDefinition` (the help page).
- IDs: PerkType 913-918, StatType 1242-1247 (inside the reserved ranges 904-923 and 1228-1247). `iprp_resperk.2da` needs no rows (no perk here restricts an item), and no TLK strings are new (perk names and descriptions are C# text). No effect icon: the owned style is always active and is not a status effect.
- Bible: the Agriculture tab, rows 29-40, written by `tools/UpdateAgriculturePerkBible.py` from the perk definitions.
- Evidence: `agriculture-angler-perks.md` (`SWLOR.CraftSim angler-perks`) and `fishing-line-parity.md` (`SWLOR.CraftSim fishing`: the fight bound, length and XP-rate gates for the unperked angler and for the whole line in each style).

## Structure

- One independent, optional line of 25 SP. It is not needed to catch fish: Quick Reel lands them at today's rate for everyone, and an unperked skilled fight lands slightly more of them (62% of at-level fish against Quick Reel's 60%) for much more XP.
- Ten purchases at Agriculture ranks 5/10/15/20/25/30/35/40/45/50, priced 1/1/2/2/3/3/3/3/3/4 SP. The rank-20 purchase is the line's style, a choice of two mutually exclusive styles through `RequirementCannotHavePerk`; switching costs a Perk Refund Token, like any perk. The rank-50 purchase is the capstone with no quest requirement.
- Every effect acts only on the Line, the fishing fight. All four actions (Set Hook, Reel, Give Line, Hold) are available at rank 0, so no perk unlocks an action; each perk adjusts one number. **No perk touches Quick Reel**, whose chance is today's formula at one fixed Willpower score (a test pins that the Angler stats have no reader there).
- The owned style is always active: no toggle, no status effect.
- When a bite opens the fight, `Fishing.cs` reads the angler's stat totals once into an immutable `LineAnglerEffects` (on the `FishingBite`), and `FishingFightLifecycle` passes it to `LineSetup.ForFish`. The engine reads only that record, never a perk, so previews and results stay pure, and a perk bought or refunded during a fight changes the next fight, not this one. The fish's pull is a share of the Tension cap before perks, so a perk that raises the cap does not make the fish stronger.
- **Rare fish** (owner decision): the fight perks act at half strength, rounded down (a bonus of 1 does nothing), Fish Sense does not work, and Trophy Catch is the only perk at full strength.

## Angler (fishing)

| Rank | SP | Perk | Type | Effect | StatTypes the engine reads |
| --- | --- | --- | --- | --- | --- |
| 5 | 1 | Strong Line I | trait | The line holds 2 more Tension before it snaps. | `AnglerTensionCap` +2 |
| 10 | 1 | Hard Set I | trait | Set Hook drains 1 more Stamina. | `AnglerSetHookDrain` +1 |
| 15 | 2 | Fish Sense I | trait | The window shows the fish's next two moves on the first turn of a fight. | `AnglerForecastTurns` +1 |
| 20 | 2 | Patient Angler | style (exclusive with Aggressive Angler) | Give Line lets out 1 less Distance (3 instead of 4). | `AnglerGiveLineDistanceReduction` +1 |
| 20 | 2 | Aggressive Angler | style (exclusive with Patient Angler) | Each Reel pulls the fish in 1 more Distance. | `AnglerReelDistance` +1 |
| 25 | 3 | Strong Line II | trait | The line holds 5 more Tension in total (replaces Strong Line I's 2). | `AnglerTensionCap` +5 |
| 30 | 3 | Hard Set II | trait | Set Hook drains 2 more Stamina in total. | `AnglerSetHookDrain` +2 |
| 35 | 3 | Fish Sense II | trait | The two-move view lasts the first 3 turns. | `AnglerForecastTurns` +3 |
| 40 | 3 | Strong Line III | trait | The line holds 8 more Tension in total. | `AnglerTensionCap` +8 |
| 45 | 3 | Hard Set III | trait | Set Hook drains 3 more Stamina in total. | `AnglerSetHookDrain` +3 |
| 50 | 4 | Trophy Catch | capstone, trait | A rare fish fights as if it were 4 steps easier on the difficulty ladder, a little over half a level weaker. It is the only perk that works on a rare fish at full strength. | `AnglerRareFishRungs` +4 |

The rank-20 row appears twice because the purchase is a choice; the line has ten purchases and the angler owns one of the two at rank 20. SP check: 1 + 1 + 2 + 2 + 3 + 3 + 3 + 3 + 3 + 4 = 25.

Owner decisions applied (October 2026): Steady Hold is dropped; Fish Sense is limited to the first turns of a fight (one turn at rank 15, three at rank 35); the +15% bound over Quick Reel applies to the unperked skilled fight only, and the whole line lifts the fight to about 90% of the fish at the angler's own level; fight perks at half strength on a rare fish and Trophy Catch the only full-strength rare-fish perk; one help paragraph per style (`LineDefinition`).

Decisions the line creates:

- Patient Angler against Aggressive Angler: Patient keeps the fish close by making retreat cheap, so it suits an angler who gives line to calm a Dive or a Thrash; Aggressive reels faster, so it suits one who reels against Tire and Run. The styles are balanced to within a couple of points (below).
- Hard Set against Strong Line: draining Stamina ends the fight early because an exhausted fish only rests, while a bigger cap lets the angler survive a longer one.
- Fish Sense turns "react to the next move" into "plan around two moves" for the first turns, which are the turns that set the fight's course.

## Balance evidence

`agriculture-angler-perks.md` (seed 1, 1000 fights per case; the adaptive player stands for the skilled fight) and `fishing-line-parity.md` (the gates).

- **At the fish's own level**, the unperked skilled fight lands about 64% (Quick Reel 60%, bound 72%). With every purchase the rank allows, at rank 50: **Patient Angler 92%, Aggressive Angler 91%** against the owner's target of about 90% (judged within 5 points). The line is worth +14 to +21 points at rank 20 and 30 and about +28 at ranks 40 and 50.
- **Fish above the level**: the line closes some of the gap to Quick Reel's cliff, as the tables in `agriculture-angler-perks.md` show, and no fish Quick Reel cannot land at all (6 or more levels above the rank) becomes landable.
- **Styles**: with the whole line Patient Angler is the better style on about half the cases and Aggressive Angler on the other half, with a mean gap under 1 point (ties within 2 points count half).
- **Length** (3-6 actions): passes for the unperked angler and for the line in either style, at every row.
- **XP rate** (gate 6): the line shortens fights, which shortens the XP a fight pays, so the multipliers of fish-level bands 11-20, 21-30 and 31-40 rose to 8.65, 8.35 and 8.65 (`LineRules`, from 8.5, 8.0 and 8.2): the least steps that pass for the unperked angler and for both styles. The unperked angler's XP per hour is 1.00x to 1.06x of today's in those bands. Gate 6 passes for all three.
- **Rare fish** (level 52, rank 50, tier 4 rod): no perks 39%; Trophy Catch alone 46% (the old bound is 49.0%); the line below the capstone with fight perks at half strength 51%; the whole line with Trophy Catch 49-52%. The old bound judges the rows no perk but Trophy Catch touches; the rows with the fight perks are evidence, not judged, since the owner replaced the bound with the half-strength rule.

## Needs an owner decision

1. **Trophy Catch adds little on top of the line.** The half-strength fight perks already lift a rare fish from 39% to about 51% (Strong Line III's +8 halves to +4), so Trophy Catch's 4 rungs add only 0 to 2 points beside them (alone it adds 7). Making it larger (6 rungs lands the whole line at 53-54%) would put rare fish about 5 points over the old 49.0% check. Options: leave it (rare fish are supply-limited by the spawn, not the catch rate), raise Trophy Catch, or cut the fight perks on a rare fish further than half.
2. **Aggressive Angler is a little weaker at rank 50 than Patient Angler** in some samples (89-92% against 85-91% across seeds), inside the style tolerance. A second point of Reel Distance did not help in the simulator, so the number was left at 1.

## Player descriptions

The Bible and the perk window use the text in `AgricultureAnglerPerkDefinition.cs` (ASCII, no internal terms). The Line window's help page has a paragraph on Angler perks and one per style.
