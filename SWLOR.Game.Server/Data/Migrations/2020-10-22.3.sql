INSERT INTO CooldownCategory (ID , Name, BaseCooldownTime)
Values (41, 'Force Spark', 30);

INSERT into Perk (ID, Name, IsActive, BaseCastingTime, Description, PerkCategoryID, CooldownCategoryID, ExecutionTypeID, IsTargetSelfOnly, Enmity, EnmityAdjustmentRuleID, CastAnimationID, ForceBalanceTypeID)
Values (186, 'Force Spark', 1, 0, 'Damages the Target for 10 + Intelligence Modifier.', 40, 41, 3, 0, 10, 1, null, 2);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2329, 186, 1, 1, 'Damages the Target for 10 + Intelligence Modifier.', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2330, 186, 2, 2, 'Damages the Target for 12 + (Intelligence Modifier * 115%).', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2331, 186, 3, 3, 'Damages the Target for 14 + (Intelligence Modifier * 125%).', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2332, 186, 4, 4, 'Damages the Target for 16 + (Intelligence Modifier * 150%).', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2333, 186, 5, 5, 'Damages the Target for 20 + (Intelligence Modifier * 200%).', 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (122, 186, 1251, 1, 5, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (123, 186, 1252, 2, 6, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (124, 186, 1253, 3, 7, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (125, 186, 1254, 4, 8, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (126, 186, 1255, 5, 9, 0, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2251, 2329, 19, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2252, 2330, 19, 10);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2253, 2331, 19, 20);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2254, 2332, 19, 30);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2255, 2333, 19, 40);



INSERT INTO CooldownCategory (ID , Name, BaseCooldownTime)
Values (42, 'Force Crush', 30);

INSERT into Perk (ID, Name, IsActive, BaseCastingTime, Description, PerkCategoryID, CooldownCategoryID, ExecutionTypeID, IsTargetSelfOnly, Enmity, EnmityAdjustmentRuleID, CastAnimationID, ForceBalanceTypeID)
Values (187, 'Force Crush', 1, 0, 'Damages the Target for 2 plus average of Wisdom and Intelliegence Modifiers', 40, 42, 7, 0, 10, 1, null, 2);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2334, 187, 1, 2, 'Damages the Target for 2 Average of Wisdom and Intelligence Modifiers', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2335, 187, 2, 3, 'Damages the Target for 3 Average of Wisdom and Intelligence Modifiers', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2336, 187, 3, 4, 'Damages the Target for 4 Average of Wisdom and Intelligence Modifiers', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2337, 187, 4, 5, 'Damages the Target for 5 Average of Wisdom and Intelligence Modifiers', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2338, 187, 5, 6, 'Damages the Target for 6 Average of Wisdom and Intelligence Modifiers', 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (127, 187, 1256, 1, 2, 5, 6);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (128, 187, 1257, 2, 3, 5, 6);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (129, 187, 1258, 3, 4, 5, 6);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (130, 187, 1259, 4, 5, 5, 6);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (131, 187, 1260, 5, 6, 5, 6);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2256, 2334, 19, 15);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2257, 2335, 19, 25);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2258, 2336, 19, 35);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2259, 2337, 19, 45);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2260, 2338, 19, 55);





INSERT INTO CooldownCategory (ID , Name, BaseCooldownTime)
Values (43, 'Throw Rock', 30);

INSERT into Perk (ID, Name, IsActive, BaseCastingTime, Description, PerkCategoryID, CooldownCategoryID, ExecutionTypeID, IsTargetSelfOnly, Enmity, EnmityAdjustmentRuleID, CastAnimationID, ForceBalanceTypeID)
Values (188, 'Throw Rock', 1, 0, 'Damages the Target for 5 plus Wisdom Modifier', 40, 43, 3, 0, 10, 1, null, 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2339, 188, 1, 2, 'Damages the Target for 5 plus Wisdom Modifier', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2340, 188, 2, 3, 'Damages the Target for 10 plus 125% Wisdom Modifier', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2341, 188, 3, 3, 'Damages the Target for 15 plus 150% Wisdom Modifier', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2342, 188, 4, 4, 'Damages the Target for 20 plus 175% Wisdom Modifier', 0);

INSERT into PerkLevel (ID, PerkID, Level, Price, Description, SpecializationID)
Values (2343, 188, 5, 4, 'Damages the Target for 25 plus 200% Wisdom Modifier', 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (132, 188, 1261, 1, 7, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (133, 188, 1262, 2, 8, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (134, 188, 1263, 3, 9, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (135, 188, 1264, 4, 10, 0, 0);

INSERT into PerkFeat (ID, PerkID, FeatID, PerkLevelUnlocked, BaseFPCost, ConcentrationFPCost, ConcentrationTickInterval)
VALUES (136, 188, 1265, 5, 11, 0, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2261, 2339, 19, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2262, 2340, 19, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2263, 2341, 19, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2264, 2342, 19, 0);

INSERT into PerkLevelSkillRequirement (ID, PerkLevelID, SkillID, RequiredRank)
VALUES (2265, 2343, 19, 0);