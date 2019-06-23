
DELETE FROM dbo.PerkLevelSkillRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)
)

DELETE FROM dbo.PerkLevelQuestRequirement
WHERE PerkLevelID IN (
	SELECT ID
	FROM dbo.PerkLevel
	WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)
)

DELETE FROM dbo.PCPerkRefund
WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)

DELETE FROM dbo.PerkLevel
WHERE PerkID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)

DELETE FROM dbo.Perk
WHERE ID IN (38, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 129)




INSERT INTO dbo.Perk ( ID ,
                       Name ,
                       IsActive ,
                       BaseCastingTime ,
                       Description ,
                       PerkCategoryID ,
                       CooldownCategoryID ,
                       ExecutionTypeID ,
                       IsTargetSelfOnly ,
                       Enmity ,
                       EnmityAdjustmentRuleID ,
                       CastAnimationID ,
                       ForceBalanceTypeID )
VALUES ( 38 ,    -- ID - int
         'Guild Relations' ,   -- Name - varchar(64)
         1 , -- IsActive - bit
         0.0 ,  -- BaseCastingTime - float
         N'Improves your GP acquisition with all guilds.' ,  -- Description - nvarchar(256)
         4 ,    -- PerkCategoryID - int
         NULL ,    -- CooldownCategoryID - int
         0 ,    -- ExecutionTypeID - int
         0 , -- IsTargetSelfOnly - bit
         0 ,    -- Enmity - int
         0 ,    -- EnmityAdjustmentRuleID - int
         0 ,    -- CastAnimationID - int
         0      -- ForceBalanceTypeID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         1 ,   -- Level - int
         6 ,   -- Price - int
         N'Doubles GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    ) 

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         2 ,   -- Level - int
         6 ,   -- Price - int
         N'Triples GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )

INSERT INTO dbo.PerkLevel ( PerkID ,
                            Level ,
                            Price ,
                            Description ,
                            SpecializationID )
VALUES ( 38 ,   -- PerkID - int
         3 ,   -- Level - int
         6 ,   -- Price - int
         N'Quadruples GP gain.' , -- Description - nvarchar(512)
         0     -- SpecializationID - int
    )





INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 16 , -- ID - int
         N'Viscara Crystal Spider' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 17 , -- ID - int
         N'Mon Cala Aradile' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 18 , -- ID - int
         N'Mon Cala Viper' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 19 , -- ID - int
         N'Mon Cala Amphi-Hydrus' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 20 , -- ID - int
         N'Mon Cala Eco Terrorist' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 21 , -- ID - int
         N'Vellen Flesheater' -- Name - nvarchar(32)
    )

DECLARE @GuildID INT = 1
DECLARE @QuestID INT = 566
DECLARE @Name NVARCHAR(128)
DECLARE @Quantity INT 
DECLARE @GP INT 
DECLARE @Gold INT 
DECLARE @NPCGroupID INT
DECLARE @RequiredRank INT 
DECLARE @Resref NVARCHAR(16)

------------
-- RANK 0 --
------------

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x CZ-220 Mynock'
SET @GP = 7
SET @Gold = 20
SET @NPCGroupID = 1
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x CZ-220 Malfunctioning Droid'
SET @GP = 7
SET @Gold = 37
SET @NPCGroupID = 2
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , 0 , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 1
SET @Name = '1x CZ-220 Colicoid Experiment'
SET @GP = 15
SET @Gold = 53
SET @NPCGroupID = 3
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Viscara Kath Hound'
SET @GP = 12
SET @Gold = 65
SET @NPCGroupID = 4
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Viscara Warocas'
SET @GP = 12
SET @Gold = 65
SET @NPCGroupID = 14
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Wildwoods Looter'
SET @GP = 12
SET @Gold = 65
SET @NPCGroupID = 8
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 8
SET @Name = '8x Wildwoods Gimpassa'
SET @GP = 13
SET @Gold = 70
SET @NPCGroupID = 9
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Wildwoods Kinrath'
SET @GP = 13
SET @Gold = 70
SET @NPCGroupID = 10
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Wing'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_wing'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0) 
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Tooth'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_tooth'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Wing'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_wing'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mynock Tooth'
SET @GP = 7
SET @Gold = 23
SET @Resref = 'mynock_tooth'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Kath Hound Fur'
SET @GP = 12
SET @Gold = 65
SET @Resref = 'k_hound_fur'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Kath Hound Meat'
SET @GP = 12
SET @Gold = 65
SET @Resref = 'kath_meat_1'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Kath Hound Tooth'
SET @GP = 14
SET @Gold = 67
SET @Resref = 'k_hound_tooth'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Warocas Leg'
SET @GP = 14
SET @Gold = 67
SET @Resref = 'waro_leg'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Warocas Meat'
SET @GP = 14
SET @Gold = 67
SET @Resref = 'warocas_meat'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Warocas Spine'
SET @GP = 15
SET @Gold = 68
SET @Resref = 'waro_feathers'
SET @RequiredRank = 0

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)




------------
-- RANK 1 --
------------


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mandalorian Warrior'
SET @GP = 19
SET @Gold = 76
SET @NPCGroupID = 6
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mandalorian Ranger'
SET @GP = 19
SET @Gold = 76
SET @NPCGroupID = 7
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 1
SET @Name = '1x Mandalorian Leader'
SET @GP = 24
SET @Gold = 82
SET @NPCGroupID = 5
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Valley Cairnmog'
SET @GP = 27
SET @Gold = 84
SET @NPCGroupID = 11
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Valley Raivor'
SET @GP = 27
SET @Gold = 84
SET @NPCGroupID = 13
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Valley Nashtah'
SET @GP = 27
SET @Gold = 84
SET @NPCGroupID = 15
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalore Herb'
SET @GP = 19
SET @Gold = 76
SET @Resref = 'herb_m'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Dog Tags'
SET @GP = 20
SET @Gold = 80
SET @Resref = 'man_tags'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Plexi-plate'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_plexiplate'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Blaster Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_blast_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Large Vibroblade Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_lvibro_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Lightsaber Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_ls_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Polearm Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_polearm_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Mandalorian Vibroblade Parts'
SET @GP = 25
SET @Gold = 83
SET @Resref = 'm_vibro_parts'
SET @RequiredRank = 1

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

------------
-- RANK 2 --
------------

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Blue Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_blue'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Green Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_green'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Red Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_red'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Damaged Yellow Crystal'
SET @GP = 39
SET @Gold = 122
SET @Resref = 'p_crystal_yellow'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Crystal Spider'
SET @GP = 39
SET @Gold = 122
SET @NPCGroupID = 16
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mon Cala Aradile'
SET @GP = 52
SET @Gold = 212
SET @NPCGroupID = 17
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mon Cala Viper'
SET @GP = 52
SET @Gold = 212
SET @NPCGroupID = 18
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Mon Cala Amphi-Hydrus'
SET @GP = 52
SET @Gold = 212
SET @NPCGroupID = 19
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Amphi-Hydrus Brain Stem'
SET @GP = 52
SET @Gold = 212
SET @Resref = 'amphi_brain2'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 6
SET @Name = '6x Amphi-Hydrus Brain'
SET @GP = 52
SET @Gold = 212
SET @Resref = 'amphi_brain'
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 4, 1)
INSERT INTO dbo.QuestRequiredItem ( QuestID ,Resref ,Quantity ,QuestStateID ,MustBeCraftedByPlayer )
VALUES ( @QuestID , @Resref, @Quantity, SCOPE_IDENTITY(), 0)

INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)


-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 1
SET @Name = '1x Vellen Fleshleader'
SET @GP = 44
SET @Gold = 184
SET @NPCGroupID = 12
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)

-- ===============
SET @QuestID = @QuestID + 1
SET @Quantity = 10
SET @Name = '10x Vellen Flesheater'
SET @GP = 44
SET @Gold = 184
SET @NPCGroupID = 21
SET @RequiredRank = 2

INSERT INTO dbo.Quest ( ID ,Name ,JournalTag ,FameRegionID ,RequiredFameAmount ,AllowRewardSelection ,RewardGold ,RewardKeyItemID ,RewardFame ,IsRepeatable ,MapNoteTag ,StartKeyItemID ,RemoveStartKeyItemAfterCompletion ,OnAcceptRule ,OnAdvanceRule ,OnCompleteRule ,OnKillTargetRule ,OnAcceptArgs ,OnAdvanceArgs ,OnCompleteArgs ,OnKillTargetArgs ,RewardGuildID ,RewardGuildPoints )
VALUES ( @QuestID , N'Hunter''s Guild Task: ' + @Name , N'hun_tsk_' + CAST(@QuestID AS NVARCHAR(10)),  1 ,0 ,0 ,@Gold , NULL ,0 ,1 ,N'' ,NULL ,0 ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,N'' ,@GuildID , @GP)

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 1, 1, 1)
INSERT INTO dbo.QuestKillTarget ( QuestID, NPCGroupID , Quantity , QuestStateID )
VALUES ( @QuestID, @NPCGroupID, @Quantity, SCOPE_IDENTITY())

INSERT INTO dbo.QuestState ( QuestID , Sequence , QuestTypeID , JournalStateID )
VALUES (@QuestID, 2, 2, 1)
INSERT INTO dbo.GuildTask ( GuildID , QuestID , RequiredRank , IsCurrentlyOffered )
VALUES ( @GuildID , @QuestID , @RequiredRank , 0)
