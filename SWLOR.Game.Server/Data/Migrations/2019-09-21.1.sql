USE [swlor]
GO

/****** Object:  Table [dbo].[PCHelmet]    Script Date: 9/21/2019 3:28:22 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[PCHelmet](
	[PlayerID] [uniqueidentifier] NOT NULL,
	[Helmet1] [varchar](max) NULL,
	[Helmet2] [varchar](max) NULL,
	[Helmet3] [varchar](max) NULL,
	[Helmet4] [varchar](max) NULL,
	[Helmet5] [varchar](max) NULL,
	[Helmet6] [varchar](max) NULL,
	[Helmet7] [varchar](max) NULL,
	[Helmet8] [varchar](max) NULL,
	[Helmet9] [varchar](max) NULL,
	[Helmet10] [varchar](max) NULL,
	[ClusterID] [int] IDENTITY(1,1) NOT NULL,
 CONSTRAINT [PK__PCHelmet__4A4E74A8B41DD37A] PRIMARY KEY NONCLUSTERED 
(
	[PlayerID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

ALTER TABLE [dbo].[PCHelmet]  WITH CHECK ADD  CONSTRAINT [fk_PCHelmet_PlayerID] FOREIGN KEY([PlayerID])
REFERENCES [dbo].[Player] ([ID])
GO

ALTER TABLE [dbo].[PCHelmet] CHECK CONSTRAINT [fk_PCHelmet_PlayerID]
GO


