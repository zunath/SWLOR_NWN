using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>A source of chance that returns the rolls a test scripts and fails when it is asked for more.</summary>
internal sealed class ScriptedMinigameRandom : IMinigameRandom
{
    private readonly Queue<int> _d100 = new();

    public ScriptedMinigameRandom(params int[] d100Rolls)
    {
        foreach (var roll in d100Rolls)
            _d100.Enqueue(roll);
    }

    public int Calls { get; private set; }

    public void Enqueue(int roll) => _d100.Enqueue(roll);

    public int D100()
    {
        Calls++;
        if (_d100.Count == 0)
            throw new InvalidOperationException("The test did not script a roll for this call.");

        return _d100.Dequeue();
    }

    public int Next(int minInclusive, int maxExclusive)
    {
        Calls++;
        if (_d100.Count == 0)
            throw new InvalidOperationException("The test did not script a roll for this call.");

        return Math.Clamp(_d100.Dequeue(), minInclusive, maxExclusive - 1);
    }
}
