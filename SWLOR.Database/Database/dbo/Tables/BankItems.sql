CREATE TABLE [dbo].[BankItems] (
    [BankItemID] INT            IDENTITY (1, 1) NOT NULL,
    [BankID]     INT            NOT NULL,
    [PlayerID]   NVARCHAR (60)  NOT NULL,
    [ItemID]     NVARCHAR (60)  NOT NULL,
    [ItemName]   NVARCHAR (MAX) NOT NULL,
    [ItemTag]    NVARCHAR (64)  NOT NULL,
    [ItemResref] NVARCHAR (16)  NOT NULL,
    [ItemObject] NVARCHAR (MAX) NOT NULL,
    [DateStored] DATETIME2 (7)  DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([BankItemID] ASC),
    CONSTRAINT [FK_BankItems_BankID] FOREIGN KEY ([BankID]) REFERENCES [dbo].[Banks] ([BankID]),
    CONSTRAINT [FK_BankItems_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

