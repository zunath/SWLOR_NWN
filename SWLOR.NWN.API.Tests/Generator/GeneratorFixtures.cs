using SWLOR.EngineSeamGenerator;

namespace SWLOR.NWN.API.Tests.Generator;

/// <summary>Small hand-written wrapper sources, laid out like the real ones, for generator tests that run in memory.</summary>
internal static class GeneratorFixtures
{
    public const string FirstPath = "SWLOR.NWN.API/NWScript/SampleFunctions.cs";
    public const string SecondPath = "SWLOR.NWN.API/NWScriptCustom/CustomFunctions.cs";

    public static SeamDefinition Definition => SeamDefinition.NWScript;

    public const string First = """
        using SWLOR.NWN.API.Engine;
        using Alignment = SWLOR.NWN.API.NWScript.Enum.Alignment;

        namespace SWLOR.NWN.API.NWScript
        {
            public partial class NWScript
            {
                public const int SampleConstant = 5;

                public static uint OBJECT_SELF => global::NWN.Core.NWScript.OBJECT_SELF;

                /// <summary>
                /// Gets the tag.
                /// </summary>
                /// <param name="oObject">The object.</param>
                public static string GetTag(uint oObject)
                {
                    return global::NWN.Core.NWScript.GetTag(oObject);
                }

                /// <summary>Clears actions.</summary>
                public static void ClearAllActions(bool nClearCombatState = false, uint oObject = OBJECT_INVALID)
                {
                    if (oObject == OBJECT_INVALID)
                        oObject = OBJECT_SELF;
                    global::NWN.Core.NWScript.ClearAllActions(nClearCombatState ? 1 : 0, oObject);
                }

                public static bool HasTag(uint oObject, params string[] tags)
                {
                    var tag = GetTag(oObject);
                    return tags.Contains(tag);
                }
            }
        }
        """;

    public const string Second = """
        namespace SWLOR.NWN.API.NWScript
        {
            public partial class NWScript
            {
                public static void DestroyThing(uint oThing, float fDelay = 0.0f)
                {
                    global::NWN.Core.NWScript.DestroyObject(oThing, fDelay);
                    ExecuteScript("thing_destroyed", oThing);
                }
            }
        }
        """;

    /// <summary>The two sample files as a source tree with Windows line endings in the first file.</summary>
    public static MemoryFileSystem Tree(bool crlf = false)
    {
        var first = Lf(First) + "\n";
        return new MemoryFileSystem(new Dictionary<string, string>
        {
            [FirstPath] = crlf ? first.Replace("\n", "\r\n") : first,
            [SecondPath] = Lf(Second) + "\n"
        });
    }

    /// <summary>Raw string literals take the line endings of this source file, so normalize them before choosing an ending.</summary>
    public static string Lf(string text) => text.Replace("\r\n", "\n");

    /// <summary>Generates from a tree and applies the result to it, like a first run on the real repository.</summary>
    public static GeneratorOutput GenerateAndApply(MemoryFileSystem tree)
    {
        var output = SeamGenerator.Generate(tree, tree, Definition);
        if (output.Errors.Count == 0) SeamGenerator.Apply(output, tree);
        return output;
    }
}
