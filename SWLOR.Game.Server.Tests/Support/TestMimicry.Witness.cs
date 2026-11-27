#nullable enable
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

// Part MI-2, MimicryWitnessService: the combat point and skill XP calls of the analysis and learn code, and the sweep schedule.
public sealed partial class TestMimicry
{
    public int NpcLevel { get; set; } = 30;
    public bool CombatPoints { get; set; } = true;
    public List<(uint Player, SkillType Skill, int Xp, bool IgnoreBonuses, uint Source)> XpGrants { get; } = new();
    public List<(Action Task, TimeSpan Interval)> Scheduled { get; } = new();

    protected override int GetNpcLevel(uint npc) => NpcLevel;

    protected override int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false,
        bool applyHenchmanPenalty = true, uint xpMessageSource = 0x7F000000)
    {
        XpGrants.Add((player, skill, xp, ignoreBonuses, xpMessageSource));
        return xp;
    }

    protected override int GetDeltaXP(int delta) => delta * 100;

    protected override bool HasCombatPoints(uint player, uint creature) => CombatPoints;

    protected override void ScheduleRepeating(Action task, TimeSpan interval) => Scheduled.Add((task, interval));
}
