#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The game data the saved-character migrations read: nothing is answered before it is loaded, and what is supplied is
/// answered back. (The data a server loads from the 2DAs needs the engine and is covered by the in-engine tests.)
/// </summary>
[TestFixture]
public class SavedCharacterDataStoreTests
{
    [Test]
    public void A_new_store_is_not_loaded_and_knows_the_default_container_grid()
    {
        var store = new SavedCharacterDataStore();

        store.IsLoaded.Should().BeFalse();
        store.ContainerWidth.Should().Be(7);
        store.ContainerHeight.Should().Be(5);
    }

    [Test]
    public void Reading_before_the_data_is_loaded_throws()
    {
        var store = new SavedCharacterDataStore();

        var reads = new Action[]
        {
            () => store.GetInventorySize(BaseItem.Ring),
            () => store.GetPropertyDefinition(ItemPropertyType.Accuracy),
            () => store.GetBlueprint("blueprint"),
            () => store.TryGetDamageCost(1, out _),
            () => store.GetClassHitDie((ClassType)1),
            () => store.TryGetClassPackage((ClassType)1, out _),
            () => store.WeaponHasInventoryIcon(BaseItem.Longsword, 1),
        };

        foreach (var read in reads)
            read.Should().Throw<InvalidOperationException>().WithMessage("Saved character data is not loaded.");
    }

    [Test]
    public void Supplied_data_is_answered_and_unknown_entries_name_what_is_missing()
    {
        var store = new SavedCharacterDataStore();
        store.Load(
            new Dictionary<int, (int Width, int Height)> { [(int)BaseItem.Ring] = (1, 1) },
            new Dictionary<int, (int CostTable, int Param1Table)> { [(int)ItemPropertyType.Accuracy] = (3, 255) },
            new Dictionary<string, string> { ["blueprint"] = "UTI" });

        store.IsLoaded.Should().BeTrue();
        store.GetInventorySize(BaseItem.Ring).Should().Be((1, 1));
        store.GetPropertyDefinition(ItemPropertyType.Accuracy).Should().Be((3, 255));
        store.GetBlueprint("blueprint").Should().Be("UTI");
        var missing = new Action[]
        {
            () => store.GetInventorySize(BaseItem.Longsword),
            () => store.GetPropertyDefinition(ItemPropertyType.DMG),
            () => store.GetBlueprint("other"),
        };
        foreach (var read in missing)
            read.Should().Throw<InvalidOperationException>();
    }
}
