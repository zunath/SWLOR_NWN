CREATE TABLE [dbo].[PCCustomEffects] (
    [PCCustomEffectID]  BIGINT        IDENTITY (1, 1) NOT NULL,
    [PlayerID]          NVARCHAR (60) NOT NULL,
    [CustomEffectID]    BIGINT        NOT NULL,
    [Ticks]             INT           NOT NULL,
    [EffectiveLevel]    INT           DEFAULT ((0)) NOT NULL,
    [Data]              NVARCHAR (32) DEFAULT ('') NOT NULL,
    [CasterNWNObjectID] NVARCHAR (10) DEFAULT ('') NOT NULL,
    [StancePerkID]      INT           NULL,
    CONSTRAINT [PK__PCCustom__40F2132E1A5F30A2] PRIMARY KEY CLUSTERED ([PCCustomEffectID] ASC),
    CONSTRAINT [fk_PCCustomEffects_CustomEffectID] FOREIGN KEY ([CustomEffectID]) REFERENCES [dbo].[CustomEffects] ([CustomEffectID]),
    CONSTRAINT [fk_PCCustomEffects_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

