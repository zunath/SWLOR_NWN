using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seams the catalog code calls: the droid test and the droid skill rank.</summary>
public sealed partial class TestSkillService
{
    public HashSet<uint> DroidCreatures { get; } = new();
    public int DroidRank { get; set; }

    protected override bool IsDroid(uint creature) => DroidCreatures.Contains(creature);

    protected override int GetDroidSkillRank(uint creature, SkillType skillType) => DroidRank;
}
