
-- Reduce crystal component requirement for mods
UPDATE dbo.CraftBlueprint
SET MainMinimum = MainMinimum-1
WHERE ID IN (
121,152,184,127,160,190,134,167,197,117,148,179,142,175,205,
122,153,185,118,149,181,131,164,194,125,158,188,129,162,192,
119,150,182,144,177,207,136,169,199,140,173,203,308,309,310,
137,170,200,130,163,193,143,176,206,126,159,189,141,174,204,
138,171,201,123,154,186,147,146,128,161,191,156,209,157,210,
133,166,196,145,178,208,139,172,202,120,151,183,135,168,198,
124,155,187)

-- Increase crystal component requirement for lightsabers/saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryMinimum = TertiaryMinimum+1,
	TertiaryMaximum = TertiaryMaximum+1
WHERE id IN (
211,622,612,632,216,627,617,637,212,623,613,633,213,624,614,634,
214,625,615,635,215,626,616,636,217,628,618,638,218,629,619,639,
219,630,620,640,220,631,621,641)



INSERT INTO dbo.LootTableItem ( LootTableID ,
                                Resref ,
                                MaxQuantity ,
                                Weight ,
                                IsActive ,
                                SpawnRule )
VALUES ( 15 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
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
VALUES ( 27 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
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
VALUES ( 31 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
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
VALUES ( 35 ,    -- LootTableID - int
         'f_crystal_blue' ,   -- Resref - varchar(16)
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
VALUES ( 17 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
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
VALUES ( 28 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
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
VALUES ( 32 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
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
VALUES ( 36 ,    -- LootTableID - int
         'f_crystal_red' ,   -- Resref - varchar(16)
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
VALUES ( 19 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
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
VALUES ( 30 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
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
VALUES ( 34 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
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
VALUES ( 38 ,    -- LootTableID - int
         'f_crystal_green' ,   -- Resref - varchar(16)
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
VALUES ( 18 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
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
VALUES ( 29 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
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
VALUES ( 33 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
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
VALUES ( 37 ,    -- LootTableID - int
         'f_crystal_yellow' ,   -- Resref - varchar(16)
         1 ,    -- MaxQuantity - int
         2 ,    -- Weight - tinyint
         1 , -- IsActive - bit
         N''    -- SpawnRule - nvarchar(64)
    )



UPDATE dbo.PerkLevelSkillRequirement
SET SkillID = 4
WHERE PerkLevelID IN (
	SELECT ID 
	FROM dbo.PerkLevel  
	WHERE PerkID = 93
)