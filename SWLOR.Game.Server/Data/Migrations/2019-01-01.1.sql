update dbo.LootTableItem set Resref='nw_it_gold001' where Resref='nw_it_ld001';

update dbo.CraftBlueprint set EnhancementSlots=1 where CraftCategoryID in (30,31,32,33,34,35,36,37,38);