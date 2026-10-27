using SWLOR.EngineSeamGenerator;

namespace SWLOR.NWN.API.Tests.Generator;

/// <summary>Small hand-written NWNX plugin wrapper sources, laid out like the real ones, for generator tests that run in memory.</summary>
internal static class PluginGeneratorFixtures
{
    public const string SamplePath = "SWLOR.NWN.API/NWNX/SamplePlugin.cs";
    public const string OtherPath = "SWLOR.NWN.API/NWNX/OtherPlugin.cs";
    public const string DataTypePath = "SWLOR.NWN.API/NWNX/JournalEntry.cs";

    public static SeamDefinition Sample => SeamDefinition.Plugin(SamplePath);

    public static SeamDefinition Other => SeamDefinition.Plugin(OtherPath);

    public const string SamplePlugin = """
        using SWLOR.Game.Server.Core.NWNX.Enum;

        namespace SWLOR.NWN.API.NWNX
        {
            /// <summary>A sample plugin.</summary>
            public static class SamplePlugin
            {
                /// <summary>Gets a count.</summary>
                /// <param name="oObject">The object.</param>
                public static int GetCount(uint oObject, int nOffset = 0)
                {
                    return global::NWN.Core.NWNX.SamplePlugin.GetCount(oObject, nOffset);
                }

                public static ChatChannel GetChannel()
                {
                    int result = global::NWN.Core.NWNX.SamplePlugin.GetChannel();
                    return (ChatChannel)result;
                }

                public static void Reset(
                    uint oObject,
                    bool bAll = false,
                    uint oOther = OBJECT_INVALID)
                {
                    global::NWN.Core.NWNX.SamplePlugin.Reset(
                        oObject,
                        bAll ? 1 : 0,
                        oOther);
                }

                public static SampleTime GetTime()
                {
                    var core = global::NWN.Core.NWNX.SamplePlugin.GetTime();
                    return new SampleTime
                    {
                        Day = core.nDay
                    };
                }

                /// <summary>A time.</summary>
                public struct SampleTime
                {
                    public int Day { get; set; }
                }

                public enum SampleKind
                {
                    One = 1,
                    Two = 2
                }
            }
        }
        """;

    public const string OtherPlugin = """
        namespace SWLOR.NWN.API.NWNX
        {
            public static class OtherPlugin
            {
                public static string GetName(uint oObject)
                {
                    return global::NWN.Core.NWNX.OtherPlugin.GetName(oObject);
                }
            }
        }
        """;

    /// <summary>A data type next to the plugins; it is not a plugin and the generator must leave it alone.</summary>
    public const string DataType = """
        namespace SWLOR.NWN.API.NWNX
        {
            public class JournalEntry
            {
                public string Name { get; set; }
            }
        }
        """;

    /// <summary>The NWScript fixture files plus two plugins and a data type, with the plugin files in their own folder.</summary>
    public static MemoryFileSystem Tree()
    {
        var files = GeneratorFixtures.Tree();
        files.WriteAllText(SamplePath, GeneratorFixtures.Lf(SamplePlugin) + "\n");
        files.WriteAllText(OtherPath, GeneratorFixtures.Lf(OtherPlugin) + "\n");
        files.WriteAllText(DataTypePath, GeneratorFixtures.Lf(DataType) + "\n");
        return files;
    }

    /// <summary>Generates every seam from a tree and applies the result to it, like a first run on the real repository.</summary>
    public static GeneratorOutput GenerateAllAndApply(MemoryFileSystem tree)
    {
        var output = SeamGenerator.GenerateAll(tree, tree);
        if (output.Errors.Count == 0) SeamGenerator.Apply(output, tree);
        return output;
    }
}
