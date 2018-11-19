-- Move Electric Fist category from Gathering to Martial Arts
UPDATE dbo.Perk
SET PerkCategoryID = 17
WHERE ID = 93


-- Add reserved names topic
INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 66 ,    -- ID - int
         N'Reserved Names/Words' ,  -- Name - nvarchar(32)
         N'The following names/words are reserved. Any characters containing them will be immediately deleted on entry to the server.\n\n"darth", "malak", "revan", "jedi", "sith", "yoda", "luke", "skywalker", "starkiller", "vader", "han", "solo", "boba", "bobba", "fett", "admiral", "ackbar", "c-3p0", "c3p0", "c-3po", "r2d2", "r2-d2", "qui-gon", "jinn", "greedo", "hutt", "the", "jabba", "mace", "windu", "padme", "padmé", "amidala", "poe", "dameron", "tarkin", "moff", "anakin", "lando", "calrissian", "leia", "finn", "maul", "emperor", "palpatine", "rey", "obi-wan", "kenobi", "kylo", "ren", "bb-8", "bb8", "chewbacca", "princess", "canderous", "ordo", "t3-m4", "hk-47", "carth", "onasi", "mission", "vao", "zaalbar", "bastila", "shan", "juhani", "jolee", "bindo", "atton", "rand", "bao", "dur", "bao-dur", "mical", "mira", "hanharr", "brianna", "visas", "marr", "g0-t0", "go-to", "goto", "zayne", "carrick", "marn", "hierogryph", "jarael", "gorman", "vandrayk", "elbee", "rohlan", "dyre", "slyssk", "sion", "nihilus", "general", "zunath", "xephnin", "taelon", "lestat", "dm", "gm"' , 
         7 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         5 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )