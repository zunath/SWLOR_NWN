using FluentAssertions;
using Nwn.Authoring.Areas.Properties;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Schemas;

namespace SWLOR.Toolset.Tests;

[TestFixture]
public sealed class AreaPropertySchemaTests
{
    [Test]
    public void SwlorAreaSchemaProjectsTheSharedNativeFieldCatalog()
    {
        var shared = AreaPropertyCatalog.Groups.SelectMany(group => group.Fields).ToArray();
        var projected = AreSchema.Build().AllFields.ToArray();

        projected.Select(field => field.FieldName).Should().Equal(shared.Select(field => field.NativeName));
        foreach (var field in shared)
        {
            var descriptor = projected.Single(candidate => candidate.FieldName == field.NativeName);
            descriptor.FieldType.Should().Be(field.FieldType);
            descriptor.IsReadOnly.Should().Be(field.IsReadOnly);
        }

        AreSchema.Build().HasVarTable.Should().BeFalse();
    }
}
