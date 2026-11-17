using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Tests.Composition.ContentSamples;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>The composition entry points refuse missing arguments, and the content factory covers its remaining shapes.</summary>
[TestFixture]
public class CompositionGuardTests
{
    private FacadeContainer _container = null!;

    private IContentFactory Content => _container.Content;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [Test]
    public void TheCompositionRootRefusesAMissingHostOrProvider()
    {
        ((Action)(() => CompositionRoot.Build(null!))).Should().Throw<ArgumentNullException>().WithParameterName("host");
        ((Action)(() => CompositionRoot.CheckContent(null!))).Should().Throw<ArgumentNullException>().WithParameterName("provider");
        ((Action)(() => CompositionRoot.InstallForTests(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => CompositionRoot.RegisterHostedServices(new ServiceCollection(), null!, null!)))
            .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void TheContainerHostRefusesAMissingType()
    {
        var host = new ContainerHost();
        host.Build(services => services.AddSingleton<SampleSingleton>());

        ((Action)(() => host.IsRegistered(null!))).Should().Throw<ArgumentNullException>().WithParameterName("type");
        host.IsRegistered(typeof(SampleSingleton)).Should().BeTrue();
        host.IsRegistered(typeof(string)).Should().BeFalse("a value-like type is never a registration");
    }

    private static readonly Type RegisteredTypes = typeof(CompositionRoot).Assembly.GetType("SWLOR.Game.Server.Composition.RegisteredTypes")!;

    private static Func<Type, bool> PredicateOf(Type sourceType, object? source)
    {
        var method = RegisteredTypes.GetMethod("Of", new[] { sourceType })!;
        try
        {
            return (Func<Type, bool>)method.Invoke(null, new[] { source })!;
        }
        catch (System.Reflection.TargetInvocationException exception)
        {
            throw exception.InnerException!;
        }
    }

    [Test]
    public void TheRegisteredTypePredicatesRefuseAMissingSource()
    {
        ((Action)(() => PredicateOf(typeof(IServiceProvider), null))).Should().Throw<ArgumentNullException>();
        ((Action)(() => PredicateOf(typeof(IServiceCollection), null))).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void ContainerHelpersAreNeverRegistrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SampleSingleton>();
        services.AddSingleton(typeof(IServiceProviderIsKeyedService), new object());
        var registered = PredicateOf(typeof(IServiceCollection), services);

        registered(typeof(SampleSingleton)).Should().BeTrue();
        registered(typeof(IServiceProviderIsKeyedService)).Should().BeFalse();
        registered(typeof(IServiceProvider)).Should().BeFalse();
        registered(typeof(IEnumerable<SampleSingleton>)).Should().BeFalse();
        registered(null!).Should().BeFalse();
    }

    [Test]
    public void TheContentFactoryRefusesMissingArguments()
    {
        var factory = (ContentFactory)Content;

        ((Action)(() => factory.Create<SampleSingleton>(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => factory.Check(null!, Array.Empty<ContentSignature>()))).Should().Throw<ArgumentNullException>();
        ((Action)(() => factory.Check(Array.Empty<Type>(), null!))).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void FiveValuesBuild()
    {
        Content.Create<FiveValueContent, int, int, int, int, string>(1, 2, 3, 4, "five").Values.Should().Be("1,2,3,4,five");
    }

    [Test]
    public void AnOptionalValueDefaultingToNullOrANullableEnumMemberIsFilledIn()
    {
        var made = Content.Create<NullDefaultContent, int>(3);

        made.Potency.Should().Be(3);
        made.Note.Should().BeNull();
        made.Kind.Should().Be(SampleKind.First);
    }

    [Test]
    public void TheContentCheckNamesATypeTheFactoryCanNeverBuild()
    {
        var factory = (ContentFactory)Content;

        var problems = factory.Check(Array.Empty<Type>(), new[]
        {
            new ContentSignature(typeof(List<>)),
            new ContentSignature(typeof(SampleKind))
        });

        problems.Should().HaveCount(2);
        problems.Should().OnlyContain(problem => problem.Contains("not a class the factory can build"));
    }
}
