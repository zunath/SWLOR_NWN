using FluentAssertions;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The world the Mimicry tests share: the engine and plugin fakes, the in-memory database, the scripted quick-cast, DM presence, feedback and
/// Gui stand-ins, the <see cref="TestMimicry"/> instance (with its loadout part), the trait stat service and the helpers that build a technique,
/// a trait and a player. The unconverted statics (Ability, Perk, Stat, Skill, StatusEffect, CombatPoint, Scheduler) are answered by the doubles.
/// </summary>
public abstract class MimicryTestBase
{
    protected const uint ObjectInvalid = 0x7F000000;
    protected const uint Player = 1001;
    protected const uint Bystander = 1002;
    protected const uint Npc = 2001;
    protected const uint Area = 500;
    protected const string PlayerId = "uuid-1001";

    protected static readonly FeatType Fire1 = (FeatType)3001;
    protected static readonly FeatType Fire2 = (FeatType)3002;
    protected static readonly FeatType Ice1 = (FeatType)3003;
    protected static readonly FeatType Trait1 = (FeatType)3004;
    protected static readonly FeatType Trait2 = (FeatType)3005;
    protected static readonly FeatType Plain = (FeatType)3099;
    protected static readonly FeatType NpcFire1 = (FeatType)3101;

    protected readonly List<IDisposable> _scopes = new();
    protected FakeNWScript _engine = null!;
    protected FakeCreaturePlugin _creature = null!;
    protected FakePlayerPlugin _playerPlugin = null!;
    protected FakeEventsPlugin _events = null!;
    protected InMemoryDBService _db = null!;
    protected ScriptedProxy _quickCastScript = null!;
    protected ScriptedProxy _presenceScript = null!;
    protected ScriptedProxy _feedbackScript = null!;
    protected TestMimicry _mimicry = null!;
    protected MimicryTraitStatsService _traitStats = null!;
    protected IStatService _statReader = null!;
    protected readonly List<uint> _nearby = new();
    protected readonly List<string> _quickCastCalls = new();
    protected readonly List<string> _warnings = new();
    protected readonly HashSet<uint> _controlledByDm = new();
    protected readonly HashSet<uint> _invalid = new();
    protected readonly HashSet<FeatType> _knownFeats = new();
    protected readonly Dictionary<int, QuickBarSlot> _bar = new();
    protected readonly Dictionary<(string, int), string> _twoDa = new();
    protected int _cursor;
    protected int _techniqueChanges;
    protected bool _inCombat;
    protected bool _playerDead;
    protected int _playerHitPoints = 100;
    protected uint _playerArea = Area;
    protected uint _self = Player;
    protected uint _entering = Player;
    protected uint _exiting = Player;
    protected bool _isDm;
    protected int _perception = 10;

    [SetUp]
    public void SetUp()
    {
        _nearby.Clear();
        _quickCastCalls.Clear();
        _warnings.Clear();
        _controlledByDm.Clear();
        _invalid.Clear();
        _knownFeats.Clear();
        _bar.Clear();
        _twoDa.Clear();
        _cursor = 0;
        _techniqueChanges = 0;
        _inCombat = false;
        _playerDead = false;
        _playerHitPoints = 100;
        _playerArea = Area;
        _self = Player;
        _entering = Player;
        _exiting = Player;
        _isDm = false;
        _perception = 10;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! != Npc);
        _engine.SetReturnHandler("GetIsDM", _ => _isDm);
        _engine.SetReturn("GetIsDMPossessed", false);
        _engine.SetReturnHandler("GetObjectUUID", args => $"uuid-{(uint)args[0]!}");
        _engine.SetReturnHandler("GetArea", args => (uint)args[0]! == Player ? _playerArea : Area);
        _engine.SetReturn("GetDistanceBetween", 5f);
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _cursor = 0;
            return _nearby.Count > 0 ? _nearby[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ =>
        {
            _cursor++;
            return _cursor < _nearby.Count ? _nearby[_cursor] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Player && _playerDead);
        _engine.SetReturnHandler("GetCurrentHitPoints", args => (uint)args[0]! == Player ? _playerHitPoints : 100);
        _engine.SetReturnHandler("GetAbilityScore", _ => _perception);
        _engine.SetReturnHandler("GetIsInCombat", _ => _inCombat);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetEnteringObject", _ => _entering);
        _engine.SetReturnHandler("GetExitingObject", _ => _exiting);
        _engine.SetReturnHandler("StringToObject", args => (string)args[0]! == "npc" ? Npc : ObjectInvalid);
        _engine.SetReturnHandler("Get2DAString", args => _twoDa.GetValueOrDefault(((string)args[1]!, (int)args[2]!), "****"));
        _engine.SetReturnHandler("GetStringByStrRef", args => $"text-{(int)args[0]!}");
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _creature = new FakeCreaturePlugin();
        _scopes.Add(CreaturePluginRuntime.Override(_creature));
        _playerPlugin = new FakePlayerPlugin();
        _playerPlugin.SetReturnHandler("GetQuickBarSlot", args =>
            _bar.TryGetValue((int)args[1]!, out var slot) ? slot : new QuickBarSlot { ObjectType = QuickBarSlotType.Empty });
        _scopes.Add(PlayerPluginRuntime.Override(_playerPlugin));
        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", args => (string)args[0]! switch
        {
            "NPC" => "npc",
            "SKILL_TYPE_ID" => ((int)SkillType.Mimicry).ToString(),
            _ => string.Empty
        });
        _scopes.Add(EventsPluginRuntime.Override(_events));

        _db = new InMemoryDBService().With(NewPlayer(rank: 20, analyzer: 1));

        var quickCast = ScriptedProxy.Create<IQuickCastService>(out _quickCastScript);
        _quickCastScript.On("GetOriginalFeat", args => args[0]);
        _quickCastScript.On("GetDisplayedFeat", args => args[1]);
        _quickCastScript.On("KnowsFeat", args => _knownFeats.Contains((FeatType)args[1]!));
        _quickCastScript.On("RemoveFeat", args =>
        {
            _quickCastCalls.Add($"RemoveFeat:{args[1]}");
            _knownFeats.Remove((FeatType)args[1]!);
            return null;
        });
        _quickCastScript.On("ApplyPreferences", _ =>
        {
            _quickCastCalls.Add("ApplyPreferences");
            return null;
        });

        var presence = ScriptedProxy.Create<IDMPresenceService>(out _presenceScript);
        _presenceScript.On("IsControlledByDM", args => _controlledByDm.Contains((uint)args[0]!));

        var feedback = ScriptedProxy.Create<IPlayerFeedbackService>(out _feedbackScript);
        _feedbackScript.On("SendWarningToPlayer", args =>
        {
            _warnings.Add((string)args[1]!);
            return null;
        });

        var gui = ScriptedProxy.Create<IGuiService>(out var guiScript);
        guiScript.On("PublishRefreshEvent", _ =>
        {
            _techniqueChanges++;
            return null;
        });
        _mimicry = new TestMimicry(_db, quickCast, presence, feedback, gui);

        var catalog = ScriptedProxy.Create<IAbilityCatalog>(out var catalogScript);
        catalogScript.On("GetAllAbilityDetails", _ => _mimicry.Abilities);
        _traitStats = new MimicryTraitStatsService(_db, catalog);
        _statReader = ScriptedProxy.Create<IStatService>(out var statScript);
        statScript.On("GetStatAdjustment", args => _mimicry.StatAdjustments.GetValueOrDefault((StatType)args[1]!));
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    protected static Player NewPlayer(int rank, int analyzer, int memory = 0, bool overclocked = false)
    {
        var player = new Player { Id = PlayerId };
        player.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = rank };
        if (analyzer > 0)
            player.Perks[PerkType.CombatAnalyzer] = analyzer;
        if (memory > 0)
            player.Perks[PerkType.AnalyzerMemory] = memory;
        if (overclocked)
            player.Perks[PerkType.OverclockedAnalyzer] = 1;
        return player;
    }

    protected Player Record => _db.Get<Player>(PlayerId);

    protected FeatType Technique(FeatType feat, int rank = 0, int slots = 1, CombatDamageType element = CombatDamageType.Invalid,
        FeatType source = FeatType.Invalid, bool registered = true)
    {
        _mimicry.Abilities[feat] = new AbilityDetail
        {
            Name = $"Technique {(int)feat}",
            IsMimicryTechnique = true,
            MimicrySkillRequirement = rank,
            MimicrySlotCost = slots,
            MimicryElement = element,
            MimicrySourceFeat = source,
            ImpactAction = (_, _, _, _) => { }
        };
        if (registered)
            _mimicry.Loadout.Registered.Add(feat);
        return feat;
    }

    protected FeatType Trait(FeatType feat, Dictionary<StatType, int>? stats = null, Dictionary<ResistanceType, int>? resistances = null)
    {
        var detail = new AbilityDetail
        {
            Name = $"Trait {(int)feat}",
            IsMimicryTechnique = true,
            IsMimicryTrait = true,
            MimicryTraitStats = stats ?? new Dictionary<StatType, int>(),
            MimicryTraitResistances = resistances ?? new Dictionary<ResistanceType, int>()
        };
        _mimicry.Abilities[feat] = detail;
        return feat;
    }

    protected List<string> Messages() => _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    protected static int SeedWhere(Func<System.Random, bool> rolls)
    {
        for (var seed = 1; seed < 20000; seed++)
        {
            if (rolls(new System.Random(seed)))
                return seed;
        }

        throw new InvalidOperationException("No seed gives the wanted rolls.");
    }

    protected void SeedLearnRoll(bool succeeds, int chance) =>
        Random.SetSeed(SeedWhere(random => (random.Next(1, 101) <= chance) == succeeds));
}
