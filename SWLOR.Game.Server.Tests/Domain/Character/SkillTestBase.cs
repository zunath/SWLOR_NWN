using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using Player = SWLOR.Game.Server.Entity.Player;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Domain.Companions;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The world the skill unit tests share: the player engine, the in-memory store with the player's record, the events plugin, the skill
/// instance (<see cref="TestSkillService"/> and its XP part) and the helpers that read the engine's call list as one ordered trace.
/// </summary>
public abstract class SkillTestBase
{
    protected const uint Creature = 1001;
    protected const uint Weapon = 2002;
    protected const uint MessageSource = 3003;

    protected static readonly SkillType[] Fillers =
    {
        SkillType.Piloting, SkillType.FirstAid, SkillType.Smithery, SkillType.Fabrication, SkillType.Gathering,
        SkillType.Leadership, SkillType.BeastMastery, SkillType.Force
    };

    protected PlayerEngine _player = null!;
    protected FakeNWScript _engine = null!;
    protected FakeEventsPlugin _events = null!;
    protected IDisposable _eventsScope = null!;
    protected IReadOnlyList<string> _log => Trace();
    protected InMemoryDBService _store = null!;
    protected ScriptedProxy _twinProxy = null!;
    protected bool _twinPair;
    protected TestSkillService _service = null!;
    protected Player _record = null!;

    protected TestSkillService NewService()
    {
        var gui = ScriptedProxy.Create<IGuiService>(out var guiProxy);
        guiProxy.On("PublishRefreshEvent", _ =>
        {
            Mark("refresh");
            return null;
        });
        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out var feedbackProxy);
        feedbackProxy.On("SendWarningToPlayer", args =>
        {
            Mark("warning:" + args[1]);
            return null;
        });
        var twin = ScriptedProxy.Create<ITwinPistolEquipmentService>(out _twinProxy);
        _twinProxy.On("IsActivePair", _ => _twinPair);
        return new TestSkillService(new LoggingDB(_store, Mark), gui, feedback, twin);
    }

    [SetUp]
    public void SetUp()
    {
        _player = new PlayerEngine();
        _engine = _player.Engine;
        _engine.SetReturn("GetCurrentHitPoints", 100);

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("SignalEvent", args =>
        {
            Mark("signal:" + args[0]);
            return true;
        });
        _events.SetReturnHandler("PushEventData", args =>
        {
            Mark("push:" + args[0] + "=" + args[1]);
            return null;
        });
        _eventsScope = EventsPluginRuntime.Override(_events);

        _store = new InMemoryDBService();
        _record = new Player { Id = PlayerEngine.PlayerId };
        _store.With(_record);
        _twinPair = false;

        _service = NewService();
        _service.Catalog.CacheData();
        _service.Catalog.CacheXPChartData();
        _service.Catalog.LoadMappings();
        _engine.ClearCalls();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        _eventsScope.Dispose();
        _player.Dispose();
    }

    protected PlayerSkill Give(SkillType skill, int rank, int xp = 0, bool locked = false, int? limit = null)
    {
        var state = new PlayerSkill { Rank = rank, XP = xp, IsLocked = locked, RankLimit = limit };
        _record.Skills[skill] = state;
        return state;
    }

    protected void FillOthers(int rank, bool locked = false, int? limit = null)
    {
        foreach (var filler in Fillers)
            Give(filler, rank, 0, locked, limit);
    }

    /// <summary>Records a step in the engine's call list, so reads, writes, messages and events share one order.</summary>
    protected static void Mark(string step) => WriteTimestampedLogEntry(step);

    protected List<string> Trace()
    {
        var steps = new List<string>();
        foreach (var call in _engine.Calls)
        {
            switch (call.Member)
            {
                case "SendMessageToPC":
                    steps.Add("message:" + call.Arguments[1]);
                    break;
                case "FloatingTextStringOnCreature":
                    steps.Add("floating:" + call.Arguments[0]);
                    break;
                case "WriteTimestampedLogEntry":
                    steps.Add((string)call.Arguments[0]!);
                    break;
            }
        }

        return steps;
    }

    protected IEnumerable<string> LogStartingWith(string prefix) => Trace().Where(x => x.StartsWith(prefix));
}
