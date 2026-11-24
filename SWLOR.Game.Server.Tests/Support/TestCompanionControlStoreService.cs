#nullable enable
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Creatures;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The companion control store (part CC-2) with the droid check answered by the test.</summary>
public sealed class TestCompanionControlStoreService : CompanionControlStoreService
{
    private readonly CompanionTestState _state;

    public TestCompanionControlStoreService(ICompanionLookup companions, CompanionTestState state)
        : base(companions)
    {
        _state = state;
    }

    protected override bool IsDroid(uint creature) => _state.Droids.Contains(creature);
}
