using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>A test double for the game engine: it records everything a visit to a scavenge site asks for and lets a test move the scavenger.</summary>
internal sealed class FakeScavengeWorld : IScavengeWorld
{
    private readonly List<(float Seconds, Action Action)> _scheduled = new();

    public List<string> Log { get; } = new();
    public List<string> Messages { get; } = new();
    public List<string> SkillChecks { get; } = new();
    public List<int> Xp { get; } = new();
    public List<(uint Site, string Resref, int Quantity)> Created { get; } = new();
    public List<IReadOnlyList<ScavengeFind>> PendingSaves { get; } = new();
    public List<(uint Site, uint Player)> BonusLoot { get; } = new();
    public List<uint> Searched { get; } = new();
    public List<(uint Site, double Seconds)> Opened { get; } = new();
    public List<(uint Site, double Seconds)> Held { get; } = new();
    public List<ScavengeFind> Returned { get; } = new();
    public List<(uint Site, double Seconds)> Destroyed { get; } = new();
    public List<ScavengeSession> Windows { get; } = new();

    public bool WindowOpen { get; set; }
    public bool CanOpenWindow { get; set; } = true;
    public bool OpeningTheWindowThrows { get; set; }
    public bool IsValid { get; set; } = true;
    public bool SiteValid { get; set; } = true;
    public bool IsDead { get; set; }
    public bool IsInCombat { get; set; }
    public float Distance { get; set; } = 1f;

    public int ScheduledCount => _scheduled.Count;

    public void Message(uint player, string text)
    {
        Log.Add("message");
        Messages.Add(text);
    }

    public void SkillCheckMessage(uint player, string text)
    {
        Log.Add("check");
        SkillChecks.Add(text);
    }

    public void GiveGatheringXp(uint player, int xp)
    {
        Log.Add("xp");
        Xp.Add(xp);
    }

    public string CreateItem(uint site, string resref, int quantity)
    {
        Log.Add("create");
        Created.Add((site, resref, quantity));
        return "Item " + resref;
    }

    public void SavePendingFinds(uint player, IReadOnlyList<ScavengeFind> finds)
    {
        Log.Add("pending");
        PendingSaves.Add(finds.ToList());
        Pending = finds.ToList();
    }

    /// <summary>What is on the scavenger's record now: the last list saved, until it is returned.</summary>
    public IReadOnlyList<ScavengeFind> Pending { get; private set; } = Array.Empty<ScavengeFind>();

    public void ReturnPendingFinds(uint player, bool atLogin = false)
    {
        Log.Add("return");
        Returned.AddRange(Pending);
        Pending = Array.Empty<ScavengeFind>();
    }

    public void SpawnBonusLoot(uint site, uint player)
    {
        Log.Add("bonus");
        BonusLoot.Add((site, player));
    }

    public void MarkSearched(uint site)
    {
        Log.Add("mark");
        Searched.Add(site);
    }

    public void OpenSite(uint player, uint site, double animationSeconds)
    {
        Log.Add("site");
        Opened.Add((site, animationSeconds));
    }

    public void HoldSite(uint player, uint site, double animationSeconds)
    {
        Log.Add("hold");
        Held.Add((site, animationSeconds));
    }

    public void DestroySite(uint player, uint site, double animationSeconds)
    {
        Log.Add("destroy");
        Destroyed.Add((site, animationSeconds));
    }

    public bool OpenWindow(uint player, ScavengeSession session)
    {
        Log.Add("open");
        if (OpeningTheWindowThrows)
            throw new InvalidOperationException("The window cannot be created.");

        if (!CanOpenWindow)
            return false;

        Windows.Add(session);
        WindowOpen = true;
        return true;
    }

    public void CloseWindow(uint player)
    {
        Log.Add("close");
        WindowOpen = false;
    }

    public ScavengePlayerState GetPlayerState(uint player, uint site) =>
        new(IsValid, IsDead, IsInCombat, WindowOpen, Distance, SiteValid);

    /// <summary>The clock the visit's pacing reads; a test moves it forward.</summary>
    public DateTime UtcNow { get; set; } = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

    public void Schedule(float seconds, Action action) => _scheduled.Add((seconds, action));

    /// <summary>Runs the next scheduled check, the way a second passing would.</summary>
    public void RunNextScheduled()
    {
        var next = _scheduled[0];
        _scheduled.RemoveAt(0);
        next.Action();
    }
}
