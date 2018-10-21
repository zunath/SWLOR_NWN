CREATE TABLE [dbo].[PCSkills] (
    [PCSkillID] INT           IDENTITY (1, 1) NOT NULL,
    [PlayerID]  NVARCHAR (60) NOT NULL,
    [SkillID]   INT           NOT NULL,
    [XP]        INT           CONSTRAINT [DF__PCSkills__XP__02C769E9] DEFAULT ((0)) NOT NULL,
    [Rank]      INT           CONSTRAINT [DF__PCSkills__Rank__03BB8E22] DEFAULT ((0)) NOT NULL,
    [IsLocked]  BIT           CONSTRAINT [DF__PCSkills__IsLock__04AFB25B] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK__PCSkills__F00838A44ECEB5BD] PRIMARY KEY CLUSTERED ([PCSkillID] ASC),
    CONSTRAINT [FK_PCSkills_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID]),
    CONSTRAINT [FK_PCSkills_SkillID] FOREIGN KEY ([SkillID]) REFERENCES [dbo].[Skills] ([SkillID])
);

