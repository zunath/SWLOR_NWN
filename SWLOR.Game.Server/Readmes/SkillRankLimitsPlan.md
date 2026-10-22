# Skill Rank Limits Plan

Owner-approved design for an optional rank limit on each skill. A player can say "stop Piloting at 25" and stop worrying about decay. The feature is off unless a player sets a limit.

It ships on its own pull request, ahead of the space revamp (`SpaceRevampPlan.md`), so players can set limits before they spend the Rebuild Token that release grants. Delete this file after release; the lasting rules live in the code, the tests and the Player Guide.

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
5. **Blocked at the cap:** when no candidate remains, XP is blocked as today. Update both existing `SKILL_CAP` messages to say "locked or at their limits" instead of "locked from decay".
6. **Scope:** only skills with `ContributesToSkillCap`, the same set as the decay lock. Languages and other non-contributing skills have no limit control and ignore any stored value.
7. **RP XP** (`Feature/GuiDefinition/ViewModel/DistributeRPXPViewModel.cs`):
   - The window refuses a skill at its limit.
   - It caps the amount at the XP needed to reach the limit. Distribution calls `GiveSkillXP` with `ignoreBonuses: true` and no henchman penalty, so the needed XP is exact.
   - The window explains the refusal or the cap in its own text.
8. **Rebuilds:** `CharacterFullRebuildViewModel` ignores limits during manual allocation. `RefundAllSkills` leaves `RankLimit` untouched; it still resets `IsLocked` as today. Limits persist through full rebuilds and Rebuild Tokens.
9. **Other rank writers:**
   - `PlayerInitialization` sets language ranks, and languages are out of scope.
   - The rebuild ignores limits (rule 8).
   - No other code raises a skill's rank outside `GiveSkillXP`. Recheck this with a search before coding.

## 3. Data

- **Field:** add `int? RankLimit` to `PlayerSkill` in `Entity/Player.cs`. `null` means None.
- **Migration:** a missing field deserializes as `null`, so no migration should be needed. Confirm this against how `Player` records are read (`Readmes/Entities.md`, `Readmes/Migrations.md`). Only add a character migration if existing records would fail to load.
- **Writing a limit:** only the Skills window writes it. Clamp to 0..`MaxRank`.

## 4. Skills window

Files: `Feature/GuiDefinition/SkillsDefinition.cs` and `Feature/GuiDefinition/ViewModel/SkillsViewModel.cs`. Follow `NuiLayoutRules.md`, `GuiWindowAuthoring.md` and `skills/swlor-gui-window-generation/SKILL.md`.

- **The control:** add a **Limit** control to each row, beside the existing Decay Lock button. It's a combo with "None" and every rank from 0 to the skill's max rank, defaulting to None.
- **When enabled:** only for skills that count toward the cap, matching `DecayLockButtonEnabled`.
- **Display:** the row shows the current choice ("Limit: None" or "Limit: 25").
- **Tooltip:** "Stop this skill at a chosen rank. At the 400-rank cap, skills above their limit decay first, and a skill never decays below its limit."
- **Saving:** a change saves at once and refreshes the row, the way `ToggleDecayLock` does.

## 5. Player Guide

`Feature/GuiDefinition/ViewModel/PlayerGuideViewModel.cs` has a "Skill Decay" article, decay FAQs and a Skills-window description. Update them:

- **Skill Decay article:** explain limits next to the decay lock.
- **"Why did I get no skill XP?":** add "or the skill is at the limit you set".
- **Skills window description:** add the Limit control.

Keep `PlayerGuideContentTests` passing. Game text uses ASCII punctuation (`CodingStandards.md`).

## 6. Tests and evidence

- **NUnit:** extend `SWLOR.Game.Server.Tests/Service/SkillXPAwardTests.cs` (or add `SkillRankLimitTests`) for:
  - gains stopping at the limit, including mid-award
  - XP debt repaid while at the limit
  - a limit below the current rank
  - decay picking above-limit skills first
  - decay never going below a limit
  - XP blocked when every other skill is locked or at its limit
  - non-contributing skills ignoring limits
  - the RP XP refusal and amount cap
  - limits surviving a rebuild reset
- **Existing tests to keep green:** `PlayerSkillPointAccountingTests` and `CharacterRebuildSkillAvailabilityTests`.
- **Message audit:** add the new `SKILL_LIMIT` message and the changed `SKILL_CAP` text to `Readmes/PlayerMessageAudit.json` (`PlayerFeedback.md`).
- **Client evidence** (`InEngineVisualVerification.md`):
  - the Skills window with limits set
  - the limit warning after earning XP at a limit
  - a cap decay taking an above-limit skill
  - the RP XP window refusing a skill at its limit

## 7. Release

- One pull request from up-to-date `origin/master`, with the Codex review loop (`PullRequests.md`).
- No server or character migration expected (section 3).
- Add a line to the next Wiki release notes (`skills/swlor-release-notes/SKILL.md`).
