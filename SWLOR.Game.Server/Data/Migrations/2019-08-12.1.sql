-- Lestat: We're adding in Tatooine creatures and their loot tables.

INSERT INTO dbo.LootTableItem ( 
                              LootTableID ,
                              Resref ,
                              MaxQuantity ,
                              Weight ,
                              IsActive ,
                              SpawnRule 
							 )
VALUES ( 
         102,   -- LootTableID - int
         N'womprathide' , -- Resref - nvarchar(16)
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
         102,   -- LootTableID - int
         N'womprattooth' , -- Resref - nvarchar(16)
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
         102,   -- LootTableID - int
         N'wompratclaw' , -- Resref - nvarchar(16)
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
         102,   -- LootTableID - int
         N'wompratmeat' , -- Resref - nvarchar(16)
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
         103,   -- LootTableID - int
         N'sandswimmerfin' , -- Resref - nvarchar(16)
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
         103,   -- LootTableID - int
         N'sandswimmerh' , -- Resref - nvarchar(16)
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
         103,   -- LootTableID - int
         N'sandswimmerleg' , -- Resref - nvarchar(16)
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
         104,   -- LootTableID - int
         N'wraidskin' , -- Resref - nvarchar(16)
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
         104,   -- LootTableID - int
         N'wraidtooth' , -- Resref - nvarchar(16)
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
         105,   -- LootTableID - int
         N'sanddemonclaw' , -- Resref - nvarchar(16)
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
         105,   -- LootTableID - int
         N'sanddemonhide' , -- Resref - nvarchar(16)
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
         106,   -- LootTableID - int
         N'tusk_highe' , -- Resref - nvarchar(16)
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
         106,   -- LootTableID - int
         N'tusk_leather' , -- Resref - nvarchar(16)
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
         106,   -- LootTableID - int
         N't_blast_parts' , -- Resref - nvarchar(16)
         1 ,   -- MaxQuantity - int
         N'5' , -- Weight - nvarchar(32)
         1,   -- IsActive - bool
         N'' -- SpawnRule
		)


dbo.LootTable

INSERT INTO dbo.LootTable ( ID ,
                              Name)
VALUES ( 102 ,   -- ID - int
        'Tatooine Womprat' -- Message
		)

dbo.LootTable

INSERT INTO dbo.LootTable ( ID ,
                              Name)
VALUES ( 103 ,   -- ID - int
        'Tatooine Sandswimmer' -- Message
		)

INSERT INTO dbo.LootTable ( ID ,
                              Name)
VALUES ( 104 ,   -- ID - int
        'Tatooine Wraid' -- Message
		)

INSERT INTO dbo.LootTable ( ID ,
                              Name)
VALUES ( 105 ,   -- ID - int
        'Tatooine Sand Demon' -- Message
		)

INSERT INTO dbo.LootTable ( ID ,
                              Name)
VALUES ( 106 ,   -- ID - int
        'Tatooine Tusken Raider' -- Message
		)

-- Now we're looking at monsters.

INSERT INTO dbo.NPCGroup ( ID ,
                              Name 
 )
VALUES ( 22 ,   -- ID - int
		 N'Womprat' -- Name
    )

INSERT INTO dbo.NPCGroup ( ID ,
                              Name 
 )
VALUES ( 23 ,   -- ID - int
		 N'Sandswimmer' -- Name
    )

INSERT INTO dbo.NPCGroup ( ID ,
                              Name 
 )
VALUES ( 24 ,   -- ID - int
		 N'Wraid' -- Name
    )

INSERT INTO dbo.NPCGroup ( ID ,
                              Name 
 )
VALUES ( 25 ,   -- ID - int
		 N'Sand Demon' -- Name
    )

INSERT INTO dbo.NPCGroup ( ID ,
                              Name 
 )
VALUES ( 26 ,   -- ID - int
		 N'Tusken Raider' -- Name
    )


INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 42 ,   -- ID - int
         N'Tatooine Womprat' , -- Name - nvarchar(64)
         1     -- SpawnObjectTypeID - int
		)

INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 43 ,   -- ID - int
         N'Tatooine Sandswimmer' , -- Name - nvarchar(64)
         1     -- SpawnObjectTypeID - int
		)

INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 44 ,   -- ID - int
         N'Tatooine Wraid' , -- Name - nvarchar(64)
         1     -- SpawnObjectTypeID - int
		)

INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 45 ,   -- ID - int
         N'Tatooine Sand Demon' , -- Name - nvarchar(64)
         1     -- SpawnObjectTypeID - int
		)

INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 46 ,   -- ID - int
         N'Tatooine Tusken Raider' , -- Name - nvarchar(64)
         1     -- SpawnObjectTypeID - int
		)


INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID,
                              AIFlags
                              )
VALUES ( 105 ,   -- ID - int
         42 ,   -- SpawnID - int
         N'womprat' , -- Resref - nvarchar(16)
         50 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         22,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0,     -- DeathVFXID - int
         7        -- AIFlags - int
    )


INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID,
                              AIFlags
                              )
VALUES ( 106 ,   -- ID - int
         43 ,   -- SpawnID - int
         N'sandswimmer' , -- Resref - nvarchar(16)
         50 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         23,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0,     -- DeathVFXID - int
         7        -- AIFlags - int
    )


INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID,
                              AIFlags
                              )
VALUES ( 107 ,   -- ID - int
         44 ,   -- SpawnID - int
         N'sandbeetle' , -- Resref - nvarchar(16)
         50 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         24,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0,     -- DeathVFXID - int
         7        -- AIFlags - int
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID,
                              AIFlags
                              )
VALUES ( 108 ,   -- ID - int
         45 ,   -- SpawnID - int
         N'sanddemon' , -- Resref - nvarchar(16)
         50 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         25,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0,     -- DeathVFXID - int
         7        -- AIFlags - int
    )

INSERT INTO dbo.SpawnObject ( ID ,
                              SpawnID ,
                              Resref ,
                              Weight ,
                              SpawnRule ,
                              NPCGroupID ,
                              BehaviourScript ,
                              DeathVFXID,
                              AIFlags
                              )
VALUES ( 109 ,   -- ID - int
         46 ,   -- SpawnID - int
         N'ext_tusken_tr003' , -- Resref - nvarchar(16)
         50 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         26,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0,     -- DeathVFXID - int
         7        -- AIFlags - int
    )