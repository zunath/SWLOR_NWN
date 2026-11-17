using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// The item reads the item cache makes (P5-15a): each answer is the one the item service gives for the same item.
/// </summary>
[TestFixture]
public class ItemCacheInfoTests
{
    private const uint Item = 5001;

    [Test]
    public void Each_read_is_the_item_services_answer_for_that_item()
    {
        var items = ScriptedProxy.Create<IItemService>(out var script);
        script
            .On(nameof(IItemService.GetIconResref), args => (uint)args[0]! == Item ? "icon_a" : "")
            .On(nameof(IItemService.IsLegacyItem), args => (uint)args[0]! == Item)
            .On(nameof(IItemService.IsEconomyRestricted), args => (uint)args[0]! != Item);
        var info = new ItemCacheInfo(items);

        info.GetIconResref(Item).Should().Be("icon_a");
        info.IsLegacyItem(Item).Should().BeTrue();
        info.IsEconomyRestricted(Item).Should().BeFalse();
        info.IsEconomyRestricted(Item + 1).Should().BeTrue();
        script.Calls.Select(call => call.Member).Should().Equal(
            nameof(IItemService.GetIconResref), nameof(IItemService.IsLegacyItem), nameof(IItemService.IsEconomyRestricted), nameof(IItemService.IsEconomyRestricted));
    }
}
