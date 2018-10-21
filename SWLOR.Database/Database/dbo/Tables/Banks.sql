CREATE TABLE [dbo].[Banks] (
    [BankID]     INT            NOT NULL,
    [AreaName]   NVARCHAR (255) NOT NULL,
    [AreaTag]    NVARCHAR (64)  NOT NULL,
    [AreaResref] NVARCHAR (16)  NOT NULL,
    PRIMARY KEY CLUSTERED ([BankID] ASC)
);

