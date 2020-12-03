Update PerkLevelSkillRequirement Set RequiredRank = 10 WHERE PerkLevelID = 2340 AND SkillID = 19;
Update PerkLevelSkillRequirement Set RequiredRank = 20 WHERE PerkLevelID = 2341 AND SkillID = 19;
Update PerkLevelSkillRequirement Set RequiredRank = 30 WHERE PerkLevelID = 2342 AND SkillID = 19;
Update PerkLevelSkillRequirement Set RequiredRank = 40 WHERE PerkLevelID = 2343 AND SkillID = 19;


Update CooldownCategory Set BaseCooldownTime = 300 WHERE ID = 17;

Insert into JukeboxSong (ID , AmbientMusicID, FileName, DisplayName, IsActive)
Values(459, 614 , 'simsbuy', 'Sims Buy' , 1);
