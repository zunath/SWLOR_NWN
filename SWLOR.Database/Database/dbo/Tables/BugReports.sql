CREATE TABLE [dbo].[BugReports] (
    [BugReportID]               INT             IDENTITY (1, 1) NOT NULL,
    [SenderPlayerID]            NVARCHAR (60)   NULL,
    [CDKey]                     NVARCHAR (20)   NOT NULL,
    [Text]                      NVARCHAR (1000) NOT NULL,
    [TargetName]                NVARCHAR (64)   NOT NULL,
    [AreaResref]                NVARCHAR (16)   NOT NULL,
    [SenderLocationX]           FLOAT (53)      NOT NULL,
    [SenderLocationY]           FLOAT (53)      NOT NULL,
    [SenderLocationZ]           FLOAT (53)      NOT NULL,
    [SenderLocationOrientation] FLOAT (53)      NOT NULL,
    [DateSubmitted]             DATETIME2 (7)   DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([BugReportID] ASC),
    CONSTRAINT [FK_BugReports_SenderPlayerID] FOREIGN KEY ([SenderPlayerID]) REFERENCES [dbo].[PlayerCharacters] ([PlayerID])
);

