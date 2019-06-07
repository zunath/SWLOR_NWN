
-- This table is redundant because all skills use the same XP requirements. If they didn't, skill decay wouldn't
-- work properly. I've changed the code to read XP requirements from a static dictionary instead of pinging
-- the DB/cache. This table is no longer necessary and will be dropped.
DROP TABLE dbo.SkillXPRequirement