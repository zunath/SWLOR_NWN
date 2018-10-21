CREATE TABLE [dbo].[Perks] (
    [PerkID]                 INT            NOT NULL,
    [Name]                   VARCHAR (64)   CONSTRAINT [DF__Perks__Name__0F2D40CE] DEFAULT ('') NOT NULL,
    [FeatID]                 INT            CONSTRAINT [DF__Perks__FeatID__10216507] DEFAULT ((0)) NULL,
    [IsActive]               BIT            CONSTRAINT [DF__Perks__IsActive__11158940] DEFAULT ((0)) NOT NULL,
    [ScriptName]             VARCHAR (64)   CONSTRAINT [DF__Perks__JavaScrip__1209AD79] DEFAULT ('') NOT NULL,
    [BaseFPCost]             INT            CONSTRAINT [DF__Perks__BaseFPC__12FDD1B2] DEFAULT ((0)) NOT NULL,
    [BaseCastingTime]        FLOAT (53)     CONSTRAINT [DF__Perks__BaseCasti__13F1F5EB] DEFAULT ((0.0)) NOT NULL,
    [Description]            NVARCHAR (256) CONSTRAINT [DF__Perks__Descripti__14E61A24] DEFAULT ('') NOT NULL,
    [PerkCategoryID]         INT            NOT NULL,
    [CooldownCategoryID]     INT            NULL,
    [ExecutionTypeID]        INT            CONSTRAINT [DF__Perks__Execution__15DA3E5D] DEFAULT ((0)) NOT NULL,
    [ItemResref]             NVARCHAR (16)  NULL,
    [IsTargetSelfOnly]       BIT            CONSTRAINT [DF__Perks__IsTargetS__16CE6296] DEFAULT ((0)) NOT NULL,
    [Enmity]                 INT            DEFAULT ((0)) NOT NULL,
    [EnmityAdjustmentRuleID] INT            DEFAULT ((0)) NOT NULL,
    [CastAnimationID]        INT            NULL,
    CONSTRAINT [PK__Perks__2432566E1A11FD39] PRIMARY KEY CLUSTERED ([PerkID] ASC),
    CONSTRAINT [fk_Perks_CooldownCategoryID] FOREIGN KEY ([CooldownCategoryID]) REFERENCES [dbo].[CooldownCategories] ([CooldownCategoryID]),
    CONSTRAINT [fk_Perks_EnmityAdjustmentRuleID] FOREIGN KEY ([EnmityAdjustmentRuleID]) REFERENCES [dbo].[EnmityAdjustmentRule] ([EnmityAdjustmentRuleID]),
    CONSTRAINT [fk_Perks_ExecutionTypeID] FOREIGN KEY ([ExecutionTypeID]) REFERENCES [dbo].[PerkExecutionTypes] ([PerkExecutionTypeID]),
    CONSTRAINT [fk_Perks_PerkCategoryID] FOREIGN KEY ([PerkCategoryID]) REFERENCES [dbo].[PerkCategories] ([PerkCategoryID])
);

