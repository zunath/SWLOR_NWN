CREATE TABLE [dbo].[KeyItems] (
    [KeyItemID]         INT             NOT NULL,
    [KeyItemCategoryID] INT             NOT NULL,
    [Name]              NVARCHAR (64)   NOT NULL,
    [Description]       NVARCHAR (1000) NOT NULL,
    CONSTRAINT [PK__KeyItems__95F54E1C55214D3E] PRIMARY KEY CLUSTERED ([KeyItemID] ASC),
    CONSTRAINT [fk_KeyItems_KeyItemCategoryID] FOREIGN KEY ([KeyItemCategoryID]) REFERENCES [dbo].[KeyItemCategories] ([KeyItemCategoryID])
);

