using System.Reflection;
using System.Runtime.Loader;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// <see cref="GameAssemblies"/> (P4-4): every discovery scan lists its assemblies through it. In a process with one load
/// context (the server today, and this test process) it must return what <c>AppDomain.CurrentDomain.GetAssemblies()</c>
/// returned, in the same order, so each scan finds the same types in the same order as before. The architecture test
/// <c>Only_GameAssemblies_lists_the_assemblies_of_the_process</c> guarantees no scan lists assemblies any other way.
/// </summary>
[TestFixture]
public class GameAssembliesTests
{
    private const string EngineTestsAssembly = "SWLOR.Game.Server.EngineTests";

    // The base type of every definition scan in the server (each scan is "types assignable to it, neither interface nor abstract").
    private static readonly string[] ScannedBaseTypes =
    [
        "SWLOR.Game.Server.Domain.Abilities.IAbilityListDefinition",
        "SWLOR.Game.Server.Domain.Character.IPerkListDefinition",
        "SWLOR.Game.Server.Domain.Character.StatusEffectBase",
        "SWLOR.Game.Server.Domain.Companions.IBeastListDefinition",
        "SWLOR.Game.Server.Domain.Crafting.IRecipeListDefinition",
        "SWLOR.Game.Server.Domain.Creatures.IAIProfileDefinition",
        "SWLOR.Game.Server.Domain.Creatures.ISpawnListDefinition",
        "SWLOR.Game.Server.Domain.Foundation.ConversationMenuDefinition",
        "SWLOR.Game.Server.Domain.Foundation.IConversationGraphDefinition",
        "SWLOR.Game.Server.Domain.Foundation.IServerMigration",
        "SWLOR.Game.Server.Domain.Foundation.ISnippetListDefinition",
        "SWLOR.Game.Server.Domain.Items.IItemListDefinition",
        "SWLOR.Game.Server.Domain.Items.ILootTableDefinition",
        "SWLOR.Game.Server.Domain.Quests.IQuestListDefinition",
        "SWLOR.Game.Server.Domain.Social.IChatCommandListDefinition",
        "SWLOR.Game.Server.Domain.Space.IShipListDefinition",
        "SWLOR.Game.Server.Domain.Space.IShipModuleListDefinition",
        "SWLOR.Game.Server.Domain.Space.ISpaceObjectListDefinition",
        "SWLOR.Game.Server.Domain.UI.IGuiWindowDefinition",
        "SWLOR.Game.Server.Domain.World.IFishingLocationDefinition",
        "SWLOR.Game.Server.Domain.World.IPropertyLayoutListDefinition"
    ];

    private static IEnumerable<Assembly> Loaded(IEnumerable<Assembly> assemblies) => assemblies.Where(assembly => !assembly.IsDynamic);

    [Test]
    public void It_returns_the_assemblies_of_the_app_domain_in_the_same_order_when_there_is_one_load_context()
    {
        Assembly.Load(new AssemblyName(EngineTestsAssembly));

        var before = Loaded(AppDomain.CurrentDomain.GetAssemblies()).ToList();
        var found = Loaded(GameAssemblies.GetAssemblies()).ToList();
        var after = Loaded(AppDomain.CurrentDomain.GetAssemblies()).ToList();

        // The app domain may grow between the two reads (another test loads something); the game's list is a prefix-ordered subset of it.
        found.Should().OnlyHaveUniqueItems();
        found.Should().BeSubsetOf(after);
        before.Where(found.Contains).Should().Equal(found.Where(before.Contains), "the order is the app domain's order");
        var gameContext = AssemblyLoadContext.GetLoadContext(typeof(GameAssemblies).Assembly);
        AssemblyLoadContext.Default.Assemblies.Where(assembly => !assembly.IsDynamic).Should().BeSubsetOf(found,
            "the game assembly is in the default context, so every assembly of that context is scanned");
        before.Where(assembly => !found.Contains(assembly))
            .Should().OnlyContain(assembly => AssemblyLoadContext.GetLoadContext(assembly) != gameContext,
                "only assemblies of another load context are left out");
    }

    [Test]
    public void It_includes_the_game_assembly_and_the_engine_test_assembly_in_the_game_context()
    {
        var engineTests = Assembly.Load(new AssemblyName(EngineTestsAssembly));

        var found = GameAssemblies.GetAssemblies();

        found.Should().Contain(typeof(GameAssemblies).Assembly);
        found.Should().Contain(engineTests);
        AssemblyLoadContext.GetLoadContext(engineTests).Should().BeSameAs(AssemblyLoadContext.GetLoadContext(typeof(GameAssemblies).Assembly));
    }

    [Test]
    public void It_leaves_out_an_assembly_that_another_load_context_holds()
    {
        var context = new AssemblyLoadContext("game-assemblies-probe", isCollectible: true);
        try
        {
            var source = typeof(FluentAssertions.AssertionExtensions).Assembly.Location;
            using var stream = File.OpenRead(source);

            var other = context.LoadFromStream(stream);

            AppDomain.CurrentDomain.GetAssemblies().Should().Contain(other, "the app domain lists every context's assemblies");
            GameAssemblies.GetAssemblies().Should().NotContain(other, "a scan never sees an assembly of another build's context");
            GameAssemblies.GetAssemblies().Should().Contain(typeof(FluentAssertions.AssertionExtensions).Assembly,
                "the copy in the game's own context is still scanned");
        }
        finally
        {
            context.Unload();
        }
    }

    [TestCaseSource(nameof(ScannedBaseTypes))]
    public void A_definition_scan_finds_the_same_types_in_the_same_order_as_the_app_domain_scan(string baseTypeName)
    {
        Assembly.Load(new AssemblyName(EngineTestsAssembly));
        var baseType = typeof(GameAssemblies).Assembly.GetType(baseTypeName, throwOnError: true)!;

        List<Type> Scan(IEnumerable<Assembly> assemblies) => assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => baseType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        var legacy = Scan(AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic));
        var scoped = Scan(GameAssemblies.GetAssemblies().Where(assembly => !assembly.IsDynamic));

        scoped.Should().Equal(legacy);
        scoped.Should().NotBeEmpty("the game declares definitions of every scanned kind");
    }

    [Test]
    public void The_engine_test_discovery_finds_the_same_methods_in_the_same_order_as_the_app_domain_scan()
    {
        var engineTests = Assembly.Load(new AssemblyName(EngineTestsAssembly));
        var attribute = engineTests.GetType("SWLOR.Game.Server.EngineTests.Framework.EngineTestAttribute", throwOnError: true)!;

        List<MethodBase> Scan(IEnumerable<Assembly> assemblies) => assemblies
            .SelectMany(assembly =>
            {
                try { return assembly.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(type => type != null).Select(type => type!); }
            })
            .SelectMany(type =>
            {
                try
                {
                    return type.GetMethods(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(method => method.GetCustomAttribute(attribute) != null)
                        .ToArray();
                }
                catch (TypeLoadException)
                {
                    // A type of the test host whose dependency this process cannot load: the same answer for both scans.
                    return [];
                }
            })
            .Cast<MethodBase>()
            .ToList();

        var legacy = Scan(AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic));
        var scoped = Scan(GameAssemblies.GetAssemblies().Where(assembly => !assembly.IsDynamic));

        scoped.Should().Equal(legacy);
        scoped.Should().NotBeEmpty("the engine-test assembly declares tests");
    }
}
