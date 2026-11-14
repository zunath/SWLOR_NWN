using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Tests.Composition.ContentSamples;
using SWLOR.Game.Server.Tests.Core.ContentCarry;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// The reload carry of an object that takes services (MaintainabilityRefactorContentInjection.md, "Hot reload carry"): the new build's
/// content factory builds it through its values-free constructor with the new build's services, and the carried fields are then read
/// into it; the fields that hold a service or content singleton are never written, read, checked or hashed. Proved with a test-only
/// carried hierarchy (<see cref="CarriedSampleEffect"/>) between two containers, as the old and new build.
/// </summary>
[TestFixture]
public class ReloadCarryContentTests
{
    private static readonly Assembly Samples = typeof(CarriedSampleHolder).Assembly;

    private static void Register(IServiceCollection services) => services.AddSingleton<SampleSingleton>();

    private static IReadOnlyList<FieldInfo> HolderFields() =>
        ReloadStateCarrier.CarriedClasses(Samples).Single(entry => entry.Type == typeof(CarriedSampleHolder)).Fields;

    private static ReloadStateCarrier CarrierOf(FacadeContainer container) =>
        new(Samples, content: container.Content, isService: container.IsRegistered);

    [Test]
    public void An_effect_with_services_round_trips_without_them_and_takes_the_new_containers_services()
    {
        using var old = new FacadeContainer(Register);
        using var next = new FacadeContainer(Register);
        var effect = old.Content.Create<CarriedSampleEffect, int>(7);
        effect.DurationTicks = 5;
        effect.Stacks.Add(3);
        var holder = new CarriedSampleHolder();
        holder.Add(42u, effect);

        var text = CarrierOf(old).Write(HolderFields(), holder);

        text.Should().NotContain(nameof(CarriedSampleEffect.Party)).And.NotContain(nameof(CarriedSampleEffect.Singleton),
            "a field that holds a service or content singleton is never written");
        text.Should().Contain("_potency").And.Contain(effect.Id.ToString());

        var adopted = new CarriedSampleHolder();
        CarrierOf(next).Read(HolderFields(), adopted, text);

        var carried = adopted.ByCreature[42u].Should().ContainSingle().Which.Should().BeOfType<CarriedSampleEffect>().Subject;
        carried.Should().NotBeSameAs(effect);
        carried.Id.Should().Be(effect.Id);
        carried.DurationTicks.Should().Be(5);
        carried.Stacks.Should().Equal(3);
        carried.Potency.Should().Be(7, "the carried value replaces what the values-free constructor set");
        carried.Party.Should().BeSameAs(next.Get<IPartyService>(), "the services are the new container's");
        carried.Party.Should().NotBeSameAs(old.Get<IPartyService>());
        carried.Singleton.Should().BeSameAs(next.Get<SampleSingleton>(), "a class-typed content singleton is the new container's too");
        adopted.Ticking.Should().ContainSingle().Which.Should().BeSameAs(carried, "an effect held in two collections is one object again");
    }

    [Test]
    public void Without_the_content_factory_an_object_with_services_cannot_be_read_back()
    {
        using var container = new FacadeContainer(Register);
        var holder = new CarriedSampleHolder();
        holder.Add(1u, container.Content.Create<CarriedSampleEffect>());
        var text = CarrierOf(container).Write(HolderFields(), holder);

        var withoutFactory = new ReloadStateCarrier(Samples, isService: container.IsRegistered);
        withoutFactory.Invoking(carrier => carrier.Read(HolderFields(), new CarriedSampleHolder(), text))
            .Should().Throw<JsonSerializationException>().WithMessage("*content factory*");
    }

    [Test]
    public void The_problems_check_leaves_out_service_fields_and_still_fails_for_one_the_predicate_misses()
    {
        using var container = new FacadeContainer(Register);
        var field = HolderFields().Single(candidate => candidate.Name == "_byCreature");

        ReloadStateCarrier.Problems(field.FieldType, Samples, isService: container.IsRegistered).Should().BeEmpty();

        ReloadStateCarrier.Problems(field.FieldType, Samples).Should().Contain(problem => problem.Contains(nameof(CarriedSampleEffect.Party)),
            "without the container's registrations the service field is checked like data, and a service is not data");
        ReloadStateCarrier.Problems(typeof(IPartyService), Samples, isService: container.IsRegistered).Should().ContainSingle()
            .Which.Should().Contain("injected, never carried");
    }

    [Test]
    public void Each_carried_type_with_services_builds_through_its_values_free_constructor_without_engine_calls()
    {
        using var container = new FacadeContainer(Register);
        var engine = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(engine);
        var withServices = HolderFields()
            .SelectMany(field => ReloadStateCarrier.TypesWithServices(field.FieldType, Samples, container.IsRegistered))
            .Distinct()
            .ToArray();

        withServices.Should().Equal(typeof(CarriedSampleEffect));
        foreach (var type in withServices)
            container.Content.Invoking(content => content.Create<object>(type)).Should().NotThrow();
        engine.Calls.Should().BeEmpty("the carry builds these before the new build's first frame; their constructors call no engine");
    }

    [Test]
    public void The_game_builds_carried_types_with_services_without_engine_calls()
    {
        using var container = new FacadeContainer();
        var engine = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(engine);
        var game = ScriptHandlerTestSupport.GameAssembly;

        var withServices = ReloadStateCarrier.CarriedClasses(game)
            .SelectMany(entry => entry.Fields)
            .SelectMany(field => ReloadStateCarrier.TypesWithServices(field.FieldType, game, container.IsRegistered))
            .Distinct()
            .ToArray();

        TestContext.Out.WriteLine($"Carried game types that take services: {(withServices.Length == 0 ? "none" : string.Join(", ", withServices.Select(type => type.Name)))}");
        foreach (var type in withServices)
            container.Content.Invoking(content => content.Create<object>(type)).Should().NotThrow($"{type.Name} is carried and takes services");
        engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void The_digests_count_a_class_typed_singleton_field_as_a_dependency()
    {
        using var old = new FacadeContainer(Register);
        using var next = new FacadeContainer(Register);
        var before = old.Content.Create<CarriedSampleEffect, int>(2);
        var after = next.Content.Create<CarriedSampleEffect, int>(2);
        typeof(CarriedSampleEffectBase).GetProperty(nameof(CarriedSampleEffectBase.Id))!.SetValue(after, before.Id);

        new ReloadDigestHasher(Samples).HoldsDependency(typeof(SampleSingleton)).Should().BeFalse();
        new ReloadDigestHasher(Samples, next.IsRegistered).HoldsDependency(typeof(SampleSingleton)).Should().BeTrue();

        new ReloadDigestHasher(Samples, old.IsRegistered).Hash(before)
            .Should().Be(new ReloadDigestHasher(Samples, next.IsRegistered).Hash(after), "the injected singleton belongs to its own digest");
        new ReloadDigestHasher(Samples).Hash(before)
            .Should().NotBe(new ReloadDigestHasher(Samples).Hash(after), "without the predicate the singleton's own content counts");
    }

    [Test]
    public void The_adopted_digests_read_the_carried_effects_without_their_services()
    {
        using var old = new FacadeContainer(Register);
        using var next = new FacadeContainer(Register);
        var effect = old.Content.Create<CarriedSampleEffect, int>(4);
        var holder = new CarriedSampleHolder();
        holder.Add(9u, effect);
        var text = CarrierOf(old).Write(HolderFields(), holder);
        var adopted = new CarriedSampleHolder();
        CarrierOf(next).Read(HolderFields(), adopted, text);

        var digestBefore = ReloadDigests.Adopted(Samples, type => type == typeof(CarriedSampleHolder) ? holder : null, old.IsRegistered)
            .Where(line => line.Name.StartsWith(typeof(CarriedSampleHolder).FullName!, StringComparison.Ordinal)).ToArray();
        var digestAfter = ReloadDigests.Adopted(Samples, type => type == typeof(CarriedSampleHolder) ? adopted : null, next.IsRegistered)
            .Where(line => line.Name.StartsWith(typeof(CarriedSampleHolder).FullName!, StringComparison.Ordinal)).ToArray();

        digestBefore.Should().HaveCount(2);
        ReloadDigests.Compare(digestBefore, digestAfter).Should().BeEmpty("the carried and adopted digests stay equal");
    }
}
