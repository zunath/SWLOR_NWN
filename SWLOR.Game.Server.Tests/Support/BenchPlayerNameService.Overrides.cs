namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seams the override and AFK code calls: the repeating check and the generated descriptor.</summary>
public sealed partial class BenchPlayerNameService
{
    public Action? RepeatingTask { get; private set; }
    public int DisposedChecks { get; private set; }
    public List<uint> EnsuredDescriptors { get; } = new();

    protected override IDisposable ScheduleRepeating(Action task, TimeSpan schedule)
    {
        RepeatingTask = task;
        return new Handle(this);
    }

    public override bool EnsureUnknownDisplayName(uint player)
    {
        EnsuredDescriptors.Add(player);
        return true;
    }

    private sealed class Handle : IDisposable
    {
        private readonly BenchPlayerNameService _owner;

        public Handle(BenchPlayerNameService owner) => _owner = owner;

        public void Dispose() => _owner.DisposedChecks++;
    }
}
