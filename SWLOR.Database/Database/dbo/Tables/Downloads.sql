CREATE TABLE [dbo].[Downloads] (
    [DownloadID]  INT             NOT NULL,
    [Name]        NVARCHAR (50)   CONSTRAINT [DF__Downloads__Name__65370702] DEFAULT ('') NOT NULL,
    [Description] NVARCHAR (1000) CONSTRAINT [DF__Downloads__Descr__662B2B3B] DEFAULT ('') NOT NULL,
    [Url]         NVARCHAR (200)  CONSTRAINT [DF__Downloads__Url__671F4F74] DEFAULT ('') NOT NULL,
    [IsActive]    BIT             CONSTRAINT [DF__Downloads__IsAct__681373AD] DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_Downloads_DownloadID] PRIMARY KEY CLUSTERED ([DownloadID] ASC)
);

