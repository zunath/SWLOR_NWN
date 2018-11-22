
UPDATE dbo.Perk
SET PerkCategoryID = 8
WHERE ID = 94

UPDATE dbo.PerkLevel
SET Description = 'Grants the Precision Targeting perk.'
WHERE PerkID = 133


-- Remove gold reward for first rites quest.
UPDATE dbo.Quest
SET RewardGold = 0
WHERE ID = 30



-- Adjust component types for red lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID = 29
WHERE ID IN (612,613,614,615,616,617,618,619,620,621)

-- Adjust component types for green lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID	= 31
WHERE ID IN (622,623,624,625,626, 627,628,629,630,631)

-- Adjust component types for yellow lightsabers and saberstaffs
UPDATE dbo.CraftBlueprint
SET TertiaryComponentTypeID	= 31
WHERE ID IN (632,633,634,635,636, 637,638,639,640,641)


-- Add the Mandalorian background to the website topics.
INSERT INTO dbo.GameTopic ( ID ,
                        Name ,
                        Text ,
                        GameTopicCategoryID ,
                        IsActive ,
                        Sequence ,
                        Icon )
VALUES ( 67 ,    -- ID - int
        N'Mandalorian' ,  -- Name - nvarchar(32)
        N'Temporary Bonus: Mandalorian Armor\nPermanent Bonus: +1 Base Attack Bonus when equipped with blaster rifles and you learn the Mandoa language.' ,  -- Text - nvarchar(max)
        3 ,    -- GameTopicCategoryID - int
        1 , -- IsActive - bit
        33 ,    -- Sequence - int
        N''    -- Icon - nvarchar(32)
)


-- Disable cooking perks until the system gets finished. Leave the skill in place.
UPDATE dbo.Perk
SET IsActive = 0
WHERE ID IN (100,102,142,148)


-- Add the Wookiee game topic

INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 68 ,    -- ID - int
         N'Wookiee' ,  -- Name - nvarchar(32)
         N'Bonuses: +1 STR, +1 CON\nPenalties: -1 DEX, -1 CHA\nStarting Languages: Galactic Basic, Shyriiwook\nNOTE: Wookiees may only speak in their native language, Shyriiwook.\n\nThe Wookiees, whose name for themselves translates to the People of the Trees, are a species of hairy bipedal humanoids that were inhabitants of the planet Kashyyyk. They have the ability to easily learn most languages. However, Wookiees lack the vocal structure to speak anything other than their own languages. The Wookiees spoke an array of dialects collectively known as Wookiee language, all of which consisted in a combination of barks, roars, moans and growls.\n\nAs their name implies, Wookiees live in tree-houses in their forested planet. Their community is led by a chieftain. Wookie society is generally peaceful, and the Holder of Laws oversees Wookiee trials and judges offenses. Murder is not permitted and is punishable by death.\n\nAlthough their culture may seem primitive, they are surprisingly capable warriors. They are strong enough to rip off the limbs of their foes with their bare hands, and can pick up combat techniques pretty quickly. This legendary strength has made them prized quarry for slavers and hunters looking to use that strength for manual labor, entertainment in gladiatorial combat, or just to test their mettle against the Wookiees'' combat prowess. Some Wookiees branch out and make names for themselves in the galaxy as hired muscle, pirates, smugglers, pilots, or mechanics.' ,  -- Text - nvarchar(max)
         2 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         9 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )
