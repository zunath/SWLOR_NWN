CREATE TABLE [dbo].[Plants] (
    [PlantID]    INT           NOT NULL,
    [Name]       NVARCHAR (32) CONSTRAINT [DF__Plants__Name__17C286CF] DEFAULT ('') NOT NULL,
    [BaseTicks]  INT           CONSTRAINT [DF__Plants__BaseTick__18B6AB08] DEFAULT ((0)) NOT NULL,
    [Resref]     NVARCHAR (16) CONSTRAINT [DF__Plants__Resref__19AACF41] DEFAULT ('') NOT NULL,
    [WaterTicks] INT           DEFAULT ((0)) NOT NULL,
    [Level]      INT           DEFAULT ((0)) NOT NULL,
    [SeedResref] NVARCHAR (16) DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__Plants__98FE46BC83E7C439] PRIMARY KEY CLUSTERED ([PlantID] ASC)
);

