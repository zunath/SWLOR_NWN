CREATE TABLE [dbo].[ServerConfiguration] (
    [ServerConfigurationID] INT            NOT NULL,
    [ServerName]            VARCHAR (50)   CONSTRAINT [DF__ServerCon__Serve__308E3499] DEFAULT ('') NOT NULL,
    [MessageOfTheDay]       VARCHAR (1024) CONSTRAINT [DF__ServerCon__Messa__318258D2] DEFAULT ('') NOT NULL,
    [AreaBakeStep]          INT            DEFAULT ((2)) NOT NULL,
    CONSTRAINT [PK__ServerCo__90C495B665B9B563] PRIMARY KEY CLUSTERED ([ServerConfigurationID] ASC)
);

