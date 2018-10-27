
UPDATE dbo.Perks
SET Name = REPLACE(Name, 'Magic', 'Force'),
	ScriptName = 'Weaponsmith.ForceModInstallationWeapons',
	Description = REPLACE(Description, 'magic', 'force')
WHERE PerkID IN (86)

GO

UPDATE dbo.Perks
SET Name = REPLACE(Name, 'Magic', 'Force'),
	ScriptName = 'Armorsmith.ForceModInstallationArmors',
	Description = REPLACE(Description, 'magic', 'force')
WHERE PerkID IN (110)

GO

UPDATE dbo.PerkLevels
SET Description = REPLACE(Description, 'runes', 'mods')
WHERE Description LIKE '%runes%'
