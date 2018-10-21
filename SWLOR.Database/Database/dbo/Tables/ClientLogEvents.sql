CREATE TABLE [dbo].[ClientLogEvents] (
    [ClientLogEventID]     INT             IDENTITY (1, 1) NOT NULL,
    [ClientLogEventTypeID] INT             NOT NULL,
    [PlayerID]             NVARCHAR (60)   NULL,
    [CDKey]                NVARCHAR (20)   NOT NULL,
    [AccountName]          NVARCHAR (1024) NOT NULL,
    [DateOfEvent]          DATETIME2 (7)   CONSTRAINT [DF__ClientLog__DateO__5D95E53A] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_ClientLogEvents_ClientLogEventID] PRIMARY KEY CLUSTERED ([ClientLogEventID] ASC),
    CONSTRAINT [FK_ClientLogEvents_ClientLogEventTypeID] FOREIGN KEY ([ClientLogEventTypeID]) REFERENCES [dbo].[ClientLogEventTypesDomain] ([ClientLogEventTypeID]),
    CONSTRAINT [FK_ClientLogEvents_PlayerID] FOREIGN KEY ([PlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

