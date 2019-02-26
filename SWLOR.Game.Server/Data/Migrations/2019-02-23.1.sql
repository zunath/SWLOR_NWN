
INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 29 ,   -- ID - int
         N'Viscara - Cavern Resources' , -- Name - nvarchar(64)
         64     -- SpawnObjectTypeID - int
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID )
VALUES ( 80 ,   -- ID - int
         29 ,   -- SpawnID - int
         N'ore_vein' , -- Resref - nvarchar(16)
         30 ,   -- Weight - int
         N'OreSpawnRule' , -- SpawnRule - nvarchar(32)
         NULL ,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0     -- DeathVFXID - int
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID )
VALUES ( 81 ,   -- ID - int
         29 ,   -- SpawnID - int
         N'crystal_cluster' , -- Resref - nvarchar(16)
         70 ,   -- Weight - int
         N'CrystalClusterSpawnRule' , -- SpawnRule - nvarchar(32)
         NULL ,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0     -- DeathVFXID - int
    )