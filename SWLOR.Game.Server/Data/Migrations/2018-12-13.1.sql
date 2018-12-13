
ALTER TABLE dbo.PCBaseStructureItem
ADD CONSTRAINT UQ_PCBaseStructureItem_ItemGlobalID UNIQUE(ItemGlobalID)
GO

ALTER TABLE dbo.BankItem
ADD CONSTRAINT UQ_BankItem_ItemID UNIQUE(ItemID)