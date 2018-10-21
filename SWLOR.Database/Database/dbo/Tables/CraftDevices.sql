CREATE TABLE [dbo].[CraftDevices] (
    [CraftDeviceID] INT           NOT NULL,
    [Name]          NVARCHAR (32) CONSTRAINT [DF__CraftDevic__Name__6442E2C9] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__CraftDev__5CCBD473CCCE6D67] PRIMARY KEY CLUSTERED ([CraftDeviceID] ASC)
);

