using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The bound on decoded previews.
    /// </summary>
    /// <remarks>
    /// It is the only thing standing between a builder who scrolls a gallery and a resident set the
    /// size of the artwork they scrolled past, so the eviction order and the capacity are worth
    /// asserting rather than assuming. A null entry means "this key has no artwork", which is why
    /// the hit/miss answer is the return value rather than a null check - and why this can be
    /// exercised without a graphics platform.
    /// </remarks>
    [TestFixture]
    public class BitmapMemoryCacheTests
    {
        [Test]
        public void AKnownKeyWithNoArtworkIsAHitRatherThanAMiss()
        {
            var cache = new BitmapMemoryCache(4);
            cache.Set("no-artwork", null);

            cache.TryGet("no-artwork", out var bitmap).Should().BeTrue();
            bitmap.Should().BeNull();

            cache.TryGet("never-seen", out _).Should().BeFalse();
        }

        [Test]
        public void TheOldestUnreadEntryIsTheOneEvicted()
        {
            var cache = new BitmapMemoryCache(3);
            cache.Set("a", null);
            cache.Set("b", null);
            cache.Set("c", null);

            // Reading "a" makes it the most recent, so "b" becomes the oldest.
            cache.TryGet("a", out _).Should().BeTrue();
            cache.Set("d", null);

            cache.Count.Should().Be(3);
            cache.TryGet("b", out _).Should().BeFalse();
            cache.TryGet("a", out _).Should().BeTrue();
            cache.TryGet("c", out _).Should().BeTrue();
            cache.TryGet("d", out _).Should().BeTrue();
        }

        [Test]
        public void TheCacheNeverGrowsPastItsCapacity()
        {
            var cache = new BitmapMemoryCache(8);

            for (var index = 0; index < 500; index++)
                cache.Set($"key-{index}", null);

            cache.Count.Should().Be(8);
            cache.TryGet("key-499", out _).Should().BeTrue();
            cache.TryGet("key-0", out _).Should().BeFalse();
        }

        [Test]
        public void SettingAKeyTwiceReplacesItRatherThanCountingItTwice()
        {
            var cache = new BitmapMemoryCache(2);
            cache.Set("a", null);
            cache.Set("a", null);
            cache.Set("b", null);

            cache.Count.Should().Be(2);
            cache.TryGet("a", out _).Should().BeTrue();
            cache.TryGet("b", out _).Should().BeTrue();
        }

        [Test]
        public void ForgettingAKeyMakesTheNextRequestReRender()
        {
            var cache = new BitmapMemoryCache(4);
            cache.Set("a", null);

            cache.Remove("a");
            cache.TryGet("a", out _).Should().BeFalse();

            // Removing something that was never there is not an error.
            cache.Remove("never-seen");

            cache.Set("b", null);
            cache.Clear();
            cache.Count.Should().Be(0);
        }

        [Test]
        public void KeysAreMatchedTheWayResRefsAre()
        {
            var cache = new BitmapMemoryCache(4);
            cache.Set("Custom:Utc:NPC_Guard", null);

            cache.TryGet("custom:utc:npc_guard", out _).Should().BeTrue(
                "a resref differing only in case names the same resource");
        }

        [Test]
        public void ACapacityBelowOneIsRefused()
        {
            var construct = () => new BitmapMemoryCache(0);
            construct.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
