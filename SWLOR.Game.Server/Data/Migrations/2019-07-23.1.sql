
UPDATE dbo.Quest
SET Name = 'Coxxion Initiation' WHERE ID = 25

UPDATE dbo.Quest
SET Name = 'Repairing Coxxion Equipment' WHERE ID = 26

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 22 , -- ID - int
         N'Abandoned Station Boss' -- Name - nvarchar(32)
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
                        OnKillTargetArgs ,
                        RewardGuildID ,
                        RewardGuildPoints )
VALUES ( 31 ,    -- ID - int
         N'The Abandoned Station' ,  -- Name - nvarchar(100)
         N'aban_station' ,  -- JournalTag - nvarchar(32)
         1 ,    -- FameRegionID - int
         0 ,    -- RequiredFameAmount - int
         0 , -- AllowRewardSelection - bit
         4000 ,    -- RewardGold - int
         NULL ,    -- RewardKeyItemID - int
         20 ,    -- RewardFame - int
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
         N'' ,  -- OnKillTargetArgs - nvarchar(256)
         NULL ,    -- RewardGuildID - int
         0      -- RewardGuildPoints - int
    )

INSERT INTO dbo.QuestState ( QuestID ,
                             Sequence ,
                             QuestTypeID ,
                             JournalStateID )
VALUES ( 31 , -- QuestID - int
         1 , -- Sequence - int
         0 , -- QuestTypeID - int
         1   -- JournalStateID - int
    )

INSERT INTO dbo.QuestKillTarget ( QuestID ,
                                  NPCGroupID ,
                                  Quantity ,
                                  QuestStateID )
VALUES ( 31 , -- QuestID - int
         22 , -- NPCGroupID - int
         1 , -- Quantity - int
         SCOPE_IDENTITY()   -- QuestStateID - int
    )

INSERT INTO dbo.QuestState ( QuestID ,
                             Sequence ,
                             QuestTypeID ,
                             JournalStateID )
VALUES ( 31 , -- QuestID - int
         2 , -- Sequence - int
         0 , -- QuestTypeID - int
         2   -- JournalStateID - int
    )

INSERT INTO dbo.QuestPrerequisite ( ID ,
                                    QuestID ,
                                    RequiredQuestID )
VALUES ( 74 , -- ID - int
         31 , -- QuestID - int
         29   -- RequiredQuestID - int
    )