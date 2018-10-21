-- =============================================
-- Author:		zunath
-- Create date: 2018-09-07
-- Description:	Removes all walkmesh tiles for a given area ID
-- =============================================
CREATE PROCEDURE dbo.DeleteAreaWalkmeshes
	@AreaID NVARCHAR(60)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	
	BEGIN TRY
		BEGIN TRANSACTION
			DELETE FROM dbo.AreaWalkmesh
			WHERE AreaID = @AreaID

		COMMIT TRANSACTION
	END TRY
	BEGIN CATCH
		IF @@TRANCOUNT > 0
			ROLLBACK TRAN

		RAISERROR('Failed to delete all of baked area''s walkmeshes. ID = (DeleteAreaWalkmeshes)', -1, -1)
	END CATCH
END
