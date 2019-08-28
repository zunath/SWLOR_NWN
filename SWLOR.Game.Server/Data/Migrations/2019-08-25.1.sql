
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

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 115 , -- ID - int
         N'Hutlar - Byysk Rare Items' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'spear_byssk' ,   -- Resref - varchar(16)
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
VALUES ( 110 ,    -- LootTableID - int
         'quarterstaff_byy' ,   -- Resref - varchar(16)
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
VALUES ( 110 ,    -- LootTableID - int
         'longsword_byysk' ,   -- Resref - varchar(16)
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
VALUES ( 110 ,    -- LootTableID - int
         'dart_byysk' ,   -- Resref - varchar(16)
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
VALUES ( 110 ,    -- LootTableID - int
         'axe_byysk' ,   -- Resref - varchar(16)
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
VALUES ( 110 ,    -- LootTableID - int
         'force_robe_byysk' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         3 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'breastplate_byys' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         1 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'tunic_byysk' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         2 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )


INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 115 ,    -- LootTableID - int
         'byysk_def_ring' ,   -- Resref - varchar(16)
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
VALUES ( 115 ,    -- LootTableID - int
         'bag_byysk' ,   -- Resref - varchar(16)
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
VALUES ( 110 ,    -- LootTableID - int
         'byysk_shield' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         1 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'powerglove_byysk' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         4 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'stim_cha1' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         2 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'stim_dex1' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         2 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

	
INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 110 ,    -- LootTableID - int
         'stim_wis1' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         2 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 116 , -- ID - int
         N'Hutlar - Qion Slugs' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTable ( ID ,
                            Name )
VALUES ( 117 , -- ID - int
         N'Hutlar - Qion Tigers' -- Name - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 117 ,    -- LootTableID - int
         'lth_imperfect' ,   -- Resref - varchar(16)
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
VALUES ( 117 ,    -- LootTableID - int
         'lth_high' ,   -- Resref - varchar(16)
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
VALUES ( 117 ,    -- LootTableID - int
         'qion_tiger_fang' ,   -- Resref - varchar(16)
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
VALUES ( 117 ,    -- LootTableID - int
         'tiger_blood' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         8 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )



INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 111 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 111 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 111 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 111 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 111 ,    -- LootTableID - int
         'c_cluster_blue' ,   -- Resref - varchar(16)
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
VALUES ( 111 ,    -- LootTableID - int
         'power_crystal_im' ,   -- Resref - varchar(16)
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
VALUES ( 111 ,    -- LootTableID - int
         'raw_croknor' ,   -- Resref - varchar(16)
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
VALUES ( 111 ,    -- LootTableID - int
         'raw_hemorgite' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         15 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 111 ,    -- LootTableID - int
         'corylus' ,   -- Resref - varchar(16)
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
VALUES ( 111 ,    -- LootTableID - int
         'coonlank_blue' ,   -- Resref - varchar(16)
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
VALUES ( 113 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 113 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 113 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 113 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 113 ,    -- LootTableID - int
         'c_cluster_red' ,   -- Resref - varchar(16)
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
VALUES ( 113 ,    -- LootTableID - int
         'power_crystal_im' ,   -- Resref - varchar(16)
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
VALUES ( 113 ,    -- LootTableID - int
         'raw_croknor' ,   -- Resref - varchar(16)
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
VALUES ( 113 ,    -- LootTableID - int
         'raw_hemorgite' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         15 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 113 ,    -- LootTableID - int
         'corylus' ,   -- Resref - varchar(16)
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
VALUES ( 113 ,    -- LootTableID - int
         'coonlank_red' ,   -- Resref - varchar(16)
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
VALUES ( 112 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 112 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 112 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 112 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 112 ,    -- LootTableID - int
         'c_cluster_yellow' ,   -- Resref - varchar(16)
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
VALUES ( 112 ,    -- LootTableID - int
         'power_crystal_im' ,   -- Resref - varchar(16)
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
VALUES ( 112 ,    -- LootTableID - int
         'raw_croknor' ,   -- Resref - varchar(16)
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
VALUES ( 112 ,    -- LootTableID - int
         'raw_hemorgite' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         15 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 112 ,    -- LootTableID - int
         'corylus' ,   -- Resref - varchar(16)
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
VALUES ( 112 ,    -- LootTableID - int
         'coonlank_yellow' ,   -- Resref - varchar(16)
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
VALUES ( 114 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 114 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 114 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 114 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         40 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 114 ,    -- LootTableID - int
         'c_cluster_green' ,   -- Resref - varchar(16)
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
VALUES ( 114 ,    -- LootTableID - int
         'power_crystal_im' ,   -- Resref - varchar(16)
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
VALUES ( 114 ,    -- LootTableID - int
         'raw_croknor' ,   -- Resref - varchar(16)
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
VALUES ( 114 ,    -- LootTableID - int
         'raw_hemorgite' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         15 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )

INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 114 ,    -- LootTableID - int
         'corylus' ,   -- Resref - varchar(16)
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
VALUES ( 114 ,    -- LootTableID - int
         'coonlank_green' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         5 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )
