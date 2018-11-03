-- =============================================
-- Author:		zunath
-- Create date: 2018-11-03
-- Description:	Returns all loot tables and associated loot table items.
-- =============================================
CREATE PROCEDURE GetLootTables

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	SELECT lt.LootTableID ,
           lt.Name ,
           lti.LootTableItemID ,
           lti.Resref ,
           lti.MaxQuantity ,
           lti.Weight ,
           lti.IsActive ,
           lti.SpawnRule 
	FROM dbo.LootTables lt
	JOIN dbo.LootTableItems lti ON lti.LootTableID = lt.LootTableID 

END
GO