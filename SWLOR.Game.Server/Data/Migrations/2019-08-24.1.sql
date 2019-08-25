--Fabrication

-- Structure Frames
-- Small
Update dbo.CraftBlueprint
Set BaseLevel = 2
Where ID = 316

Update dbo.CraftBlueprint
Set MainMinimum = 2
Where ID = 316


--Medium
Update dbo.CraftBlueprint
Set BaseLevel = 4
Where ID = 317

Update dbo.CraftBlueprint
Set SecondaryMinimum = 2
Where ID = 317


--Large
Update dbo.CraftBlueprint
Set BaseLevel = 6
Where ID = 318

Update dbo.CraftBlueprint
Set SecondaryMinimum = 2
Where ID = 318

--Power Relay/ComputingModule/Mainframe/LargeHouse/LargeTower Base Level Drop
Update dbo.CraftBlueprint
Set BaseLevel = 1
Where ID = 320

Update dbo.CraftBlueprint
Set BaseLevel = 2
Where ID = 315

Update dbo.CraftBlueprint
Set BaseLevel = 3
Where ID = 314

Update dbo.CraftBlueprint
Set BaseLevel = 20
Where ID = 473

Update dbo.CraftBlueprint
Set BaseLevel = 6
Where ID = 313

-- Fixing Silo's
--Level 1
Update dbo.CraftBlueprint
Set BaseLevel = 5 
Where ID IN (490 ,495 ,599 ,604)
--Level 2
Update dbo.CraftBlueprint
Set BaseLevel = 10
Where ID IN (491 ,496 ,600 ,605)
--Level 3
Update dbo.CraftBlueprint
Set BaseLevel = 15
Where ID IN (492 ,497 ,601 ,606)
--Level 4
Update dbo.CraftBlueprint
Set BaseLevel = 20
Where ID IN (493 ,498 ,602 ,607)

--Repair Kit
Update dbo.CraftBlueprint
Set SecondaryMinimum = 1
Where ID = 665

-- End Fabrication

-- Engineering

--Starship Fixing
Update dbo.CraftBlueprint
Set BaseLevel = 0
Where ID = 661

Update dbo.CraftBlueprint
Set MainMinimum = 2
Where ID = 661

--Technically amrorsmithing but who cares...
Update dbo.CraftBlueprint
Set BaseLevel = 4
Where ID = 660

-- Fuel Cell

Update dbo.CraftBlueprint
Set Quantity = 5
Where ID = 484

Update dbo.CraftBlueprint
Set Quantity = 15
Where ID = 485

Update dbo.CraftBlueprint
Set Quantity = 20
Where ID = 486

Update dbo.CraftBlueprint
Set Quantity = 25
Where ID = 487

Update dbo.CraftBlueprint
Set Quantity = 35
Where ID = 488

-- End Engineering