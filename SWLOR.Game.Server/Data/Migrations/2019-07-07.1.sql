
-- Modify the Download table for the website.

EXEC dbo.ADM_Drop_Column @TableName = N'Download' , -- nvarchar(200)
                         @ColumnName = N'Url'  -- nvarchar(200)


ALTER TABLE dbo.Download
ADD LocalPath NVARCHAR(64) NOT NULL 

ALTER TABLE dbo.Download
ADD FileName NVARCHAR(64) NOT NULL

ALTER TABLE dbo.Download
ADD ContentType NVARCHAR(32) NOT NULL

ALTER TABLE dbo.Download
ADD Instructions NVARCHAR(1000) NOT NULL DEFAULT ''
GO

INSERT INTO dbo.Download ( ID ,
                           Name ,
                           Description ,
						   Instructions,
                           IsActive ,
                           LocalPath ,
                           FileName,
						   ContentType )
VALUES ( 1 ,    -- ID - int
         N'GUI Override' ,  -- Name - nvarchar(50)
         N'This overrides the graphics of your user interface. It will affect all servers do you will need to move it out of your override folder if you no longer wish to use it.' ,  -- Description - nvarchar(1000)
		 N'Extract all files to your My Documents/Neverwinter Nights/override directory.',
         1 , -- IsActive - bit
         N'/var/www/swlor_public_files/SWLOR GUI.rar' ,  -- LocalPath - nvarchar(64)
         N'SWLOR GUI.rar',    -- FileName - nvarchar(64)
		 N'application/octet-stream'
    )
INSERT INTO dbo.Download ( ID ,
                           Name ,
                           Description ,
						   Instructions,
                           IsActive ,
                           LocalPath ,
                           FileName,
						   ContentType )
VALUES ( 2 ,    -- ID - int
         N'SWLOR Music Package' ,  -- Name - nvarchar(50)
         N'This is all of the music we use on SWLOR. Because we cannot currently distribute music files via NWSync, you will need to download this separately. We highly recommend it, but the download is optional! File last updated 2019-03-03.',
		 N'Extract all files to your My Documents/Neverwinter Nights/music directory.',
         1 , -- IsActive - bit
         N'/var/www/swlor_public_files/SWLOR_Music_v2.rar' ,  -- LocalPath - nvarchar(64)
         N'SWLOR Music.rar',    -- FileName - nvarchar(64)
		 N'application/octet-stream'
    )
	
INSERT INTO dbo.Download ( ID ,
                           Name ,
                           Description ,
						   Instructions,
                           IsActive ,
                           LocalPath ,
                           FileName,
						   ContentType )
VALUES ( 3 ,    -- ID - int
         N'SWLOR Haks' ,  -- Name - nvarchar(50)
         N'These are the DEVELOPMENT-ONLY hakpaks. If you only want to play, you DO NOT need these. Simply connect to the server to get the files and start playing.',
		 N'Extract all .hak files to your My Documents/Neverwinter Nights/hak directory. Extract the swlor_tlk.tlk file to your tlk directory. Make the tlk directory if it doesn''t already exist.',
         1 , -- IsActive - bit
         N'/var/www/swlor_public_files/SWLOR Haks.rar' ,  -- LocalPath - nvarchar(64)
         N'SWLOR Development Haks.rar',    -- FileName - nvarchar(64)
		 N'application/octet-stream '
    )

