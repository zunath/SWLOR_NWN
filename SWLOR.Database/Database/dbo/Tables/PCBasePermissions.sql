CREATE TABLE [dbo].[PCBasePermissions] (
    [PCBasePermissionID]          INT           IDENTITY (1, 1) NOT NULL,
    [PCBaseID]                    INT           NOT NULL,
    [PlayerID]                    NVARCHAR (60) NOT NULL,
    [CanPlaceEditStructures]      BIT           DEFAULT ((0)) NOT NULL,
    [CanAccessStructureInventory] BIT           DEFAULT ((0)) NOT NULL,
    [CanManageBaseFuel]           BIT           DEFAULT ((0)) NOT NULL,
    [CanExtendLease]              BIT           DEFAULT ((0)) NOT NULL,
    [CanAdjustPermissions]        BIT           DEFAULT ((0)) NOT NULL,
    [CanEnterBuildings]           BIT           DEFAULT ((0)) NOT NULL,
    [CanRetrieveStructures]       BIT           DEFAULT ((0)) NOT NULL,
    [CanCancelLease]              BIT           DEFAULT ((0)) NOT NULL,
    [CanRenameStructures]         BIT           DEFAULT ((0)) NOT NULL,
    [CanEditPrimaryResidence]     BIT           DEFAULT ((0)) NOT NULL,
    [CanRemovePrimaryResidence]   BIT           DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([PCBasePermissionID] ASC),
    CONSTRAINT [FK_PCBasePermissions_PCBaseID] FOREIGN KEY ([PCBaseID]) REFERENCES [dbo].[PCBases] ([PCBaseID]),
    CONSTRAINT [FK_PCBasePermissions_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

