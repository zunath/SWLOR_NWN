using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// A hot reload drops the closure that applies a delayed item use and releases its user (P4-15), so the old build interrupts the use
/// while it quiesces (<see cref="ItemService.QuiesceForReload"/>), and only that activity.
/// </summary>
[TestFixture]
public class ItemServiceReloadTests
{
    private const uint UsingItem = 1;
    private const uint Fishing = 2;
    private const uint Idle = 3;

    [Test]
    public void A_reload_interrupts_an_item_use_still_waiting_on_its_delay_and_nothing_else()
    {
        var activity = ScriptedProxy.Create<IActivityService>(out var proxy);
        proxy
            .On(nameof(IActivityService.GetBusyType), args => (uint)args[0]! switch
            {
                UsingItem => ActivityStatusType.UseItem,
                Fishing => ActivityStatusType.Fishing,
                _ => ActivityStatusType.Invalid,
            })
            .On(nameof(IActivityService.ClearBusy), _ => null);
        var service = new ReloadItemService(activity);

        service.QuiesceForReload(new[] { UsingItem, Fishing, Idle });

        proxy.Calls.Where(call => call.Member == nameof(IActivityService.ClearBusy)).Select(call => (uint)call.Arguments[0]!)
            .Should().Equal(UsingItem);
        service.StoppedTimingBars.Should().Equal(UsingItem);
    }

    private sealed class ReloadItemService : ItemService
    {
        public ReloadItemService(IActivityService activity)
            : base(null!, null!, activity, null!, null!, null!, null!, null!, null!, null!)
        {
        }

        public List<uint> StoppedTimingBars { get; } = new();

        protected override void StopTimingBarForReload(uint player) => StoppedTimingBars.Add(player);
    }
}
