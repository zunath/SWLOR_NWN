using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The name override instance with its one seam answered by the test: the repeating AFK movement check.</summary>
public sealed class BenchPlayerNameOverrideService : PlayerNameOverrideService
{
    public BenchPlayerNameOverrideService(IPlayerNameService playerName, IPlayerListRefresh playerListRefresh, IPortraitTellGuard portraitTellGuard)
        : base(playerName, playerListRefresh, portraitTellGuard)
    {
    }

    public Action? RepeatingTask { get; private set; }
    public int DisposedChecks { get; private set; }

    protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule)
    {
        RepeatingTask = task;
        return new Handle(this);
    }

    private sealed class Handle : IDisposable
    {
        private readonly BenchPlayerNameOverrideService _owner;

        public Handle(BenchPlayerNameOverrideService owner) => _owner = owner;

        public void Dispose() => _owner.DisposedChecks++;
    }
}
