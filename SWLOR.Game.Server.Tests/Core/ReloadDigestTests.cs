using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The reload digests: a content hash that two builds of the same code agree on (<see cref="ReloadDigestHasher"/>), the
/// per-class cache and adopted digests (<see cref="ReloadDigests"/>) and their comparison.
/// </summary>
[TestFixture]
public class ReloadDigestTests
{
    private interface IDependency
    {
    }

    private sealed class Dependency : IDependency
    {
        public int Calls;
    }

    private sealed class Definition
    {
        public string Name { get; init; }
        public int Level { get; init; }
        public Action Run { get; init; }
        public Definition Parent { get; set; }
    }

    private sealed class Owner
    {
        public readonly Dictionary<string, Definition> Definitions = new();
        public readonly HashSet<string> Tags = new();
        public readonly List<int> Ordered = new();
        public DateTime LoadedAt;
        public IDependency Other;

        [ReloadState(ReloadStateKind.Runtime)]
        public readonly Dictionary<uint, int> PerCreature = new();

        [ReloadState(ReloadStateKind.Adopted)]
        public readonly Dictionary<uint, string> Live = new();

        public static void Callback() { }

        public static void OtherCallback() { }
    }

    private static Owner Build(bool reverse)
    {
        var owner = new Owner { LoadedAt = reverse ? DateTime.UtcNow : DateTime.MinValue, Other = new Dependency { Calls = reverse ? 7 : 0 } };
        var names = reverse ? new[] { "b", "a" } : new[] { "a", "b" };
        foreach (var name in names)
        {
            var definition = new Definition { Name = name, Level = name.Length, Run = Owner.Callback };
            definition.Parent = definition;
            owner.Definitions[name] = definition;
            owner.Tags.Add("tag-" + name);
        }

        owner.Ordered.AddRange(new[] { 1, 2, 3 });
        owner.PerCreature[reverse ? 9u : 4u] = 1;
        owner.Live[1] = reverse ? "x" : "y";
        return owner;
    }

    private static ReloadDigestLine Cache(Owner owner) =>
        new ReloadDigestHasher(typeof(ReloadDigestTests).Assembly).Describe(ReloadDigests.CacheKind, "owner", typeof(Owner), owner,
            field => ReloadDigestHasher.StateOf(field) == null);

    [Test]
    public void Equal_content_hashes_alike_whatever_the_order_of_dictionaries_and_sets_the_time_the_dependencies_and_the_runtime_state()
    {
        var first = Cache(Build(reverse: false));
        var second = Cache(Build(reverse: true));

        second.Should().Be(first);
        first.Count.Should().Be(2 + 2 + 3 + 1, "the definitions, the tags, the list and the time count; the dependency does not");
        first.Hash.Should().MatchRegex("^[0-9a-f]{16}$");
    }

    [Test]
    public void A_change_of_content_list_order_or_callback_changes_the_hash()
    {
        var baseline = Cache(Build(reverse: false));

        var renamed = Build(reverse: false);
        renamed.Definitions["a"] = new Definition { Name = "a", Level = 2, Run = Owner.Callback };
        var reordered = Build(reverse: false);
        reordered.Ordered.Reverse();
        var otherCallback = Build(reverse: false);
        otherCallback.Definitions["b"] = new Definition { Name = "b", Level = 1, Run = Owner.OtherCallback, Parent = otherCallback.Definitions["b"] };
        var extraTag = Build(reverse: false);
        extraTag.Tags.Add("tag-c");

        Cache(renamed).Hash.Should().NotBe(baseline.Hash);
        Cache(reordered).Hash.Should().NotBe(baseline.Hash);
        Cache(otherCallback).Hash.Should().NotBe(baseline.Hash);
        Cache(extraTag).Should().NotBe(baseline);
    }

    [Test]
    public void The_adopted_digest_covers_only_the_adopted_fields()
    {
        var hasher = new ReloadDigestHasher(typeof(ReloadDigestTests).Assembly);
        var first = Build(reverse: false);
        var second = Build(reverse: false);
        second.Live[1] = "changed";
        second.Ordered.Add(4);

        bool Adopted(System.Reflection.FieldInfo field) => ReloadDigestHasher.StateOf(field) == ReloadStateKind.Adopted;
        hasher.Describe(ReloadDigests.AdoptedKind, "owner", typeof(Owner), first, Adopted).Hash
            .Should().NotBe(hasher.Describe(ReloadDigests.AdoptedKind, "owner", typeof(Owner), second, Adopted).Hash);
        hasher.Describe(ReloadDigests.AdoptedKind, "owner", typeof(Owner), first, Adopted).Count.Should().Be(1);
    }

    [Test]
    public void Cycles_and_shared_objects_hash_the_same_every_time()
    {
        var hasher = new ReloadDigestHasher(typeof(ReloadDigestTests).Assembly);
        var definition = new Definition { Name = "loop" };
        definition.Parent = definition;

        hasher.Hash(new[] { definition, definition }).Should().Be(hasher.Hash(new[] { definition, definition }));
        hasher.Hash((1, "a")).Should().Be(hasher.Hash((1, "a"))).And.NotBe(hasher.Hash((1, "b")));
        hasher.Hash(new KeyValuePair<int, string>(1, "a")).Should().NotBe(hasher.Hash(new KeyValuePair<int, string>(2, "a")));
        hasher.Hash(new Lazy<int>(() => 1)).Should().Be(hasher.Hash(new Lazy<int>(() => 2)), "a lazy value not yet created counts as such");
    }

    [Test]
    public void Comparing_names_the_digests_that_differ_are_missing_or_are_new()
    {
        var boot = new[]
        {
            new ReloadDigestLine("cache", "Same", 1, "0000000000000001"),
            new ReloadDigestLine("cache", "Changed", 2, "0000000000000002"),
            new ReloadDigestLine("cache", "Gone", 3, "0000000000000003"),
        };
        var reload = new[]
        {
            new ReloadDigestLine("cache", "Same", 1, "0000000000000001"),
            new ReloadDigestLine("cache", "Changed", 2, "0000000000000009"),
            new ReloadDigestLine("world", "New", 4, "0000000000000004"),
        };

        ReloadDigests.Compare(boot, reload).Should().Equal(
            "cache Changed: count 2 hash 0000000000000002 before, count 2 hash 0000000000000009 after",
            "cache Gone: missing after the reload",
            "world New: only after the reload");
        ReloadDigests.Compare(boot, boot).Should().BeEmpty();
    }

    [Test]
    public void A_digest_line_is_logged_in_the_form_the_hot_reload_check_reads()
    {
        new ReloadDigestLine("adopted", "SWLOR.Game.Server.Domain.Creatures.SpawnService", 12, "00000000000000ff").Format(3, "reload")
            .Should().Be("SWLOR_RELOAD_DIGEST generation=3 phase=reload kind=adopted name=SWLOR.Game.Server.Domain.Creatures.SpawnService count=12 hash=00000000000000ff");
    }

    [Test]
    public void The_servers_cache_digests_cover_every_instance_owner_of_a_rerun_handler_and_repeat_exactly()
    {
        // Only the instance handlers: reading a static class's fields runs its static initializer, which on a server has run
        // (its handler ran first) but here may need the engine, and a failed one fails every later use in the test process.
        var registry = new ScriptRegistry();
        var instanceMethods = ScriptHandlerTestSupport.GameAssembly.GetTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
        registry.LoadHandlers(instanceMethods, ScriptHandlerManifest.LoadEmbedded(ScriptHandlerTestSupport.GameAssembly),
            ScriptHandlerTestSupport.Owners);

        var first = ReloadDigests.Cache(registry);
        var second = ReloadDigests.Cache(registry);

        first.Select(line => line.Name).Should().OnlyHaveUniqueItems().And.Contain(new[]
        {
            "SWLOR.Game.Server.Domain.Foundation.CacheService",
            "SWLOR.Game.Server.Domain.UI.GuiService",
            "SWLOR.Game.Server.Domain.Creatures.SpawnService",
            "SWLOR.Game.Server.Domain.Crafting.CraftService",
        });
        first.Should().OnlyContain(line => line.Kind == ReloadDigests.CacheKind && line.IsTaken);
        ReloadDigests.Compare(first, second).Should().BeEmpty();
    }

    [Test]
    public void A_class_whose_fields_cannot_be_read_gets_a_digest_that_never_matches_and_the_others_are_still_taken()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlers(typeof(ThrowingOwner).GetMethods().Concat(typeof(PlainOwner).GetMethods()), ScriptHandlerManifest.Empty);

        var lines = ReloadDigests.Cache(registry);

        lines.Should().HaveCount(2);
        var failed = lines.Single(line => line.Name == typeof(ThrowingOwner).FullName);
        failed.Hash.Should().Be(ReloadDigestLine.ErrorHashPrefix + nameof(InvalidOperationException));
        failed.IsTaken.Should().BeFalse();
        failed.Matches(failed).Should().BeFalse("a digest that was not taken cannot show that a reload kept anything");
        lines.Single(line => line.Name == typeof(PlainOwner).FullName).IsTaken.Should().BeTrue();
        ReloadDigests.Compare(lines, lines).Should().ContainSingle().Which.Should().StartWith("cache " + typeof(ThrowingOwner).FullName);
    }

    private static class ThrowingOwner
    {
        private static readonly int Value = Fail();

        private static int Fail() => throw new InvalidOperationException("no engine");

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Load() => _ = Value;
    }

    private static class PlainOwner
    {
        private static readonly List<string> Names = new() { "a" };

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void Load() => Names.Add("b");
    }

    [Test]
    public void The_servers_adopted_digests_cover_each_class_with_adopted_state()
    {
        var lines = ReloadDigests.Adopted(ScriptHandlerTestSupport.ServerContainer.ResolveHandlerOwner);

        lines.Select(line => line.Name).Should().Equal(
            "SWLOR.Game.Server.Domain.Creatures.SpawnService",
            "SWLOR.Game.Server.Domain.Space.SpaceService",
            "SWLOR.Game.Server.Domain.World.PropertyLoaderService",
            "SWLOR.Game.Server.Domain.World.ShuttleService",
            "SWLOR.Game.Server.Domain.World.WeatherService",
            "SWLOR.Game.Server.Feature.StoreManagement");
        lines.Should().OnlyContain(line => line.Kind == ReloadDigests.AdoptedKind && line.Hash != "unresolved");
    }

    [Test]
    public void A_class_with_adopted_instance_state_the_container_does_not_hold_never_matches()
    {
        var lines = ReloadDigests.Adopted(typeof(ReloadDigestTests).Assembly, _ => null);

        lines.Should().Contain(line => line.Name == typeof(Owner).FullName && line.Count == -1 && line.Hash == "unresolved");
    }
}
