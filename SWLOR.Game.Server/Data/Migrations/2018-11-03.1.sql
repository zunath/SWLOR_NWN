

/****** Object:  Table [dbo].[Backgrounds]    Script Date: 11/3/2018 5:19:02 PM ******/
DROP TABLE [dbo].[Backgrounds]
GO


ALTER TABLE dbo.AreaWalkmesh
ALTER COLUMN LocationX FLOAT NOT NULL 
GO

ALTER TABLE dbo.AreaWalkmesh
ALTER COLUMN LocationY FLOAT NOT NULL 