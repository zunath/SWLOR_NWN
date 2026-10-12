using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests.Render;

[TestFixture]
public sealed class CloakTileCacheTests
{
    private static TextureImage Tile() =>
        new() { Width = 1, Height = 1, Pixels = new byte[4], SourceFormat = TextureSourceFormat.Dds };

    [Test]
    public void FullCacheEvictsOnlyTheLeastRecentlyUsedTile()
    {
        var cache = new CloakTileCache(2);
        var loads = new Dictionary<string, int>();
        TextureImage? Load(string key)
        {
            loads[key] = loads.GetValueOrDefault(key) + 1;
            return Tile();
        }

        cache.GetOrLoad("a", () => Load("a"));
        cache.GetOrLoad("b", () => Load("b"));
        cache.GetOrLoad("a", () => Load("a"));
        cache.GetOrLoad("c", () => Load("c"));
        cache.GetOrLoad("a", () => Load("a"));
        cache.GetOrLoad("b", () => Load("b"));

        cache.Count.Should().Be(2);
        loads.Should().Equal(new Dictionary<string, int> { ["a"] = 1, ["b"] = 2, ["c"] = 1 },
            "a stays cached while recently used and only the oldest tile is evicted");
    }

    [Test]
    public void ConcurrentRequestsForOneTileShareASingleDecode()
    {
        var cache = new CloakTileCache(4);
        var decodes = 0;
        using var release = new ManualResetEventSlim();
        var results = Enumerable.Range(0, 16).Select(_ => Task.Run(() => cache.GetOrLoad("tile", () =>
        {
            Interlocked.Increment(ref decodes);
            release.Wait(TimeSpan.FromSeconds(5));
            return Tile();
        }))).ToArray();

        release.Set();
        Task.WaitAll(results, TimeSpan.FromSeconds(10)).Should().BeTrue();
        decodes.Should().Be(1);
        results.Select(task => task.Result).Distinct().Should().ContainSingle();
    }

    [Test]
    public void ClearDropsEveryTile()
    {
        var cache = new CloakTileCache(4);
        var loads = 0;
        cache.GetOrLoad("tile", () => { loads++; return Tile(); });
        cache.Clear();
        cache.GetOrLoad("tile", () => { loads++; return Tile(); });
        loads.Should().Be(2);
    }
}
