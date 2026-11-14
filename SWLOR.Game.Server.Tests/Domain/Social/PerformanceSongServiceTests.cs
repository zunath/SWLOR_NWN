using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>Which Cantina song a musician plays, over a scripted music catalog.</summary>
[TestFixture]
public class PerformanceSongServiceTests
{
    private ScriptedProxy _musicScript = null!;
    private PerformanceSongService _songs = null!;
    private HashSet<int> _collected = null!;

    [SetUp]
    public void SetUp()
    {
        _collected = new HashSet<int>();
        // Song 10 is the starter; 11 and 20 are collectible only; 30 is not a Cantina song.
        var catalog = new List<MusicSong>
        {
            new(default, 10, MusicCategoryType.Cantina, MusicAcquisitionType.Starter),
            new(default, 11, MusicCategoryType.Cantina, MusicAcquisitionType.Store),
            new(default, 20, MusicCategoryType.Cantina, MusicAcquisitionType.Store),
            new(default, 30, MusicCategoryType.Battle, MusicAcquisitionType.Starter)
        };
        var music = ScriptedProxy.Create<IMusicService>(out _musicScript);
        _musicScript.On("GetCollectedTrackIds", _ => _collected);
        _musicScript.On("GetCollectibleSongs", _ => catalog);
        _musicScript.On("GetTrackName", arguments => $"Track {(int)arguments[0]!}");
        _songs = new PerformanceSongService(music);
    }

    [Test]
    public void TheAvailableSongsAreTheStarterAndTheCollectedCantinaSongsInCatalogOrder()
    {
        _collected.Add(20);
        _collected.Add(30);

        var songs = _songs.GetAvailableSongs("uuid");

        songs.Select(song => song.TrackId).Should().Equal(10, 20);
        songs.Select(song => song.Name).Should().Equal("Track 10", "Track 20");
    }

    [Test]
    public void ThePickedSongIsPlayedWhenTheMusicianOwnsIt()
    {
        _collected.Add(11);
        _collected.Add(20);

        _songs.ChooseSong("uuid", 20).Should().Be(20);
        _songs.ChooseSong("uuid", 10).Should().Be(10, "the starter song belongs to everyone");
    }

    [Test]
    public void AnAutomaticPickPlaysTheFirstCollectedSongThenTheStarter()
    {
        _songs.ChooseSong("uuid").Should().Be(10);

        _collected.Add(20);
        _collected.Add(11);

        _songs.ChooseSong("uuid", PerformanceSongs.AutomaticSong).Should().Be(11);
    }

    [Test]
    public void APickTheMusicianDoesNotOwnFallsBackToTheAutomaticChoice()
    {
        _collected.Add(20);

        _songs.ChooseSong("uuid", 11).Should().Be(20);
        _songs.ChooseSong("uuid", 30).Should().Be(20, "only Cantina songs can be played");
    }
}
