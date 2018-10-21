CREATE TABLE [dbo].[Backgrounds] (
    [BackgroundID] INT            NOT NULL,
    [Name]         NVARCHAR (64)  DEFAULT ('') NOT NULL,
    [Description]  NVARCHAR (512) DEFAULT ('') NOT NULL,
    [Bonuses]      NVARCHAR (512) DEFAULT ('') NOT NULL,
    [IsActive]     BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BackgroundID] ASC)
);

