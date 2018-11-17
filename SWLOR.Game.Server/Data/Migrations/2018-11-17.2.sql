
INSERT INTO dbo.GameTopicCategory ( ID ,
                                    Name )
VALUES ( 7 , -- ID - int
         N'Rules' -- Name - nvarchar(32)
    )

INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 11 ,    -- ID - int
         N'Rule #1 - Role Play' ,  -- Name - nvarchar(32)
         N'You are expected to role play in all interactions with players and DMs. You may not use famous characters, unearned titles or character concepts which are harmful to the world (e.g. Luke Skywalker, Revan or Darth Vader). Character concepts may be rejected at a DM’s discretion and you are expected to remake without complaint.' ,  -- Text - nvarchar(max)
         7 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         1 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )

INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 63 ,    -- ID - int
         N'Rule #2 - Play Respectfully' ,  -- Name - nvarchar(32)
         N'Cybering and erotic role play (ERP) is NOT PERMITTED on this server. Our doors are open to a vast number of age ranges and, for this reason, restrict you to a “PG-13” level of interaction. Unwarranted rudeness, potentially offensive role play, inappropriate sexual references, harassment, exploiting known or unknown bugs, logging to avoid consequences, etc are prohibited.' ,  -- Text - nvarchar(max)
         7 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         2 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )

INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 64 ,    -- ID - int
         N'Rule #3 - Listen to the DMs' ,  -- Name - nvarchar(32)
         N'Dungeon Masters are to be considered the final authority in any dispute, question, or issue that comes up. By playing Star Wars: Legends of the Old Republic you agree to abide by their decisions. If there is a dispute with a DM ruling or you feel you’ve been dealt with unfairly, OBEY THE RULING at the time and then contact the admin staff. You may reach us through Discord or in a private message on the forums.' ,  -- Text - nvarchar(max)
         7 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         3 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )

INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 65 ,    -- ID - int
         N'Rule #4 - PvP' ,  -- Name - nvarchar(32)
         N'Combat actions against other PCs (PvP) must be interactively role played. This means: you interact, they interact, BEFORE any battle occurs. You are expected to wait one real-world day before participating in PvP or interacting in any way with that PC or other hostile PCs from the battle unless both sides explicitly agree otherwise. Having an opposing character type (i.e Jedi vs Sith) is not a sufficient reason for a PvP action.' ,  -- Text - nvarchar(max)
         7 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         4 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )