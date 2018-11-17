-- Crystal quest
INSERT INTO dbo.Quest ( ID ,
                        Name ,
                        JournalTag ,
                        FameRegionID ,
                        RequiredFameAmount ,
                        AllowRewardSelection ,
                        RewardGold ,
                        RewardKeyItemID ,
                        RewardFame ,
                        IsRepeatable ,
                        MapNoteTag ,
                        StartKeyItemID ,
                        RemoveStartKeyItemAfterCompletion ,
                        OnAcceptRule ,
                        OnAdvanceRule ,
                        OnCompleteRule ,
                        OnKillTargetRule ,
                        OnAcceptArgs ,
                        OnAdvanceArgs ,
                        OnCompleteArgs ,
                        OnKillTargetArgs )
VALUES ( 9 ,    -- ID - int
         N'Daggers for Crystal' ,  -- Name - nvarchar(100)
         N'daggers_crystal' ,  -- JournalTag - nvarchar(32)
         3 ,    -- FameRegionID - int
         0 ,    -- RequiredFameAmount - int
         0 , -- AllowRewardSelection - bit
         0 ,    -- RewardGold - int
         NULL ,    -- RewardKeyItemID - int
         15 ,    -- RewardFame - int
         0 , -- IsRepeatable - bit
         N'' ,  -- MapNoteTag - nvarchar(32)
         NULL ,    -- StartKeyItemID - int
         0 , -- RemoveStartKeyItemAfterCompletion - bit
         N'' ,  -- OnAcceptRule - nvarchar(32)
         N'' ,  -- OnAdvanceRule - nvarchar(32)
         N'' ,  -- OnCompleteRule - nvarchar(32)
         N'' ,  -- OnKillTargetRule - nvarchar(32)
         N'' ,  -- OnAcceptArgs - nvarchar(256)
         N'' ,  -- OnAdvanceArgs - nvarchar(256)
         N'' ,  -- OnCompleteArgs - nvarchar(256)
         N''    -- OnKillTargetArgs - nvarchar(256)
    )

INSERT INTO dbo.QuestState ( QuestID ,
                             Sequence ,
                             QuestTypeID ,
                             JournalStateID )
VALUES ( 9 , -- QuestID - int
         1 , -- Sequence - int
         4 , -- QuestTypeID - int
         1   -- JournalStateID - int
    )

DECLARE @QuestStateID INT = SCOPE_IDENTITY()
INSERT INTO dbo.QuestRequiredItem ( QuestID ,
                                    Resref ,
                                    Quantity ,
                                    QuestStateID ,
                                    MustBeCraftedByPlayer )
VALUES ( 9 ,   -- QuestID - int
         N'dagger_b' , -- Resref - nvarchar(16)
         5 ,   -- Quantity - int
         @QuestStateID ,   -- QuestStateID - int
         0  -- MustBeCraftedByPlayer - bit
    )

INSERT INTO dbo.QuestState ( QuestID ,
                             Sequence ,
                             QuestTypeID ,
                             JournalStateID )
VALUES ( 9 , -- QuestID - int
         2 , -- Sequence - int
         2 , -- QuestTypeID - int
         2   -- JournalStateID - int
    )


	
INSERT INTO dbo.QuestRewardItem ( QuestID ,
                                  Resref ,
                                  Quantity )
VALUES ( 9 ,   -- QuestID - int
         N'p_crystal_red_qs' , -- Resref - nvarchar(16)
         1     -- Quantity - int
    )


-- Wookiee Rug fabrication structure.


INSERT INTO dbo.BaseStructure ( ID ,
                                BaseStructureTypeID ,
                                Name ,
                                PlaceableResref ,
                                ItemResref ,
                                IsActive ,
                                Power ,
                                CPU ,
                                Durability ,
                                Storage ,
                                HasAtmosphere ,
                                ReinforcedStorage ,
                                RequiresBasePower ,
                                ResourceStorage ,
                                RetrievalRating ,
                                FuelRating )
VALUES ( 178 ,    -- ID - int
         8 ,    -- BaseStructureTypeID - int
         N'Wookiee Rug' ,  -- Name - nvarchar(64)
         N'wookiee_rug' ,  -- PlaceableResref - nvarchar(16)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 , -- IsActive - bit
         0.0 ,  -- Power - float
         0.0 ,  -- CPU - float
         0.0 ,  -- Durability - float
         0 ,    -- Storage - int
         1 , -- HasAtmosphere - bit
         0 ,    -- ReinforcedStorage - int
         0 , -- RequiresBasePower - bit
         0 ,    -- ResourceStorage - int
         0 ,    -- RetrievalRating - int
         0      -- FuelRating - int
    )

INSERT INTO dbo.CraftBlueprint ( ID ,
                                 CraftCategoryID ,
                                 BaseLevel ,
                                 ItemName ,
                                 ItemResref ,
                                 Quantity ,
                                 SkillID ,
                                 CraftDeviceID ,
                                 PerkID ,
                                 RequiredPerkLevel ,
                                 IsActive ,
                                 MainComponentTypeID ,
                                 MainMinimum ,
                                 SecondaryComponentTypeID ,
                                 SecondaryMinimum ,
                                 TertiaryComponentTypeID ,
                                 TertiaryMinimum ,
                                 EnhancementSlots ,
                                 MainMaximum ,
                                 SecondaryMaximum ,
                                 TertiaryMaximum ,
                                 BaseStructureID )
VALUES ( 642 ,    -- ID - int
         30 ,    -- CraftCategoryID - int
         10 ,    -- BaseLevel - int
         N'Wookiee Rug' ,  -- ItemName - nvarchar(64)
         N'furniture' ,  -- ItemResref - nvarchar(16)
         1 ,    -- Quantity - int
         15 ,    -- SkillID - int
         5 ,    -- CraftDeviceID - int
         2 ,    -- PerkID - int
         3 ,    -- RequiredPerkLevel - int
         1 , -- IsActive - bit
         13 ,    -- MainComponentTypeID - int
         1 ,    -- MainMinimum - int
         12 ,    -- SecondaryComponentTypeID - int
         2 ,    -- SecondaryMinimum - int
         0 ,    -- TertiaryComponentTypeID - int
         0 ,    -- TertiaryMinimum - int
         0 ,    -- EnhancementSlots - int
         1 ,    -- MainMaximum - int
         2 ,    -- SecondaryMaximum - int
         0 ,    -- TertiaryMaximum - int
         178      -- BaseStructureID - int
    )
