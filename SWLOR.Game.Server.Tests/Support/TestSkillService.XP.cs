namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The seams the XP award code calls: the experience bonus read and the scheduled XP messages.</summary>
public sealed partial class TestSkillService
{
    public int StatAdjustment { get; set; }
    public List<uint> StatAdjustmentRequests { get; } = new();
    public List<Action> Scheduled { get; } = new();

    protected override int GetExperiencePercentAdjustment(uint player)
    {
        StatAdjustmentRequests.Add(player);
        return StatAdjustment;
    }

    protected override void ScheduleXPMessages(Action callback) => Scheduled.Add(callback);
}
