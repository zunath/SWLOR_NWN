CREATE TABLE [dbo].[GameTopics] (
    [GameTopicID]         INT            NOT NULL,
    [Name]                NVARCHAR (32)  NOT NULL,
    [Text]                NVARCHAR (MAX) NOT NULL,
    [GameTopicCategoryID] INT            NOT NULL,
    [IsActive]            BIT            DEFAULT ((0)) NOT NULL,
    [Sequence]            INT            DEFAULT ((0)) NOT NULL,
    [Icon]                NVARCHAR (32)  DEFAULT ('') NOT NULL,
    PRIMARY KEY CLUSTERED ([GameTopicID] ASC),
    CONSTRAINT [FK_GameTopics_GameTopicCategoryID] FOREIGN KEY ([GameTopicCategoryID]) REFERENCES [dbo].[GameTopicCategories] ([GameTopicCategoryID])
);

