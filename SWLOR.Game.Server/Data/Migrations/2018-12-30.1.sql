
-- Fix the categories for prismatic item blueprints.
update SWLOR.dbo.CraftBlueprint set CraftCategoryID = 19 where ID=275;
update SWLOR.dbo.CraftBlueprint set CraftCategoryID = 18 where ID=276;

-- Fix the required components for prismatic item blueprints.
update SWLOR.dbo.CraftBlueprint set SecondaryComponentTypeID=33,TertiaryComponentTypeID=36 where ID=307;
update SWLOR.dbo.CraftBlueprint set SecondaryComponentTypeID=35,TertiaryComponentTypeID=38 where ID=275;
update SWLOR.dbo.CraftBlueprint set SecondaryComponentTypeID=34,TertiaryComponentTypeID=37 where ID=276;
update SWLOR.dbo.CraftBlueprint set SecondaryComponentTypeID=34,TertiaryComponentTypeID=37 where ID=306;