using Nwn.Hosting.Contracts.Generations;

namespace SWLOR.Game.Host.Reload;

/// <summary>
/// Where a development reload's files are, following the hosting package's conventions (<c>DevelopmentReloadFiles</c>,
/// <c>DevelopmentReloadRequest</c>): each candidate build is published once into its own folder,
/// <c>&lt;generation root&gt;/generations/&lt;id&gt;/</c>, never over a build the server has loaded; the signal file names the
/// candidate by its id; the host answers each attempt with a receipt named after the same id. On a server the generation
/// root is the host's own folder (<c>dotnet/</c>, which the server mounts read only) and the reload folder is
/// <c>reload/</c> in the server home, which the server writes its receipts to.
/// </summary>
public sealed class ReloadLayout
{
    /// <summary>The game assembly in every candidate folder.</summary>
    public const string GameAssemblyFileName = "SWLOR.Game.Server.dll";

    /// <summary>The signal file in the reload folder: a <c>DevelopmentReloadRequest</c> that names the candidate.</summary>
    public const string SignalFileName = "signal.json";

    /// <summary>The folder of the receipts in the reload folder, one <c>&lt;id&gt;.json</c> per attempt.</summary>
    public const string ReceiptsDirectoryName = "receipts";

    /// <summary>The host's folder in a server home.</summary>
    public const string DotnetDirectoryName = "dotnet";

    /// <summary>The reload folder in a server home.</summary>
    public const string ReloadDirectoryName = "reload";

    /// <param name="generationRoot">The folder that holds <c>generations/</c>: the host's own folder on a server.</param>
    /// <param name="reloadDirectory">The folder that holds the signal file and the receipts.</param>
    public ReloadLayout(string generationRoot, string reloadDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(generationRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(reloadDirectory);
        GenerationRoot = Path.GetFullPath(generationRoot);
        ReloadDirectory = Path.GetFullPath(reloadDirectory);
    }

    /// <summary>The layout of a server home as the host reads it by default: <c>dotnet/</c> and <c>reload/</c>.</summary>
    public static ReloadLayout ForServerHome(string serverHome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverHome);
        return new ReloadLayout(Path.Combine(serverHome, DotnetDirectoryName), Path.Combine(serverHome, ReloadDirectoryName));
    }

    /// <summary>The folder that holds <c>generations/</c>.</summary>
    public string GenerationRoot { get; }

    /// <summary>The folder that holds the signal file and the receipts.</summary>
    public string ReloadDirectory { get; }

    /// <summary>The folder every candidate is published under.</summary>
    public string GenerationsDirectory => Path.Combine(GenerationRoot, DevelopmentReloadRequest.GenerationsDirectoryName);

    /// <summary>The signal file.</summary>
    public string SignalPath => Path.Combine(ReloadDirectory, SignalFileName);

    /// <summary>The folder of the receipts.</summary>
    public string ReceiptsDirectory => Path.Combine(ReloadDirectory, ReceiptsDirectoryName);

    /// <summary>The folder candidate <paramref name="generationId"/> is published into.</summary>
    public string CandidateDirectory(Guid generationId) => Path.Combine(GenerationsDirectory, IdText(generationId));

    /// <summary>The game assembly of candidate <paramref name="generationId"/>, where the package's request convention puts it.</summary>
    public string CandidateAssembly(Guid generationId) =>
        Path.Combine(GenerationRoot, new DevelopmentReloadRequest(DevelopmentReloadFiles.CurrentFormatVersion, generationId)
            .GetAssemblyRelativePath(GameAssemblyFileName));

    /// <summary>The receipt of the attempt to activate candidate <paramref name="generationId"/>.</summary>
    public string ReceiptPath(Guid generationId) => Path.Combine(ReceiptsDirectory, IdText(generationId) + ".json");

    /// <summary>How a generation id is written in folder and file names.</summary>
    public static string IdText(Guid generationId) => generationId.ToString("N");
}
