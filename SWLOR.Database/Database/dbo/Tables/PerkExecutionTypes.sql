CREATE TABLE [dbo].[PerkExecutionTypes] (
    [PerkExecutionTypeID] INT           NOT NULL,
    [Name]                NVARCHAR (32) CONSTRAINT [DF__PerkExecut__Name__0A688BB1] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__PerkExec__8133420289767A5A] PRIMARY KEY CLUSTERED ([PerkExecutionTypeID] ASC)
);

