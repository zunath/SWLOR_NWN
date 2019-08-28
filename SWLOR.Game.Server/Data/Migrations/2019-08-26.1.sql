-- Fixes Tables for drills in Hutlar
UPDATE dbo.LootTableItem SET Resref = 'p_crystal_blue'
WHERE ID IN (1722 ,1732 ,1742 , 1752)

UPDATE dbo.LootTableItem SET Resref = 'p_crystal_red'
WHERE ID IN (1723 ,1733 ,1743 , 1753)

UPDATE dbo.LootTableItem SET Resref = 'p_crystal_green'
WHERE ID IN (1724 ,1734 ,1744 , 1754)

UPDATE dbo.LootTableItem SET Resref = 'p_crystal_yellow'
WHERE ID IN (1725 ,1735 ,1745 , 1755)