
-- No longer using a ScriptHandler field in the CustomEffect table. Drop the column.
EXEC dbo.ADM_Drop_Column @TableName = N'dbo.CustomEffect' , -- nvarchar(200)
                         @ColumnName = N'ScriptHandler'  -- nvarchar(200)


-- StartMessage, ContinueMessage, and WornOffMessage are now defined in the custom effect handler classes.
-- No longer need the database columns.
EXEC dbo.ADM_Drop_Column @TableName = N'dbo.CustomEffect' , -- nvarchar(200)
                         @ColumnName = N'StartMessage'  -- nvarchar(200)

EXEC dbo.ADM_Drop_Column @TableName = N'dbo.CustomEffect' , -- nvarchar(200)
                         @ColumnName = N'ContinueMessage'  -- nvarchar(200)

EXEC dbo.ADM_Drop_Column @TableName = N'dbo.CustomEffect' , -- nvarchar(200)
                         @ColumnName = N'WornOffMessage'  -- nvarchar(200)

-- Drop the CustomEffectCategoryID as well. Stored in the handler classes.
EXEC dbo.ADM_Drop_Column @TableName = N'dbo.CustomEffect' , -- nvarchar(200)
                         @ColumnName = N'CustomEffectCategoryID'  -- nvarchar(200)

			
-- Remove the food effect until we implement cooking.
DELETE FROM dbo.CustomEffect
WHERE ID = 25


-- Drop the ScriptName column on the Perk table. No longer needed since we're linking by ID now.
EXEC dbo.ADM_Drop_Column @TableName = N'dbo.Perk' , -- nvarchar(200)
                         @ColumnName = N'ScriptName'  -- nvarchar(200)


-- Mod table is no longer necessary or useful. Drop it.
DROP TABLE dbo.Mod
