using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Fixtures.Rules;
using SWLOR.Architecture.Tests.Fixtures.Rules.Composition;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>Proves the fact scanner on the small types in <c>Fixtures/Rules</c>, one mechanism at a time.</summary>
[TestFixture]
public class AssemblyFactScannerTests
{
    private static readonly AssemblyFacts Facts = AssemblyFactScanner.Scan(typeof(AssemblyFactScannerTests).Assembly.Location);

    [Test]
    public void Describes_the_kind_of_each_top_level_type()
    {
        var types = Facts.Types.ToDictionary(type => type.Key);

        types[KeyOf(typeof(StaticStateHolder))].IsStatic.Should().BeTrue();
        types[KeyOf(typeof(CleanInstanceService))].IsStatic.Should().BeFalse();
        types[KeyOf(typeof(CleanInstanceService))].IsInterface.Should().BeFalse();
        types[KeyOf(typeof(IFixtureContract))].IsInterface.Should().BeTrue();
        types[KeyOf(typeof(FixtureImplementation))].IsSealed.Should().BeTrue();
        types[KeyOf(typeof(FixtureImplementation))].Interfaces.Should().Equal(KeyOf(typeof(IFixtureContract)).ToString());
    }

    [Test]
    public void Lists_every_static_field_with_its_kind()
    {
        var fields = Facts.StaticFields.Where(field => field.Owner == KeyOf(typeof(StaticStateHolder))).ToDictionary(field => field.Name);

        fields.Keys.Should().BeEquivalentTo("Limit", "Cache", "Counter");
        fields["Limit"].IsLiteral.Should().BeTrue();
        fields["Cache"].IsInitOnly.Should().BeTrue();
        fields["Cache"].IsLiteral.Should().BeFalse();
        fields["Counter"].IsInitOnly.Should().BeFalse();
    }

    [Test]
    public void Leaves_out_the_static_fields_the_compiler_generates()
    {
        // CleanInstanceService has a static lambda, so the compiler adds a nested <>c class with a static cache field.
        Facts.StaticFields.Where(field => field.Owner == KeyOf(typeof(CleanInstanceService)))
            .Select(field => field.Name)
            .Should().Equal("Name");
    }

    [Test]
    public void Lists_the_methods_that_carry_the_event_handler_attribute()
    {
        var handlers = Facts.Handlers
            .Where(handler => handler.Owner.Namespace == typeof(HandlerOnFacade).Namespace)
            .Select(handler => $"{handler.Owner.Name}.{handler.Method}");

        handlers.Should().BeEquivalentTo("HandlerOnInstance.Handle", "HandlerOnFacade.Handle");
    }

    [TestCase(typeof(ProviderUser), "System.IServiceProvider", "ProviderUser::Get", TestName = "Call on IServiceProvider")]
    [TestCase(typeof(ExtensionUser), "Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions", "ExtensionUser::Get", TestName = "Dependency-injection extension method")]
    [TestCase(typeof(ParameterUser), "System.IServiceProvider", "ParameterUser::.ctor", TestName = "Constructor parameter")]
    [TestCase(typeof(ActivatorUser), "Microsoft.Extensions.DependencyInjection.ActivatorUtilities", "ActivatorUser::Make", TestName = "ActivatorUtilities")]
    public void Records_the_use_of_a_type_from_another_assembly(Type source, string externalType, string member)
    {
        var reference = Facts.ExternalReferences.Single(r => r.Source == KeyOf(source) && r.ExternalType == externalType);

        reference.Members.Should().Contain(member);
    }

    [Test]
    public void Records_no_container_type_for_a_type_that_uses_none()
    {
        Facts.ExternalReferences
            .Where(reference => reference.Source == KeyOf(typeof(CleanUser)))
            .Select(reference => reference.ExternalType)
            .Should().NotContain(name => name == "System.IServiceProvider" || name.StartsWith("Microsoft.Extensions.DependencyInjection", StringComparison.Ordinal));
    }

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);
}
