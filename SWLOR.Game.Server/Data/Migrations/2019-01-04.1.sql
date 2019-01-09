-- Correct the region on Orlando Doon's quests (was Global, should be Viscera). 
update dbo.Quest set FameRegionID=3 where ID in (20,21);