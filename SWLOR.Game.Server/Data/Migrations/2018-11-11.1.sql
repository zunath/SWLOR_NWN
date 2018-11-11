
INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 46 , -- ID - int
         N'Viscara - Crystal Cavern Spiders' -- Name - nvarchar(64)
    )
	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 46 ,    -- LootTableID - int
         'p_crystal_blue' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 46 ,    -- LootTableID - int
         'p_crystal_red' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 46 ,    -- LootTableID - int
         'p_crystal_green' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 46 ,    -- LootTableID - int
         'p_crystal_yellow' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )



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
VALUES ( 30 ,    -- ID - int
         N'First Rites' ,  -- Name - nvarchar(100)
         N'first_rites' ,  -- JournalTag - nvarchar(32)
         1 ,    -- FameRegionID - int
         0 ,    -- RequiredFameAmount - int
         0 , -- AllowRewardSelection - bit
         1000 ,    -- RewardGold - int
         NULL ,    -- RewardKeyItemID - int
         10 ,    -- RewardFame - int
         0 , -- IsRepeatable - bit
         N'' ,  -- MapNoteTag - nvarchar(32)
         NULL ,    -- StartKeyItemID - int
         0 , -- RemoveStartKeyItemAfterCompletion - bit
         N'ShowQuestObjectRule' ,  -- OnAcceptRule - nvarchar(32)
         N'HideQuestGiverRule' ,  -- OnAdvanceRule - nvarchar(32)
         N'' ,  -- OnCompleteRule - nvarchar(32)
         N'' ,  -- OnKillTargetRule - nvarchar(32)
         N'81533EBB-2084-4C97-B004-8E1D8C395F56' ,  -- OnAcceptArgs - nvarchar(256)
         N'' ,  -- OnAdvanceArgs - nvarchar(256)
         N'' ,  -- OnCompleteArgs - nvarchar(256)
         N''    -- OnKillTargetArgs - nvarchar(256)
    )
	
INSERT INTO dbo.QuestState ( QuestID ,
                             Sequence ,
                             QuestTypeID ,
                             JournalStateID )
VALUES ( 30 , -- QuestID - int
         1 , -- Sequence - int
         0 , -- QuestTypeID - int
         1   -- JournalStateID - int
    )
INSERT INTO dbo.QuestState ( QuestID ,
                             Sequence ,
                             QuestTypeID ,
                             JournalStateID )
VALUES ( 30 , -- QuestID - int
         2 , -- Sequence - int
         0 , -- QuestTypeID - int
         1   -- JournalStateID - int
    )