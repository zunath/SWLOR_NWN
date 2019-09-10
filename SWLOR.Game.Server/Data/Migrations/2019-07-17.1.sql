
UPDATE dbo.SpawnObject
SET AIFlags = AIFlags + 8
WHERE AIFlags > 0
