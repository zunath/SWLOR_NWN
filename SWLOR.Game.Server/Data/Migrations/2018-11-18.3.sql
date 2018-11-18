
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



INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 14 , -- ID - int
         N'Viscara Warocas' -- Name - nvarchar(32)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 48 , -- ID - int
         N'Viscara - Warocas' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 49 , -- ID - int
         N'Viscara - Warocas (Rares)' -- Name - nvarchar(64)
    )
	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 48 ,    -- LootTableID - int
         'warocas_beak' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         20 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 48 ,    -- LootTableID - int
         'waro_feathers' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         5 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 49 ,    -- LootTableID - int
         'waro_leg' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         20 ,    -- Weight - tinyint
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
VALUES ( 3 ,   -- ID - int
         18 ,   -- SpawnID - int
         N'warocas' , -- Resref - nvarchar(16)
         40 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         14 ,   -- NPCGroupID - int
         N'SightAggroBehaviour' , -- BehaviourScript - nvarchar(64)
         0     -- DeathVFXID - int
    )


INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 15 , -- ID - int
         N'Valley Nashtah' -- Name - nvarchar(32)
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID )
VALUES ( 4 ,   -- ID - int
         25 ,   -- SpawnID - int
         N'vall_nashtah' , -- Resref - nvarchar(16)
         20 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         15 ,   -- NPCGroupID - int
         N'SoundAggroRandomWalkBehaviour' , -- BehaviourScript - nvarchar(64)
         0     -- DeathVFXID - int
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 50 , -- ID - int
         N'Viscara - Valley Nashtah' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 50 ,    -- LootTableID - int
         'nashtah_meat' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         30 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 50 ,    -- LootTableID - int
         'nash_scale' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         10 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 50 ,    -- LootTableID - int
         'nash_tail' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         2 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )