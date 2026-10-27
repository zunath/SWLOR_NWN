namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Names and paths of one engine seam: the hand-written static wrapper class, and the generated interface,
/// pass-through implementation, runtime and fake that sit behind it. Paths are relative to the repository root and
/// use '/' separators.
/// </summary>
internal sealed record SeamDefinition(
    string Namespace,
    string StaticClassName,
    string InterfaceName,
    string PassThroughName,
    string RuntimeName,
    string FakeName,
    string FakeNamespace,
    string FakeBaseName,
    IReadOnlyList<string> SourceDirectories,
    string InterfaceRoot,
    string PassThroughRoot,
    string RuntimeFile,
    string FakeRoot)
{
    /// <summary>The NWScript wrappers in SWLOR.NWN.API.</summary>
    public static SeamDefinition NWScript { get; } = new(
        Namespace: "SWLOR.NWN.API.NWScript",
        StaticClassName: "NWScript",
        InterfaceName: "INWScript",
        PassThroughName: "NwnCoreNWScript",
        RuntimeName: "NWScriptRuntime",
        FakeName: "FakeNWScript",
        FakeNamespace: "SWLOR.NWN.API.Testing",
        FakeBaseName: "FakeEngine",
        SourceDirectories: new[] { "SWLOR.NWN.API/NWScript", "SWLOR.NWN.API/NWScriptCustom" },
        InterfaceRoot: "SWLOR.NWN.API/Generated/INWScript",
        PassThroughRoot: "SWLOR.NWN.API/Generated/NwnCoreNWScript",
        RuntimeFile: "SWLOR.NWN.API/Generated/NWScriptRuntime.cs",
        FakeRoot: "SWLOR.NWN.API.Testing/Generated/FakeNWScript");

    /// <summary>The path of the generated part for a source file, below a generated root.</summary>
    public string PartPath(string root, string sourcePath)
    {
        // sourcePath is "SWLOR.NWN.API/NWScript/X.cs"; keep the part below the project folder, e.g. "NWScript/X.cs".
        var firstSlash = sourcePath.IndexOf('/');
        return root + "/" + sourcePath[(firstSlash + 1)..];
    }
}
