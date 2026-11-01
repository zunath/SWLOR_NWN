using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.EngineTests.Definitions;

public static partial class MigrationEngineTests
{
    [EngineTest("Private production pelvis items match native conversion field for field",
        Category = "PelvisProductionParity", TimeoutSeconds = 900f)]
    public static async Task PelvisProductionCopyMatchesNative(EngineTestContext ctx)
    {
        var directory = Environment.GetEnvironmentVariable("SWLOR_PELVIS_CORPUS_DIRECTORY");
        if (string.IsNullOrEmpty(directory))
            ctx.Skip("Supply an isolated private directory of copied character files to compare production pelvis items.");
        var dataType = typeof(PelvisAppearanceMigration).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData");
        object Parse(byte[] data) => Activator.CreateInstance(dataType,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { data }, null);
        object Root(object document) => dataType.GetProperty("Root", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(document);
        static IEnumerable<object> Fields(object node) => ((IEnumerable)node.GetType().GetField("Fields").GetValue(node)).Cast<object>();
        static string Name(object field) => (string)field.GetType().GetProperty("Name").GetValue(field);
        static byte[] Data(object field) => (byte[])field.GetType().GetField("Data").GetValue(field);
        static IEnumerable<object> Children(object field) =>
            ((IEnumerable)field.GetType().GetField("Children").GetValue(field))?.Cast<object>() ?? Enumerable.Empty<object>();
        IEnumerable<object> LegacyItems(object node)
        {
            var fields = Fields(node).ToArray();
            var pelvis = fields.FirstOrDefault(field => Name(field) == "xArmorPart_Pelvi") ??
                         fields.FirstOrDefault(field => Name(field) == "ArmorPart_Pelvis");
            if (fields.Any(field => Name(field) == "BaseItem") && pelvis != null && BitConverter.ToUInt32(Data(pelvis), 0) == 35)
                yield return node;
            foreach (var field in fields)
            foreach (var child in Children(field))
            foreach (var item in LegacyItems(child))
                yield return item;
        }
        string Canonical(object node) => node.GetType().GetField("Type").GetValue(node) + "{" +
            string.Join(";", Fields(node).OrderBy(Name).Select(field => Name(field) + ":" +
                field.GetType().GetField("Type").GetValue(field) + ":" +
                (Data(field) == null ? "" : Convert.ToHexString(Data(field))) + "[" +
                string.Join(",", Name(field) == "VarTable" ? Children(field).Select(Canonical).OrderBy(value => value) :
                    Children(field).Select(Canonical)) + "]")) + "}";
        string ReadNative(string saved, out string normalized)
        {
            var item = Deserialize(ctx, saved);
            try
            {
                normalized = ObjectPlugin.Serialize(item);
                return Canonical(Root(Parse(Convert.FromBase64String(normalized))));
            }
            finally { Invoke("MigrationObject", "DestroyTemporaryObject", item); }
        }
        var actor = ctx.SpawnCreature("civilian");
        await ctx.WaitFrameAsync();
        var files = 0;
        var items = 0;
        foreach (var path in Directory.EnumerateFiles(directory, "*.bic"))
        {
            files++;
            var document = Parse(File.ReadAllBytes(path));
            foreach (var node in LegacyItems(Root(document)))
            {
                items++;
                var caseNumber = items;
                var legacy = (string)Invoke("StoredObjectData", "ItemToUti", node);
                await ctx.ExecuteInCreatureContextAsync(actor, () =>
                {
                    var native = Deserialize(ctx, legacy);
                    string expected;
                    try
                    {
                        ctx.Assert(PelvisAppearanceMigration.MigrateObject(native), $"Production case {caseNumber}: native conversion changed pelvis35");
                        expected = ObjectPlugin.Serialize(native);
                    }
                    finally { Invoke("MigrationObject", "DestroyTemporaryObject", native); }
                    ctx.Assert(PelvisAppearanceMigration.MigrateSavedObject(legacy, out var saved), $"Production case {caseNumber}: saved conversion changed pelvis35");
                    // Compare complete decoded native saves, not binary layout or a selected-field projection.
                    var expectedFields = ReadNative(expected, out var normalizedExpected);
                    var savedFields = ReadNative(saved, out var normalizedSaved);
                    if (expectedFields != savedFields)
                    {
                        // Keep failure payloads only beside the explicitly supplied private corpus, never in logs.
                        File.WriteAllBytes(Path.Combine(directory, $"native-result-{caseNumber:0000}.uti"), Convert.FromBase64String(normalizedExpected));
                        File.WriteAllBytes(Path.Combine(directory, $"saved-result-{caseNumber:0000}.uti"), Convert.FromBase64String(normalizedSaved));
                    }
                    ctx.Assert(expectedFields == savedFields, $"Production case {caseNumber}: complete decoded native/saved results agree (variable-table entry order ignored)");
                    ctx.Assert(!PelvisAppearanceMigration.MigrateSavedObject(saved, out _), $"Production case {caseNumber}: repeat conversion is a no-op");
                });
                await ctx.WaitFrameAsync();
            }
        }
        ctx.Assert(items > 0, "The supplied private corpus contains affected production items");
        ctx.SetResultDetail($"Compared {items} pelvis35 items from {files} private copied character files; complete decoded engine saves agree and every saved conversion is idempotent.");
    }
}
