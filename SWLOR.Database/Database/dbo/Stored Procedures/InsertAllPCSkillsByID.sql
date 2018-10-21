-- =============================================
-- Author:		zunath
-- Create date: 2018-08-10
-- Description:	Inserts any missing PCSkills records on player module entry.
-- =============================================
CREATE PROCEDURE dbo.InsertAllPCSkillsByID
	@PlayerID NVARCHAR(60)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	BEGIN TRY
		BEGIN TRANSACTION
			INSERT INTO dbo.PCSkills ( PlayerID ,
									   SkillID ,
									   XP ,
									   Rank,
									   IsLocked)
			SELECT @PlayerID,
				s.SkillID,
				0,
				0,
				0
			FROM dbo.Skills s
			WHERE s.SkillID NOT IN (
				SELECT pcs.SkillID
				FROM dbo.PCSkills pcs
				WHERE pcs.PlayerID = @PlayerID
			)

		COMMIT TRANSACTION
	END TRY
	BEGIN CATCH
		IF @@TRANCOUNT > 0
			ROLLBACK TRAN

		RAISERROR('Failed to insert all PC Skills from OnModuleEnter. (InsertAllPCSkillsByID)', -1, -1)
	END CATCH
END
