-- =============================================
-- Author:		Poet
-- Create date: 2018-12-06
-- Description: Adding Weapon Finesse for Sabers
-- =============================================

BEGIN TRANSACTION

DECLARE @PerkID INT = (SELECT MAX(ID) +1 FROM dbo.Perk)

INSERT INTO [dbo].[Perk]
           ([ID]
		   ,[Name]
           ,[FeatID]
           ,[IsActive]
           ,[ScriptName]
           ,[BaseFPCost]
           ,[BaseCastingTime]
           ,[Description]
           ,[PerkCategoryID]
           ,[CooldownCategoryID]
           ,[ExecutionTypeID]
           ,[ItemResref]
           ,[IsTargetSelfOnly]
           ,[Enmity]
           ,[EnmityAdjustmentRuleID]
           ,[CastAnimationID])
     VALUES
           (@PerkId
		   ,'Saber Finesse'
           ,null
           ,1
           ,'Lightsaber.WeaponFinesse'
           ,0
           ,0
           ,'You make melee attack rolls with your DEX if it is higher than your STR. Must be equipped with a lightsaber or saberstaff.'
           ,2
           ,null
           ,5
           ,null
           ,0
           ,0
           ,0
           ,null)

INSERT INTO [dbo].[PerkLevel]
            ([PerkID], [Level], [Price], [Description])
     VALUES (@PerkID, 1, 3, 'You gain the Weapon Finesse feat.')

DECLARE @PerkLevelID INT = SCOPE_IDENTITY();

INSERT INTO [dbo].[PerkLevelSkillRequirement] 
			([PerkLevelID], [SkillID], [RequiredRank])
	 VALUES (@PerkLevelID, 14, 5)


COMMIT TRANSACTION
