using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.World;
using static SWLOR.Game.Server.Tests.Core.ScriptHandlerTestSupport;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Proves how <see cref="ScriptRegistry"/> binds an event handler declared on an instance method (a converted service
/// whose facade was removed, P2-Z): once, on the container's singleton of the class that declares it, under the name
/// <c>DeclaringType.FullName + "." + Name</c>; never again for a subclass (a test double); and never for a class the
/// container does not register, which stops the load. Static handlers bind exactly as before.
/// </summary>
[TestFixture]
public class ScriptRegistryInstanceHandlerTests
{
    private const string TaxiHandler = "SWLOR.Game.Server.Domain.World.TaxiService.LoadTaxiDestinations";

    private static MethodInfo[] TaxiMethods => typeof(TaxiService).GetMethods();

    [Test]
    public void A_handler_on_an_instance_method_is_bound_once_to_the_containers_singleton()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly }, Owners);

        var taxi = registry.GetActionScripts(ScriptName.OnModuleCacheBefore)
            .Where(handler => handler.Action.Method.DeclaringType == typeof(TaxiService))
            .ToList();

        var handler = taxi.Should().ContainSingle("the handler is registered once").Subject;
        var instance = ServerContainer.Provider.GetRequiredService<ITaxiService>();
        handler.Action.Target.Should().BeSameAs(instance, "the handler runs on the container's singleton, the one GameServices.Taxi returns");
        ScriptHandlerManifest.HandlerName(handler.Action.Method).Should().Be(TaxiHandler);
        handler.Name.Should().Be("TaxiService.LoadTaxiDestinations");

        handler.Action();
        instance.GetDestinationsByRegionId(1).Should().NotBeEmpty("running the handler filled the singleton's destination cache");
    }

    [Test]
    public void A_subclass_that_inherits_a_handler_never_registers_a_second_copy()
    {
        var registry = new ScriptRegistry();
        var methods = TaxiMethods.Concat(typeof(TaxiServiceDouble).GetMethods()).ToArray();
        methods.Count(IsHandler).Should().Be(2, "the double inherits the handler, so reflection returns it once per class");

        registry.LoadHandlers(methods, ScriptHandlerManifest.Empty, Owners);

        var handler = registry.GetActionScripts(ScriptName.OnModuleCacheBefore).Should().ContainSingle().Subject;
        handler.Action.Target.Should().BeOfType<TaxiService>().And.BeSameAs(ServerContainer.Provider.GetRequiredService<ITaxiService>());
    }

    [Test]
    public void No_handler_of_the_game_or_the_engine_tests_is_bound_twice_or_to_an_instance_of_another_class()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly, EngineTestAssembly }, Owners);

        foreach (var script in ScriptHandlerOrderSnapshot.AllScriptNames(registry))
        {
            var bound = Bound(registry, script).ToList();
            bound.Select(handler => handler.Method).Should().OnlyHaveUniqueItems(script);
            foreach (var handler in bound)
            {
                if (handler.Method.IsStatic)
                    handler.Target.Should().BeNull(script);
                else
                    handler.Target!.GetType().Should().Be(handler.Method.DeclaringType!, script);
            }
        }
    }

    [Test]
    public void Instance_handlers_bind_as_actions_and_as_conditional_scripts_on_one_instance()
    {
        var fixture = new InstanceHandlerFixture { Answer = false };
        var asked = new List<Type>();
        var registry = new ScriptRegistry();

        registry.LoadHandlers(typeof(InstanceHandlerFixture).GetMethods(), ScriptHandlerManifest.Empty, type =>
        {
            asked.Add(type);
            return fixture;
        });

        asked.Should().Equal(new[] { typeof(InstanceHandlerFixture) }, "the owner is asked for once per class and load");
        var action = registry.GetActionScripts(InstanceHandlerFixture.ActionScript).Should().ContainSingle().Subject;
        var conditional = registry.GetConditionalScripts(InstanceHandlerFixture.ConditionalScript).Should().ContainSingle().Subject;

        action.Action();
        action.Action();
        fixture.Runs.Should().Be(2);
        conditional.Action().Should().BeFalse();
        fixture.Answer = true;
        conditional.Action().Should().BeTrue();
        registry.HasConditionalScript(InstanceHandlerFixture.ConditionalScript).Should().BeTrue();
    }

    [Test]
    public void A_handler_on_a_class_the_container_does_not_register_stops_the_load_with_a_message()
    {
        var registry = new ScriptRegistry();
        registry.LoadHandlersFromAssemblies(new[] { GameAssembly }, Owners);

        var load = () => registry.LoadHandlersFromAssemblies(new[] { GameAssembly }, _ => null!);

        load.Should().Throw<InvalidOperationException>()
            .WithMessage($"{TaxiHandler} is an event handler on an instance method of SWLOR.Game.Server.Domain.World.TaxiService, which the service container does not register*");
        registry.GetActionScriptNames().Should().BeEmpty("a load that fails registers nothing, not even the static handlers found before it");
        registry.GetConditionalScriptNames().Should().BeEmpty();
    }

    [Test]
    public void A_load_without_a_resolver_refuses_an_instance_handler()
    {
        var registry = new ScriptRegistry();

        var load = () => registry.LoadHandlersFromAssemblies(new[] { GameAssembly });

        load.Should().Throw<InvalidOperationException>().WithMessage($"{TaxiHandler} is an event handler on an instance method, but the script registry was loaded without a resolver*");
    }

    [Test]
    public void A_resolver_that_answers_with_an_instance_of_another_class_is_refused()
    {
        var registry = new ScriptRegistry();
        var double_ = new TaxiServiceDouble(ServerContainer.Provider.GetRequiredService<IDBService>());

        var load = () => registry.LoadHandlers(TaxiMethods, ScriptHandlerManifest.Empty, _ => double_);

        load.Should().Throw<InvalidOperationException>().WithMessage("*answered SWLOR.Game.Server.Domain.World.TaxiService with an instance of*TaxiServiceDouble*");
    }

    [Test]
    public void Static_handlers_bind_exactly_as_before_and_need_no_resolver()
    {
        var manifest = ScriptHandlerManifest.LoadEmbedded(GameAssembly);
        var all = new ScriptRegistry();
        all.LoadHandlersFromAssemblies(new[] { GameAssembly }, manifest, Owners);
        var staticOnly = new ScriptRegistry();
        staticOnly.LoadHandlers(MethodsOf(GameAssembly).Where(method => method.IsStatic), manifest);

        var instanceHandlers = MethodsOf(GameAssembly)
            .Where(method => !method.IsStatic && IsHandler(method))
            .Select(ScriptHandlerManifest.HandlerName)
            .ToHashSet();

        foreach (var script in ScriptHandlerOrderSnapshot.AllScriptNames(all))
        {
            foreach (var conditional in new[] { false, true })
            {
                var expected = Names(all, script, conditional).Where(name => !instanceHandlers.Contains(name));
                Names(staticOnly, script, conditional).Should().Equal(expected, $"{script}: static handlers keep their order without the instance ones");
            }

            Bound(all, script).Where(handler => handler.Method.IsStatic).Should().OnlyContain(handler => handler.Target == null, script);
        }

        staticOnly.StaleManifestEntries.Select(entry => entry[(entry.LastIndexOf(' ') + 1)..]).Distinct()
            .Should().BeEquivalentTo(instanceHandlers, "the manifest lines the static-only load left unbound are exactly the instance handlers");
    }

    private static IEnumerable<Delegate> Bound(ScriptRegistry registry, string script) =>
        registry.GetActionScripts(script).Select(handler => (Delegate)handler.Action)
            .Concat(registry.GetConditionalScripts(script).Select(handler => (Delegate)handler.Action));
}
