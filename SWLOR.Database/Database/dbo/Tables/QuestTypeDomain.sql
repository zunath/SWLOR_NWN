CREATE TABLE [dbo].[QuestTypeDomain] (
    [QuestTypeID] INT           NOT NULL,
    [Name]        NVARCHAR (30) NOT NULL,
    CONSTRAINT [PK_QuestTypeDomain_QuestTypeID] PRIMARY KEY CLUSTERED ([QuestTypeID] ASC)
);

