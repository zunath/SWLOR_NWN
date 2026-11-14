using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using NUnit.Framework;

namespace SWLOR.Game.Host.Tests.Support;

/// <summary>
/// The Release build of the game (<c>SWLOR.Game.Server</c>) as the solution build leaves it, and the engine fakes
/// (<c>SWLOR.NWN.API.Testing</c>) built beside it. The host tests reference neither: a test copies the build output to a
/// folder of its own and loads it through the package's <c>ReloadHost</c>, as the server loads its game build.
/// </summary>
internal sealed class RealGameBuild
{
    /// <summary>The command that produces both outputs.</summary>
    public const string BuildCommand = "dotnet build SWLOR.Game.Server.sln -c Release -p:RunPostBuildEvent=Never";

    private const string GameAssemblyFile = "SWLOR.Game.Server.dll";
    private const string FakesAssemblyFile = "SWLOR.NWN.API.Testing.dll";
    private const string ManifestResourceName = "ScriptHandlerOrder.txt";

    private RealGameBuild(string outputDirectory, string fakesAssemblyPath)
    {
        OutputDirectory = outputDirectory;
        FakesAssemblyPath = fakesAssemblyPath;
    }

    /// <summary>The game's Release output folder.</summary>
    public string OutputDirectory { get; }

    /// <summary>The Release build of <c>SWLOR.NWN.API.Testing</c>.</summary>
    public string FakesAssemblyPath { get; }

    /// <summary>
    /// Finds both Release outputs from the repository root above the test's folder. Fails the test, naming the build
    /// command, when either is missing: the test must never pass without loading the real build.
    /// </summary>
    public static RealGameBuild Locate()
    {
        var root = RepositoryRoot();
        var output = Path.Combine(root, "SWLOR.Game.Server", "bin", "Release", "net10.0");
        var fakes = Path.Combine(root, "SWLOR.NWN.API.Testing", "bin", "Release", "net10.0", FakesAssemblyFile);

        foreach (var required in new[] { Path.Combine(output, GameAssemblyFile), fakes })
        {
            if (!File.Exists(required))
                Assert.Fail($"The Release build is missing ({required}). Build it from the repository root first: {BuildCommand}");
        }

        return new RealGameBuild(output, fakes);
    }

    /// <summary>Copies the whole output folder into <paramref name="destination"/> and returns the game assembly's path there.</summary>
    public string CopyTo(string destination)
    {
        foreach (var file in Directory.EnumerateFiles(OutputDirectory, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(OutputDirectory, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        return Path.Combine(destination, GameAssemblyFile);
    }

    /// <summary>
    /// Every script name the build's embedded handler manifest (<c>Core/ScriptHandlerOrder.txt</c>) lists, with whether it
    /// lists a handler for it, read from the assembly's metadata without loading it.
    /// </summary>
    public static IReadOnlyList<ManifestScript> ManifestScripts(string gameAssemblyPath)
    {
        var scripts = new List<ManifestScript>();
        string? current = null;
        var handlers = 0;
        foreach (var raw in ReadManifest(gameAssemblyPath).Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith("==", StringComparison.Ordinal))
                continue;

            if (!char.IsWhiteSpace(line[0]))
            {
                if (current != null)
                    scripts.Add(new ManifestScript(current, handlers > 0));
                current = line.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0];
                handlers = 0;
            }
            else if (line.TrimStart().StartsWith("action ", StringComparison.Ordinal) ||
                     line.TrimStart().StartsWith("conditional ", StringComparison.Ordinal))
            {
                handlers++;
            }
        }

        if (current != null)
            scripts.Add(new ManifestScript(current, handlers > 0));
        return scripts;
    }

    private static string ReadManifest(string gameAssemblyPath)
    {
        using var stream = File.OpenRead(gameAssemblyPath);
        using var image = new PEReader(stream);
        var metadata = image.GetMetadataReader();
        foreach (var handle in metadata.ManifestResources)
        {
            var resource = metadata.GetManifestResource(handle);
            if (!resource.Implementation.IsNil || metadata.GetString(resource.Name) != ManifestResourceName)
                continue;

            var directory = image.PEHeaders.CorHeader!.ResourcesDirectory;
            var block = image.GetSectionData(directory.RelativeVirtualAddress);
            var reader = block.GetReader((int)resource.Offset, directory.Size - (int)resource.Offset);
            var length = reader.ReadInt32();
            return Encoding.UTF8.GetString(reader.ReadBytes(length));
        }

        throw new InvalidOperationException($"{gameAssemblyPath} embeds no {ManifestResourceName}.");
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        }

        throw new InvalidOperationException($"No SWLOR.Game.Server.sln above {TestContext.CurrentContext.TestDirectory}.");
    }
}
