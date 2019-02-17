

CREATE TABLE MarketCategory(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(64) NOT NULL,
	IsActive BIT NOT NULL DEFAULT 0
)

CREATE TABLE MarketRegion(
	ID INT NOT NULL PRIMARY KEY,
	Name NVARCHAR(32) NOT NULL
) 

CREATE TABLE PCMarketListing(
	ID UNIQUEIDENTIFIER PRIMARY KEY NOT NULL,
	PlayerID UNIQUEIDENTIFIER NOT NULL,
	Note NVARCHAR(1024) NOT NULL DEFAULT '',
	Price INT NOT NULL DEFAULT 0,
	MarketRegionID INT NOT NULL,
	MarketCategoryID INT NOT NULL,
	DatePosted DATETIME2 NOT NULL,
	DateExpires DATETIME2 NOT NULL,
	DateSold DATETIME2 NULL,
	ItemID NVARCHAR(60) NOT NULL,
	ItemName NVARCHAR(MAX) NOT NULL,
	ItemTag NVARCHAR(32) NOT NULL,
	ItemResref NVARCHAR(16) NOT NULL,
	ItemObject NVARCHAR(MAX) NOT NULL,
	ItemRecommendedLevel INT NOT NULL DEFAULT 0,
	ItemStackSize INT NOT NULL,


	CONSTRAINT FK_PCMarketListing_PlayerID FOREIGN KEY(PlayerID)
		REFERENCES dbo.Player(ID),
	CONSTRAINT FK_PCMarketListing_MarketRegionID FOREIGN KEY(MarketRegionID)
		REFERENCES dbo.MarketRegion(ID),
	CONSTRAINT FK_PCMarketListing_MarketCategoryID FOREIGN KEY(MarketCategoryID)
		REFERENCES dbo.MarketCategory(ID)

)

ALTER TABLE dbo.Player
ADD GoldTill INT NOT NULL DEFAULT 0


INSERT INTO dbo.MarketRegion ( ID ,
                               Name )
VALUES ( 1 , -- ID - int
         N'Viscara' -- Name - nvarchar(32)
    )
INSERT INTO dbo.MarketRegion ( ID ,
                               Name )
VALUES ( 2 , -- ID - int
         N'Tatooine' -- Name - nvarchar(32)
    )


INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 1 ,   -- ID - int
         N'Heavy Vibroblade GA' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 2 ,   -- ID - int
         N'Vibroblade BA' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 3 ,   -- ID - int
         N'Vibroblade BS' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 4 ,   -- ID - int
         N'Finesse Vibroblade D' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 5 ,   -- ID - int
         N'Heavy Vibroblade GS' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 6 ,   -- ID - int
         N'Lightsaber' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 7 ,   -- ID - int
         N'Vibroblade LS' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 8 ,   -- ID - int
         N'Finesse Vibroblade R' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 9 ,   -- ID - int
         N'Vibroblade K' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 10 ,   -- ID - int
         N'Vibroblade SS' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 11 ,   -- ID - int
         N'Baton C' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 12 ,   -- ID - int
         N'Baton M' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 13 ,   -- ID - int
         N'Baton MS' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 14 ,   -- ID - int
         N'Saberstaff' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 15 ,   -- ID - int
         N'Quarterstaff' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 16 ,   -- ID - int
         N'Twin Vibroblade DA' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 17 ,   -- ID - int
         N'Twin Vibroblade TS' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 18 ,   -- ID - int
         N'Finesse Vibroblade K' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 19 ,   -- ID - int
         N'Polearm H' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 20 ,   -- ID - int
         N'Polearm S' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 21 ,   -- ID - int
         N'Blaster Rifle' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 22 ,   -- ID - int
         N'Blaster Pistol' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 23 ,   -- ID - int
         N'Clothing' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 24 ,   -- ID - int
         N'Light Armor' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 25 ,   -- ID - int
         N'Force Armor' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 26 ,   -- ID - int
         N'Heavy Armor' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 27 ,   -- ID - int
         N'Helmet' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 28 ,   -- ID - int
         N'Shield' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 29 ,   -- ID - int
         N'Book' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 30 ,   -- ID - int
         N'Power Glove' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 31 ,   -- ID - int
         N'Scanner' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 32 ,   -- ID - int
         N'Harvester' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 33 ,   -- ID - int
         N'Component (Raw Ore)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 34 ,   -- ID - int
         N'Component (Metal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 35 ,   -- ID - int
         N'Component (Organic)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 36 ,   -- ID - int
         N'Component (Small Blade)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 37 ,   -- ID - int
         N'Component (Medium Blade)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 38 ,   -- ID - int
         N'Component (Large Blade)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 39 ,   -- ID - int
         N'Component (Shaft)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 40 ,   -- ID - int
         N'Component (Small Handle)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 41 ,   -- ID - int
         N'Component (Medium Handle)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 42 ,   -- ID - int
         N'Component (Large Handle)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 43 ,   -- ID - int
         N'Component (Enhancement)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 44 ,   -- ID - int
         N'Component (Fiberplast)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 45 ,   -- ID - int
         N'Component (Leather)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 46 ,   -- ID - int
         N'Component (Padding)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 47 ,   -- ID - int
         N'Component (Electronics)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 48 ,   -- ID - int
         N'Component (Wood Baton Frame)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 49 ,   -- ID - int
         N'Component (Metal Baton Frame)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 50 ,   -- ID - int
         N'Component (Ranged Weapon Core)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 51 ,   -- ID - int
         N'Component (Rifle Barrel)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 52 ,   -- ID - int
         N'Component (Pistol Barrel)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 53 ,   -- ID - int
         N'Component (Power Crystal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 54 ,   -- ID - int
         N'Component (Saber Hilt)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 55 ,   -- ID - int
         N'Component (Seeds)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 56 ,   -- ID - int
         N'Component (Blue Crystal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 57 ,   -- ID - int
         N'Component (Red Crystal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 58 ,   -- ID - int
         N'Component (Green Crystal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 59 ,   -- ID - int
         N'Component (Yellow Crystal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 60 ,   -- ID - int
         N'Component (Blue Crystal Cluster)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 61 ,   -- ID - int
         N'Component (Red Crystal Cluster)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 62 ,   -- ID - int
         N'Component (Green Crystal Cluster)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 63 ,   -- ID - int
         N'Component (Yellow Crystal Cluster)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 64 ,   -- ID - int
         N'Component (Power Crystal Cluster)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 65 ,   -- ID - int
         N'Component (Heavy Armor Core)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 66 ,   -- ID - int
         N'Component (Light Armor Core)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 67 ,   -- ID - int
         N'Component (Force Armor Core)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 68 ,   -- ID - int
         N'Component (Heavy Armor Segment)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 69 ,   -- ID - int
         N'Component (Light Armor Segment)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 70 ,   -- ID - int
         N'Component (Force Armor Segment)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 71 ,   -- ID - int
         N'Component (Small Structure Frame)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 72 ,   -- ID - int
         N'Component (Medium Structure Frame)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 73 ,   -- ID - int
         N'Component (Large Structure Frame)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 74 ,   -- ID - int
         N'Component (Computing Module)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 75 ,   -- ID - int
         N'Component (Construction Parts)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 76 ,   -- ID - int
         N'Component (Mainframe)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 77 ,   -- ID - int
         N'Component (Power Relay)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 78 ,   -- ID - int
         N'Component (Power Core)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 79 ,   -- ID - int
         N'Component (Ingredient)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 80 ,   -- ID - int
         N'Component (Herb)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 81 ,   -- ID - int
         N'Component (Carbosyrup)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 82 ,   -- ID - int
         N'Component (Meat)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 83 ,   -- ID - int
         N'Component (Cereal)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 84 ,   -- ID - int
         N'Component (Grain)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 85 ,   -- ID - int
         N'Component (Vegetable)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 86 ,   -- ID - int
         N'Component (Water)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 87 ,   -- ID - int
         N'Component (Curry Paste)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 88 ,   -- ID - int
         N'Component (Soup)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 89 ,   -- ID - int
         N'Component (Spiced Milk)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 90 ,   -- ID - int
         N'Component (Dough)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 91 ,   -- ID - int
         N'Component (Butter)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 92 ,   -- ID - int
         N'Component (Noodles)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 93 ,   -- ID - int
         N'Component (Eggs)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 94 ,   -- ID - int
         N'Component (Emitter)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 95 ,   -- ID - int
         N'Component (Hyperdrive)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 96 ,   -- ID - int
         N'Component (Hull Plating)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 97 ,   -- ID - int
         N'Component (Starship Weapon)' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 98 ,   -- ID - int
         N'Blue Mod' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 99 ,   -- ID - int
         N'Green Mod' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 100 ,   -- ID - int
         N'Red Mod' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 101 ,   -- ID - int
         N'Yellow Mod' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 102 ,   -- ID - int
         N'Necklace' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 103 ,   -- ID - int
         N'Ring' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 104 ,   -- ID - int
         N'Repair Kit' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 105 ,   -- ID - int
         N'Stim Pack' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 106 ,   -- ID - int
         N'Force Pack' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 107 ,   -- ID - int
         N'Healing Kit' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 108 ,   -- ID - int
         N'Resuscitation Device' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 109 ,   -- ID - int
         N'Starchart' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 110 ,   -- ID - int
         N'Fuel' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 111 ,   -- ID - int
         N'Control Tower' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 112 ,   -- ID - int
         N'Drill' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 113 ,   -- ID - int
         N'Resource Silo' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 114 ,   -- ID - int
         N'Turret' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 115 ,   -- ID - int
         N'Building' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 116 ,   -- ID - int
         N'Mass Production' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 117 ,   -- ID - int
         N'Starship Production' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 118 ,   -- ID - int
         N'Furniture' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 119 ,   -- ID - int
         N'Stronidium Silo' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 120 ,   -- ID - int
         N'Fuel Silo' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 121 ,   -- ID - int
         N'Crafting Device' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 122 ,   -- ID - int
         N'Persistent Storage' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )
	
	
INSERT INTO dbo.MarketCategory ( ID ,
                                 Name ,
                                 IsActive )
VALUES ( 123 ,   -- ID - int
         N'Starship' , -- Name - nvarchar(64)
         1 -- IsActive - bit
    )

