UPDATE dbo.Perk
SET Description = 'Provides a bonus to repairs when using Electronic repair kits.'
WHERE ID = 152
UPDATE dbo.Perk
SET Description = 'Provides a bonus to repairs when using armor repair kits'
WHERE ID = 150
UPDATE dbo.Perk
SET Description = 'Provides a bonus to repairs when using weapon repair kits'
WHERE ID = 149
UPDATE dbo.PerkLevel
SET Description = 'Gain +2 to armor repair when using an armor repair kit.'
WHERE PerkID = 150 AND Level = 1
UPDATE dbo.PerkLevel
SET Description = 'Gains +4 to armor repair when using an armor repair kit'
WHERE PerkID = 150 AND Level = 2
UPDATE dbo.PerkLevel
SET Description = 'Gains +6 to armor repair when using an armor repair kit'
WHERE PerkID = 150 AND Level = 3
UPDATE dbo.PerkLevel
SET Description = 'Gains +8 to armor repair when using an armor repair kit'
WHERE PerkID = 150 AND Level = 4
UPDATE dbo.PerkLevel
SET Description = 'Gains +2 to weapon repair when using an weapon repair kit'
WHERE PerkID = 149 AND Level = 1
UPDATE dbo.PerkLevel
SET Description = 'Gains +4 to weapon repair when using an weapon repair kit'
WHERE PerkID = 149 AND Level = 2
UPDATE dbo.PerkLevel
SET Description = 'Gains +6 to weapon repair when using an weapon repair kit'
WHERE PerkID = 149 AND Level = 3
UPDATE dbo.PerkLevel
SET Description = 'Gains +8 to weapon repair when using an weapon repair kit'
WHERE PerkID = 149 AND Level = 4
UPDATE dbo.PerkLevel
SET Description = 'Gains +2 to electronic repair when using an electronic repair kit'
WHERE PerkID = 152 AND Level = 1
UPDATE dbo.PerkLevel
SET Description = 'Gains +4 to electronic repair when using an electronic repair kit'
WHERE PerkID = 152 AND Level = 2
UPDATE dbo.PerkLevel
SET Description = 'Gains +6 to electronic repair when using an electronic repair kit'
WHERE PerkID = 152 AND Level = 3
UPDATE dbo.PerkLevel
SET Description = 'Gains +8 to electronic repair when using an electronic repair kit'
WHERE PerkID = 152 AND Level = 4
