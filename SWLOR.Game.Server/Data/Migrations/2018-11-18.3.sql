
INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 47 , -- ID - int
         N'Viscara - Herb Patch' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 47 ,    -- LootTableID - int
         'herb_v' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         30 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID )
VALUES ( 1 ,   -- ID - int
         16 ,   -- SpawnID - int
         N'herbs_patch' , -- Resref - nvarchar(16)
         30 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         NULL,   -- NPCGroupID - int
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
VALUES ( 2 ,   -- ID - int
         17 ,   -- SpawnID - int
         N'herbs_patch' , -- Resref - nvarchar(16)
         30 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         NULL,   -- NPCGroupID - int
         N'' , -- BehaviourScript - nvarchar(64)
         0     -- DeathVFXID - int
    )
