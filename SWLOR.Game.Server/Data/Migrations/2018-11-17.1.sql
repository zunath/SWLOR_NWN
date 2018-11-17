
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