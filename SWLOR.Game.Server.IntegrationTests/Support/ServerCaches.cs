using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The static caches the server fills at the module-cache boot step, built on first use (they are process-wide and
/// read-only afterwards). Call from a test whose game code reads them.
/// </summary>
public static class ServerCaches
{
    private static readonly object Gate = new();
    private static bool _skills;
    private static bool _perks;

    /// <summary>Registers the skill definitions, as <c>Skill.CacheData</c> does at boot.</summary>
    public static void EnsureSkills()
    {
        lock (Gate)
        {
            if (_skills)
                return;
            Skill.CacheData();
            _skills = true;
        }
    }

    /// <summary>Registers the perk definitions, as <c>Perk.CacheData</c> does at boot (it needs the skill definitions too).</summary>
    public static void EnsurePerks()
    {
        EnsureSkills();
        lock (Gate)
        {
            if (_perks)
                return;
            Perk.CacheData();
            _perks = true;
        }
    }
}
