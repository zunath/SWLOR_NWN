
UPDATE dbo.Skill
SET Name = 'Mirialan',
	MaxRank = 20,
	IsActive = 1,
	Description = 'Ability to speak the Mirialan language.',
	[Primary] = 0,
	Secondary = 0,
	Tertiary = 0,
	ContributesToSkillCap = 0
WHERE ID = 18



INSERT INTO dbo.GameTopic ( ID ,
                            Name ,
                            Text ,
                            GameTopicCategoryID ,
                            IsActive ,
                            Sequence ,
                            Icon )
VALUES ( 69 ,    -- ID - int
         N'Mirialan' ,  -- Name - nvarchar(32)
         N'Bonuses: +1 DEX, +1 WIS\nPenalties: -1 STR, -1 CON\nStarting Languages: Galactic Basic, Mirialan\n
The Mirialan people are deeply religious and practice a primitive understanding of the Force. They believe each individual''s actions contribute to their destiny, building upon past successes and failures to drive them towards their fates.\n\nMirialans place a unique, often geometrically repeated tattoo on their face and hands to signify that they have completed a certain test or task, or achieved sufficient aptitude for a certain skill. The number of tattoos thus often act as a good indicator of how mature and/or skilled a Mirialan is.\n\nBecause the more markings bring about a form of status, Mirialan society is stratified and allows the heavily marked citizens to access greater opportunities. Despite its importance, most Mirialans do not know the entirety of the tattoo lexicon due to its complexity. The interaction between placement and positioning of the shapes is incredibly subtle and changes meaning greatly.\n\nWithin their belief system is the view that individual actions ripple through the Force, also affecting the destiny of the species as a whole. Because the Force is understood through the notion of the Cosmic Force on a basic level, the cultural significance of the energy is defined as fate. Those who are not sensitive to its call still have faith in fate, and feel that it guides their lives.' ,  -- Text - nvarchar(max)
         2 ,    -- GameTopicCategoryID - int
         1 , -- IsActive - bit
         10 ,    -- Sequence - int
         N''    -- Icon - nvarchar(32)
    )