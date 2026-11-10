using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The namespace guard (P2-Z2, owner decision 2026-11-06): no namespace of <c>SWLOR.Game.Server</c> hides a type, no leaf
/// segment sits under two parents, and no <c>Service.&lt;Name&gt;Service</c> support namespace is left, with no exceptions
/// (<see cref="NamespaceRules"/>). The fixture tests prove each check fails on a violation and passes on the right shape.
/// </summary>
/// <remarks>
/// The fixtures use made-up service and namespace names (<c>Widget</c>), never real ones or the old names that
/// <c>tools/Refactor/Move-SupportNamespaces.ps1</c> renames: the script rewrites every text file of the repository, so a real
/// old name here would be rewritten by its next run.
/// </remarks>
[TestFixture]
public class NamespaceGuardTests
{
    private const string Root = "SWLOR.Game.Server";

    [Test]
    public void No_namespace_of_the_server_hides_a_type_repeats_a_leaf_or_is_a_support_namespace()
    {
        var problems = NamespaceRules.Violations(ArchitectureProject.Facts.Types.Select(type => type.Key));

        if (problems.Count > 0)
            Assert.Fail($"The namespaces of SWLOR.Game.Server break the namespace guard ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }

    [Test]
    public void The_guard_reads_the_namespaces_of_the_real_server()
    {
        var types = ArchitectureProject.Facts.Types.Select(type => type.Key).ToList();

        types.Should().Contain(type => type.Namespace == Root + ".Domain.Combat", "the server has domain namespaces for the guard to read");
        types.Should().Contain(type => type.Namespace == Root + ".Domain.Crafting.Minigame", "topic namespaces are read too");
    }

    [Test]
    public void Distinct_namespaces_and_type_names_pass()
    {
        NamespaceRules.Violations(
            [
                Type("Domain.Widgets", "WidgetService"),
                Type("Domain.Widgets", "WidgetResult"),
                Type("Domain.Widgets.Assembly", "AssemblyBoard"),
                Type("Domain.Widgets.Assembly.Gears", "GearSetup"),
                Type("Feature.WidgetDefinition", "WidgetDefinitionBase"),
                Type("Core", "NWNEventHandler"),
                Type("Service", "ColorToken"),
                new TypeKey(string.Empty, "<Module>"),
                new TypeKey(Root + ".Domain.Widgets", "<>c")
            ]).Should().BeEmpty();
    }

    [Test]
    public void A_segment_named_like_a_type_is_reported()
    {
        var problems = NamespaceRules.Violations(
            [
                Type("Domain.Widgets", "WidgetResult"),
                Type("Service", "Widgets"),
                Type("Feature.WidgetDefinition.Gadget", "Gadget")
            ]);

        problems.Should().Contain(problem => problem.StartsWith($"Namespace {Root}.Domain.Widgets: the segment 'Widgets' is also a type ({Root}.Service.Widgets)"));
        problems.Should().Contain(problem => problem.StartsWith($"Namespace {Root}.Feature.WidgetDefinition.Gadget: the segment 'Gadget' is also a type"));
        problems.Should().HaveCount(2, "only the namespaces whose segments name a type are reported");
    }

    [Test]
    public void A_segment_named_like_a_type_is_reported_in_an_enclosing_namespace_without_types()
    {
        var problems = NamespaceRules.Violations([Type("Domain.Widgets.Topic", "WidgetResult"), Type("Native", "Widgets")]);

        problems.Should().Contain(problem => problem.StartsWith($"Namespace {Root}.Domain.Widgets: the segment 'Widgets'"));
        problems.Should().Contain(problem => problem.StartsWith($"Namespace {Root}.Domain.Widgets.Topic: the segment 'Widgets'"));
        problems.Should().HaveCount(2);
    }

    [Test]
    public void A_leaf_under_two_parents_is_reported()
    {
        var problems = NamespaceRules.Violations(
            [
                Type("Core", "NWNEventHandler"),
                Type("Domain.Widgets.Core", "WidgetStore"),
                Type("Domain.Widgets.Assembly.Gears", "GearBoard"),
                Type("Domain.Widgets.Gears", "GearSite")
            ]);

        problems.Should().Contain($"The namespace segment 'Core' is the leaf of 2 namespaces ({Root}.Core, {Root}.Domain.Widgets.Core): give each a name of its own.");
        problems.Should().Contain(problem => problem.StartsWith("The namespace segment 'Gears' is the leaf of 2 namespaces"));
        problems.Should().HaveCount(2);
    }

    [Test]
    public void A_support_namespace_is_reported()
    {
        var problems = NamespaceRules.Violations(
            [
                Type("Service.WidgetService", "WidgetStyle"),
                Type("Service.WidgetService.Assembly", "AssemblyBoard"),
                Type("Service", "Log")
            ]);

        problems.Should().Contain($"Namespace {Root}.Service.WidgetService is a Service.<Name>Service support namespace: put the types in Domain.<Domain>.");
        problems.Should().Contain($"Namespace {Root}.Service.WidgetService.Assembly is a Service.<Name>Service support namespace: put the types in Domain.<Domain>.");
        problems.Should().HaveCount(2, "Service itself, which holds the remaining static helpers, is not a support namespace");
    }

    private static TypeKey Type(string relativeNamespace, string name) => new(Root + "." + relativeNamespace, name);
}
