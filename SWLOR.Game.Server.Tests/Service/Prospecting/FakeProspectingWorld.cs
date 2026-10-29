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
    public List<string> WorkActions { get; } = new();
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

    /// <summary>
    /// The payout reservations ("reserve"), node saves ("node"), harvest deliveries ("harvest"), node removals ("destroy") and XP awards
    /// ("xp"), in order.
    /// </summary>
    public List<string> Order { get; } = new();

    public void SaveNode(uint node, string content)
    {
        Order.Add("node");
        Nodes[node] = content;
        LiveNodes.Add(node);
    }

    /// <summary>The units a node kept from harvesting before Prospecting, by node.</summary>
    public Dictionary<uint, int> LegacyStocks { get; } = new();

    public int LegacyStock(uint node) => LegacyStocks.TryGetValue(node, out var units) ? units : 0;

    public bool NodeExists(uint node) => LiveNodes.Contains(node);

    public double GemChance(string lootTable) => GemChanceValue;

    public void DestroyNode(uint node)
    {
        Order.Add("destroy");
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

    /// <summary>Runs as a harvest is delivered, so a test can see what the node held at that moment.</summary>
    public Action OnHarvest { get; set; }

    /// <summary>The payouts reserved and not yet delivered, with their XP: what a login would give after a stop.</summary>
    public List<HarvestPayout> Reserved { get; } = new();

    /// <summary>Runs as a payout is reserved, so a test can see what the node held at that moment.</summary>
    public Action OnReserve { get; set; }

    public HarvestPayout ReservePayout(uint player, string lootTable, HarvestDelivery delivery, int xp)
    {
        Order.Add("reserve");
        OnReserve?.Invoke();
        var payout = new HarvestPayout(delivery, Enumerable.Repeat(lootTable, delivery.Items).ToList(), Reserved.Count + 1, xp);
        Reserved.Add(payout);
        return payout;
    }

    public void GiveHarvest(uint player, uint node, HarvestPayout payout)
    {
        Order.Add("harvest");
        OnHarvest?.Invoke();
        Reserved.Remove(payout);
        Harvests.Add(payout.Delivery);
    }

    public int GetGatheringRank(uint player) => GatheringRank;

    public void GiveGatheringXp(uint player, int xp)
    {
        Order.Add("xp");
        Xp.Add(xp);
    }

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

    public void PlayWork(uint player, string actionId, float seconds)
    {
        Work.Add(seconds);
        WorkActions.Add(actionId);
    }

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
