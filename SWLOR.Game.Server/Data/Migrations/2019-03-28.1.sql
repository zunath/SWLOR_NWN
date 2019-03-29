
CREATE NONCLUSTERED INDEX IX_PCCooldown_PlayerID
ON dbo.PCCooldown(PlayerID)
INCLUDE (CooldownCategoryID, DateUnlocked);

CREATE NONCLUSTERED INDEX IX_PCCraftedBlueprint_PlayerID
ON dbo.PCCraftedBlueprint(PlayerID)
INCLUDE (CraftBlueprintID, DateFirstCrafted);

CREATE NONCLUSTERED INDEX IX_PCCustomEffect_PlayerID
ON dbo.PCCustomEffect(PlayerID)
INCLUDE (CustomEffectID, Ticks, EffectiveLevel, Data, CasterNWNObjectID, StancePerkID);

CREATE NONCLUSTERED INDEX IX_PCImpoundedItem_PlayerID
ON dbo.PCImpoundedItem(PlayerID)
INCLUDE (ItemName, ItemTag, ItemResref, ItemObject, DateImpounded, DateRetrieved);

CREATE NONCLUSTERED INDEX IX_PCKeyItem_PlayerID
ON dbo.PCKeyItem(PlayerID)
INCLUDE (KeyItemID, AcquiredDate);

CREATE NONCLUSTERED INDEX IX_PCMapPin_PlayerID
ON dbo.PCMapPin(PlayerID)
INCLUDE (AreaTag, PositionX, PositionY, NoteText);

CREATE NONCLUSTERED INDEX IX_PCMapProgression_PlayerID
ON dbo.PCMapProgression(PlayerID)
INCLUDE (AreaResref, Progression);

CREATE NONCLUSTERED INDEX IX_PCObjectVisibility_PlayerID
ON dbo.PCObjectVisibility(PlayerID)
INCLUDE (VisibilityObjectID, IsVisible);

CREATE NONCLUSTERED INDEX IX_PCOutfit_PlayerID
ON dbo.PCOutfit(PlayerID)
INCLUDE (Outfit1, Outfit2, Outfit3, Outfit4, Outfit5, Outfit6, Outfit7, Outfit8, Outfit9, Outfit10);

CREATE NONCLUSTERED INDEX IX_PCOverflowItem_PlayerID
ON dbo.PCOverflowItem(PlayerID)
INCLUDE (ItemName, ItemTag, ItemResref, ItemObject);

CREATE NONCLUSTERED INDEX IX_PCPerk_PlayerID
ON dbo.PCPerk(PlayerID)
INCLUDE (AcquiredDate, PerkID, PerkLevel);

CREATE NONCLUSTERED INDEX IX_PCQuestItemProgress_PlayerID
ON dbo.PCQuestItemProgress(PlayerID)
INCLUDE (PCQuestStatusID, Resref, Remaining, MustBeCraftedByPlayer);

CREATE NONCLUSTERED INDEX IX_PCQuestKillTargetProgress_PlayerID
ON dbo.PCQuestKillTargetProgress(PlayerID)
INCLUDE (PCQuestStatusID, NPCGroupID, RemainingToKill);

CREATE NONCLUSTERED INDEX IX_PCQuestStatus_PlayerID
ON dbo.PCQuestStatus(PlayerID)
INCLUDE (QuestID, CurrentQuestStateID, CompletionDate, SelectedItemRewardID);

CREATE NONCLUSTERED INDEX IX_PCRegionalFame_PlayerID
ON dbo.PCRegionalFame(PlayerID)
INCLUDE (FameRegionID, Amount);

CREATE NONCLUSTERED INDEX IX_PCSearchSite_PlayerID
ON dbo.PCSearchSite(PlayerID)
INCLUDE (SearchSiteID, UnlockDateTime);

CREATE NONCLUSTERED INDEX IX_PCSkill_PlayerID
ON dbo.PCSkill(PlayerID)
INCLUDE (SkillID, XP, Rank, IsLocked);

CREATE NONCLUSTERED INDEX IX_BankItem_PlayerID
ON dbo.BankItem(PlayerID)
INCLUDE (BankID, ItemID, ItemName, ItemTag, ItemResref, ItemObject, DateStored);

CREATE NONCLUSTERED INDEX IX_PCSkillPool_PlayerID
ON dbo.PCSkillPool(PlayerID)
INCLUDE (SkillCategoryID, Levels);
