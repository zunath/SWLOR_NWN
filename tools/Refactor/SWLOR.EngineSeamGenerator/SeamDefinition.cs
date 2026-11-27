namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Names and paths of one engine seam: the hand-written static wrapper class, and the generated interface,
/// pass-through implementation, runtime and fake that sit behind it. Paths are relative to the repository root and
/// use '/' separators.
/// </summary>
/// <remarks>
/// A seam either covers a whole folder of wrapper files that share one class (NWScript: the class is partial and the
/// generated interface, pass-through and fake are partial too, one part per wrapper file; the three roots are folders),
/// or one wrapper file that holds the whole class (an NWNX plugin: the three roots and the runtime are single files).
/// </remarks>
internal sealed record SeamDefinition(
    string Namespace,
    string StaticClassName,
    IReadOnlyList<string> StaticClassModifiers,
    string InterfaceName,
    string PassThroughName,
    string RuntimeName,
    string FakeName,
    string FakeNamespace,
    string FakeBaseName,
    IReadOnlyList<string> SourceDirectories,
    string? SourceFile,
    string InterfaceRoot,
    string PassThroughRoot,
    string RuntimeFile,
    string FakeRoot,
    IReadOnlyList<string> GeneratedRoots,
    bool AllowDataOnlyNestedTypes)
{
    /// <summary>The folder of the NWNX plugin wrappers in SWLOR.NWN.API.</summary>
    public const string PluginSourceDirectory = "SWLOR.NWN.API/NWNX";

    /// <summary>The folders that hold only generated plugin seam files (the interfaces, pass-throughs and runtimes; the fakes).</summary>
    public static IReadOnlyList<string> PluginGeneratedRoots { get; } = new[]
    {
        "SWLOR.NWN.API/Generated/NWNX",
        "SWLOR.NWN.API.Testing/Generated/NWNX"
    };

    /// <summary>The NWScript wrappers in SWLOR.NWN.API.</summary>
    public static SeamDefinition NWScript { get; } = new(
        Namespace: "SWLOR.NWN.API.NWScript",
        StaticClassName: "NWScript",
        StaticClassModifiers: new[] { "public", "partial" },
        InterfaceName: "INWScript",
        PassThroughName: "NwnCoreNWScript",
        RuntimeName: "NWScriptRuntime",
        FakeName: "FakeNWScript",
        FakeNamespace: "SWLOR.NWN.API.Testing",
        FakeBaseName: "FakeEngine",
        SourceDirectories: new[] { "SWLOR.NWN.API/NWScript", "SWLOR.NWN.API/NWScriptCustom" },
        SourceFile: null,
        InterfaceRoot: "SWLOR.NWN.API/Generated/INWScript",
        PassThroughRoot: "SWLOR.NWN.API/Generated/NwnCoreNWScript",
        RuntimeFile: "SWLOR.NWN.API/Generated/NWScriptRuntime.cs",
        FakeRoot: "SWLOR.NWN.API.Testing/Generated/FakeNWScript",
        GeneratedRoots: new[]
        {
            "SWLOR.NWN.API/Generated/INWScript",
            "SWLOR.NWN.API/Generated/NwnCoreNWScript",
            "SWLOR.NWN.API.Testing/Generated/FakeNWScript"
        },
        AllowDataOnlyNestedTypes: false);

    /// <summary>
    /// The seam of one NWNX plugin wrapper, e.g. `SWLOR.NWN.API/NWNX/ChatPlugin.cs` gives `IChatPlugin`, `NwnCoreChatPlugin`,
    /// `ChatPluginRuntime` and `FakeChatPlugin`, written to `Generated/NWNX/ChatPlugin/`.
    /// </summary>
    public static SeamDefinition Plugin(string sourcePath)
    {
        var name = Path.GetFileNameWithoutExtension(sourcePath);
        var apiFolder = $"SWLOR.NWN.API/Generated/NWNX/{name}";
        var testingFolder = $"SWLOR.NWN.API.Testing/Generated/NWNX/{name}";

        return new SeamDefinition(
            Namespace: "SWLOR.NWN.API.NWNX",
            StaticClassName: name,
            StaticClassModifiers: new[] { "public", "static" },
            InterfaceName: "I" + name,
            PassThroughName: "NwnCore" + name,
            RuntimeName: name + "Runtime",
            FakeName: "Fake" + name,
            FakeNamespace: "SWLOR.NWN.API.Testing",
            FakeBaseName: "FakeEngine",
            SourceDirectories: Array.Empty<string>(),
            SourceFile: sourcePath,
            InterfaceRoot: $"{apiFolder}/I{name}.cs",
            PassThroughRoot: $"{apiFolder}/NwnCore{name}.cs",
            RuntimeFile: $"{apiFolder}/{name}Runtime.cs",
            FakeRoot: $"{testingFolder}/Fake{name}.cs",
            GeneratedRoots: PluginGeneratedRoots,
            AllowDataOnlyNestedTypes: true);
    }

    /// <summary>True when the seam covers one wrapper file and each generated root is a single file.</summary>
    public bool IsSingleFile => SourceFile != null;

    /// <summary>The seams to generate: NWScript, then one per NWNX plugin file (a top-level `*Plugin.cs` in the NWNX folder).</summary>
    public static IReadOnlyList<SeamDefinition> Discover(IGeneratorFileSystem files)
    {
        var definitions = new List<SeamDefinition> { NWScript };
        definitions.AddRange(files.ListFiles(PluginSourceDirectory, recursive: false)
            .Where(path => path.EndsWith("Plugin.cs", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(Plugin));
        return definitions;
    }

    /// <summary>The path of the generated part for a source file, below a generated root.</summary>
    public string PartPath(string root, string sourcePath)
    {
        // A single-file seam writes its one file; the root is that file.
        if (IsSingleFile) return root;

        // sourcePath is "SWLOR.NWN.API/NWScript/X.cs"; keep the part below the project folder, e.g. "NWScript/X.cs".
        var firstSlash = sourcePath.IndexOf('/');
        return root + "/" + sourcePath[(firstSlash + 1)..];
    }

    /// <summary>The existing generated files of one root: the files below a folder, or the single file itself when it exists.</summary>
    public IReadOnlyList<string> ExistingParts(IGeneratorFileSystem files, string root)
    {
        if (!IsSingleFile) return files.ListFiles(root, recursive: true);

        return files.ReadAllText(root) == null ? Array.Empty<string>() : new[] { root };
    }
}
