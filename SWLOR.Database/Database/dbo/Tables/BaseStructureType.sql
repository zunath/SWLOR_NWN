CREATE TABLE [dbo].[BaseStructureType] (
    [BaseStructureTypeID] INT           NOT NULL,
    [Name]                NVARCHAR (64) DEFAULT ('') NOT NULL,
    [IsActive]            BIT           DEFAULT ((0)) NOT NULL,
    [CanPlaceInside]      BIT           DEFAULT ((0)) NOT NULL,
    [CanPlaceOutside]     BIT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([BaseStructureTypeID] ASC)
);

