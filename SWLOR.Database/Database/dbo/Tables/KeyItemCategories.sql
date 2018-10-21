CREATE TABLE [dbo].[KeyItemCategories] (
    [KeyItemCategoryID] INT           NOT NULL,
    [Name]              NVARCHAR (32) NOT NULL,
    [IsActive]          BIT           NOT NULL,
    CONSTRAINT [PK__KeyItemC__CD3A52E2821EBDCD] PRIMARY KEY CLUSTERED ([KeyItemCategoryID] ASC)
);

