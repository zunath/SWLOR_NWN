CREATE TABLE [dbo].[ComponentTypes] (
    [ComponentTypeID] INT           NOT NULL,
    [Name]            NVARCHAR (32) DEFAULT ('') NOT NULL,
    PRIMARY KEY CLUSTERED ([ComponentTypeID] ASC)
);

