﻿Update PerkLevelSkillRequirement Set RequiredRank = 10 WHERE PerkLevelID = 2340 AND SkillID = 19;
Update PerkLevelSkillRequirement Set RequiredRank = 20 WHERE PerkLevelID = 2341 AND SkillID = 19;
Update PerkLevelSkillRequirement Set RequiredRank = 30 WHERE PerkLevelID = 2342 AND SkillID = 19;
Update PerkLevelSkillRequirement Set RequiredRank = 40 WHERE PerkLevelID = 2343 AND SkillID = 19;


Update CooldownCategory Set BaseCooldownTime = 300 WHERE ID = 17;

Insert into JukeboxSong (ID , AmbientMusicID, FileName, DisplayName, IsActive)
Values(459, 614 , 'simsbuy', 'Sims Buy' , 1);


Update PerkFeat Set ConcentrationFPCost = 3 WHERE PerkID = 187 AND FeatID = 1256;
Update PerkFeat Set ConcentrationFPCost = 3 WHERE PerkID = 187 AND FeatID = 1257;
Update PerkFeat Set ConcentrationFPCost = 4 WHERE PerkID = 187 AND FeatID = 1258;
Update PerkFeat Set ConcentrationFPCost = 4 WHERE PerkID = 187 AND FeatID = 1259;
Update PerkFeat Set ConcentrationFPCost = 5 WHERE PerkID = 187 AND FeatID = 1260;

Update PerkFeat Set ConcentrationTickInterval = 6 WHERE PerkID = 181 AND FeatID = 1208;
Update PerkFeat Set ConcentrationTickInterval = 6 WHERE PerkID = 181 AND FeatID = 1209;
Update PerkFeat Set ConcentrationTickInterval = 6 WHERE PerkID = 181 AND FeatID = 1210;
Update PerkFeat Set ConcentrationTickInterval = 6 WHERE PerkID = 181 AND FeatID = 1211;
Update PerkFeat Set ConcentrationTickInterval = 6 WHERE PerkID = 181 AND FeatID = 1212;

Update PerkFeat Set ConcentrationFPCost = 3 WHERE PerkID = 181 AND FeatID = 1208;
Update PerkFeat Set ConcentrationFPCost = 4 WHERE PerkID = 181 AND FeatID = 1209;
Update PerkFeat Set ConcentrationFPCost = 5 WHERE PerkID = 181 AND FeatID = 1210;
Update PerkFeat Set ConcentrationFPCost = 6 WHERE PerkID = 181 AND FeatID = 1211;
Update PerkFeat Set ConcentrationFPCost = 7 WHERE PerkID = 181 AND FeatID = 1212;

Update CooldownCategory Set BaseCooldownTime = 14 WHERE ID  = 41;
Update CooldownCategory Set BaseCooldownTime = 14 WHERE ID  = 42;
Update CooldownCategory Set BaseCooldownTime = 14 WHERE ID  = 43;

Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 3;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 13;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 19;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 174;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 181;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 182;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 184;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 185;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 4;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 76;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 126;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 173;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 183;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 175;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 176;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 177;
Update PerkLevel Set SpecializationID = 0  WHERE PerkID = 180;