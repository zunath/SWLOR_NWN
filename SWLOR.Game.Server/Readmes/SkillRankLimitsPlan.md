# Skill Rank Limits Plan

Owner-approved design for an optional rank limit on each skill. A player can say "stop Piloting at 25" and stop worrying about decay. The feature is off unless a player sets a limit.

It ships on its own pull request, ahead of the space revamp, so players can set limits before they spend the Rebuild Token that release grants. The space revamp plan is not on `master` yet: it is `SWLOR.Game.Server/Readmes/SpaceRevampPlan.md` on branch `claude/festive-edison-bbnnxi`. Its migration section grants every character one Rebuild Token and points back to this plan. Delete this file after release; the lasting rules live in the code, the tests and the Player Guide.

## 1. What players get

- Every skill that counts toward the 400 skill cap gets a **Rank Limit**: **None** (the default) or a rank from 0 to the skill's max rank.
- **It's a ceiling.** A skill at or above its limit gains no ranks.
- **It's also a floor for decay.** At the skill cap, decay takes ranks from skills above their limits first, and never takes a limited skill below its limit.
- **It pairs with the decay lock.** The existing decay lock means "never lose ranks". A limit means "settle at this rank". Lock and limit at the same rank freeze a skill completely.
- **Off by default.** With None on every skill, everything behaves exactly as it does today.

## 2. Rules

1. **XP at the limit:** a skill at or above its limit behaves like a maxed skill does today (`Skill.GiveSkillXP`, the `pcSkill.Rank >= details.MaxRank` branch).
   - Its XP still pays down XP debt.
   - The rest is discarded, and its XP progress is set to 0.
   - The player gets a throttled warning through `PlayerFeedback.SendWarningToPlayer` with a new key (`SKILL_LIMIT`): "{Skill} is at your limit of {N}. Raise it in the Skills window."
2. **Reaching the limit mid-award:** the rank-up loop stops when the rank reaches the limit. Leftover XP is discarded and progress is set to 0.
3. **Limit below the current rank:** allowed. The skill keeps its ranks and gains nothing. It only moves down through cap decay (rule 4).
4. **Decay pick at the cap** (`skillsPossibleToDecay` in `Skill.GiveSkillXP`):
   - Candidates are the other skills that count toward the cap, are not locked, have rank above 0, and, when they have a limit, sit **above** it.
   - Pick at random among candidates above their limit first. Only if there are none, pick at random among unlocked skills with no limit, which is today's rule.
   - A limited skill at or below its limit never decays.
   - Recompute the candidates after every decay in the rank-up loop: a decay can bring a skill down to its limit, which can empty the above-limit pool.
5. **Blocked at the cap:** when no candidate remains, XP is blocked as today. Update both existing `SKILL_CAP` messages to say "locked or at their limits" instead of "locked from decay". XP debt the award already repaid stays repaid: the blocked path saves it (today it sends the "removed from your debt" message without saving).
6. **Scope:** every skill, languages included (players asked to stop learning languages they don't want). Languages max out at rank 20; the shared combo options run to 50, so a language pick above 20 saves as 20. Languages never decay and send no `SKILL_LIMIT` warning, because they gain XP from overheard speech and would warn whenever someone spoke the language.
7. **RP XP** (`Feature/GuiDefinition/ViewModel/DistributeRPXPViewModel.cs`): RP XP is never lost to a blocked award.
   - **Today's bug:** the window subtracts the amount from `UnallocatedXP` *before* calling `GiveSkillXP`. If the award is then blocked (at the cap with every other skill locked), the RP XP is lost. Limits make that state more common, so fix it here.
   - **Return the accepted amount:** change `GiveSkillXP` to return the XP it actually used: XP debt repaid plus XP applied to the skill. Discarded XP is not counted, so a blocked award returns only the debt it repaid (0 without debt). Existing callers can ignore the return value.
   - **Debit only what was accepted:** the window calls `GiveSkillXP` first, re-reads the player, and subtracts only the returned amount from `UnallocatedXP`.
   - **Refuse up front** when the skill is at its limit, or when the player is at the cap and no decay candidate remains (rule 5). Explain why in the window's own text.
   - **Cap the amount** at the XP needed to reach the limit. Distribution calls `GiveSkillXP` with `ignoreBonuses: true` and no henchman penalty, so the needed XP is exact.
8. **Rebuilds:** `CharacterFullRebuildViewModel` ignores limits during manual allocation. `RefundAllSkills` leaves `RankLimit` untouched; it still resets `IsLocked` as today. Limits persist through full rebuilds and Rebuild Tokens.
9. **Other rank writers:**
   - `PlayerInitialization` sets starting language ranks directly, outside `GiveSkillXP`, so it ignores limits; a limit only stops later gains.
   - The rebuild ignores limits (rule 8).
   - No other code raises a skill's rank outside `GiveSkillXP`. Recheck this with a search before coding.

## 3. Data

- **Field:** add `int? RankLimit` to `PlayerSkill` in `Entity/Player.cs`. `null` means None.
- **Migration:** none. `DB.Get` reads `Player` records with `JsonConvert.DeserializeObject`, which leaves a missing `RankLimit` as `null`.
- **Writing a limit:** only the Skills window writes it. Clamp to 0..`MaxRank`.

## 4. Skills window

Files: `Feature/GuiDefinition/SkillsDefinition.cs` and `Feature/GuiDefinition/ViewModel/SkillsViewModel.cs`. Follow `NuiLayoutRules.md`, `GuiWindowAuthoring.md` and `skills/swlor-gui-window-generation/SKILL.md`.

- **The control:** add a **Limit** control to each row, beside the existing Decay Lock button. It's a combo with "None" and every rank from the skill's max rank down to 0, in steps of 1, defaulting to None. The ranks run highest first because the dropdown opens at the top of its list and players usually limit a skill near the top of its range.
  - List-template combos share one static option list, so the options run to the highest max rank among skills that count toward the cap (50 for every such skill today); saving clamps to the row's own max rank.
  - The selection is a watched `GuiBindingList<int>` (value 0 = None, otherwise limit + 1). A watch event reports the whole list, so the view model diffs it against the last saved selections and saves only the changed rows. This is the first watched per-row list in the codebase, so the client evidence must show a combo change reaching the Player record.
- **When enabled:** on every row, languages included (rule 6).
- **Display:** the row shows the current choice ("None" or "25") under a "Limit" column header. Longer labels such as "Limit: None" were clipped at the window's default width.
- **Tooltip:** on the "Limit" header: "Stop a skill at a chosen rank. It never decays below its limit." A tooltip on the combo itself kept it from opening in the client, and the longer text ran off screen.
- **Saving:** a change saves at once and refreshes the row, the way `ToggleDecayLock` does.

## 5. Player Guide

`Feature/GuiDefinition/ViewModel/PlayerGuideViewModel.cs` has a "Skill Decay" article, decay FAQs and a Skills-window description. Update them:

- **Skill Decay article:** explain limits next to the decay lock.
- **"Why did I get no skill XP?":** add "or the skill is at the limit you set".
- **Skills window description:** add the Limit control.

Keep `PlayerGuideContentTests` passing. Game text uses ASCII punctuation (`CodingStandards.md`).

## 6. Tests and evidence

- **Harness:** `SkillXPAwardTests` compiles named `Skill.cs` methods into a harness, so any helper `GiveSkillXP` calls (rank limit, ceiling, decay candidates) must be added to its method list.
- **NUnit:** extend `SWLOR.Game.Server.Tests/Service/SkillXPAwardTests.cs` (or add `SkillRankLimitTests`) for:
  - gains stopping at the limit, including mid-award
  - XP debt repaid while at the limit
  - a limit below the current rank
  - decay picking above-limit skills first
  - decay never going below a limit
  - XP blocked when every other skill is locked or at its limit
  - non-contributing skills ignoring limits
  - the RP XP refusal and amount cap
  - RP XP kept, not debited, when the award is blocked at the cap with no decay candidate
  - `GiveSkillXP` returning the accepted amount, including debt repayment
  - limits surviving a rebuild reset
- **Existing tests to keep green:** `PlayerSkillPointAccountingTests` and `CharacterRebuildSkillAvailabilityTests`.
- **Message audit:**
  - Add the new `SKILL_LIMIT` message and the changed `SKILL_CAP` text to `Readmes/PlayerMessageAudit.json` (`PlayerFeedback.md`).
  - Update `PlayerMessageAuditTests.SkillCapWarnings_ShareOneProductionLimitAcrossSkillsAndOutcomes`. It requires exactly two `SendWarningToPlayer` calls in `Skill.cs`, all keyed `SKILL_CAP`. Keep that invariant for the cap warnings, and allow and assert exactly one separate `SKILL_LIMIT` warning.
- **Client evidence** (`InEngineVisualVerification.md`):
  - the Skills window with limits set
  - the limit warning after earning XP at a limit
  - a cap decay taking an above-limit skill
  - the RP XP window refusing a skill at its limit

## 7. Release

- One pull request from up-to-date `origin/master`, with the Codex review loop (`PullRequests.md`).
- No server or character migration expected (section 3).
- Add a line to the next Wiki release notes (`skills/swlor-release-notes/SKILL.md`).
