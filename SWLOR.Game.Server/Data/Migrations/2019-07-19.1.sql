
INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 30 ,   -- ID - int
         N'Viscara - Crystal Cavern' , -- Name - nvarchar(64)
         1     -- SpawnObjectTypeID - int
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID ,
                              AIFlags )
VALUES ( 5 ,   -- ID - int
         30 ,   -- SpawnID - int
         N'crystalspider' , -- Resref - nvarchar(16)
         10 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         16 ,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         15     -- AIFlags - int
    )
