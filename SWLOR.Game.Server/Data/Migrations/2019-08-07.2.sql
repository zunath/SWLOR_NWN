
-- Quest data is now stored in class objects rather than the DB, for ease of use.
-- For this reason the following tables are no longer necessary.
DROP TABLE dbo.QuestKillTarget
DROP TABLE dbo.QuestPrerequisite
DROP TABLE dbo.QuestRequiredItem
DROP TABLE dbo.QuestRequiredKeyItem
DROP TABLE dbo.QuestState

ALTER TABLE dbo.QuestRewardItem
DROP CONSTRAINT FK_QuestRewards_QuestID

ALTER TABLE dbo.PCQuestStatus
DROP CONSTRAINT FK_PCQuesttatus_SelectedRewardID
DROP TABLE dbo.QuestRewardItem  

ALTER TABLE dbo.GuildTask
DROP CONSTRAINT FK_GuildTask_QuestID
ALTER TABLE dbo.PerkLevelQuestRequirement
DROP CONSTRAINT FK_PerkLevelQuestRequirement_RequiredQuestID
ALTER TABLE dbo.PCQuestStatus
DROP CONSTRAINT FK_PCQuesttatus_QuestID 

DROP TABLE dbo.Quest

DROP TABLE dbo.QuestType


