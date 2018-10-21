CREATE TABLE [dbo].[DiscordChatQueue] (
    [DiscordChatQueueID] INT             IDENTITY (1, 1) NOT NULL,
    [SenderName]         NVARCHAR (255)  NOT NULL,
    [Message]            NVARCHAR (MAX)  DEFAULT ('') NOT NULL,
    [DateSent]           DATETIME2 (7)   DEFAULT (getutcdate()) NOT NULL,
    [DatePosted]         DATETIME2 (7)   NULL,
    [DateForRetry]       DATETIME2 (7)   NULL,
    [ResponseContent]    NVARCHAR (MAX)  NULL,
    [RetryAttempts]      INT             DEFAULT ((0)) NOT NULL,
    [SenderAccountName]  NVARCHAR (1024) DEFAULT ('') NOT NULL,
    [SenderCDKey]        NVARCHAR (20)   DEFAULT ('') NOT NULL,
    PRIMARY KEY CLUSTERED ([DiscordChatQueueID] ASC)
);

