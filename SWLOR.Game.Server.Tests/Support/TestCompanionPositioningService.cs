#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The companion positioning part (part CC-3) with the clock and the busy check answered by the test.</summary>
public sealed class TestCompanionPositioningService : CompanionPositioningService
{
    private readonly CompanionTestState _state;

    public TestCompanionPositioningService(ICompanionControlStoreService store, CompanionTestState state)
        : base(store, FacadeForwarder.For<IActivityService>())
    {
        _state = state;
    }

    protected override DateTime UtcNow() => _state.Now;

    protected override bool IsBusy(uint target) => _state.Busy.Contains(target);
}
