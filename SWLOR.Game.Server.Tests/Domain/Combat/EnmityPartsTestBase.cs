using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The enmity parts over one scripted engine: the table part, the change part and the attack part, built for real over the same
/// stand-ins. The engine is a recording fake, the converted neighbours (AI, activity, party, holo-com, beast mastery) are scripted
/// stand-ins, and the unconverted hot core is answered by the subclass of each part.
/// </summary>
public abstract class EnmityPartsTestBase
{
    protected const uint ObjectInvalid = 0x7F000000;
    protected const uint Player = 1;
    protected const uint Player2 = 2;
    protected const uint Enemy = 100;
    protected const uint Enemy2 = 101;
    protected const uint AreaA = 500;
    protected const uint AreaB = 501;

    protected static readonly DateTime Start = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    protected FakeNWScript _engine = null!;
    protected FakeEventsPlugin _events = null!;
    protected IDisposable _engineScope = null!;
    protected IDisposable _eventsScope = null!;
    protected IDisposable _clock = null!;
    protected TestEnmity _enmity = null!;
    protected TestEnmityChange _change = null!;
    protected TestEnmityAttack _attack = null!;
    protected DateTime _now;

    protected uint _self;
    protected uint _lastDamager;
    protected int _damage;
    protected uint _lastAttacker;
    protected uint _lastDied;
    protected uint _exiting;
    protected readonly Dictionary<string, string> _eventData = new();
    protected readonly Dictionary<uint, uint> _areas = new();
    protected readonly HashSet<uint> _players = new();
    protected readonly HashSet<uint> _dead = new();
    protected readonly Dictionary<uint, int> _hitPoints = new();
    protected readonly Dictionary<uint, uint> _masters = new();
    protected readonly Dictionary<uint, uint> _attackTargets = new();
    protected readonly Dictionary<uint, ActionType> _actions = new();
    protected readonly Dictionary<(uint, uint), float> _distances = new();
    protected readonly HashSet<uint> _unseen = new();
    protected readonly HashSet<uint> _evading = new();
    protected readonly HashSet<(uint, uint)> _evadeStarts = new();
    protected readonly HashSet<(uint, uint)> _aggroRange = new();
    protected readonly HashSet<(uint, uint)> _beyondAggroRange = new();
    protected readonly HashSet<uint> _busy = new();
    protected readonly HashSet<(uint, uint)> _partyPairs = new();
    protected readonly HashSet<uint> _holograms = new();
    protected readonly HashSet<uint> _playerBeasts = new();
    protected readonly List<uint> _combatStopped = new();

    [SetUp]
    public void SetUp()
    {
        _now = Start;
        _self = Enemy;
        _lastDamager = Player;
        _damage = 0;
        _lastAttacker = Player;
        _lastDied = Player;
        _exiting = Player;
        _eventData.Clear();
        _areas.Clear();
        _players.Clear();
        _players.Add(Player);
        _players.Add(Player2);
        _dead.Clear();
        _hitPoints.Clear();
        _masters.Clear();
        _attackTargets.Clear();
        _actions.Clear();
        _distances.Clear();
        _unseen.Clear();
        _evading.Clear();
        _evadeStarts.Clear();
        _aggroRange.Clear();
        _beyondAggroRange.Clear();
        _busy.Clear();
        _partyPairs.Clear();
        _holograms.Clear();
        _playerBeasts.Clear();
        _combatStopped.Clear();

        _clock = Clock.OverrideForTests(() => _now);
        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetLastDamager", _ => _lastDamager);
        _engine.SetReturnHandler("GetTotalDamageDealt", _ => _damage);
        _engine.SetReturnHandler("GetLastAttacker", _ => _lastAttacker);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => _lastDied);
        _engine.SetReturnHandler("GetExitingObject", _ => _exiting);
        _engine.SetReturnHandler("GetArea", args => _areas.GetValueOrDefault((uint)args[0]!, AreaA));
        _engine.SetReturnHandler("GetIsPC", args => _players.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _hitPoints.GetValueOrDefault((uint)args[0]!, 100));
        _engine.SetReturnHandler("GetMaster", args => _masters.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetAttackTarget", args => _attackTargets.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetCurrentAction", args => _actions.GetValueOrDefault((uint)args[0]!, ActionType.Invalid));
        _engine.SetReturnHandler("GetLocalBool", args => (string)args[1]! == CreatureAIRules.LeashEvadeActiveVariable && _evading.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetDistanceBetween", args =>
        {
            var pair = ((uint)args[0]!, (uint)args[1]!);
            if (_aggroRange.Contains(pair))
                return 1f;
            if (_distances.TryGetValue(pair, out var distance))
                return distance;
            return _beyondAggroRange.Contains(pair) ? CreatureAIRules.AggroRadius + 1f : 5f;
        });
        _engine.SetReturnHandler("GetName", args => $"name-{(uint)args[0]!}");
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => true);
        _engine.SetReturnHandler("LineOfSightObject", _ => true);
        _engine.SetReturnHandler("StringToObject", args => uint.TryParse((string)args[0]!, out var id) ? id : ObjectInvalid);
        _engineScope = NWScriptRuntime.Override(_engine);

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", args => _eventData.GetValueOrDefault((string)args[0]!, string.Empty));
        _eventsScope = EventsPluginRuntime.Override(_events);

        var ai = ScriptedProxy.Create<IAIService>(out var aiScript);
        aiScript.On("IsLeashEvading", args => _evading.Contains((uint)args[0]!));
        aiScript.On("TryStartCombatLeashEvade", args => _evadeStarts.Contains(((uint)args[0]!, (uint)args[1]!)));
        aiScript.On("IsInAggroRange", args => _aggroRange.Contains(((uint)args[0]!, (uint)args[1]!)));
        aiScript.On("StopCombatAfterProximityLoss", args =>
        {
            _combatStopped.Add((uint)args[0]!);
            return null;
        });
        var activity = ScriptedProxy.Create<IActivityService>(out var activityScript);
        activityScript.On("IsBusy", args => _busy.Contains((uint)args[0]!));
        var party = ScriptedProxy.Create<IPartyRoster>(out var partyScript);
        partyScript.On("IsInParty", args => _partyPairs.Contains(((uint)args[0]!, (uint)args[1]!)));
        var holoCom = ScriptedProxy.Create<IHologramQuery>(out var holoScript);
        holoScript.On("IsHologram", args => _holograms.Contains((uint)args[0]!));
        var beast = ScriptedProxy.Create<ICompanionLookup>(out var beastScript);
        beastScript.On("IsPlayerBeast", args => _playerBeasts.Contains((uint)args[0]!));

        _enmity = new TestEnmity();
        _attack = new TestEnmityAttack(_enmity, ai, activity);
        _change = new TestEnmityChange(_enmity, _attack, ai, holoCom, party, beast);
    }

    [TearDown]
    public void TearDown()
    {
        _eventsScope.Dispose();
        _engineScope.Dispose();
        _clock.Dispose();
    }

    protected List<string> ScriptsRun() =>
        _engine.CallsTo("ExecuteScript").Select(call => (string)call.Arguments[0]!).ToList();

    protected void RunAssigned()
    {
        // Runs the commands the service handed to the engine, including those they queue in turn.
        var run = 0;
        while (run < _engine.CallsTo("AssignCommand").Count + _engine.CallsTo("ActionDoCommand").Count)
        {
            var assigned = _engine.CallsTo("AssignCommand");
            var queued = _engine.CallsTo("ActionDoCommand");
            var actions = assigned.Concat(queued).Select(call => (Action)call.Arguments.Last()!).ToList();
            var next = actions[run++];
            next();
        }
    }

    protected void Heal(uint healer, uint target, int requested, int before, int after)
    {
        _eventData["TARGET_OBJECT_ID"] = target.ToString();
        _eventData["HEAL_AMOUNT"] = requested.ToString();
        _self = healer;
        _hitPoints[target] = before;
        _change.HealingStarted();
        _hitPoints[target] = after;
        _change.HealingCompleted();
    }
}
