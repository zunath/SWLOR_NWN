using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Fixtures.Sources;
using SWLOR.Architecture.Tests.Fixtures.Targets;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// Proves the IL scanner on small fixture types (<c>Fixtures/</c>) that reach their targets one mechanism at a time.
/// </summary>
[TestFixture]
public class AssemblyScannerTests
{
    private static readonly TypeReferenceGraph Graph = AssemblyScanner.Scan(typeof(AssemblyScannerTests).Assembly.Location);

    [TestCase(typeof(MethodTarget), "Source::CallsMethod", TestName = "Call of a static method")]
    [TestCase(typeof(FieldTarget), "Source::ReadsStaticField", TestName = "Read of a static field")]
    [TestCase(typeof(ConstructedTarget), "Source::Constructs", TestName = "Object creation")]
    [TestCase(typeof(CastTarget), "Source::Casts", TestName = "Type test")]
    [TestCase(typeof(TypeofTarget), "Source::TakesTypeof", TestName = "typeof")]
    [TestCase(typeof(ParameterTarget), "Source::TakesParameter", TestName = "Parameter type")]
    [TestCase(typeof(ReturnTarget), "Source::Returns", TestName = "Return type")]
    [TestCase(typeof(LocalTarget), "Source::UsesLocal", TestName = "Local variable type")]
    [TestCase(typeof(GenericArgumentTarget), "Source::UsesGenericArgument", TestName = "Generic argument of a framework type")]
    [TestCase(typeof(GenericMethodTarget), "Source::UsesGenericMethod", TestName = "Generic argument of a framework method")]
    [TestCase(typeof(ArrayTarget), "Source::UsesArray", TestName = "Array element type")]
    [TestCase(typeof(LambdaTarget), "Source::UsesLambda", TestName = "Lambda body is attributed to its method")]
    [TestCase(typeof(AsyncTarget), "Source::UsesAsync", TestName = "Async state machine is attributed to its method")]
    [TestCase(typeof(CatchTarget), "Source::Catches", TestName = "Catch type")]
    [TestCase(typeof(NestedCallTarget), "Source+Nested::Inner", TestName = "Nested type is attributed to its top-level type")]
    public void Records_the_reference_from_the_declaring_top_level_type(Type target, string member)
    {
        var members = MembersReferencing(typeof(Source), target);

        members.Should().Contain(member);
    }

    [Test]
    public void Records_a_base_type()
    {
        MembersReferencing(typeof(DerivedSource), typeof(BaseTarget)).Should().Contain("DerivedSource::(type)");
    }

    [Test]
    public void Records_an_implemented_interface()
    {
        MembersReferencing(typeof(DerivedSource), typeof(InterfaceTarget)).Should().Contain("DerivedSource::(type)");
    }

    [Test]
    public void Records_a_custom_attribute()
    {
        MembersReferencing(typeof(AttributedSource), typeof(AttributeTarget)).Should().Contain("AttributedSource::(type)");
    }

    [Test]
    public void Records_a_generic_constraint()
    {
        MembersReferencing(typeof(ConstrainedSource<>), typeof(ConstraintTarget)).Should().Contain("ConstrainedSource::(type)");
    }

    [Test]
    public void Records_a_field_type()
    {
        MembersReferencing(typeof(FieldHolder), typeof(FieldTypeTarget)).Should().Contain("FieldHolder::Held");
    }

    [TestCase(typeof(AttributeTypeofTarget), TestName = "typeof argument of a custom attribute")]
    [TestCase(typeof(AttributeNamedTypeTarget), TestName = "typeof in a named argument of a custom attribute")]
    [TestCase(typeof(AttributeArrayTypeTarget), TestName = "typeof in an array argument of a custom attribute")]
    [TestCase(typeof(AttributeGenericTypeTarget), TestName = "Generic argument of a typeof argument")]
    [TestCase(typeof(AttributeEnumTarget), TestName = "Enum type of an enum argument of a custom attribute")]
    public void Records_the_types_named_by_custom_attribute_arguments(Type target)
    {
        MembersReferencing(typeof(TypeArgumentSource), target).Should().Contain("TypeArgumentSource::(type)");
    }

    [TestCase(typeof(ConstantTarget), TestName = "An inlined enum constant leaves no reference")]
    [TestCase(typeof(NamedOnlyTarget), TestName = "A type named only in a string or nameof leaves no reference")]
    [TestCase(typeof(UnrelatedTarget), TestName = "A type nothing uses leaves no reference")]
    public void Records_no_reference_without_a_use_in_the_code(Type target)
    {
        // Only Source is checked: this test class names the targets in its own [TestCase] attributes.
        Graph.References.Keys.Should().NotContain(key => key.Source.Equals(KeyOf(typeof(Source))) && key.Target.Equals(KeyOf(target)));
    }

    [Test]
    public void A_type_never_references_itself()
    {
        Graph.References.Keys.Should().NotContain(key => key.Source.Equals(key.Target));
    }

    [Test]
    public void Every_reference_points_at_a_type_of_the_scanned_assembly()
    {
        var known = Graph.Types.ToHashSet();

        Graph.References.Keys.Select(key => key.Target).Distinct().Should().OnlyContain(target => known.Contains(target));
    }

    [Test]
    public void Lists_every_top_level_type_without_the_generic_arity()
    {
        Graph.Types.Should().Contain(KeyOf(typeof(ConstrainedSource<>)));
        Graph.Types.Should().Contain(KeyOf(typeof(Source)));
        Graph.Types.Should().NotContain(type => type.Name.Contains('+') || type.Name.Contains('`'));
    }

    private static IReadOnlySet<string> MembersReferencing(Type source, Type target)
    {
        Graph.References.Should().ContainKey((KeyOf(source), KeyOf(target)), "{0} should reference {1}", source.Name, target.Name);
        return Graph.References[(KeyOf(source), KeyOf(target))];
    }

    private static TypeKey KeyOf(Type type)
    {
        var name = type.Name;
        var tick = name.IndexOf('`');
        return new TypeKey(type.Namespace ?? string.Empty, tick < 0 ? name : name[..tick]);
    }
}
