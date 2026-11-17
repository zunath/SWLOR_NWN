using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Foundation;

/// <summary>
/// A sound heard by the players near a source: the source itself when it is a player, then every player within range,
/// nearest first, stopping at the first one out of range.
/// </summary>
[TestFixture]
public class NearbySoundTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Source = 100;
    private const uint Near = 201;
    private const uint Far = 202;
    private const uint Beyond = 203;

    private FakeNWScript _engine = null!;
    private FakePlayerPlugin _players = null!;
    private readonly List<IDisposable> _scopes = new();
    private readonly List<uint> _nearest = new();
    private readonly Dictionary<uint, float> _distance = new();
    private bool _sourceIsPlayer;
    private bool _areaValid;

    [SetUp]
    public void SetUp()
    {
        _nearest.Clear();
        _distance.Clear();
        _sourceIsPlayer = false;
        _areaValid = true;
        _engine = new FakeNWScript();
        _players = new FakePlayerPlugin();
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        _engine.SetReturnHandler("GetIsPC", args => _sourceIsPlayer && (uint)args[0]! == Source);
        _engine.SetReturnHandler("GetNearestCreature", args =>
        {
            var nth = (int)args[3]!;
            return nth <= _nearest.Count ? _nearest[nth - 1] : Invalid;
        });
        _engine.SetReturnHandler("GetNearestCreatureToLocation", args =>
        {
            var nth = (int)args[3]!;
            return nth <= _nearest.Count ? _nearest[nth - 1] : Invalid;
        });
        _engine.SetReturnHandler("GetDistanceBetween", args => _distance[(uint)args[1]!]);
        _engine.SetReturnHandler("GetLocation", args => EngineStructures.Create<Location>((nint)(uint)args[0]!));
        _engine.SetReturnHandler("GetDistanceBetweenLocations", args => _distance[(uint)((Location)args[0]!).Handle]);
        _engine.SetReturnHandler("GetAreaFromLocation", _ => _areaValid ? 1u : Invalid);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(PlayerPluginRuntime.Override(_players));
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private List<(uint Player, string Sound, uint Target)> Played() =>
        _players.CallsTo("PlaySound").Select(c => ((uint)c.Arguments[0]!, (string)c.Arguments[1]!, (uint)c.Arguments[2]!)).ToList();

    [Test]
    public void Play_ReachesEveryPlayerInRangeAndStopsAtTheFirstOneOutOfRange()
    {
        _nearest.AddRange(new[] { Near, Far, Beyond });
        _distance[Near] = 3f;
        _distance[Far] = 10f;
        _distance[Beyond] = 11f;

        NearbySound.Play(Source, "snd_boom", 10f);

        Played().Should().Equal((Near, "snd_boom", Source), (Far, "snd_boom", Source));
    }

    [Test]
    public void Play_ASourcePlayerHearsItsOwnSoundOnceEvenWhenListedAsNearest()
    {
        _sourceIsPlayer = true;
        _nearest.AddRange(new[] { Source, Near });
        _distance[Source] = 0f;
        _distance[Near] = 2f;

        NearbySound.Play(Source, "snd_boom", 10f);

        Played().Should().Equal((Source, "snd_boom", Source), (Near, "snd_boom", Source));
    }

    [Test]
    public void Play_DoesNothingForAnInvalidSourceOrAnEmptySound()
    {
        _nearest.Add(Near);
        _distance[Near] = 1f;

        NearbySound.Play(Invalid, "snd_boom", 10f);
        NearbySound.Play(Source, " ", 10f);
        NearbySound.Play(Source, null!, 10f);

        Played().Should().BeEmpty();
    }

    [Test]
    public void PlayAtLocation_ReachesEveryPlayerInRangeWithoutASourceObject()
    {
        _nearest.AddRange(new[] { Near, Far, Beyond });
        _distance[Near] = 3f;
        _distance[Far] = 10f;
        _distance[Beyond] = 12f;
        var location = EngineStructures.Create<Location>(0);
        _engine.SetReturnHandler("GetDistanceBetweenLocations", args => _distance[(uint)((Location)args[0]!).Handle]);

        NearbySound.PlayAtLocation(location, "snd_boom", 10f);

        Played().Select(p => p.Player).Should().Equal(Near, Far);
        Played().Should().OnlyContain(p => p.Target == Invalid && p.Sound == "snd_boom");
    }

    [Test]
    public void PlayAtLocation_DoesNothingOutsideAnAreaOrWithoutASound()
    {
        _nearest.Add(Near);
        _distance[Near] = 1f;
        var location = EngineStructures.Create<Location>(0);

        _areaValid = false;
        NearbySound.PlayAtLocation(location, "snd_boom", 10f);
        _areaValid = true;
        NearbySound.PlayAtLocation(location, "", 10f);

        Played().Should().BeEmpty();
    }
}
