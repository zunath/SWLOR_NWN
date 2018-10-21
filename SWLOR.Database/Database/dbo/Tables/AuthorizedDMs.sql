CREATE TABLE [dbo].[AuthorizedDMs] (
    [AuthorizedDMID] INT            IDENTITY (1, 1) NOT NULL,
    [Name]           NVARCHAR (255) NOT NULL,
    [CDKey]          NVARCHAR (20)  NOT NULL,
    [DMRole]         INT            NOT NULL,
    [IsActive]       BIT            NOT NULL,
    CONSTRAINT [PK__Authoriz__D233D9E915E4120B] PRIMARY KEY CLUSTERED ([AuthorizedDMID] ASC)
);

