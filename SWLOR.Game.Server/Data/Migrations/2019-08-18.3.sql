INSERT INTO dbo.Spawn ( ID ,
                        Name,
						SpawnObjectTypeID)
VALUES ( 47 ,   -- ID - int
        'Tatooine Exterior Resources',
		64
		)


INSERT INTO dbo.SpawnObject ( ID ,
                        SpawnID,
						Resref,
						Weight,
						SpawnRule,
						NPCGroupID,
						BehaviourScript,
						DeathVFXID,
						AIFlags)
VALUES ( 110 ,   -- ID - int
        47,
		'fiberplast_shrub',
		100,
		'FiberplastSpawnRule',
		NULL,
		'',
		0,
		0
		)

INSERT INTO dbo.LootTable ( ID ,
                              Name)
VALUES ( 107 ,   -- ID - int
        'Tatooine Tusken Camp Scavenge Points' -- Message
		)

INSERT INTO dbo.LootTableItem ( 
                              LootTableID ,
                              Resref ,
                              MaxQuantity ,
                              Weight ,
                              IsActive ,
                              SpawnRule 
							 )
VALUES ( 
         107,   -- LootTableID - int
         N'const_parts_tsk' , -- Resref - nvarchar(16)
         1 ,   -- MaxQuantity - int
         N'10' , -- Weight - nvarchar(32)
         1,   -- IsActive - bool
         N'' -- SpawnRule
		)

INSERT INTO dbo.LootTableItem ( 
                              LootTableID ,
                              Resref ,
                              MaxQuantity ,
                              Weight ,
                              IsActive ,
                              SpawnRule 
							 )
VALUES ( 
         107,   -- LootTableID - int
         N'tsk_electronics' , -- Resref - nvarchar(16)
         1 ,   -- MaxQuantity - int
         N'5' , -- Weight - nvarchar(32)
         1,   -- IsActive - bool
         N'' -- SpawnRule
		)

INSERT INTO dbo.LootTableItem ( 
                              LootTableID ,
                              Resref ,
                              MaxQuantity ,
                              Weight ,
                              IsActive ,
                              SpawnRule 
							 )
VALUES ( 
         107,   -- LootTableID - int
         N'tsk_metal' , -- Resref - nvarchar(16)
         1 ,   -- MaxQuantity - int
         N'1' , -- Weight - nvarchar(32)
         1,   -- IsActive - bool
         N'' -- SpawnRule
		)