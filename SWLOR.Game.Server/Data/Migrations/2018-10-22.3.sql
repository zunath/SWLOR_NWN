
IF COL_LENGTH('PlayerCharacters','IsUsingNovelEmoteStyle') IS NULL
BEGIN
	ALTER TABLE dbo.PlayerCharacters
	ADD IsUsingNovelEmoteStyle BIT NOT NULL DEFAULT 0
END