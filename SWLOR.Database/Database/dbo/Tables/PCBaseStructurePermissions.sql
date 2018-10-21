CREATE TABLE [dbo].[PCBaseStructurePermissions] (
    [PCBaseStructurePermissionID] INT           IDENTITY (1, 1) NOT NULL,
    [PCBaseStructureID]           INT           NOT NULL,
    [PlayerID]                    NVARCHAR (60) NOT NULL,
    [CanPlaceEditStructures]      BIT           DEFAULT ((0)) NOT NULL,
    [CanAccessStructureInventory] BIT           DEFAULT ((0)) NOT NULL,
    [CanEnterBuilding]            BIT           DEFAULT ((0)) NOT NULL,
    [CanRetrieveStructures]       BIT           DEFAULT ((0)) NOT NULL,
    [CanAdjustPermissions]        BIT           DEFAULT ((0)) NOT NULL,
    [CanRenameStructures]         BIT           DEFAULT ((0)) NOT NULL,
    [CanEditPrimaryResidence]     BIT           DEFAULT ((0)) NOT NULL,
    [CanRemovePrimaryResidence]   BIT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCBaseStructurePermissionID] ASC),
    CONSTRAINT [FK_PCBaseStructurePermissions_PCBaseStructureID] FOREIGN KEY ([PCBaseStructureID]) REFERENCES [dbo].[PCBaseStructures] ([PCBaseStructureID]),
    CONSTRAINT [FK_PCBaseStructurePermissions_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

