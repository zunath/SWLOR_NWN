CREATE TABLE [dbo].[FameRegions] (
    [FameRegionID] INT           NOT NULL,
    [Name]         NVARCHAR (32) NOT NULL,
    CONSTRAINT [QuestFameRegions_FameRegionID] PRIMARY KEY CLUSTERED ([FameRegionID] ASC)
);

