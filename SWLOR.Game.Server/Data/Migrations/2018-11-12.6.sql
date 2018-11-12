
ALTER TABLE dbo.BaseStructure
ADD FuelRating INT NOT NULL DEFAULT 0
GO

UPDATE dbo.BaseStructure
SET FuelRating = 1 WHERE ID = 1
UPDATE dbo.BaseStructure
SET FuelRating = 2 WHERE ID = 2
UPDATE dbo.BaseStructure
SET FuelRating = 3 WHERE ID = 3

