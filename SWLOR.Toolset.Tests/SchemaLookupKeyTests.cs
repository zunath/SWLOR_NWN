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
                (nameof(UtdSchema), "GenericType_New", LookupKeys.DoorTypes),
                (nameof(UtpSchema), "Appearance", LookupKeys.Placeables),
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
    }
}
