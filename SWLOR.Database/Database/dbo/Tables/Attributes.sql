CREATE TABLE [dbo].[Attributes] (
    [AttributeID] INT          NOT NULL,
    [NWNValue]    INT          CONSTRAINT [DF__Attribute__NWNVa__56E8E7AB] DEFAULT ((0)) NOT NULL,
    [Name]        NVARCHAR (3) CONSTRAINT [DF__Attributes__Name__57DD0BE4] DEFAULT ('') NOT NULL,
    CONSTRAINT [PK__Attribut__C189298A024C3D44] PRIMARY KEY CLUSTERED ([AttributeID] ASC)
);

