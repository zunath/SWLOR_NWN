using System.Numerics;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.Game.Server.Tests.Service.Line;

/// <summary>A test double for the game engine: it records everything a bite and its fight ask for and lets a test move the angler.</summary>
internal sealed class FakeFishingFightWorld : IFishingFightWorld
{
    private readonly List<(float Seconds, Action Action)> _scheduled = new();

    public List<string> Log { get; } = new();
    public List<string> Messages { get; } = new();
    public List<string> FishCreated { get; } = new();
    public Dictionary<uint, int> Bait { get; } = new();
    public Dictionary<uint, int> PointAttempts { get; } = new();
    public List<uint> ExhaustedPoints { get; } = new();
    public List<int> Xp { get; } = new();
    public List<FishingFight> Opened { get; } = new();

    public bool Busy { get; private set; }
    public bool WindowOpen { get; set; }
    public bool CanOpenWindow { get; set; } = true;
    public bool IsValid { get; set; } = true;
    public bool IsDead { get; set; }
    public bool IsInCombat { get; set; }
    public Vector3 Position { get; set; } = new(10f, 20f, 0f);

    public int ScheduledCount => _scheduled.Count;

    public void SetRemainingBait(uint rod, int remaining)
    {
        Log.Add("bait");
        Bait[rod] = remaining;
    }

    public void SetFishingPointAttempts(uint fishingPoint, int remaining)
    {
        Log.Add("attempts");
        PointAttempts[fishingPoint] = remaining;
    }

    public void ExhaustFishingPoint(uint fishingPoint)
    {
        Log.Add("exhaust");
        ExhaustedPoints.Add(fishingPoint);
    }

    public void GiveAgricultureXp(uint player, int xp)
    {
        Log.Add("xp");
        Xp.Add(xp);
    }

    public void Message(uint player, string text)
    {
        Log.Add("message");
        Messages.Add(text);
    }

    public void CreateFish(uint player, string resref)
    {
        Log.Add("fish");
        FishCreated.Add(resref);
    }

    public void BeginFight(uint player)
    {
        Log.Add("busy");
        Busy = true;
    }

    public void EndFight(uint player)
    {
        Log.Add("free");
        Busy = false;
    }

    public bool OpenWindow(uint player, FishingFight fight)
    {
        Log.Add("open");
        if (!CanOpenWindow)
            return false;

        Opened.Add(fight);
        WindowOpen = true;
        return true;
    }

    public void CloseWindow(uint player)
    {
        Log.Add("close");
        WindowOpen = false;
    }

    public FishingPlayerState GetPlayerState(uint player) =>
        new(IsValid, IsDead, IsInCombat, Position, WindowOpen);

    public void Schedule(float seconds, Action action) => _scheduled.Add((seconds, action));

    /// <summary>Runs the next scheduled check, the way a second passing would.</summary>
    public void RunNextScheduled()
    {
        var next = _scheduled[0];
        _scheduled.RemoveAt(0);
        next.Action();
    }
}
