--Remove casting time from Dash
UPDATE dbo.Perk
SET BaseCastingTime = 0
WHERE ID = 89