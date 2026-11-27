using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>The skill XP service with its calls into unconverted code answered by the test: the experience bonus read and the scheduled XP messages.</summary>
public sealed class TestSkillXPService : SkillXPService
{
    public TestSkillXPService(IDBService db, IGuiService gui, IPlayerFeedbackService feedback, ISkillService skill, ISkillRankService skillRank)
        : base(db, gui, feedback, FacadeForwarder.LazyFor<IStatService>(), skill, skillRank)
    {
    }

    /// <summary>The service with every seam at its default body (the original calls), for the test that proves each one runs without a server.</summary>
    public static SkillXPService CreateWithDefaultSeams(IDBService db, ISkillService skill, ISkillRankService skillRank) =>
        new(db, ScriptedProxy.Create<IGuiService>(out _), ScriptedProxy.Create<IPlayerFeedbackService>(out _),
            FacadeForwarder.LazyFor<IStatService>(), skill, skillRank);

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
