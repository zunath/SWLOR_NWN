
INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 28 , -- ID - int
         N'Hutlar - Byysk' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 29 , -- ID - int
         N'Hutlar - Qion Slugs' -- Name - nvarchar(32)
    )

INSERT INTO dbo.NPCGroup ( ID ,
                           Name )
VALUES ( 30 , -- ID - int
         N'Qion Tiger' -- Name - nvarchar(32)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 110 , -- ID - int
         N'Hutlar - Byysk' -- Name - nvarchar(64)
    )

INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 31 ,   -- ID - int
         N'Hutlar - Byysk' , -- Name - nvarchar(64)
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
VALUES ( 6 ,   -- ID - int
         31 ,   -- SpawnID - int
         N'byysk_warrior' , -- Resref - nvarchar(16)
         10 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         28 ,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         11     -- AIFlags - int
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
VALUES ( 7 ,   -- ID - int
         31 ,   -- SpawnID - int
         N'byysk_warrior2' , -- Resref - nvarchar(16)
         10 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         28 ,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         11     -- AIFlags - int
    )

INSERT INTO dbo.Spawn ( ID ,
                        Name ,
                        SpawnObjectTypeID )
VALUES ( 32 ,   -- ID - int
         N'Hutlar - Qion Animals' , -- Name - nvarchar(64)
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
VALUES ( 8 ,   -- ID - int
         32 ,   -- SpawnID - int
         N'qion_slug' , -- Resref - nvarchar(16)
         10 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         29 ,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         15     -- AIFlags - int
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
VALUES ( 76 ,   -- ID - int
         32 ,   -- SpawnID - int
         N'qion_tiger' , -- Resref - nvarchar(16)
         8 ,   -- Weight - int
         N'' , -- SpawnRule - nvarchar(32)
         30 ,   -- NPCGroupID - int
         N'StandardBehaviour' , -- BehaviourScript - nvarchar(64)
         0 ,   -- DeathVFXID - int
         15     -- AIFlags - int
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 111 , -- ID - int
         N'Qion Tundra - Northeast' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 112 , -- ID - int
         N'Qion Tundra - Northwest' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 113 , -- ID - int
         N'Qion Tundra - Southeast' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 114 , -- ID - int
         N'Qion Tundra - Southwest' -- Name - nvarchar(64)
    )

INSERT INTO dbo.SpaceStarport ( ID ,
                                Planet ,
                                Name ,
                                CustomsDC ,
                                Cost ,
                                Waypoint )
VALUES ( '4A882E34-437E-4300-ACE4-D43428F2CFE0' , -- ID - uniqueidentifier
         N'Hutlar' ,  -- Planet - nvarchar(32)
         N'Hutlar Outpost Starport' ,  -- Name - nvarchar(32)
         50 ,    -- CustomsDC - int
         400 ,    -- Cost - int
         N'HUTLAR_LANDING'    -- Waypoint - nvarchar(32)
    )