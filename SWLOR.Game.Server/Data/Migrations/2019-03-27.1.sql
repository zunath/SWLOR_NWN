
INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 70 ,    -- ID - int
         N'Echani' ,  -- Name - nvarchar(32)
         N'Bonuses: +1 DEX, +1 CON\nPenalties: -1 WIS, -1 CHA\nStarting Languages: Galactic Basic\n\nThe Echani are a matriarchal caste-based society originating from the Inner Rim world of Eshan.\n\nThe Echani have similar anatomy to that of humans, but are physically distinct due to their light skin, white hair and silver eyes. They exhibit among themselves a remarkable similarity to each other in their body type and facial features, with close family members such as siblings often appearing indistinguishable to an outside observer.\n\nIt''s believed that the Echani are a result of a Arkanian experimentation with the Human genome, a theory which could explain the resemblance among these members of their species.\n\nDue to the all-encompassing use of combat in all levels of their culture, Echani Generals are typically seen by others as having the ability to predict their opponent''s next move. This is not a biological trait inherent to the Echani. Instead, it arose from a culture where combat is seen as the truest form of communication.' ,  -- Text - nvarchar(max)
         2 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         11 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )
	
INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 71 ,    -- ID - int
         N'Mon Calamari' ,  -- Name - nvarchar(32)
         N'Bonuses: +1 WIS, +1 CON\nPenalties: -1 INT, -1 CHA\nStarting Languages: Galactic Basic, Mon Calamarian\n\nThe Mon Calamari are a fish-like amphibious humanoid species with salmon-colored skin, webbed hands, high domed heads and huge, fish-like eyes. They are equally capable of breathing both on land and in water with them being at home in either environment. \n\nThe Mon Calamari have developed a very advanced and civilized culture. Art, music, literature and science show a creativity surpassed by by few in the galaxy. In addition to this, they have developed a reputation for being one of the most skilled starship designers in the galaxy. This partly stemmed from the fact that they see everything as a work of art rather than being a simple tool or weapon.\n\nTheir species has long been recognized for their organizational and analytical skills. It''s this trait that make them brilliant strategists and tacticians.\n\nIn terms of governance, the Mon Calamari make use of a highly efficient representative form of government. They tend to be lawful and an organized people with little tolerance for those that operate outside the status quo. As such, Mon Calamari tend to dislike and mistrust smugglers.',
         2 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         12 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )