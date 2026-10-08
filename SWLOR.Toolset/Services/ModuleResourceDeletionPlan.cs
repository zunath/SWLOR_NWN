using Nwn.Authoring.Editing;
using Nwn.Authoring.Documents.Native;

namespace SWLOR.Toolset.Services;

/// <summary>
/// Captures the resource generations and root locks accepted when a delete confirmation is opened.
/// </summary>
public sealed class ModuleResourceDeletionPlan
{
    internal ModuleResourceDeletionPlan(
        string moduleRoot,
        ResourceType type,
        string resRef,
        FileTransactionPlan fileTransactionPlan,
        IReadOnlyList<string> lockRoots,
        string? ifoPath,
        byte[]? expectedIfo,
        bool removesAreaRegistration)
    {
        ModuleRoot = moduleRoot;
        Type = type;
        ResRef = resRef;
        FileTransactionPlan = fileTransactionPlan;
        LockRoots = lockRoots;
        IfoPath = ifoPath;
        ExpectedIfo = expectedIfo;
        RemovesAreaRegistration = removesAreaRegistration;
    }

    internal string ModuleRoot { get; }

    public ResourceType Type { get; }

    public string ResRef { get; }

    /// <summary>The resource files that existed when the confirmation was opened.</summary>
    public IReadOnlyList<string> ExistingFileNames => FileTransactionPlan.ExistingDeletionPaths
        .Select(path => Path.GetFileName(path)!)
        .ToArray();

    /// <summary>Whether committing this plan also removes one or more module.ifo area entries.</summary>
    public bool RemovesAreaRegistration { get; }

    internal FileTransactionPlan FileTransactionPlan { get; }
    internal IReadOnlyList<string> LockRoots { get; }
    internal string? IfoPath { get; }
    internal byte[]? ExpectedIfo { get; }

}
