
ALTER TABLE dbo.ChatLog
ALTER COLUMN SenderDMName NVARCHAR(300) NULL
GO

ALTER TABLE dbo.ChatLog
ALTER COLUMN ReceiverDMName NVARCHAR(300) NULL 
GO


UPDATE dbo.CraftBlueprint
SET ItemResref = 's_frame_medium'
WHERE ID = 317