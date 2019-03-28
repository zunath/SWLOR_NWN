
ALTER TABLE dbo.SpawnObject
ADD AIFlags INT NOT NULL DEFAULT 0
GO

-- SightAggroBehaviour becomes StandardBehaviour with Flags: AggroNearby, RandomWalk
UPDATE dbo.SpawnObject
SET BehaviourScript = 'StandardBehaviour', AIFlags = 5 -- 1 & 4
WHERE ID IN (103,104,3  ,100,101,49 ,50 ,51 ,54 ,60)

-- SoundAggroBehaviour becomes StandardBehaviour with Flags: AggroNearby, Link
UPDATE dbo.SpawnObject
SET BehaviourScript = 'StandardBehaviour', AIFlags = 3 -- 1 & 2
WHERE ID IN (61,62,63,64,66,67,68,69,73,74,75)

-- SoundAggroRandomWalkBehaviour becomes StandardBehaviour with Flags: AggroNearby, Link, RandomWalk
UPDATE dbo.SpawnObject
SET BehaviourScript = 'StandardBehaviour', AIFlags = 7 -- 1 & 2 & 4
WHERE ID IN (78,79,4 ,70,71,72)

-- DarkForceUser uses the same script but has flags: AggroNearby, Link, RandomWalk
UPDATE dbo.SpawnObject
SET AIFlags = 7
WHERE ID = 102

-- Make some creatures link even though they didn't used to.
UPDATE dbo.SpawnObject
SET AIFlags = 7
WHERE ID IN (103, 104, 101)
