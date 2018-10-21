CREATE TABLE [dbo].[ChatLog] (
    [ChatLogID]           BIGINT          IDENTITY (1, 1) NOT NULL,
    [ChatChannelID]       INT             NOT NULL,
    [SenderPlayerID]      NVARCHAR (60)   NULL,
    [SenderAccountName]   NVARCHAR (1024) CONSTRAINT [DF__ChatLog__SenderA__59C55456] DEFAULT ('') NOT NULL,
    [SenderCDKey]         NVARCHAR (20)   CONSTRAINT [DF__ChatLog__SenderC__5AB9788F] DEFAULT ('') NOT NULL,
    [ReceiverPlayerID]    NVARCHAR (60)   NULL,
    [ReceiverAccountName] NVARCHAR (1024) NULL,
    [ReceiverCDKey]       NVARCHAR (20)   NULL,
    [Message]             NVARCHAR (MAX)  CONSTRAINT [DF__ChatLog__Message__5BAD9CC8] DEFAULT ('') NOT NULL,
    [DateSent]            DATETIME2 (7)   CONSTRAINT [DF__ChatLog__DateSen__5CA1C101] DEFAULT (getutcdate()) NOT NULL,
    [SenderDMName]        NVARCHAR (60)   NULL,
    [ReceiverDMName]      NVARCHAR (60)   NULL,
    CONSTRAINT [PK__ChatLog__454604C4BBAF0C10] PRIMARY KEY CLUSTERED ([ChatLogID] ASC),
    CONSTRAINT [fk_ChatLog_ChatChannelID] FOREIGN KEY ([ChatChannelID]) REFERENCES [dbo].[ChatChannelsDomain] ([ChatChannelID]),
    CONSTRAINT [fk_ChatLog_ReceiverPlayerID] FOREIGN KEY ([ReceiverPlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID]),
    CONSTRAINT [fk_ChatLog_SenderPlayerID] FOREIGN KEY ([SenderPlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

