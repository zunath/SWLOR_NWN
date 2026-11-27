#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PropertyLayoutDefinition;
using SWLOR.Game.Server.Service.PropertyService;
using static SWLOR.Game.Server.Tests.Feature.SpaceItemFixtures;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Ships hold more furniture than they get item storage from: only their best pieces, up to the old furniture limit, add
/// storage, so raising the furniture limit never raises the most storage a ship can reach.
/// </summary>
public class FurnitureStorageTests
{
    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(RepositoryRoot(), "SWLOR.Game.Server"), Path.Combine)).Replace("\r\n", "\n");

    [Test]
    public void OnlyTheBestPiecesCount_WhateverTheOrder()
    {
        FurnitureStorageRules.GetFurnitureStorage(new[] { 1, 7, 1, 3 }, 0).Should().Be(12, "no limit counts every piece");
        FurnitureStorageRules.GetFurnitureStorage(new[] { 1, 7, 1, 3 }, 2).Should().Be(10, "the 7 and the 3");
        FurnitureStorageRules.GetFurnitureStorage(new[] { 3, 1, 1, 7 }, 2).Should().Be(10, "order does not matter");
        FurnitureStorageRules.GetFurnitureStorage(new[] { -5, 2 }, 0).Should().Be(2, "a piece never takes storage away");
    }

    [Test]
    public void PlacingPastTheStorageLimit_AddsStorageOnlyForABetterPiece()
    {
        var full = Enumerable.Repeat(1, 100).ToList();
        FurnitureStorageRules.GetStorageGained(full, 1, 100).Should().Be(0, "a plain piece past the limit is decoration");
        FurnitureStorageRules.GetStorageGained(full, 7, 100).Should().Be(6, "an enhanced piece takes a plain one's place");
        FurnitureStorageRules.GetStorageGained(full.Take(99).ToList(), 1, 100).Should().Be(1, "below the limit every piece counts");
        FurnitureStorageRules.GetStorageGained(full, 1, 0).Should().Be(1, "properties without a storage limit work as before");
    }

    [Test]
    public void PickingUpACountedPiece_PromotesTheBestDecoration()
    {
        var pieces = new List<int> { 7, 1, 1, 1 };
        FurnitureStorageRules.GetStorageLost(pieces, 1, 2).Should().Be(0, "a plain counted piece is replaced by another plain one");
        FurnitureStorageRules.GetStorageLost(pieces, 0, 2).Should().Be(6, "the 7 is replaced by a 1");
        FurnitureStorageRules.GetStorageLost(new List<int> { 7, 1 }, 0, 2).Should().Be(7, "nothing left to promote");
        FurnitureStorageRules.GetStorageLost(pieces, 0, 0).Should().Be(7, "without a limit a piece takes what it gave");
    }

    [Test]
    public void ACorvette_ReachesNoMoreStorageThanBefore()
    {
        // Base 40, the old limit of 100 pieces, each with a Structure Bonus V (1 + 6).
        var before = 40 + FurnitureStorageRules.GetFurnitureStorage(Enumerable.Repeat(7, 100), 0);
        var after = 40 + FurnitureStorageRules.GetFurnitureStorage(Enumerable.Repeat(7, 200), 100);
        after.Should().Be(before, "the extra 100 pieces decorate only");
    }

    [Test]
    public void ShipLayouts_RaiseTheFurnitureLimit_AndKeepTheOldOneForStorage()
    {
        var layouts = new StarshipLayoutDefinition().Build();
        var expected = new Dictionary<PropertyLayoutType, (int Furniture, int Storage)>
        {
            [PropertyLayoutType.Corvette] = (200, 100),
            [PropertyLayoutType.Consular] = (140, 80),
            [PropertyLayoutType.Throne] = (140, 80),
            [PropertyLayoutType.Merchant] = (110, 70),
            [PropertyLayoutType.Condor] = (90, 60),
            [PropertyLayoutType.Fighter] = (6, 6),
            [PropertyLayoutType.Basilisk] = (40, 40),
        };
        foreach (var (type, (furniture, storage)) in expected)
        {
            layouts[type].StructureLimit.Should().Be(furniture, $"{type} furniture limit");
            layouts[type].StoragePieceLimit.Should().Be(storage, $"{type} pieces that add storage");
        }

        layouts.Values.Should().OnlyContain(x => x.StoragePieceLimit > 0 && x.StoragePieceLimit <= x.StructureLimit);
    }

    [Test]
    public void BothCaps_AreShownToPlayers()
    {
        FurnitureStorageRules.DescribeFurniture(150, 200).Should().Be("Furniture: 150 / 200");
        FurnitureStorageRules.DescribeItemStorage(112, 200).Should().Be("Item storage: 112 / 200 items");

        var window = ReadSource("Feature", "GuiDefinition", "ManageStructuresDefinition.cs");
        window.Should().Contain(".BindText(model => model.FurnitureCount)").And.Contain(".BindText(model => model.ItemStorageText)");
        var model = ReadSource("Feature", "GuiDefinition", "ViewModel", "ManageStructuresViewModel.cs");
        model.Should().Contain("FurnitureCount = Property.DescribeFurniture(property);")
            .And.Contain("ItemStorageText = FurnitureStorageRules.DescribeItemStorage(itemCount, property.ItemStorageCount);");

        var property = ReadSource("Service", "Property.cs");
        property.Should().Contain("parentProperty.ItemStorageCount += GetStorageGainedOnPlacement(parentProperty, structureItemStorage);");
        property.Should().Contain("{FurnitureStorageRules.DescribeFurniture(structureCount + 1, structureLimit)}. ")
            .And.Contain("$\"Item storage: {placedIn?.ItemStorageCount ?? 0} items.\"", "placing a piece on a ship names both caps");
        model.Should().Contain("var storageLost = Property.GetStorageLostOnRetrieval(parentProperty, structure);")
            .And.Contain("if (itemCount > parentProperty.ItemStorageCount - storageLost)")
            .And.Contain("parentProperty.ItemStorageCount -= storageLost;");
    }
}
