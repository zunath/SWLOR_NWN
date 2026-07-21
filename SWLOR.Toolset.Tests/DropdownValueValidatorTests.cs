using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;
using SWLOR.Toolset.Domain.Gff;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Coverage for <see cref="DropdownValueValidator"/>, the guard that stops a blueprint from
    /// opening when a dropdown-backed field holds a value its lookup cannot represent. The point is
    /// data safety: a blank combo box hides the real value, so the editor reports it and leaves the
    /// file alone instead.
    /// </summary>
    public class DropdownValueValidatorTests
    {
        private static JsonGffDocument Document(params (string Field, int Value)[] fields)
        {
            var sb = new StringBuilder("{\"__data_type\":\"UTP \"");
            foreach (var (field, value) in fields)
                sb.Append($",\"{field}\":{{\"type\":\"dword\",\"value\":{value}}}");
            sb.Append('}');
            return JsonGffDocument.Parse(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        /// <summary>A one-field schema so each test states exactly what it exercises.</summary>
        private static EditorSchema SchemaWithDropdown(string fieldName, string lookupKey) => new()
        {
            ResourceType = Domain.Workspace.ResourceType.Utp,
            Groups = new[]
            {
                new FieldGroup
                {
                    Title = "Test",
                    Fields = new[]
                    {
                        new FieldDescriptor
                        {
                            Label = "Appearance", FieldName = fieldName,
                            Kind = EditorKind.TwoDaDropdown, LookupKey = lookupKey
                        }
                    }
                }
            }
        };

        private static Func<string, IReadOnlyCollection<long>> Ids(params long[] ids) => _ => ids;

        [Test]
        public void ValuePresentInTheLookup_IsNotReported()
        {
            var unresolved = DropdownValueValidator.FindUnresolved(
                Document(("Appearance", 1007)),
                SchemaWithDropdown("Appearance", "placeables"),
                Ids(1005, 1007, 1009));

            unresolved.Should().BeEmpty();
        }

        [Test]
        public void ValueMissingFromTheLookup_IsReportedWithEnoughDetailToAct()
        {
            var unresolved = DropdownValueValidator.FindUnresolved(
                Document(("Appearance", 1005)),
                SchemaWithDropdown("Appearance", "placeables"),
                Ids(1007, 1009));

            unresolved.Should().ContainSingle();
            var problem = unresolved[0];
            problem.Value.Should().Be(1005, "the report must name the value that could not be shown");
            problem.FieldName.Should().Be("Appearance");
            problem.Label.Should().Be("Appearance");
            problem.LookupKey.Should().Be("placeables");
        }

        [Test]
        public void UnavailableLookup_IsNotTreatedAsAnError()
        {
            // No ids means the 2DA/service did not load. The editor already degrades those fields to
            // a numeric box that shows and preserves the raw value, so there is nothing to protect
            // against - blocking here would make a missing data file lock every blueprint.
            var unresolved = DropdownValueValidator.FindUnresolved(
                Document(("Appearance", 1005)),
                SchemaWithDropdown("Appearance", "placeables"),
                Ids());

            unresolved.Should().BeEmpty();
        }

        [Test]
        public void FieldAbsentFromTheDocument_IsNotReported()
        {
            var unresolved = DropdownValueValidator.FindUnresolved(
                Document(("SomethingElse", 3)),
                SchemaWithDropdown("Appearance", "placeables"),
                Ids(1007));

            unresolved.Should().BeEmpty("a field with no stored value has nothing that could be lost");
        }

        [Test]
        public void NonDropdownFields_AreIgnored()
        {
            var schema = new EditorSchema
            {
                ResourceType = Domain.Workspace.ResourceType.Utp,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Test",
                        Fields = new[]
                        {
                            new FieldDescriptor
                            {
                                Label = "Charges", FieldName = "Charges", Kind = EditorKind.Integer
                            }
                        }
                    }
                }
            };

            DropdownValueValidator.FindUnresolved(Document(("Charges", 999)), schema, Ids(1))
                .Should().BeEmpty();
        }

        [Test]
        public void EveryUnresolvedFieldIsReported_NotJustTheFirst()
        {
            var schema = new EditorSchema
            {
                ResourceType = Domain.Workspace.ResourceType.Utc,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Test",
                        Fields = new[]
                        {
                            new FieldDescriptor { Label = "A", FieldName = "FieldA", Kind = EditorKind.TwoDaDropdown, LookupKey = "k" },
                            new FieldDescriptor { Label = "B", FieldName = "FieldB", Kind = EditorKind.TwoDaDropdown, LookupKey = "k" }
                        }
                    }
                }
            };

            var unresolved = DropdownValueValidator.FindUnresolved(
                Document(("FieldA", 50), ("FieldB", 51)), schema, Ids(1));

            unresolved.Should().HaveCount(2, "the user needs the full list to fix the data in one pass");
            unresolved.Select(u => u.Value).Should().BeEquivalentTo(new long[] { 50, 51 });
        }

        [TestCase(65535, GffFieldType.Word, TestName = "Word 'none' (65535)")]
        [TestCase(255, GffFieldType.Byte, TestName = "Byte 'none' (255)")]
        [TestCase(-1, GffFieldType.Int, TestName = "signed 'unset' (-1)")]
        public void UnsetSentinelValues_AreNotReportedAsBroken(int value, GffFieldType fieldType)
        {
            // 45 real creature blueprints store SoundSetFile = 65535, which means "no sound set".
            // Refusing to open those would be a defect in this guard, not in the data.
            var schema = new EditorSchema
            {
                ResourceType = Domain.Workspace.ResourceType.Utc,
                Groups = new[]
                {
                    new FieldGroup
                    {
                        Title = "Test",
                        Fields = new[]
                        {
                            new FieldDescriptor
                            {
                                Label = "Sound Set", FieldName = "SoundSetFile",
                                Kind = EditorKind.TwoDaDropdown, FieldType = fieldType,
                                LookupKey = "soundsets"
                            }
                        }
                    }
                }
            };

            var document = JsonGffDocument.Parse(Encoding.UTF8.GetBytes(
                $"{{\"__data_type\":\"UTC \",\"SoundSetFile\":{{\"type\":\"int\",\"value\":{value}}}}}"));

            DropdownValueValidator.FindUnresolved(document, schema, Ids(1, 2, 3))
                .Should().BeEmpty("an explicit 'nothing assigned' marker is valid data, not a broken row reference");
        }

        [Test]
        public void ZeroIsNotTreatedAsUnset()
        {
            // Row 0 is a real row in every table wired to a dropdown, so a 0 that is missing from
            // the lookup is a genuine problem and must still be reported.
            DropdownValueValidator.FindUnresolved(
                    Document(("Appearance", 0)),
                    SchemaWithDropdown("Appearance", "placeables"),
                    Ids(1, 2))
                .Should().ContainSingle().Which.Value.Should().Be(0);
        }

        [Test]
        public void RealCorpusPlaceable_WithAnEmpty2DaRow_WouldBeBlocked()
        {
            // placeables.2da row 1005 is entirely "****" - no label, no model. A blueprint pointing
            // at such a row is exactly the case this guard exists for.
            var unresolved = DropdownValueValidator.FindUnresolved(
                Document(("Appearance", 1005)),
                UtpSchema.Build(),
                key => key == "placeables" ? new long[] { 1004, 1007 } : Array.Empty<long>());

            unresolved.Should().ContainSingle().Which.Value.Should().Be(1005);
        }
    }
}
