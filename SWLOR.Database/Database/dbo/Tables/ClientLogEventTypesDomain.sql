CREATE TABLE [dbo].[ClientLogEventTypesDomain] (
    [ClientLogEventTypeID] INT           NOT NULL,
    [Name]                 NVARCHAR (30) NOT NULL,
    CONSTRAINT [PK_ClientLogEventTypesDomain_ClientLogEventTypeID] PRIMARY KEY CLUSTERED ([ClientLogEventTypeID] ASC)
);

