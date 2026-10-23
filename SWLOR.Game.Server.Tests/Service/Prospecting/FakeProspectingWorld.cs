using System.Numerics;
using SWLOR.Game.Server.Service.ProspectingService;

namespace SWLOR.Game.Server.Tests.Service.Prospecting;

/// <summary>A test double for the game engine: it records everything a session asks for and lets a test move the player.</summary>
internal sealed class FakeProspectingWorld : IProspectingWorld
{
    private readonly List<(float Seconds, Action Action)> _scheduled = new();

    public Dictionary<uint, string> Nodes { get; } = new();
    public HashSet<uint> LiveNodes { get; } = new();
    public List<uint> DestroyedNodes { get; } = new();
    public Dictionary<uint, int> Charges { get; } = new();
    public List<uint> DestroyedHarvesters { get; } = new();
    public List<HarvestDelivery> Harvests { get; } = new();
    public List<int> Xp { get; } = new();
    public List<string> Messages { get; } = new();
    public List<float> Work { get; } = new();
    public List<float> ScheduledSeconds { get; } = new();
    public List<ProspectingSession> Opened { get; } = new();
    public double GemChanceValue { get; set; } = 0.1;
    public int GatheringRank { get; set; } = 20;

    public bool Busy { get; private set; }

    /// <summary>True while the player's busy state is the prospecting one the session set; a test clears it to stand in for a rest.</summary>
    public bool OwnsActivity { get; set; }

    public bool HasHarvester { get; set; } = true;
    public bool WindowOpen { get; set; }
    public bool CanOpenWindow { get; set; } = true;
    public bool OpeningTheWindowThrows { get; set; }
    public bool IsValid { get; set; } = true;
    public bool IsDead { get; set; }
    public bool IsInCombat { get; set; }
    public Vector3 Position { get; set; } = new(10f, 20f, 0f);

    public int ScheduledCount => _scheduled.Count;

    public int ItemsHarvested => Harvests.Sum(harvest => harvest.Items);

    public string LoadNode(uint node) => Nodes.TryGetValue(node, out var content) ? content : null;

    public void SaveNode(uint node, string content)
    {
        Nodes[node] = content;
        LiveNodes.Add(node);
    }

    public bool NodeExists(uint node) => LiveNodes.Contains(node);

    public double GemChance(string lootTable) => GemChanceValue;

    public void DestroyNode(uint node)
    {
        DestroyedNodes.Add(node);
        LiveNodes.Remove(node);
        Nodes.Remove(node);
    }

    public int GetCharges(uint harvester) => Charges.TryGetValue(harvester, out var charges) ? charges : 0;

    public void SetCharges(uint harvester, int charges) => Charges[harvester] = charges;

    public void DestroyHarvester(uint harvester)
    {
        DestroyedHarvesters.Add(harvester);
        Charges.Remove(harvester);
        HasHarvester = false;
    }

    public void GiveHarvest(uint player, uint node, string lootTable, HarvestDelivery delivery) => Harvests.Add(delivery);

    public int GetGatheringRank(uint player) => GatheringRank;

    public void GiveGatheringXp(uint player, int xp) => Xp.Add(xp);

    public void Message(uint player, string text) => Messages.Add(text);

    public void BeginSession(uint player)
    {
        Busy = true;
        OwnsActivity = true;
    }

    public void EndSession(uint player)
    {
        Busy = false;
        OwnsActivity = false;
    }

    public void PlayWork(uint player, float seconds) => Work.Add(seconds);

    public bool OpenWindow(uint player, ProspectingSession session)
    {
        if (OpeningTheWindowThrows)
            throw new InvalidOperationException("The window cannot be created.");

        if (!CanOpenWindow)
            return false;

        Opened.Add(session);
        WindowOpen = true;
        return true;
    }

    public void CloseWindow(uint player) => WindowOpen = false;

    public ProspectingPlayerState GetPlayerState(uint player, uint harvester) =>
        new(IsValid, IsDead, IsInCombat, Position, WindowOpen, HasHarvester, OwnsActivity);

    /// <summary>The clock the idle limit reads; a test moves it forward.</summary>
    public DateTime UtcNow { get; set; } = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    public void Schedule(float seconds, Action action)
    {
        ScheduledSeconds.Add(seconds);
        _scheduled.Add((seconds, action));
    }

    /// <summary>Runs the next scheduled callback, the way the seconds it waited passing would.</summary>
    public void RunNext()
    {
        var next = _scheduled[0];
        _scheduled.RemoveAt(0);
        next.Action();
    }

    /// <summary>Runs scheduled callbacks until the next one is the session's periodic check, which a test runs only on purpose.</summary>
    public void RunNextWork(float expectedSeconds)
    {
        var index = _scheduled.FindIndex(entry => entry.Seconds == expectedSeconds);
        if (index < 0)
            throw new InvalidOperationException($"No callback is scheduled to run after {expectedSeconds} seconds.");

        var entry = _scheduled[index];
        _scheduled.RemoveAt(index);
        entry.Action();
    }
}
