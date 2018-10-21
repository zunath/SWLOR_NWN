CREATE TABLE [dbo].[CustomEffects] (
    [CustomEffectID]  BIGINT        NOT NULL,
    [Name]            NVARCHAR (32) NOT NULL,
    [IconID]          INT           NOT NULL,
    [ScriptHandler]   NVARCHAR (64) NOT NULL,
    [StartMessage]    NVARCHAR (64) NOT NULL,
    [ContinueMessage] NVARCHAR (64) NOT NULL,
    [WornOffMessage]  NVARCHAR (64) NOT NULL,
    [IsStance]        BIT           DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__CustomEf__18502FBA6D986D4A] PRIMARY KEY CLUSTERED ([CustomEffectID] ASC)
);

