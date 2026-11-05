using FluentAssertions;
using NUnit.Framework;
using Random = SWLOR.Game.Server.Service.Random;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>
/// The Mimicry instance: the technique cache, the trait bonuses, witnessing and learning, the slot budget, equipping, the
/// saved builds and the handlers. The engine and the NWNX plugins are recording fakes, the database an in-memory one,
/// the quick-cast, DM presence and feedback services scripted stand-ins, and the unconverted statics (Ability, Perk,
/// Stat, Skill, StatusEffect, CombatPoint, Gui, Scheduler) are answered by a subclass.
/// </summary>
[TestFixture]
public class MimicryServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Bystander = 1002;
    private const uint Npc = 2001;
    private const uint Area = 500;
    private const string PlayerId = "uuid-1001";

    private static readonly FeatType Fire1 = (FeatType)3001;
    private static readonly FeatType Fire2 = (FeatType)3002;
    private static readonly FeatType Ice1 = (FeatType)3003;
    private static readonly FeatType Trait1 = (FeatType)3004;
    private static readonly FeatType Trait2 = (FeatType)3005;
    private static readonly FeatType Plain = (FeatType)3099;
    private static readonly FeatType NpcFire1 = (FeatType)3101;

    private readonly List<IDisposable> _scopes = new();
    private FakeNWScript _engine = null!;
    private FakeCreaturePlugin _creature = null!;
    private FakePlayerPlugin _playerPlugin = null!;
    private FakeEventsPlugin _events = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _quickCastScript = null!;
    private ScriptedProxy _presenceScript = null!;
    private ScriptedProxy _feedbackScript = null!;
    private TestMimicry _mimicry = null!;
    private readonly List<uint> _nearby = new();
    private readonly List<string> _quickCastCalls = new();
    private readonly List<string> _warnings = new();
    private readonly HashSet<uint> _controlledByDm = new();
    private readonly HashSet<uint> _invalid = new();
    private readonly HashSet<FeatType> _knownFeats = new();
    private readonly Dictionary<int, QuickBarSlot> _bar = new();
    private readonly Dictionary<(string, int), string> _twoDa = new();
    private int _cursor;
    private int _techniqueChanges;
    private bool _inCombat;
    private bool _playerDead;
    private int _playerHitPoints = 100;
    private uint _playerArea = Area;
    private uint _self = Player;
    private uint _entering = Player;
    private uint _exiting = Player;
    private bool _isDm;
    private int _perception = 10;

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
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private static Player NewPlayer(int rank, int analyzer, int memory = 0, bool overclocked = false)
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

    private Player Record => _db.Get<Player>(PlayerId);

    private FeatType Technique(FeatType feat, int rank = 0, int slots = 1, CombatDamageType element = CombatDamageType.Invalid,
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
            _mimicry.Registered.Add(feat);
        return feat;
    }

    private FeatType Trait(FeatType feat, Dictionary<StatType, int>? stats = null, Dictionary<ResistanceType, int>? resistances = null)
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

    private List<string> Messages() => _engine.CallsTo("SendMessageToPC").Select(call => (string)call.Arguments[1]!).ToList();

    private static int SeedWhere(Func<System.Random, bool> rolls)
    {
        for (var seed = 1; seed < 20000; seed++)
        {
            if (rolls(new System.Random(seed)))
                return seed;
        }

        throw new InvalidOperationException("No seed gives the wanted rolls.");
    }

    private void SeedLearnRoll(bool succeeds, int chance) =>
        Random.SetSeed(SeedWhere(random => (random.Next(1, 101) <= chance) == succeeds));

    // ---- The technique cache --------------------------------------------------------------------------------------

    [Test]
    public void CacheData_IndexesTheTechniquesAndResolvesTheirPresentationData()
    {
        Technique(Fire1, source: NpcFire1);
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.MimicryPotencyPercent] = 3 });
        _mimicry.Abilities[Plain] = new AbilityDetail { Name = "Not a technique" };
        _twoDa[("ICON", (int)Fire1)] = "ife_fire";
        _twoDa[("DESCRIPTION", (int)Fire1)] = "7";
        _twoDa[("ICON", (int)Trait1)] = "ife_trait";

        _mimicry.CacheData();

        _mimicry.TechniqueCount.Should().Be(2);
        _mimicry.IsTechnique(Fire1).Should().BeTrue();
        _mimicry.IsTechnique(Plain).Should().BeFalse();
        _mimicry.GetTechniqueDetail(Fire1).Should().BeSameAs(_mimicry.Abilities[Fire1]);
        _mimicry.GetTechniqueDetail(Plain).Should().BeNull();
        _mimicry.GetTechniqueIcon(Fire1).Should().Be("ife_fire");
        _mimicry.GetTechniqueIcon(Plain).Should().BeEmpty();
        _mimicry.GetTechniqueDescription(Fire1).Should().Be("text-7");
        _mimicry.GetTechniqueDescription(Trait1).Should().BeEmpty("a technique without a description string reference has no text");
        _mimicry.GetTechniqueDescription(Plain).Should().BeEmpty();
    }

    [Test]
    public void CacheData_RebuildsTheCacheAndSchedulesTheWitnessSweepOnce()
    {
        Technique(Fire1);
        _mimicry.CacheData();
        _mimicry.Abilities.Clear();
        Technique(Ice1);

        _mimicry.CacheData();

        _mimicry.IsTechnique(Fire1).Should().BeFalse();
        _mimicry.IsTechnique(Ice1).Should().BeTrue();
        _mimicry.Scheduled.Should().ContainSingle().Which.Interval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Test]
    public void CacheData_KeepsTheTechniquesWhenTheEngineCannotReadThe2da()
    {
        Technique(Fire1);
        _twoDa[("ICON", (int)Fire1)] = "ife_fire";
        _engine.SetReturnHandler("Get2DAString", _ => throw new InvalidOperationException("no engine"));

        _mimicry.CacheData();

        _mimicry.IsTechnique(Fire1).Should().BeTrue();
        _mimicry.GetTechniqueIcon(Fire1).Should().BeEmpty();
        _mimicry.GetTechniqueDescription(Fire1).Should().BeEmpty();
    }

    // ---- Trait bonuses --------------------------------------------------------------------------------------------

    private void EquipForBonuses(params FeatType[] feats)
    {
        Record.EquippedTechniques = feats.ToList();
        _mimicry.CacheData();
    }

    [Test]
    public void GetStatSources_ListsEachEquippedTraitThatAdjustsTheStat()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        Trait(Trait2, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 0 });
        EquipForBonuses(Trait1, Trait2);

        var sources = _mimicry.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier);

        sources.Should().ContainSingle();
        sources[0].Key.Should().Be($"trait:{(int)Trait1}");
        sources[0][StatType.StaffMightModifierDamageMultiplier].Should().Be(10);
    }

    [Test]
    public void GetStatSources_ScalesTheTraitByThePotencyStat()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        EquipForBonuses(Trait1);
        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 50;

        var sources = _mimicry.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier);

        sources[0][StatType.StaffMightModifierDamageMultiplier].Should().Be(15);
    }

    [Test]
    public void GetStatSources_IsEmptyForUnknownStatsNonPlayersAndMissingRecords()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        EquipForBonuses(Trait1);

        _mimicry.GetStatSources(Player, StatType.MimicryPotencyPercent).Should().BeEmpty("no trait adjusts that stat");
        _mimicry.GetStatSources(Npc, StatType.StaffMightModifierDamageMultiplier).Should().BeEmpty("an NPC has no loadout");
        _isDm = true;
        _mimicry.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier).Should().BeEmpty("a DM has no loadout");
        _isDm = false;
        _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");
        _mimicry.GetStatSources(Player, StatType.StaffMightModifierDamageMultiplier).Should().BeEmpty("there is no record");
    }

    [Test]
    public void GetStatBonus_SumsTheEquippedTraitsWithTheirPotency()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        Trait(Trait2, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 3 });
        EquipForBonuses(Trait1, Trait2);

        _mimicry.GetStatBonus(Player, StatType.StaffMightModifierDamageMultiplier).Should().Be(13);

        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 50;
        _mimicry.GetStatBonus(Player, StatType.StaffMightModifierDamageMultiplier).Should().Be(15 + 5, "each amount is raised and rounded up");
    }

    [Test]
    public void GetStatBonus_DoesNotApplyPotencyToThePotencyStatItself()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.MimicryTraitPotencyPercent] = 10 });
        EquipForBonuses(Trait1);
        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 100;

        _mimicry.GetStatBonus(Player, StatType.MimicryTraitPotencyPercent).Should().Be(10);
    }

    [Test]
    public void GetStatBonus_AddsTheElementalResonanceToTechniquePotency()
    {
        Technique(Fire1, element: CombatDamageType.Fire);
        Technique(Fire2, element: CombatDamageType.Fire);
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.MimicryPotencyPercent] = 2 });
        EquipForBonuses(Fire1, Fire2, Trait1);

        _mimicry.GetStatBonus(Player, StatType.MimicryPotencyPercent).Should().Be(5 + 2);
    }

    [Test]
    public void GetStatBonus_GivesOnlyTheResonanceWhenNoTraitAdjustsPotency()
    {
        Technique(Fire1, element: CombatDamageType.Fire);
        Technique(Fire2, element: CombatDamageType.Fire);
        EquipForBonuses(Fire1, Fire2);

        _mimicry.GetStatBonus(Player, StatType.MimicryPotencyPercent).Should().Be(5);
    }

    [Test]
    public void GetStatBonus_IsZeroWithoutContributorsOrALoadout()
    {
        Trait(Trait1, new Dictionary<StatType, int> { [StatType.StaffMightModifierDamageMultiplier] = 10 });
        EquipForBonuses(Trait1);

        _mimicry.GetStatBonus(Player, StatType.MimicryTraitPotencyPercent).Should().Be(0, "no trait adjusts that stat");
        _mimicry.GetStatBonus(Npc, StatType.StaffMightModifierDamageMultiplier).Should().Be(0);
        _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");
        _mimicry.GetStatBonus(Player, StatType.StaffMightModifierDamageMultiplier).Should().Be(0);
    }

    [Test]
    public void GetResistanceBonus_SumsTheEquippedTraitsWithTheirPotency()
    {
        Trait(Trait1, resistances: new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 10 });
        Trait(Trait2, resistances: new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 4, [ResistanceType.Ice] = 6 });
        EquipForBonuses(Trait1, Trait2);

        _mimicry.GetResistanceBonus(Player, ResistanceType.Fire).Should().Be(14);
        _mimicry.StatAdjustments[StatType.MimicryTraitPotencyPercent] = 50;
        _mimicry.GetResistanceBonus(Player, ResistanceType.Fire).Should().Be(15 + 6);
    }

    [Test]
    public void GetResistanceBonus_IsZeroWithoutContributorsOrALoadout()
    {
        Trait(Trait1, resistances: new Dictionary<ResistanceType, int> { [ResistanceType.Fire] = 10 });
        EquipForBonuses(Trait1);

        _mimicry.GetResistanceBonus(Player, ResistanceType.Ice).Should().Be(0);
        _mimicry.GetResistanceBonus(Npc, ResistanceType.Fire).Should().Be(0);
        _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");
        _mimicry.GetResistanceBonus(Player, ResistanceType.Fire).Should().Be(0);
    }

    // ---- Witnessing and learning ----------------------------------------------------------------------------------

    private void WitnessSetup(int rank = 20, int rankRequired = 0)
    {
        Technique(Fire1, rank: rankRequired, source: NpcFire1);
        _db.With(NewPlayer(rank, analyzer: 1));
        _mimicry.CacheData();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;
        _nearby.Add(Player);
    }

    [Test]
    public void OnCreatureAbilityUsed_RecordsTheTechniqueForAnAnalyzingPlayerOnce()
    {
        WitnessSetup();

        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().ContainSingle().Which.Should().Contain("records Technique 3001");
    }

    [Test]
    public void OnCreatureAbilityUsed_WarnsWhenTheTechniqueIsBeyondTheAnalysisLevel()
    {
        WitnessSetup(rank: 2, rankRequired: 5);

        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
        _warnings.Should().Equal($"MIMICRY_RANK_{(int)Fire1}");
    }

    [Test]
    public void OnCreatureAbilityUsed_IgnoresPlayersCreaturesAndFeatsThatTeachNothing()
    {
        WitnessSetup();

        _mimicry.OnCreatureAbilityUsed(ObjectInvalid, NpcFire1);
        _mimicry.OnCreatureAbilityUsed(Player, NpcFire1);
        _mimicry.OnCreatureAbilityUsed(Npc, Plain);
        _invalid.Add(Npc);
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
    }

    [Test]
    public void OnCreatureAbilityUsed_IgnoresATechniqueWhoseDetailIsGoneAndPlayersControlledByDms()
    {
        WitnessSetup();
        _controlledByDm.Add(Player);

        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
        _mimicry.Abilities.Remove(Fire1);
        _controlledByDm.Clear();
        _mimicry.CacheData();
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        Messages().Should().BeEmpty();
    }

    [Test]
    public void OnCreatureAbilityUsed_IgnoresPlayersOutOfRangeInAnotherAreaOrWithoutTheAnalyzerOrWhoKnowTheTechnique()
    {
        WitnessSetup();
        _engine.SetReturn("GetDistanceBetween", 20f);
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _engine.SetReturn("GetDistanceBetween", 5f);

        _playerArea = Area + 1;
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _playerArea = Area;

        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 0;
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;

        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);

        Messages().Should().BeEmpty();
    }

    [Test]
    public void OnCreatureDeath_LetsAWitnessLearnTheTechniqueAndPaysTheLearningXp()
    {
        WitnessSetup();
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _engine.ClearCalls();
        _self = Npc;
        SeedLearnRoll(succeeds: true, chance: 20 + 2 * 20);

        _mimicry.OnCreatureDeath();

        Record.LearnedTechniques.Should().ContainKey(Fire1);
        _db.Saved.Should().Contain(Record);
        Messages().Should().ContainSingle().Which.Should().Contain("You learned the technique: Technique 3001!");
        _mimicry.XpGrants.Should().ContainSingle().Which.Should().Be((Player, SkillType.Mimicry, 400, false, Npc));
        _playerPlugin.CallsTo("PlaySound").Should().ContainSingle().Which.Arguments[1].Should().Be("gui_prompt");
    }

    [Test]
    public void OnCreatureDeath_WarnsWhenTheDecodeRollFailsAndForgetsTheWitnesses()
    {
        WitnessSetup();
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Npc;
        SeedLearnRoll(succeeds: false, chance: 60);

        _mimicry.OnCreatureDeath();

        Record.LearnedTechniques.Should().BeEmpty();
        _warnings.Should().Contain($"MIMICRY_DECODE_{(int)Fire1}");
        _mimicry.XpGrants.Should().BeEmpty();

        _warnings.Clear();
        _mimicry.OnCreatureDeath();
        _warnings.Should().BeEmpty("the witness entry is cleared on death");
    }

    [Test]
    public void OnCreatureDeath_SkipsWitnessesWhoCannotLearnRightNow()
    {
        WitnessSetup();
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Npc;
        SeedLearnRoll(succeeds: true, chance: 60);

        _playerDead = true;
        _mimicry.OnCreatureDeath();
        Record.LearnedTechniques.Should().BeEmpty();
    }

    [TestCase("hp")]
    [TestCase("area")]
    [TestCase("dm")]
    [TestCase("learned")]
    [TestCase("rank")]
    [TestCase("unwitnessed")]
    public void TryLearn_IsSkippedWhen(string reason)
    {
        WitnessSetup();
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Npc;
        SeedLearnRoll(succeeds: true, chance: 60);
        switch (reason)
        {
            case "hp": _playerHitPoints = 0; break;
            case "area": _playerArea = Area + 1; break;
            case "dm": _isDm = true; break;
            case "learned": Record.LearnedTechniques[Fire1] = DateTime.UtcNow; break;
            case "rank": Record.Skills[SkillType.Mimicry].Rank = 0; _mimicry.Abilities[Fire1].MimicrySkillRequirement = 9; break;
            case "unwitnessed": _nearby.Clear(); _nearby.Add(Bystander); break;
        }
        var before = Record.LearnedTechniques.Count;

        _mimicry.OnCreatureDeath();

        Record.LearnedTechniques.Count.Should().Be(before);
        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void OnCreatureDeath_DoesNothingForACreatureNobodyWitnessed()
    {
        WitnessSetup();
        _self = Npc;

        _mimicry.OnCreatureDeath();

        _engine.CountOf("GetFirstPC").Should().Be(0);
    }

    [TestCase(0, 10, 0, 10, 20 + 2 * -10 + 0 + 0)]
    [TestCase(20, 10, 2, 10, 20 + 2 * 10 + 20 + 0)]
    [TestCase(10, 0, 1, 15, 20 + 20 + 10 + 5)]
    [TestCase(50, 0, 3, 30, 75)]
    public void CalculateLearnChance_ScalesWithRankPerkAndPerceptionAndIsCapped(
        int rank, int required, int patternRecognition, int perception, int expected)
    {
        _mimicry.CalculateLearnChance(rank, required, patternRecognition, perception).Should().Be(expected);
    }

    [Test]
    public void CalculateLearnChance_IgnoresPerceptionBelowTheBaseline()
    {
        _mimicry.CalculateLearnChance(5, 5, 0, 3).Should().Be(20);
    }

    // ---- Observation XP -------------------------------------------------------------------------------------------

    [Test]
    public void CalculateAnalysisXP_IsFivePercentOfTheRankRelativeXpCappedAtThirty()
    {
        _mimicry.CalculateAnalysisXP(20, 10).Should().Be(Math.Min(30, 1000 * 5 / 100));
        _mimicry.CalculateAnalysisXP(15, 10).Should().Be(500 * 5 / 100);
        _mimicry.CalculateAnalysisXP(20, 10).Should().BeLessThanOrEqualTo(30);
        var maxRank = SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>().MaxRank;
        _mimicry.CalculateAnalysisXP(60, maxRank).Should().Be(0, "mastered Mimicry earns no observation XP");
    }

    private void Observe()
    {
        WitnessSetup();
        _mimicry.CombatPoints = true;
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
    }

    [Test]
    public void CombatPointXPDistributed_PaysTheObservationXpOnceWithoutBonuses()
    {
        Observe();
        _self = Player;
        _mimicry.NpcLevel = 30;

        _mimicry.CombatPointXPDistributed();
        _mimicry.CombatPointXPDistributed();

        var expected = _mimicry.CalculateAnalysisXP(30, 20);
        expected.Should().BeGreaterThan(0);
        _mimicry.XpGrants.Should().ContainSingle().Which.Should().Be((Player, SkillType.Mimicry, expected, true, Npc));
    }

    [Test]
    public void CombatPointXPDistributed_PaysNothingToAPlayerWhoWasNotObserving()
    {
        Observe();
        _self = Bystander;
        _db.With(new Player { Id = "uuid-1002" });

        _mimicry.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void CombatPointXPDistributed_PaysNothingWhenTheAnalyzerWasLostOrXpIsZero()
    {
        Observe();
        _self = Player;
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 0;
        _mimicry.CombatPointXPDistributed();
        _mimicry.XpGrants.Should().BeEmpty();

        Observe();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;
        Record.Skills[SkillType.Mimicry].Rank = SkillType.Mimicry.GetAttribute<SkillType, SkillAttribute>().MaxRank;
        _mimicry.CombatPointXPDistributed();
        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void OnCreatureAbilityUsed_RecordsNoObservationWithoutCombatPointsOrTheAnalyzer()
    {
        WitnessSetup();
        _mimicry.CombatPoints = false;
        _mimicry.OnCreatureAbilityUsed(Npc, NpcFire1);
        _self = Player;
        _mimicry.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void ClearAnalysisOnPlayerExit_DropsTheLeavingPlayersObservationCredit()
    {
        Observe();
        _exiting = Player;

        _mimicry.ClearAnalysisOnPlayerExit();
        _self = Player;
        _mimicry.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    [Test]
    public void ClearAnalysisOnPlayerExit_IgnoresCreaturesAndDms()
    {
        Observe();
        _exiting = Npc;
        _mimicry.ClearAnalysisOnPlayerExit();
        _exiting = Player;
        _isDm = true;
        _mimicry.ClearAnalysisOnPlayerExit();
        _isDm = false;
        _mimicry.NpcLevel = 30;

        _mimicry.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().ContainSingle("the credit was never dropped");
    }

    [Test]
    public void TheScheduledSweep_ForgetsWitnessesAndObserversOfCreaturesThatAreGone()
    {
        Observe();
        _mimicry.CacheData();
        _invalid.Add(Npc);

        _mimicry.Scheduled[0].Task();
        _invalid.Clear();
        _self = Npc;
        _mimicry.OnCreatureDeath();

        _engine.CountOf("GetFirstPC").Should().Be(1, "only the initial witness lookup ran; the death found no entry");
    }

    [Test]
    public void TheScheduledSweep_ForgetsObserversOfDeadCreaturesAndKeepsLivingOnes()
    {
        Observe();
        _mimicry.CacheData();
        _engine.SetReturnHandler("GetIsDead", args => (uint)args[0]! == Npc);
        _mimicry.Scheduled[0].Task();
        _self = Player;
        _mimicry.NpcLevel = 30;

        _mimicry.CombatPointXPDistributed();

        _mimicry.XpGrants.Should().BeEmpty();
    }

    // ---- Granting, slots and equipping ----------------------------------------------------------------------------

    [Test]
    public void GrantAllTechniques_MarksEveryUnlearnedTechniqueAsLearned()
    {
        Technique(Fire1);
        Technique(Ice1);
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        _mimicry.CacheData();

        _mimicry.GrantAllTechniques(Player).Should().Be(1);
        Record.LearnedTechniques.Keys.Should().BeEquivalentTo(new[] { Fire1, Ice1 });
        _db.Saved.Should().ContainSingle();

        _mimicry.GrantAllTechniques(Player).Should().Be(0);
        _db.Saved.Should().ContainSingle("nothing changed the second time");
    }

    [Test]
    public void GrantAllTechniques_RefusesCreaturesAndDms()
    {
        Technique(Fire1);
        _mimicry.CacheData();

        _mimicry.GrantAllTechniques(Npc).Should().Be(0);
        _isDm = true;
        _mimicry.GrantAllTechniques(Player).Should().Be(0);
    }

    [TestCase(0, 0, false, 0)]
    [TestCase(1, 0, false, 2)]
    [TestCase(1, 3, false, 8)]
    [TestCase(1, 3, true, 10)]
    public void GetMaxSlots_GrowsWithTheAnalyzerMemoryAndTheCapstone(int analyzer, int memory, bool overclocked, int expected)
    {
        var player = NewPlayer(rank: 10, analyzer, memory, overclocked);

        _mimicry.GetMaxSlots(player).Should().Be(expected);
    }

    [Test]
    public void GetMaxSlots_IsZeroWithoutARecordOrForAnObjectThatIsNotAPlayer()
    {
        _mimicry.GetMaxSlots((Player)null!).Should().Be(0);
        _mimicry.GetMaxSlots(Npc).Should().Be(0);
        _mimicry.GetMaxSlots(Player).Should().Be(2);
    }

    [Test]
    public void GetSetBonusPotency_CountsEachElementWithTwoOrMoreTechniquesUpToTheCap()
    {
        Technique(Fire1, element: CombatDamageType.Fire);
        Technique(Fire2, element: CombatDamageType.Fire);
        Technique(Ice1, element: CombatDamageType.Ice);
        Technique((FeatType)3010, element: CombatDamageType.Ice);
        Technique((FeatType)3011, element: CombatDamageType.Ice);
        Technique((FeatType)3012, element: CombatDamageType.Invalid);
        _mimicry.CacheData();
        var player = NewPlayer(10, 1);

        _mimicry.GetSetBonusPotency(null!).Should().Be(0);
        player.EquippedTechniques = new List<FeatType> { Fire1, Ice1, (FeatType)3012, Plain };
        _mimicry.GetSetBonusPotency(player).Should().Be(0);
        player.EquippedTechniques = new List<FeatType> { Fire1, Fire2 };
        _mimicry.GetSetBonusPotency(player).Should().Be(5);
        player.EquippedTechniques = new List<FeatType> { Fire1, Fire2, Ice1, (FeatType)3010, (FeatType)3011 };
        _mimicry.GetSetBonusPotency(player).Should().Be(5 + 10);

        for (var i = 0; i < 6; i++)
            Technique((FeatType)(3020 + i), element: CombatDamageType.Fire);
        _mimicry.CacheData();
        player.EquippedTechniques = Enumerable.Range(3020, 6).Select(i => (FeatType)i).Concat(new[] { Fire1, Fire2 }).ToList();
        _mimicry.GetSetBonusPotency(player).Should().Be(20, "the bonus is capped");
    }

    [Test]
    public void GetUsedSlots_AddsTheCostOfEachKnownEquippedTechnique()
    {
        Technique(Fire1, slots: 2);
        Technique(Ice1, slots: 3);
        _mimicry.CacheData();
        Record.EquippedTechniques = new List<FeatType> { Fire1, Ice1, Plain };

        _mimicry.GetUsedSlots(Player).Should().Be(5);
        _mimicry.GetUsedSlots(Record).Should().Be(5);
        _mimicry.GetUsedSlots((Player)null!).Should().Be(0);
    }

    private void EquipSetup()
    {
        Technique(Fire1, rank: 5, slots: 1);
        Technique(Ice1, rank: 5, slots: 2);
        Trait(Trait1);
        _mimicry.CacheData();
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        Record.LearnedTechniques[Ice1] = DateTime.UtcNow;
        Record.LearnedTechniques[Trait1] = DateTime.UtcNow;
    }

    [Test]
    public void CanEquip_AcceptsALearnedTechniqueThatFitsTheBudget()
    {
        EquipSetup();

        _mimicry.CanEquip(Player, Fire1, out var error).Should().BeTrue();
        error.Should().BeEmpty();
    }

    [Test]
    public void CanEquip_ExplainsEachReasonForRefusing()
    {
        EquipSetup();

        _mimicry.CanEquip(Npc, Record, Fire1, out var error).Should().BeFalse();
        error.Should().Be("Only players may equip techniques.");
        _mimicry.CanEquip(Player, null!, Fire1, out error).Should().BeFalse();
        error.Should().Be("Only players may equip techniques.");
        _isDm = true;
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("Only players may equip techniques.");
        _isDm = false;

        _mimicry.CanEquip(Player, Record, Plain, out error).Should().BeFalse();
        error.Should().Be("That is not a valid technique.");

        Record.LearnedTechniques.Remove(Fire1);
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You have not learned that technique.");
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;

        Record.Skills.Remove(SkillType.Mimicry);
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You need Mimicry skill rank 5 to equip that technique.");
        Record.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 20 };

        Record.EquippedTechniques.Add(Fire1);
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("That technique is already equipped.");
        Record.EquippedTechniques.Clear();

        _inCombat = true;
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You cannot manage techniques while in combat.");
        _inCombat = false;

        Record.EquippedTechniques.Add(Ice1);
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeFalse();
        error.Should().Be("You do not have enough technique slots available.");
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        _mimicry.CanEquip(Player, Record, Fire1, out error).Should().BeTrue("the analyzer memory perk adds slots");
    }

    [Test]
    public void EquipTechnique_SavesTheLoadoutGrantsTheFeatAndPlacesItOnTheFirstEmptyHotbarSlot()
    {
        EquipSetup();
        _bar[0] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };

        _mimicry.EquipTechnique(Player, Fire1).Should().BeTrue();

        Record.EquippedTechniques.Should().Equal(Fire1);
        _db.Saved.Should().Contain(Record);
        _creature.CallsTo("AddFeat").Should().ContainSingle().Which.Arguments[1].Should().Be(Fire1);
        var placed = _playerPlugin.CallsTo("SetQuickBarSlot");
        placed.Should().ContainSingle().Which.Arguments[1].Should().Be(1);
        _quickCastCalls.Should().Contain("ApplyPreferences");
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void EquipTechnique_DoesNotGrantAFeatThePlayerAlreadyKnowsOrPlaceOneAlreadyOnTheBar()
    {
        EquipSetup();
        _knownFeats.Add(Fire1);
        _bar[3] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.EquipTechnique(Player, Fire1).Should().BeTrue();

        _creature.CountOf("AddFeat").Should().Be(0);
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);
    }

    [Test]
    public void EquipTechnique_LeavesTheBarAloneWhenTheFirstRowIsFullOrTheAbilityHasNoAction()
    {
        EquipSetup();
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        for (var slot = 0; slot < 12; slot++)
            _bar[slot] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };
        _mimicry.EquipTechnique(Player, Fire1).Should().BeTrue();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);

        _bar.Clear();
        _mimicry.Abilities[Ice1].ImpactAction = null;
        _mimicry.EquipTechnique(Player, Ice1).Should().BeTrue();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);

        _mimicry.Registered.Remove(Ice1);
        Record.EquippedTechniques.Remove(Ice1);
        _mimicry.EquipTechnique(Player, Ice1).Should().BeTrue();
        _playerPlugin.CountOf("SetQuickBarSlot").Should().Be(0);
    }

    [Test]
    public void EquipTechnique_TellsThePlayerWhyItFailed()
    {
        EquipSetup();
        _inCombat = true;

        _mimicry.EquipTechnique(Player, Fire1).Should().BeFalse();

        Messages().Should().ContainSingle().Which.Should().Contain("You cannot manage techniques while in combat.");
        Record.EquippedTechniques.Should().BeEmpty();
    }

    [Test]
    public void EquipTechnique_NeverGrantsOrPlacesAPassiveTrait()
    {
        EquipSetup();
        _knownFeats.Add(Trait1);
        _bar[2] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Trait1 };

        _mimicry.EquipTechnique(Player, Trait1).Should().BeTrue();

        _creature.CountOf("AddFeat").Should().Be(0);
        _quickCastCalls.Should().Contain($"RemoveFeat:{Trait1}", "a stale feat is stripped");
        var cleared = _playerPlugin.CallsTo("SetQuickBarSlot").Should().ContainSingle().Subject;
        cleared.Arguments[1].Should().Be(2);
    }

    [Test]
    public void UnequipTechnique_RemovesTheFeatItsHotbarSlotAndItsStatusEffects()
    {
        EquipSetup();
        var detail = _mimicry.Abilities[Fire1];
        detail.StatusEffectTypesRemovedOnPerkRefund.Add(typeof(string));
        detail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund.Add(typeof(int));
        Record.EquippedTechniques.Add(Fire1);
        _bar[4] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.UnequipTechnique(Player, Fire1).Should().BeTrue();

        Record.EquippedTechniques.Should().BeEmpty();
        _mimicry.RemovedEffects.Should().Equal((Player, typeof(string), false));
        _mimicry.RemovedSourceEffects.Should().Equal((Player, typeof(int), false, detail));
        _quickCastCalls.Should().Contain($"RemoveFeat:{Fire1}").And.Contain("ApplyPreferences");
        _playerPlugin.CallsTo("SetQuickBarSlot").Should().ContainSingle().Which.Arguments[1].Should().Be(4);
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void UnequipTechnique_RefusesInCombatForCreaturesAndForTechniquesNotEquipped()
    {
        EquipSetup();

        _mimicry.UnequipTechnique(Npc, Fire1).Should().BeFalse();
        _mimicry.UnequipTechnique(Player, Fire1).Should().BeFalse("it is not equipped");
        Record.EquippedTechniques.Add(Fire1);
        _inCombat = true;
        _mimicry.UnequipTechnique(Player, Fire1).Should().BeFalse();

        Messages().Should().ContainSingle().Which.Should().Contain("cannot manage techniques while in combat");
        Record.EquippedTechniques.Should().Equal(Fire1);
    }

    [Test]
    public void UnequipAllTechniques_ClearsTheLoadoutAndLogs()
    {
        EquipSetup();
        Record.EquippedTechniques.AddRange(new[] { Fire1, Ice1 });

        _mimicry.UnequipAllTechniques(Player);

        Record.EquippedTechniques.Should().BeEmpty();
        _quickCastCalls.Where(c => c.StartsWith("RemoveFeat")).Should().HaveCount(2);
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void UnequipAllTechniques_DoesNothingForCreaturesOrAnEmptyLoadout()
    {
        EquipSetup();

        _mimicry.UnequipAllTechniques(Npc);
        _mimicry.UnequipAllTechniques(Player);

        _db.Saved.Should().BeEmpty();
        _techniqueChanges.Should().Be(0);
    }

    // ---- The slot budget and login --------------------------------------------------------------------------------

    [Test]
    public void EnforceSlotBudget_UnequipsTechniquesAboveTheRankThenTheNewestUntilTheBudgetFits()
    {
        Technique(Fire1, rank: 0, slots: 1);
        Technique(Fire2, rank: 30, slots: 1);
        Technique(Ice1, rank: 0, slots: 2);
        Technique((FeatType)3010, rank: 0, slots: 2);
        _mimicry.CacheData();
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        Record.EquippedTechniques = new List<FeatType> { Fire1, Ice1, Fire2, (FeatType)3010 };

        _mimicry.EnforceSlotBudget(Player);

        Record.EquippedTechniques.Should().Equal(Fire1, Ice1).And.HaveCount(2, "Fire2 is above the rank and the newest is over budget");
        _db.Saved.Should().ContainSingle();
        _quickCastCalls.Should().Contain("ApplyPreferences");
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void EnforceSlotBudget_ChangesNothingWhenTheLoadoutIsValid()
    {
        Technique(Fire1, slots: 1);
        _mimicry.CacheData();
        Record.EquippedTechniques = new List<FeatType> { Fire1 };

        _mimicry.EnforceSlotBudget(Player);

        _db.Saved.Should().BeEmpty();
        _techniqueChanges.Should().Be(0);
    }

    [Test]
    public void EnforceSlotBudget_IgnoresCreaturesAndMissingRecords()
    {
        _mimicry.EnforceSlotBudget(Npc);
        _mimicry.EnforceSlotBudget(Player, null!);
        _mimicry.EnforceSlotBudget(Npc, Record);

        _db.Saved.Should().BeEmpty();
    }

    [Test]
    public void EnforceSlotBudget_TreatsAMissingSkillAsRankZero()
    {
        Technique(Fire1, rank: 3);
        _mimicry.CacheData();
        Record.Skills.Remove(SkillType.Mimicry);
        Record.EquippedTechniques = new List<FeatType> { Fire1 };

        _mimicry.EnforceSlotBudget(Player, Record);

        Record.EquippedTechniques.Should().BeEmpty();
    }

    [Test]
    public void OnMimicrySkillDecay_EnforcesTheBudgetOnlyForTheMimicrySkill()
    {
        Technique(Fire1, rank: 30);
        _mimicry.CacheData();
        Record.EquippedTechniques = new List<FeatType> { Fire1 };
        _events.SetReturnHandler("GetEventData", _ => ((int)SkillType.Armor).ToString());
        _mimicry.OnMimicrySkillDecay();
        Record.EquippedTechniques.Should().Equal(Fire1);

        _events.SetReturnHandler("GetEventData", _ => ((int)SkillType.Mimicry).ToString());
        _mimicry.OnMimicrySkillDecay();
        Record.EquippedTechniques.Should().BeEmpty();
    }

    [Test]
    public void OnPlayerLogin_RestoresTheEquippedTechniquesAfterTheModuleEnterHandlers()
    {
        Technique(Fire1);
        Trait(Trait1);
        _mimicry.CacheData();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = 1;
        Record.EquippedTechniques = new List<FeatType> { Fire1, Trait1 };

        _mimicry.OnPlayerLogin();
        _creature.CountOf("AddFeat").Should().Be(0, "restoration waits for the delayed command");
        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();

        _creature.CallsTo("AddFeat").Should().ContainSingle().Which.Arguments[1].Should().Be(Fire1);
        _quickCastCalls.Should().Contain("ApplyPreferences");
    }

    [Test]
    public void OnPlayerLogin_IgnoresDmsAndCreatures()
    {
        _entering = Npc;
        _mimicry.OnPlayerLogin();
        _entering = Player;
        _isDm = true;
        _mimicry.OnPlayerLogin();

        _engine.CountOf("DelayCommand").Should().Be(0);
    }

    [TestCase("invalid")]
    [TestCase("norecord")]
    [TestCase("noanalyzer")]
    public void TheDelayedRestore_DoesNothingWhen(string reason)
    {
        Technique(Fire1);
        _mimicry.CacheData();
        _mimicry.PerkLevels[PerkType.CombatAnalyzer] = reason == "noanalyzer" ? 0 : 1;
        Record.EquippedTechniques = new List<FeatType> { Fire1 };
        _mimicry.OnPlayerLogin();
        if (reason == "invalid")
            _invalid.Add(Player);
        if (reason == "norecord")
            _engine.SetReturnHandler("GetObjectUUID", _ => "nobody");

        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!)();

        _creature.CountOf("AddFeat").Should().Be(0);
    }

    // ---- Saved builds ---------------------------------------------------------------------------------------------

    [Test]
    public void ValidateLoadout_ExplainsEachProblemWithADraft()
    {
        Technique(Fire1, rank: 5, slots: 1);
        Technique(Ice1, rank: 40, slots: 1);
        Technique(Fire2, rank: 0, slots: 3);
        _mimicry.CacheData();
        foreach (var feat in new[] { Fire1, Ice1, Fire2 })
            Record.LearnedTechniques[feat] = DateTime.UtcNow;

        _mimicry.ValidateLoadout(Record, new[] { Fire1 }, out var error).Should().BeTrue();
        error.Should().BeEmpty();

        _mimicry.ValidateLoadout(Record, new[] { Fire1, Fire1 }, out error).Should().BeFalse();
        error.Should().Contain("duplicate");
        _mimicry.ValidateLoadout(Record, new[] { Plain }, out error).Should().BeFalse();
        error.Should().Contain("unavailable technique");

        Record.LearnedTechniques.Remove(Fire1);
        _mimicry.ValidateLoadout(Record, new[] { Fire1 }, out error).Should().BeFalse();
        error.Should().Be("You have not learned Technique 3001.");
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;

        _mimicry.ValidateLoadout(Record, new[] { Ice1 }, out error).Should().BeFalse();
        error.Should().Be("Technique 3003 requires Mimicry rank 40.");

        _mimicry.ValidateLoadout(Record, new[] { Fire1, Fire2 }, out error).Should().BeFalse();
        error.Should().Be("This build needs 4 technique slots; you have 2.");

        Record.Skills.Remove(SkillType.Mimicry);
        _mimicry.ValidateLoadout(Record, new[] { Fire1 }, out error).Should().BeFalse("a missing skill is rank zero");
    }

    [Test]
    public void CaptureLoadoutHotbar_RecordsTheSlotsHoldingTheBuildsTechniques()
    {
        var loadout = new MimicryLoadout { Techniques = new List<FeatType> { Fire1, Ice1 } };
        loadout.HotbarSlots[30] = Fire1;
        _bar[2] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };
        _bar[7] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Ice1 };
        _bar[8] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Plain };
        _bar[9] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item, INTParam1 = (int)Fire1 };

        _mimicry.CaptureLoadoutHotbar(Player, loadout);

        loadout.HotbarSlots.Should().BeEquivalentTo(new Dictionary<int, FeatType> { [2] = Fire1, [7] = Ice1 });
    }

    private MimicryLoadout SetUpBuilds(out MimicryLoadout incoming)
    {
        Technique(Fire1, slots: 1);
        Technique(Ice1, slots: 1);
        Trait(Trait1);
        _mimicry.CacheData();
        foreach (var feat in new[] { Fire1, Ice1, Trait1 })
            Record.LearnedTechniques[feat] = DateTime.UtcNow;
        var outgoing = new MimicryLoadout { Name = "Old", Techniques = new List<FeatType> { Fire1 } };
        incoming = new MimicryLoadout { Name = "New", Techniques = new List<FeatType> { Ice1, Trait1 } };
        Record.MimicryLoadouts = new List<MimicryLoadout> { outgoing, incoming };
        Record.ActiveMimicryLoadoutId = outgoing.Id;
        Record.EquippedTechniques = new List<FeatType> { Fire1 };
        return outgoing;
    }

    [Test]
    public void ApplyLoadout_CommitsTheBuildRevokesTheOutgoingFeatsAndGrantsTheIncomingOnes()
    {
        var outgoing = SetUpBuilds(out var incoming);
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        _bar[0] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out var error).Should().BeTrue();

        error.Should().BeEmpty();
        Record.EquippedTechniques.Should().Equal(Ice1, Trait1);
        Record.ActiveMimicryLoadoutId.Should().Be(incoming.Id);
        outgoing.HotbarSlots.Should().ContainKey(0, "the outgoing build's hotbar layout was saved");
        _quickCastCalls.Should().Contain($"RemoveFeat:{Fire1}");
        _creature.CallsTo("AddFeat").Select(c => c.Arguments[1]).Should().Equal(Ice1);
        _db.Saved.Should().Contain(Record);
        _techniqueChanges.Should().Be(1);
    }

    [Test]
    public void ApplyLoadout_RestoresSavedHotbarSlotsOnlyOverEmptyOrMimicryOwnedCells()
    {
        SetUpBuilds(out var incoming);
        Record.Perks[PerkType.AnalyzerMemory] = 1;
        incoming.HotbarSlots[5] = Ice1;
        incoming.HotbarSlots[6] = Ice1;
        incoming.HotbarSlots[7] = Trait1;
        incoming.HotbarSlots[99] = Ice1;
        _bar[6] = new QuickBarSlot { ObjectType = QuickBarSlotType.Item };
        _bar[7] = new QuickBarSlot { ObjectType = QuickBarSlotType.Feat, INTParam1 = (int)Fire1 };

        _mimicry.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out _).Should().BeTrue();

        var written = _playerPlugin.CallsTo("SetQuickBarSlot").Select(c => (int)c.Arguments[1]!).ToList();
        written.Should().Contain(5, "an empty cell takes the saved technique");
        written.Should().NotContain(6, "another shortcut takes precedence");
        written.Should().NotContain(99, "a slot beyond the bar is ignored");
        written.Count(slot => slot == 7).Should().Be(1, "the old technique's cell is cleared, and a trait is never placed");
    }

    [Test]
    public void ApplyLoadout_RefusesInvalidRequestsWithoutChangingAnything()
    {
        SetUpBuilds(out var incoming);

        _mimicry.ApplyLoadout(Npc, incoming.Id, incoming.Techniques, out var error).Should().BeFalse();
        error.Should().Be("Only players may apply technique builds.");
        _isDm = true;
        _mimicry.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out error).Should().BeFalse();
        _isDm = false;
        _inCombat = true;
        _mimicry.ApplyLoadout(Player, incoming.Id, incoming.Techniques, out error).Should().BeFalse();
        error.Should().Be("You cannot apply a technique build while in combat.");
        _inCombat = false;
        _mimicry.ApplyLoadout(Player, "missing", incoming.Techniques, out error).Should().BeFalse();
        error.Should().Be("That loadout no longer exists.");
        _mimicry.ApplyLoadout(Player, incoming.Id, new[] { Plain }, out error).Should().BeFalse();
        error.Should().Contain("unavailable technique");

        Record.EquippedTechniques.Should().Equal(Fire1);
        _db.Saved.Should().BeEmpty();
    }

    // ---- Lists and checks -----------------------------------------------------------------------------------------

    [Test]
    public void TheTechniqueLists_PairEachFeatWithItsDetailAndSkipUnknownOnes()
    {
        Technique(Fire1);
        Technique(Ice1);
        _mimicry.CacheData();
        Record.LearnedTechniques[Fire1] = DateTime.UtcNow;
        Record.LearnedTechniques[Plain] = DateTime.UtcNow;
        Record.EquippedTechniques = new List<FeatType> { Ice1, Plain };

        _mimicry.GetLearnedTechniques(Player).Should().ContainSingle().Which.Feat.Should().Be(Fire1);
        _mimicry.GetLearnedTechniques(Record).Should().ContainSingle().Which.Detail.Should().BeSameAs(_mimicry.Abilities[Fire1]);
        _mimicry.GetLearnedTechniques((Player)null!).Should().BeEmpty();
        _mimicry.GetEquippedTechniques(Player).Should().ContainSingle().Which.Feat.Should().Be(Ice1);
        _mimicry.GetEquippedTechniques(Record).Should().ContainSingle();
        _mimicry.GetEquippedTechniques((Player)null!).Should().BeEmpty();
    }

    [Test]
    public void IsTechniqueEquipped_ReadsTheLoadoutOfAPlayer()
    {
        Record.EquippedTechniques = new List<FeatType> { Fire1 };

        _mimicry.IsTechniqueEquipped(Player, Fire1).Should().BeTrue();
        _mimicry.IsTechniqueEquipped(Player, Ice1).Should().BeFalse();
        _mimicry.IsTechniqueEquipped(Record, Fire1).Should().BeTrue();
        _mimicry.IsTechniqueEquipped((Player)null!, Fire1).Should().BeFalse();
        _mimicry.IsTechniqueEquipped(Npc, Fire1).Should().BeFalse();
        _isDm = true;
        _mimicry.IsTechniqueEquipped(Player, Fire1).Should().BeFalse();
    }

    private sealed class TestMimicry : MimicryService
    {
        public TestMimicry(IDBService db, IQuickCastService quickCast, IDMPresenceService presence, IPlayerFeedbackService feedback, IGuiService gui)
            : base(db, quickCast, presence, feedback, gui)
        {
        }

        public Dictionary<FeatType, AbilityDetail> Abilities { get; } = new();
        public HashSet<FeatType> Registered { get; } = new();
        public Dictionary<PerkType, int> PerkLevels { get; } = new();
        public Dictionary<StatType, int> StatAdjustments { get; } = new();
        public int NpcLevel { get; set; } = 30;
        public bool CombatPoints { get; set; } = true;
        public List<(uint Player, SkillType Skill, int Xp, bool IgnoreBonuses, uint Source)> XpGrants { get; } = new();
        public List<(uint Creature, Type Type, bool Message)> RemovedEffects { get; } = new();
        public List<(uint Source, Type Type, bool Message, AbilityDetail Ability)> RemovedSourceEffects { get; } = new();
        public List<(Action Task, TimeSpan Interval)> Scheduled { get; } = new();

        protected override IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => Abilities;

        protected override bool IsFeatRegistered(FeatType feat) => Registered.Contains(feat);

        protected override AbilityDetail GetAbilityDetail(FeatType feat) => Abilities[feat];

        protected override int GetPerkLevel(uint creature, PerkType perkType) => PerkLevels.GetValueOrDefault(perkType);

        protected override int GetStatAdjustment(uint creature, StatType stat) => StatAdjustments.GetValueOrDefault(stat);

        protected override int GetNpcLevel(uint npc) => NpcLevel;

        protected override int GiveSkillXP(uint player, SkillType skill, int xp, bool ignoreBonuses = false,
            bool applyHenchmanPenalty = true, uint xpMessageSource = 0x7F000000)
        {
            XpGrants.Add((player, skill, xp, ignoreBonuses, xpMessageSource));
            return xp;
        }

        protected override int GetDeltaXP(int delta) => delta * 100;

        protected override void RemoveStatusEffect(uint creature, Type statusEffectType, bool sendsWornOffMessage) =>
            RemovedEffects.Add((creature, statusEffectType, sendsWornOffMessage));

        protected override void RemoveStatusEffectsFromAllTargetsBySource(uint source, Type statusEffectType,
            bool sendsWornOffMessage, AbilityDetail originatingAbility) =>
            RemovedSourceEffects.Add((source, statusEffectType, sendsWornOffMessage, originatingAbility));

        protected override bool HasCombatPoints(uint player, uint creature) => CombatPoints;


        protected override void ScheduleRepeating(Action task, TimeSpan interval) => Scheduled.Add((task, interval));
    }
}
