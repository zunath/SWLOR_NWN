using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Domain.Editors.Schemas;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Guards the schema half of the dropdown wiring: every field declared as a
    /// <see cref="EditorKind.TwoDaDropdown"/> must name a lookup key, and that key must be one the
    /// <see cref="LookupKeys"/> vocabulary actually declares.
    /// </summary>
    /// <remarks>
    /// This cannot reach the other half - whether LookupOptionProvider has a switch case for the key
    /// - because the provider lives in the Avalonia app project, which the test project does not
    /// reference (deliberately, to keep tests headless). That gap is real: door types, placeable
    /// appearances and ambient sounds all rendered as raw numeric ids for a while because their
    /// schemas and services both existed but the provider had no case, and nothing failed. The
    /// provider carries a remark pointing here.
    /// </remarks>
    public class SchemaLookupKeyTests
    {
        private static IEnumerable<(string Schema, FieldDescriptor Field)> AllFields()
        {
            var schemas = new (string Name, EditorSchema Schema)[]
            {
                (nameof(UtcSchema), UtcSchema.Build()),
                (nameof(UtiSchema), UtiSchema.Build()),
                (nameof(UtpSchema), UtpSchema.Build()),
                (nameof(UtdSchema), UtdSchema.Build()),
                (nameof(UtwSchema), UtwSchema.Build()),
                (nameof(UtsSchema), UtsSchema.Build()),
                (nameof(UttSchema), UttSchema.Build()),
                (nameof(UtmSchema), UtmSchema.Build()),
                (nameof(AreSchema), AreSchema.Build())
            };

            foreach (var (name, schema) in schemas)
            foreach (var field in schema.AllFields)
                yield return (name, field);
        }

        private static IReadOnlySet<string> DeclaredLookupKeys =>
            typeof(LookupKeys)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .Select(f => (string)f.GetRawConstantValue()!)
                .ToHashSet(StringComparer.Ordinal);

        [Test]
        public void EveryDropdownField_NamesADeclaredLookupKey()
        {
            var declared = DeclaredLookupKeys;
            var offenders = new List<string>();

            foreach (var (schema, field) in AllFields())
            {
                if (field.Kind != EditorKind.TwoDaDropdown)
                    continue;

                if (string.IsNullOrEmpty(field.LookupKey))
                    offenders.Add($"{schema}.{field.FieldName} ('{field.Label}') is a dropdown with no LookupKey");
                else if (!declared.Contains(field.LookupKey))
                    offenders.Add($"{schema}.{field.FieldName} uses undeclared LookupKey '{field.LookupKey}'");
            }

            offenders.Should().BeEmpty(
                "a dropdown whose key is missing or unknown silently renders as a raw numeric box:\n"
                + string.Join("\n", offenders));
        }

        [Test]
        public void FieldsFromTheReportedScreenshots_AreDropdownsNotNumericBoxes()
        {
            // These are the fields that were showing raw ids in the editor. Pinning them by name
            // keeps a future schema edit from quietly reverting one to a numeric box.
            var expected = new (string Schema, string Field, string LookupKey)[]
            {
                (nameof(UtcSchema), "Gender", LookupKeys.Gender),
                (nameof(UtcSchema), "Phenotype", LookupKeys.Phenotype),
                (nameof(UtcSchema), "SoundSetFile", LookupKeys.SoundSets),
                (nameof(UtiSchema), "BaseItem", LookupKeys.BaseItems),
                (nameof(UttSchema), "Type", LookupKeys.TriggerTypes)
            };

            var actual = AllFields().ToList();

            foreach (var (schema, fieldName, lookupKey) in expected)
            {
                var match = actual.FirstOrDefault(f => f.Schema == schema && f.Field.FieldName == fieldName);
                match.Field.Should().NotBeNull($"{schema} should declare a '{fieldName}' field");
                match.Field.Kind.Should().Be(EditorKind.TwoDaDropdown, $"{schema}.{fieldName} must resolve to a name");
                match.Field.LookupKey.Should().Be(lookupKey);
            }
        }

        [Test]
        public void UtpSchema_DoesNotDeclareAppearance_BecauseThePlaceableEditorHasAModelGrid()
        {
            // The placeable's appearance used to be the seventh pinned dropdown above. It is not a
            // schema field any more, and that is deliberate rather than a regression: a combo box
            // cannot represent a value it has no option for, so the 2,982 blueprints whose row is
            // blank in placeables.2da could not be opened at all. The Appearance tab shows the model
            // grid, keeps an unknown row exactly as stored, and marks it.
            //
            // Re-declaring it here would restore that block, so this test fails if anyone does.
            UtpSchema.Build().AllFields
                .Should().NotContain(field => field.FieldName == "Appearance",
                    "placeable appearance is edited by the model grid on the Appearance tab");
        }

        [Test]
        public void UtdSchema_DoesNotDeclareAppearanceFields_BecauseTheDoorEditorHasACombinedModelGrid()
        {
            // Appearance = 0 is a control value meaning that GenericType_New owns the model. The
            // dedicated door gallery understands and preserves that paired representation, while
            // two independent schema dropdowns do not. Re-declaring either field here would make
            // the pre-open validator reject ordinary doors before the gallery can show them.
            UtdSchema.Build().AllFields
                .Should().NotContain(
                    field => field.FieldName == "Appearance" ||
                             field.FieldName == "GenericType_New",
                    "door appearance is edited by the combined model grid");
        }
    }
}
