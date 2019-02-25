
ALTER TABLE dbo.ComponentType
ADD ReassembledResref NVARCHAR(16) NOT NULL DEFAULT ''

GO



UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_metal'
WHERE ID = 2

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_organic'
WHERE ID = 3

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_fiberplast'
WHERE ID = 12

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_leather'
WHERE ID = 13

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_electronics'
WHERE ID = 15

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_powcry'
WHERE ID = 21


UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_bluecry'
WHERE ID = 24

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_redcry'
WHERE ID = 25

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_greencry'
WHERE ID = 26

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_yellowcry'
WHERE ID = 27

UPDATE dbo.ComponentType
SET ReassembledResref = 'ass_herb'
WHERE ID = 48
