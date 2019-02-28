-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		zunath
-- Create date: 2019-02-26
-- Description:	Returns player market listings 
-- =============================================
CREATE PROCEDURE ADM_GetMarketListings

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
	SELECT pcml.ID AS ListingID,
		 pcml.SellerPlayerID,
		 p1.CharacterName AS SellerName,
		 pcml.Note,
		 pcml.Price,
		 mr.Name AS RegionName,
		 mc.Name AS MarketCategoryName,
		 pcml.DatePosted,
		 pcml.DateExpires,
		 pcml.DateSold,
		 pcml.DateRemoved,
		 pcml.BuyerPlayerID,
		 p2.CharacterName AS BuyerName,
		 pcml.ItemID,
		 pcml.ItemName,
		 pcml.ItemStackSize,
		 pcml.ItemTag,
		 pcml.ItemResref,
		 pcml.ItemObject,
		 pcml.ItemRecommendedLevel
	FROM dbo.PCMarketListing pcml
	JOIN dbo.MarketRegion mr ON mr.ID = pcml.MarketRegionID
	JOIN dbo.MarketCategory mc ON mc.ID = pcml.MarketCategoryID
	JOIN dbo.Player p1 ON p1.ID = pcml.SellerPlayerID
	LEFT JOIN dbo.Player p2 ON p2.ID = pcml.BuyerPlayerID
	ORDER BY pcml.DatePosted

END




