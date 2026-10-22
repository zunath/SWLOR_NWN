# SWLOR.Game.Server Guides

One line per guide. Rule guides are required reading before working in their area; the root `AGENTS.md` points to each one under Guides.

## Rules and procedures

- [CodingStandards.md](CodingStandards.md): code conventions, Module JSON editing, StatType design, TLK and 2DA data.
- [ContentStandards.md](ContentStandards.md): balance standards, item generation, loot, phenotypes, ship-access locations.
- [SkillPerkStandards.md](SkillPerkStandards.md): skill/perk progression, cross-skill gameplay, shared mechanics, balance evidence, and player descriptions.
- [Migrations.md](Migrations.md): one migration per release, one pass per data set, server before player, saved-data item sweeps.
- [PullRequests.md](PullRequests.md): branches, the Codex review loop, review threads, public text.
- [AgentEnvironment.md](AgentEnvironment.md): worktrees and the `SWLOR_Haks` submodule, git network fixes, shared-machine and disk hygiene, builds, local deploy, cloud sessions.
- [InEngineVisualVerification.md](InEngineVisualVerification.md): required NWN client evidence for every change, offline and on a live server.
- [EngineTesting.md](EngineTesting.md): `[EngineTest]` in-engine tests, including persistence and migration coverage.
- [AbilityTargeting.md](AbilityTargeting.md): active ability targeting, native feat/spell rows, quick-cast variants.
- [IconStandards.md](IconStandards.md): gameplay icon color, uniqueness, rank badges, generation, audits.
- [KeyItemIconStandards.md](KeyItemIconStandards.md): key item (`iki_`) icon format and placement.
- [VisualEffectSelection.md](VisualEffectSelection.md) and [VisualEffectReference.csv](VisualEffectReference.csv): choosing VFX by gameplay moment.
- [Conversations.md](Conversations.md): authored dialogue graphs in `ConversationData/`.
- [PlayerIdentity.md](PlayerIdentity.md): showing player names through the `PlayerName` service.
- [PlayerFeedback.md](PlayerFeedback.md): which player messages stay enabled in Production.
- [ItemAppearanceRules.md](ItemAppearanceRules.md): choosing item models; reserved helmet model 223.
- [EconomyRestrictedItems.md](EconomyRestrictedItems.md): keeping NPC-only and unobtainable items out of player economy surfaces.
- [NpcHitPoints.md](NpcHitPoints.md): creature HP, stat skins, and the 16-bit HP limit.
- [NuiLayoutRules.md](NuiLayoutRules.md) and [GuiWindowAuthoring.md](GuiWindowAuthoring.md): NUI layout rules and building a window end to end.
- [DesignBibleWorkbookRules.md](DesignBibleWorkbookRules.md): editing Design Bible workbooks safely.
- [QuestContentValidation.md](QuestContentValidation.md): the quest audit and what it enforces.

## Architecture reference

- [ProjectStructure.md](ProjectStructure.md): solution layout and build setup.
- [CoreSystems.md](CoreSystems.md): NWScript/NWNX wrappers, async, GUI framework.
- [Services.md](Services.md): the static game services.
- [Entities.md](Entities.md): Redis-persisted entities and queries.
- [Features.md](Features.md) and [Builders.md](Builders.md): content definitions and their builders.
- [AbilityDefinitions.md](AbilityDefinitions.md): ability definitions in practice.
- [DualWieldCombat.md](DualWieldCombat.md): dual-wield and double-weapon attack timing.
- [Weather.md](Weather.md): the weather service.
- [Deployment.md](Deployment.md): Docker, environments, monitoring.

## Content pipelines

- [BlasterModelImport.md](BlasterModelImport.md) and [RifleModelImport.md](RifleModelImport.md): weapon model import and fitting.
- [TintMapRendering.md](TintMapRendering.md): tint material rendering.
- [MimicryBalance.md](MimicryBalance.md): the Mimicry technique balance model.

## Plans, audits, and data

- [PlayerRecognitionAndDisguisesPlan.md](PlayerRecognitionAndDisguisesPlan.md), [BeastMutationExpansionPlan.md](BeastMutationExpansionPlan.md), [VelesTradeConcourse.md](VelesTradeConcourse.md), [SkillRankLimitsPlan.md](SkillRankLimitsPlan.md): feature plans.
- [WeatherAreaAudit.md](WeatherAreaAudit.md): per-area weather audit.
- [GameplayIconManifest.csv](GameplayIconManifest.csv), [CombatUpgradeBiblePerkManifest.csv](CombatUpgradeBiblePerkManifest.csv), [PlayerMessageAudit.json](PlayerMessageAudit.json): data read by tools and tests.

When adding a guide, add one line here and, for a rule guide, one line under Guides in `AGENTS.md`.
