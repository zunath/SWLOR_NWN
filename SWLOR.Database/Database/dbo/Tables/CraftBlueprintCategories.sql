CREATE TABLE [dbo].[CraftBlueprintCategories] (
    [CraftBlueprintCategoryID] BIGINT        NOT NULL,
    [Name]                     NVARCHAR (32) NOT NULL,
    [IsActive]                 BIT           NOT NULL,
    CONSTRAINT [PK__CraftBlu__0EB197640EC6A590] PRIMARY KEY CLUSTERED ([CraftBlueprintCategoryID] ASC)
);

