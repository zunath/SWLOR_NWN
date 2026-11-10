using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The telegraph instance: creating, cancelling and expiring markers, the creatures inside a shape when it ends, and
/// the shader uniforms sent to the players who see it. The engine is a recording fake with a few creatures placed in
/// two areas; the DM presence and party services are scripted.
/// </summary>
[TestFixture]
public class TelegraphServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint AreaOne = 100;
    private const uint AreaTwo = 200;
    private const uint Caster = 1;
    private const uint Viewer = 2;
    private const uint Ally = 3;
    private const uint Stranger = 4;
    private const uint Enemy = 10;
    private const uint FarEnemy = 11;
    private const uint DeadEnemy = 12;
    private const int MaxRenderCount = TelegraphService.MaxRenderCount;

    private readonly List<IDisposable> _scopes = new();
    private readonly Dictionary<uint, uint> _areaOf = new();
    private readonly Dictionary<uint, Vector3> _positionOf = new();
    private readonly Dictionary<uint, float> _facingOf = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _gone = new();
    private readonly HashSet<uint> _pcs = new();
    private readonly HashSet<uint> _dms = new();
    private readonly List<uint> _online = new();
    private readonly List<uint> _creaturesInArea = new();
    private readonly HashSet<(uint, uint)> _party = new();
    private readonly Dictionary<uint, uint> _controlled = new();
    private readonly Dictionary<uint, uint> _avatar = new();
    private readonly List<(uint Viewer, uint Target)> _partyChecks = new();
    private FakeNWScript _engine = null!;
    private FakeEventsPlugin _events = null!;
    private TelegraphService _service = null!;
    private int _effectCounter;
    private int _microseconds;
    private uint _entering;
    private uint _exiting;
    private uint _self;
    private int _scriptType;
    private Effect? _lastEffect;
    private uint _eventTarget;
    private uint _master;
    private int _onlineCursor;
    private int _areaCursor;

    [SetUp]
    public void SetUp()
    {
        _areaOf.Clear();
        _positionOf.Clear();
        _facingOf.Clear();
        _dead.Clear();
        _gone.Clear();
        _pcs.Clear();
        _dms.Clear();
        _online.Clear();
        _creaturesInArea.Clear();
        _party.Clear();
        _controlled.Clear();
        _avatar.Clear();
        _partyChecks.Clear();
        _effectCounter = 0;
        _microseconds = 1000;
        _entering = _exiting = _self = ObjectInvalid;
        _scriptType = 0;
        _lastEffect = null;
        _eventTarget = ObjectInvalid;
        _master = ObjectInvalid;

        _areaOf[Caster] = AreaOne;
        _areaOf[Viewer] = AreaOne;
        _areaOf[Ally] = AreaOne;
        _areaOf[Stranger] = AreaTwo;
        _areaOf[Enemy] = AreaOne;
        _areaOf[FarEnemy] = AreaOne;
        _areaOf[DeadEnemy] = AreaOne;
        _areaOf[AreaOne] = AreaOne;
        _areaOf[AreaTwo] = AreaTwo;
        _positionOf[Caster] = new Vector3(0f, 0f, 0f);
        _positionOf[Enemy] = new Vector3(3f, 0f, 0f);
        _positionOf[FarEnemy] = new Vector3(30f, 0f, 0f);
        _positionOf[DeadEnemy] = new Vector3(1f, 0f, 0f);
        _dead.Add(DeadEnemy);
        _pcs.Add(Viewer);
        _pcs.Add(Ally);
        _pcs.Add(Stranger);
        _online.AddRange(new[] { Viewer, Ally, Stranger });
        _creaturesInArea.AddRange(new[] { Enemy, FarEnemy, DeadEnemy });

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !_gone.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetArea", args => _areaOf.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        _engine.SetReturnHandler("GetPosition", args => _positionOf.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("GetFacing", args => _facingOf.GetValueOrDefault((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetCurrentHitPoints", args => _dead.Contains((uint)args[0]!) ? 0 : 50);
        _engine.SetReturnHandler("GetIsPC", args => _pcs.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDM", args => _dms.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetFirstPC", _ =>
        {
            _onlineCursor = 0;
            return _online.Count > 0 ? _online[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextPC", _ =>
        {
            _onlineCursor++;
            return _onlineCursor < _online.Count ? _online[_onlineCursor] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetFirstObjectInArea", _ =>
        {
            _areaCursor = 0;
            return _creaturesInArea.Count > 0 ? _creaturesInArea[0] : ObjectInvalid;
        });
        _engine.SetReturnHandler("GetNextObjectInArea", _ =>
        {
            _areaCursor++;
            return _areaCursor < _creaturesInArea.Count ? _creaturesInArea[_areaCursor] : ObjectInvalid;
        });
        _engine.SetReturnHandler("EffectRunScript", _ => EngineStructures.Create<Effect>(++_effectCounter));
        _engine.SetReturnHandler("GetEffectLinkId", args => $"link-{((Effect)args[0]!).Handle}");
        _engine.SetReturnHandler("GetMicrosecondCounter", _ => _microseconds);
        _engine.SetReturnHandler("GetEnteringObject", _ => _entering);
        _engine.SetReturnHandler("GetExitingObject", _ => _exiting);
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetLastRunScriptEffectScriptType", _ => _scriptType);
        _engine.SetReturnHandler("GetLastRunScriptEffect", _ => _lastEffect ?? EngineStructures.Create<Effect>(0));
        _engine.SetReturnHandler("StringToObject", args => uint.TryParse((string)args[0]!, out var id) ? id : ObjectInvalid);
        _engine.SetReturnHandler("GetMaster", _ => _master);
        _scopes.Add(NWScriptRuntime.Override(_engine));

        _events = new FakeEventsPlugin();
        _events.SetReturnHandler("GetEventData", _ => _eventTarget.ToString());
        _scopes.Add(EventsPluginRuntime.Override(_events));

        var presence = ScriptedProxy.Create<IDMPresenceService>(out var presenceScript);
        presenceScript.On("GetControlledCreature", args => _controlled.GetValueOrDefault((uint)args[0]!, (uint)args[0]!));
        presenceScript.On("GetDM", args => _avatar.GetValueOrDefault((uint)args[0]!, (uint)args[0]!));
        var party = ScriptedProxy.Create<IPartyService>(out var partyScript);
        partyScript.On("IsInParty", args =>
        {
            _partyChecks.Add(((uint)args[0]!, (uint)args[1]!));
            return _party.Contains(((uint)args[0]!, (uint)args[1]!));
        });

        _service = new TelegraphService(presence, party);
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private string Sphere(float radius = 5f, bool hostile = true, ApplyTelegraphEffect? action = null, bool persistent = false) =>
        _service.CreateSphereTelegraph(Caster, new Vector3(0f, 0f, 0f), radius, 4f, hostile, action!, persistent);

    private List<(ShaderUniformType Slot, int Value)> IntUniforms(uint player) =>
        _engine.CallsTo("SetShaderUniformInt")
            .Where(call => (uint)call.Arguments[0]! == player)
            .Select(call => ((ShaderUniformType)call.Arguments[1]!, (int)call.Arguments[2]!))
            .ToList();

    private static int DecodeShape(int packed) => packed & 0x7;
    private static int DecodeColor(int packed) => (packed >> 3) & 0x3;
    private static int DecodeSizeX(int packed) => (packed >> 5) & 0xFF;
    private static int DecodeSizeY(int packed) => (packed >> 13) & 0xFF;
    private static int DecodeRotation(int packed) => (int)(((uint)packed >> 21) & 0x3FF);

    [Test]
    public void CreateTelegraph_AppliesATimedEffectAndRecordsTheMarkerInTheCastersArea()
    {
        var id = _service.CreateTelegraph(
            Caster, new Vector3(1f, 2f, 3f), 0.5f, new Vector2(6f, 2f), 4f, true, TelegraphType.Cone, null!, true);

        id.Should().Be("link-1");
        var applied = _engine.CallsTo("ApplyEffectToObject").Single();
        applied.Arguments[0].Should().Be(DurationType.Temporary);
        applied.Arguments[2].Should().Be(Caster);
        applied.Arguments[3].Should().Be(4f);
        var markers = _service.GetTelegraphsInArea(AreaOne);
        markers.Keys.Should().Equal("link-1");
        var marker = markers["link-1"];
        marker.Area.Should().Be(AreaOne);
        marker.Start.Should().Be(1000);
        marker.End.Should().Be(1000 + 4 * 1000 * 1000);
        marker.Data.Creator.Should().Be(Caster);
        marker.Data.Shape.Should().Be(TelegraphType.Cone);
        marker.Data.Position.Should().Be(new Vector3(1f, 2f, 3f));
        marker.Data.Rotation.Should().Be(0.5f);
        marker.Data.IsHostile.Should().BeTrue();
        marker.Data.IsPersistentAreaIndicator.Should().BeTrue();
        _engine.CallsTo("EffectRunScript").Should().ContainSingle();
    }

    [Test]
    public void CreateTelegraph_SendsTheMarkerToPlayersInThatAreaOnly()
    {
        Sphere();

        IntUniforms(Viewer).Should().HaveCount(MaxRenderCount, "a viewer the server has not sent to yet gets every slot");
        IntUniforms(Ally).Should().HaveCount(MaxRenderCount);
        IntUniforms(Stranger).Should().BeEmpty("the other area's players cannot see the marker");
        var slot = IntUniforms(Viewer).First();
        slot.Slot.Should().Be(ShaderUniformType.Type1);
        DecodeShape(slot.Value).Should().Be((int)TelegraphType.Sphere);
        DecodeSizeX(slot.Value).Should().Be(50, "sizes are packed in tenths");
        _engine.CallsTo("SetShaderUniformVec").Where(call => (uint)call.Arguments[0]! == Viewer).Should().ContainSingle();
    }

    [Test]
    public void CancelTelegraph_ForgetsTheMarkerAndClearsItsSlotWithoutRunningTheAction()
    {
        var ran = false;
        var id = Sphere(action: (_, _) => ran = true);
        _engine.ClearCalls();

        _service.CancelTelegraph(id);

        ran.Should().BeFalse();
        _service.GetTelegraphsInArea(AreaOne).Should().BeEmpty();
        IntUniforms(Viewer).Should().Equal((ShaderUniformType.Type1, (int)TelegraphType.None));
        _service.IsCreatureInTelegraph(Enemy, id).Should().BeFalse();
    }

    [Test]
    public void CancelTelegraph_IgnoresAnUnknownId()
    {
        Sphere();
        _engine.ClearCalls();

        _service.CancelTelegraph("missing");

        _service.GetTelegraphsInArea(AreaOne).Should().ContainSingle();
        _engine.CallsTo("SetShaderUniformInt").Should().BeEmpty();
    }

    [Test]
    public void GetTelegraphsInArea_ReturnsACopy()
    {
        var id = Sphere();

        _service.GetTelegraphsInArea(AreaOne).Remove(id).Should().BeTrue();

        _service.GetTelegraphsInArea(AreaOne).Should().ContainKey(id);
        _service.GetTelegraphsInArea(AreaTwo).Should().BeEmpty();
    }

    [Test]
    public void CaptureGeometry_CopiesTheMarkersAndSkipsMissingIds()
    {
        var id = _service.CreateTelegraph(
            Caster, new Vector3(1f, 2f, 3f), 0.5f, new Vector2(6f, 2f), 4f, true, TelegraphType.Line, null!);

        var geometry = _service.CaptureGeometry(new[] { id, "missing" });

        geometry.Should().Equal(new TelegraphGeometry(AreaOne, TelegraphType.Line, new Vector3(1f, 2f, 3f), new Vector2(6f, 2f), 0.5f));
    }

    [Test]
    public void ShouldShowImpactFlash_SkipsAnImpactAnActivationMarkerAlreadyShowed()
    {
        var impact = new TelegraphGeometry(AreaOne, TelegraphType.Sphere, Vector3.Zero, new Vector2(5f, 5f), 0f);

        _service.ShouldShowImpactFlash(impact, null!).Should().BeTrue();
        _service.ShouldShowImpactFlash(impact, Array.Empty<TelegraphGeometry>()).Should().BeTrue();
        _service.ShouldShowImpactFlash(impact, new[] { impact }).Should().BeFalse();
        _service.ShouldShowImpactFlash(impact, new[] { impact with { Area = AreaTwo } }).Should().BeTrue();
    }

    [Test]
    public void ResolveImpactGeometry_ReturnsTheWarnedFootprintOrTheImpact()
    {
        var impact = new TelegraphGeometry(AreaOne, TelegraphType.Cone, new Vector3(4f, 0f, 0f), new Vector2(8f, 3f), 1f);
        var warning = impact with { Position = Vector3.Zero, Rotation = 0f };

        _service.ResolveImpactGeometry(impact, null!).Should().Be(impact);
        _service.ResolveImpactGeometry(impact, new[] { warning }).Should().Be(warning);
        _service.ResolveImpactGeometry(impact, new[] { warning with { Shape = TelegraphType.Line } }).Should().Be(impact);
        _service.ResolveImpactGeometry(impact, new[] { warning with { Area = AreaTwo } }).Should().Be(impact);
        _service.ResolveImpactGeometry(impact, new[] { warning with { Size = new Vector2(9f, 3f) } }).Should().Be(impact);
    }

    [TestCase(TelegraphType.Sphere, 3f, 0f, true)]
    [TestCase(TelegraphType.Sphere, 6f, 0f, false)]
    [TestCase(TelegraphType.Cone, 3f, 0f, true)]
    [TestCase(TelegraphType.Cone, -3f, 0f, false)]
    [TestCase(TelegraphType.Cone, 9f, 0f, false)]
    [TestCase(TelegraphType.Cone, 0.005f, 0f, true)]
    [TestCase(TelegraphType.Line, 3f, 0.5f, true)]
    [TestCase(TelegraphType.Line, 3f, 3f, false)]
    [TestCase(TelegraphType.Line, -1f, 0f, false)]
    [TestCase(TelegraphType.None, 0f, 0f, false)]
    public void IsCreatureInTelegraph_FollowsTheShape(TelegraphType shape, float x, float y, bool expected)
    {
        var id = _service.CreateTelegraph(Caster, Vector3.Zero, 0f, new Vector2(5f, 2f), 4f, true, shape, null!);
        _positionOf[Enemy] = new Vector3(x, y, 0f);

        _service.IsCreatureInTelegraph(Enemy, id).Should().Be(expected);
    }

    [Test]
    public void IsCreatureInTelegraph_RejectsAnotherAreaAndAnUnknownMarker()
    {
        var id = Sphere();
        _positionOf[Stranger] = Vector3.Zero;

        _service.IsCreatureInTelegraph(Stranger, id).Should().BeFalse("the creature is in another area");
        _service.IsCreatureInTelegraph(Enemy, "missing").Should().BeFalse();
    }

    [Test]
    public void IsCreatureInTelegraph_UsesTheRotatedCone()
    {
        var cone = _service.CreateTelegraph(Caster, Vector3.Zero, MathF.PI / 2f, new Vector2(5f, 2f), 4f, true, TelegraphType.Cone, null!);

        _positionOf[Enemy] = new Vector3(0f, 3f, 0f);
        _service.IsCreatureInTelegraph(Enemy, cone).Should().BeTrue();
        _positionOf[Enemy] = new Vector3(3f, 0f, 0f);
        _service.IsCreatureInTelegraph(Enemy, cone).Should().BeFalse();
    }

    [Test]
    public void OnRemoved_RunsTheActionOnLivingCreaturesInTheShapeNearestFirstThenForgetsTheMarker()
    {
        uint? creator = null;
        List<uint>? affected = null;
        var id = Sphere(radius: 5f, action: (who, list) => { creator = who; affected = list; });
        _positionOf[Ally] = new Vector3(1f, 1f, 0f);
        _creaturesInArea.Add(Ally);
        _engine.ClearCalls();

        _service.OnRemoved(Caster, id);

        creator.Should().Be(Caster);
        affected.Should().Equal(Ally, Enemy);
        _service.GetTelegraphsInArea(AreaOne).Should().BeEmpty();
        IntUniforms(Viewer).Should().Equal((ShaderUniformType.Type1, (int)TelegraphType.None));
    }

    [TestCase(TelegraphType.Cone)]
    [TestCase(TelegraphType.Line)]
    public void OnRemoved_ReachesAsFarAsTheShapeDoes(TelegraphType shape)
    {
        List<uint>? affected = null;
        var id = _service.CreateTelegraph(
            Caster, Vector3.Zero, 0f, new Vector2(5f, 2f), 4f, true, shape, (_, list) => affected = list);
        _positionOf[Enemy] = new Vector3(4f, 0.5f, 0f);

        _service.OnRemoved(Caster, id);

        affected.Should().Equal(Enemy);
    }

    [Test]
    public void OnRemoved_ReportsNoCreaturesForAShapeOfNone()
    {
        List<uint>? affected = null;
        var id = _service.CreateTelegraph(
            Caster, Vector3.Zero, 0f, new Vector2(5f, 2f), 4f, true, TelegraphType.None, (_, list) => affected = list);

        _service.OnRemoved(Caster, id);

        affected.Should().BeEmpty();
    }

    [Test]
    public void OnRemoved_ThrowsForAShapeThatCannotBeMeasured()
    {
        var id = _service.CreateTelegraph(
            Caster, Vector3.Zero, 0f, new Vector2(5f, 2f), 4f, true, (TelegraphType)99, (_, _) => { });

        var remove = () => _service.OnRemoved(Caster, id);

        remove.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void OnRemoved_UsesTheMarkersOwnAreaEvenWhenTheCreatorMoved()
    {
        List<uint>? affected = null;
        var id = Sphere(action: (_, list) => affected = list);
        _areaOf[Caster] = AreaTwo;

        _service.OnRemoved(Caster, id);

        affected.Should().Equal(Enemy);
        _service.GetTelegraphsInArea(AreaOne).Should().BeEmpty();
    }

    [Test]
    public void OnRemoved_WithoutAnActionStillClearsTheMarker()
    {
        var id = Sphere();

        _service.OnRemoved(Caster, id);

        _service.GetTelegraphsInArea(AreaOne).Should().BeEmpty();
    }

    [Test]
    public void OnRemoved_IgnoresAnUnknownMarker()
    {
        Sphere();
        _engine.ClearCalls();

        _service.OnRemoved(Caster, "missing");

        _service.GetTelegraphsInArea(AreaOne).Should().ContainSingle();
        _engine.CallsTo("SetShaderUniformInt").Should().BeEmpty();
    }

    [Test]
    public void OnRemoved_AnAreaThatNoLongerExistsFindsNoCreatures()
    {
        List<uint>? affected = null;
        var id = Sphere(action: (_, list) => affected = list);
        _gone.Add(AreaOne);

        _service.OnRemoved(Caster, id);

        affected.Should().BeEmpty();
    }

    [Test]
    public void OnTelegraphEffect_RunsTheActionOnlyWhenTheEffectIsRemoved()
    {
        var count = 0;
        var id = Sphere(action: (_, _) => count++);
        _lastEffect = EngineStructures.Create<Effect>(1);
        _self = Caster;

        _scriptType = (int)RunScriptEffectScriptType.OnApplied;
        _service.OnTelegraphEffect();
        count.Should().Be(0);

        _scriptType = (int)RunScriptEffectScriptType.OnRemoved;
        _service.OnTelegraphEffect();
        count.Should().Be(1);
        _service.GetTelegraphsInArea(AreaOne).Should().NotContainKey(id);
    }

    [Test]
    public void ClearAllTelegraphs_ForgetsEveryMarkerAndResendsToEveryone()
    {
        Sphere();
        _service.CreateSphereTelegraph(Caster, Vector3.Zero, 3f, 4f, true, null!);
        _engine.ClearCalls();

        _service.ClearAllTelegraphs();

        _service.GetTelegraphsInArea(AreaOne).Should().BeEmpty();
        IntUniforms(Viewer).Should().HaveCount(2, "both slots go back to none");
        IntUniforms(Stranger).Should().HaveCount(MaxRenderCount, "they had nothing sent before, so every slot is sent once");
    }

    [Test]
    public void UpdateShadersForAllPlayers_SendsToTheControlledCreatureOfEachPlayer()
    {
        Sphere();
        _controlled[Ally] = 77;
        _areaOf[77] = AreaOne;
        _engine.ClearCalls();
        _online.Clear();
        _online.Add(Ally);

        _service.UpdateShadersForAllPlayers();

        IntUniforms(77).Should().HaveCount(MaxRenderCount);
        IntUniforms(Ally).Should().BeEmpty();
    }

    [Test]
    public void UpdateShadersForPlayers_SkipsInvalidPlayersAndSendsNothingThatIsUnchanged()
    {
        Sphere();
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Viewer, Viewer, ObjectInvalid });

        _engine.CallsTo("SetShaderUniformInt").Should().BeEmpty();
    }

    [Test]
    public void UpdateShadersForPlayers_ResendsWhenAPartyChangeRecolorsAHostileMarker()
    {
        Sphere();
        var before = IntUniforms(Viewer).First().Value;
        _party.Add((Viewer, Caster));
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Viewer });

        var after = IntUniforms(Viewer).First().Value;
        DecodeColor(before).Should().Be((int)TelegraphColorType.Hostile);
        DecodeColor(after).Should().Be((int)TelegraphColorType.PartyMember);
    }

    [Test]
    public void UpdateShadersForPlayers_UsesTheControlledCreatureAsTheViewer()
    {
        Sphere();
        _controlled[Viewer] = 88;
        _areaOf[88] = AreaOne;
        _pcs.Add(88);
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Viewer });

        IntUniforms(88).Should().HaveCount(MaxRenderCount);
        IntUniforms(Viewer).Should().BeEmpty();
    }

    [Test]
    public void UpdateShadersForPlayers_AcceptsADMViewer()
    {
        Sphere();
        _dms.Add(Caster);
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Caster });

        IntUniforms(Caster).Should().HaveCount(MaxRenderCount);
    }

    [Test]
    public void UpdateShadersForPlayers_SkipsAViewerThatIsNeitherAPlayerNorADM()
    {
        Sphere();
        _controlled[Viewer] = 99;
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Viewer });

        _engine.CallsTo("SetShaderUniformInt").Should().BeEmpty();
    }

    [TestCase(false, false, true, TelegraphColorType.Hostile)]
    [TestCase(false, false, false, TelegraphColorType.Beneficial)]
    [TestCase(true, false, true, TelegraphColorType.PartyMember)]
    [TestCase(true, false, false, TelegraphColorType.Beneficial)]
    [TestCase(false, true, true, TelegraphColorType.Self)]
    public void TheColorDependsOnTheViewersRelationToTheCaster(bool inParty, bool ownMarker, bool hostile, TelegraphColorType expected)
    {
        if (inParty)
            _party.Add((Viewer, Caster));

        _service.CreateSphereTelegraph(ownMarker ? Viewer : Caster, Vector3.Zero, 5f, 4f, hostile, null!);

        DecodeColor(IntUniforms(Viewer).First().Value).Should().Be((int)expected);
        if (ownMarker)
            _partyChecks.Should().NotContain((Viewer, Viewer));
    }

    [Test]
    public void APossessingDMsAvatarKeepsTheDMsParty()
    {
        _avatar[Viewer] = 55;
        _party.Add((55, Caster));
        Sphere();

        DecodeColor(IntUniforms(Viewer).First().Value).Should().Be((int)TelegraphColorType.PartyMember);
    }

    [Test]
    public void PackedRotationWrapsNegativeAnglesAndSizesAreClamped()
    {
        _service.CreateTelegraph(Caster, Vector3.Zero, -MathF.PI / 2f, new Vector2(100f, -4f), 4f, true, TelegraphType.Line, null!);

        var packed = IntUniforms(Viewer).First().Value;
        DecodeSizeX(packed).Should().Be(255, "the packed size caps at 255");
        DecodeSizeY(packed).Should().Be(0, "a negative size packs as zero");
        DecodeRotation(packed).Should().Be((int)MathF.Round(0.75f * 1023f));
    }

    [Test]
    public void AnUnchangedSlotIsNotSentAgain()
    {
        Sphere();
        _engine.CountOf("SetShaderUniformInt").Should().BeGreaterThan(0);
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Viewer });

        _engine.CountOf("SetShaderUniformInt").Should().Be(0);
        _engine.CountOf("SetShaderUniformVec").Should().Be(0);
    }

    [Test]
    public void OnlyTheFirstSixteenMarkersAreRenderedAndPersistentIndicatorsComeLast()
    {
        Sphere(persistent: true);
        for (var index = 0; index < MaxRenderCount; index++)
            _service.CreateSphereTelegraph(Caster, new Vector3(index, 0f, 0f), 2f, 4f, true, null!);

        _engine.CallsTo("SetShaderUniformInt")
            .Where(call => (uint)call.Arguments[0]! == Viewer)
            .Select(call => (ShaderUniformType)call.Arguments[1]!)
            .Distinct().Should().HaveCount(MaxRenderCount);
        var lastPackedSizes = IntUniforms(Viewer).Where(entry => entry.Slot == ShaderUniformType.Type1 + 15).ToList();
        lastPackedSizes.Should().NotBeEmpty();
        lastPackedSizes.Last().Value.Should().NotBe((int)TelegraphType.None);
    }

    [Test]
    public void OnAreaEnter_ResendsEverySlotToAPlayerWhoZonesIn()
    {
        Sphere();
        _entering = Viewer;
        _engine.ClearCalls();

        _service.OnAreaEnter();

        IntUniforms(Viewer).Should().HaveCount(MaxRenderCount, "the area change forgot what was sent before");
        _engine.CountOf("SetShaderUniformVec").Should().Be(1);
    }

    [Test]
    public void OnAreaEnter_IgnoresCreaturesThatAreNeitherPlayersNorDMs()
    {
        Sphere();
        _entering = Enemy;
        _engine.ClearCalls();

        _service.OnAreaEnter();

        _engine.CallsTo("SetShaderUniformInt").Should().BeEmpty();
    }

    [Test]
    public void OnAreaEnter_AcceptsADM()
    {
        Sphere();
        _dms.Add(Caster);
        _entering = Caster;
        _engine.ClearCalls();

        _service.OnAreaEnter();

        IntUniforms(Caster).Should().HaveCount(MaxRenderCount);
    }

    [Test]
    public void ForgetShaderStateOnExit_MakesTheNextUpdateResendEverything()
    {
        Sphere();
        _exiting = Viewer;
        _service.ForgetShaderStateOnExit();
        _engine.ClearCalls();

        _service.UpdateShadersForPlayers(new[] { Viewer });

        IntUniforms(Viewer).Should().HaveCount(MaxRenderCount);
    }

    [Test]
    public void ResendShaderStateOnDMPossess_ResendsToThePossessedCreatureAfterADelay()
    {
        Sphere();
        _eventTarget = Viewer;
        _engine.ClearCalls();

        _service.ResendShaderStateOnDMPossess();

        _engine.CountOf("SetShaderUniformInt").Should().Be(0, "the resend waits for the possession to settle");
        var delay = _engine.CallsTo("DelayCommand").Single();
        delay.Arguments[0].Should().Be(0.1f);
        ((Action)delay.Arguments[1]!).Invoke();
        IntUniforms(Viewer).Should().HaveCount(MaxRenderCount);
    }

    [Test]
    public void ResendShaderStateOnDMPossess_FallsBackToTheMasterWhenThereIsNoTarget()
    {
        Sphere();
        _eventTarget = ObjectInvalid;
        _master = Ally;
        _self = Caster;
        _engine.ClearCalls();

        _service.ResendShaderStateOnDMPossess();
        ((Action)_engine.CallsTo("DelayCommand").Single().Arguments[1]!).Invoke();

        IntUniforms(Ally).Should().HaveCount(MaxRenderCount);
    }

    [Test]
    public void ResendShaderStateOnDMPossess_DoesNothingWhenNoViewerCanBeFound()
    {
        _eventTarget = ObjectInvalid;
        _master = ObjectInvalid;

        _service.ResendShaderStateOnDMPossess();

        _engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void CreateConeTelegraph_BuildsAConeFromLengthAndWidth()
    {
        var id = _service.CreateConeTelegraph(Caster, Vector3.UnitX, 1f, 6f, 2f, 4f, true, null!);

        var marker = _service.GetTelegraphsInArea(AreaOne)[id];
        marker.Data.Shape.Should().Be(TelegraphType.Cone);
        marker.Data.Size.Should().Be(new Vector2(6f, 2f));
        marker.Data.Rotation.Should().Be(1f);
        marker.Data.IsPersistentAreaIndicator.Should().BeFalse();
    }

    [Test]
    public void CreateLineTelegraph_BuildsALineFromLengthAndWidth()
    {
        var id = _service.CreateLineTelegraph(Caster, Vector3.UnitX, 1f, 6f, 2f, 4f, false, null!);

        var marker = _service.GetTelegraphsInArea(AreaOne)[id];
        marker.Data.Shape.Should().Be(TelegraphType.Line);
        marker.Data.Size.Should().Be(new Vector2(6f, 2f));
        marker.Data.IsHostile.Should().BeFalse();
    }

    [Test]
    public void CreateSphereTelegraph_UsesTheRadiusForBothSizes()
    {
        var id = Sphere(radius: 4f);

        var marker = _service.GetTelegraphsInArea(AreaOne)[id];
        marker.Data.Shape.Should().Be(TelegraphType.Sphere);
        marker.Data.Size.Should().Be(new Vector2(4f, 4f));
    }

    [Test]
    public void CreateTelegraphAtCreature_CentersOnTheTargetAndFacesItsFacing()
    {
        _positionOf[Enemy] = new Vector3(5f, 6f, 7f);
        _facingOf[Enemy] = 90f;

        var id = _service.CreateTelegraphAtCreature(Caster, Enemy, TelegraphType.Cone, new Vector2(3f, 1f), 4f, true, null!);

        var data = _service.GetTelegraphsInArea(AreaOne)[id].Data;
        data.Position.Should().Be(new Vector3(5f, 6f, 7f));
        data.Rotation.Should().BeApproximately(MathF.PI / 2f, 0.0001f);
    }

    [Test]
    public void CreateTelegraphInFrontOfCreature_OffsetsAlongTheFacing()
    {
        _positionOf[Enemy] = new Vector3(5f, 6f, 7f);
        _facingOf[Enemy] = 90f;

        var id = _service.CreateTelegraphInFrontOfCreature(Caster, Enemy, 2f, TelegraphType.Line, new Vector2(3f, 1f), 4f, true, null!);

        var data = _service.GetTelegraphsInArea(AreaOne)[id].Data;
        data.Position.X.Should().BeApproximately(5f, 0.001f);
        data.Position.Y.Should().BeApproximately(8f, 0.001f);
        data.Position.Z.Should().Be(7f);
        data.Rotation.Should().BeApproximately(MathF.PI / 2f, 0.0001f);
    }
}
