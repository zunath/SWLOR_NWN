CREATE PROCEDURE dbo.ADM_Drop_Column(@TableName nvarchar(200), @ColumnName nvarchar(200))
AS
BEGIN
	DECLARE @ConstraintName nvarchar(200)
	SELECT @ConstraintName = Name FROM SYS.DEFAULT_CONSTRAINTS
	WHERE PARENT_OBJECT_ID = OBJECT_ID(@TableName)
	AND PARENT_COLUMN_ID = (SELECT column_id FROM sys.columns
							WHERE NAME = @ColumnName
							AND object_id = OBJECT_ID(@TableName))
	
	IF @ConstraintName IS NOT NULL
	EXEC('ALTER TABLE '+ @TableName +' DROP CONSTRAINT ' + @ConstraintName)

	EXEC('ALTER TABLE '+ @TableName +' DROP COLUMN ' + @ColumnName)

END
