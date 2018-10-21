CREATE TABLE [dbo].[Users] (
    [UserID]         BIGINT         IDENTITY (1, 1) NOT NULL,
    [DiscordUserID]  NVARCHAR (MAX) NOT NULL,
    [Username]       NVARCHAR (32)  NOT NULL,
    [AvatarHash]     NVARCHAR (MAX) NOT NULL,
    [Discriminator]  NVARCHAR (4)   NOT NULL,
    [Email]          NVARCHAR (MAX) NOT NULL,
    [RoleID]         INT            CONSTRAINT [DF__Users__RoleID__4589517F] DEFAULT ((3)) NOT NULL,
    [DateRegistered] DATETIME2 (7)  CONSTRAINT [DF__Users__DateRegis__467D75B8] DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK__Users__1788CCAC642F36E9] PRIMARY KEY CLUSTERED ([UserID] ASC),
    CONSTRAINT [fk_Users_RoleID] FOREIGN KEY ([RoleID]) REFERENCES [dbo].[DMRoleDomain] ([DMRoleDomainID])
);

