CREATE TABLE [dbo].[Mods] (
    [ModID]    INT            NOT NULL,
    [Name]     NVARCHAR (64)  DEFAULT ('') NOT NULL,
    [Script]   NVARCHAR (100) DEFAULT ('') NOT NULL,
    [IsActive] BIT            DEFAULT ((0)) NOT NULL,
    PRIMARY KEY CLUSTERED ([ModID] ASC)
);

