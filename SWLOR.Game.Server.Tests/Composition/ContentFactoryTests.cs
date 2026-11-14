using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Tests.Composition.ContentSamples;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// The content factory (MaintainabilityRefactorContentInjection.md, "The factory"): made content gets its services from the container
/// and its values from the caller, the constructor is the one C# would pick, the optional values take their defaults, and the factory
/// never builds a registered, abstract or foreign type. The reload validation's content check finds what the factory would refuse.
/// </summary>
[TestFixture]
public class ContentFactoryTests
{
    private FacadeContainer _container = null!;

    private IContentFactory Content => _container.Content;

    private static void Register(IServiceCollection services) => services.AddSingleton<SampleSingleton>();

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer(Register);

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [Test]
    public void The_container_registers_the_factory_as_a_singleton()
    {
        Content.Should().BeOfType<ContentFactory>().And.BeSameAs(_container.Content);
        _container.IsRegistered(typeof(IContentFactory)).Should().BeTrue();
    }

    [Test]
    public void A_values_free_creation_gets_the_containers_services_and_the_forwarded_defaults()
    {
        var made = Content.Create<SampleContent>();

        made.Party.Should().BeSameAs(_container.Get<IPartyService>());
        made.Singleton.Should().BeSameAs(_container.Get<SampleSingleton>());
        made.Potency.Should().Be(0);
        made.Label.Should().Be("default");
    }

    [Test]
    public void A_creation_with_values_picks_the_constructor_with_the_fewest_parameters_and_fills_its_optional_values()
    {
        var made = Content.Create<SampleContent, int>(5);

        made.Potency.Should().Be(5);
        made.Label.Should().Be("default");
        made.Kind.Should().Be(SampleKind.Second, "an enum default is the enum's value");
        made.Cap.Should().Be(7, "a nullable default keeps its value");
        made.Party.Should().BeSameAs(_container.Get<IPartyService>());
    }

    [Test]
    public void The_value_types_choose_between_constructors_with_as_many_parameters()
    {
        Content.Create<SampleContent, int, string>(3, "named").Label.Should().Be("named");
        var targeted = Content.Create<SampleContent, int, uint>(3, 99u);
        targeted.Label.Should().Be("targeted");
        targeted.Target.Should().Be(99u);
        Content.Create<SampleContent, int, string, SampleKind, int?>(1, "all", SampleKind.First, null).Cap.Should().BeNull();
    }

    [Test]
    public void Every_arity_up_to_six_values_builds()
    {
        Content.Create<SixValueContent, int, int, int, int, int, string>(1, 2, 3, 4, 5, "six").Values.Should().Be("1,2,3,4,5,six");
        Content.Create<SampleContent, int, string, SampleKind>(1, "x", SampleKind.First).Kind.Should().Be(SampleKind.First);
    }

    [Test]
    public void A_subclass_passes_its_base_its_services()
    {
        var made = Content.Create<DerivedContent>();

        made.BaseParty.Should().BeSameAs(_container.Get<IPartyService>());
        made.Singleton.Should().BeSameAs(_container.Get<SampleSingleton>());
    }

    [Test]
    public void Building_by_type_returns_the_type_as_what_the_caller_asks_for()
    {
        Content.Create<object>(typeof(SampleContent)).Should().BeOfType<SampleContent>();
        Content.Create<AbstractContent>(typeof(DerivedContent)).Should().BeOfType<DerivedContent>();
        Content.Invoking(content => content.Create<SampleContent>(typeof(DerivedContent)))
            .Should().Throw<InvalidOperationException>().WithMessage("*is not one*");
    }

    [Test]
    public void Each_call_builds_a_new_object()
    {
        Content.Create<SampleContent, int>(1).Should().NotBeSameAs(Content.Create<SampleContent, int>(1));
        Content.Create<object>(typeof(SampleContent)).Should().NotBeSameAs(Content.Create<object>(typeof(SampleContent)));
    }

    [Test]
    public void It_never_builds_a_registered_type()
    {
        Content.Invoking(content => content.Create<SampleSingleton>())
            .Should().Throw<InvalidOperationException>().WithMessage("*registers it*");
        Content.Invoking(content => content.Create<object>(typeof(SampleSingleton)))
            .Should().Throw<InvalidOperationException>().WithMessage("*registers it*");
    }

    [Test]
    public void It_never_builds_an_abstract_type_a_service_class_or_a_type_of_another_assembly()
    {
        Content.Invoking(content => content.Create<AbstractContent>(typeof(AbstractContent)))
            .Should().Throw<InvalidOperationException>().WithMessage("*abstract*");
        Content.Invoking(content => content.Create<object>(typeof(TaxiService)))
            .Should().Throw<InvalidOperationException>().WithMessage("*not content*");
        Content.Invoking(content => content.Create<System.Text.StringBuilder>())
            .Should().Throw<InvalidOperationException>().WithMessage("*neither a type of the game build nor of a test assembly*");
    }

    [Test]
    public void It_refuses_a_call_no_constructor_or_two_constructors_take()
    {
        Content.Invoking(content => content.Create<SampleContent, string>("no constructor takes only a string"))
            .Should().Throw<InvalidOperationException>().WithMessage("*no public constructor*");
        Content.Invoking(content => content.Create<AmbiguousContent, int>(1))
            .Should().Throw<InvalidOperationException>().WithMessage("*2 public constructors*");
    }

    [Test]
    public void Two_containers_each_build_with_their_own_services_through_the_same_signature()
    {
        using var other = new FacadeContainer(Register);

        for (var round = 0; round < 3; round++)
        {
            Content.Create<SampleContent, int>(round).Party.Should().BeSameAs(_container.Get<IPartyService>());
            other.Content.Create<SampleContent, int>(round).Party.Should().BeSameAs(other.Get<IPartyService>());
        }
    }

    [Test]
    public void The_test_factory_without_a_container_passes_null_services()
    {
        var made = TestContent.WithoutServices.Create<PartyValueContent, int>(4);

        made.Value.Should().Be(4);
        made.Party.Should().BeNull("a test without a container never reaches a service; a call would fail as it fails today");
    }

    [Test]
    public void The_content_check_reports_a_service_after_a_value_and_a_signature_that_does_not_resolve()
    {
        var factory = (ContentFactory)Content;
        var types = new[] { typeof(SampleContent), typeof(ValueBeforeServiceContent), typeof(AbstractContent), typeof(DerivedContent) };
        var signatures = new[]
        {
            new ContentSignature(typeof(SampleContent), typeof(int)),
            new ContentSignature(typeof(SampleContent), typeof(int), typeof(uint)),
            new ContentSignature(typeof(AmbiguousContent), typeof(int)),
            new ContentSignature(typeof(SampleContent), typeof(string)),
            new ContentSignature(typeof(SampleSingleton)),
        };

        var problems = factory.Check(types, signatures);

        problems.Should().HaveCount(4);
        problems.Should().Contain(problem => problem.StartsWith("ValueBeforeServiceContent(Int32, IPartyService) takes a registered type after a value"));
        problems.Should().Contain(problem => problem.StartsWith("Create<AmbiguousContent, Int32>") && problem.Contains("2 public constructors"));
        problems.Should().Contain(problem => problem.StartsWith("Create<SampleContent, String>") && problem.Contains("no public constructor"));
        problems.Should().Contain(problem => problem.StartsWith("Create<SampleSingleton>()") && problem.Contains("registers it"));
    }

    [Test]
    public void The_server_passes_its_own_content_check()
    {
        using var container = new FacadeContainer();
        var check = () => CompositionRoot.CheckContent(container.Get<IServiceProvider>());

        check.Should().NotThrow();
        CompositionRoot.ValidateServerServices().Should().BeGreaterThan(0);
    }
}
