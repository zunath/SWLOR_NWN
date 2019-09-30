USE [swlor]
GO

/****** Object:  Table [dbo].[PCWeapon]    Script Date: 9/21/2019 3:28:22 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PCWeapon](
	[PlayerID] [uniqueidentifier] NOT NULL,
	[Weapon1] [varchar](max) NULL,
	[Weapon2] [varchar](max) NULL,
	[Weapon3] [varchar](max) NULL,
	[Weapon4] [varchar](max) NULL,
	[Weapon5] [varchar](max) NULL,
	[Weapon6] [varchar](max) NULL,
	[Weapon7] [varchar](max) NULL,
	[Weapon8] [varchar](max) NULL,
	[Weapon9] [varchar](max) NULL,
	[Weapon10] [varchar](max) NULL,
	[ClusterID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK__PCWeapon__4A4E74A8B41DD37A] PRIMARY KEY NONCLUSTERED 
(
	[PlayerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PCWeapon]  WITH CHECK ADD  CONSTRAINT [fk_PCWeapon_PlayerID] FOREIGN KEY([PlayerID])
REFERENCES [dbo].[Player] ([ID])
GO

ALTER TABLE [dbo].[PCWeapon] CHECK CONSTRAINT [fk_PCWeapon_PlayerID]
GO


