using System.Diagnostics;

namespace SWLOR.ProcgenReview;

/// <summary>
/// Packs staged GFF resource files into an ERF-family container via the repo's existing nwn_erf CLI
/// tool (tools/SWLOR.CLI/nwn_erf.exe), shared by both container kinds Program.cs produces from the
/// SAME staged resource set (see EmitArea/ConvertJsonToGff):
///   - the toolset-reviewable review MODULE ("SWLOR Procgen Review.mod"): every staged resource,
///     including module.ifo -- the "MOD " erf-type header.
///   - the standalone-area ERF export: only .are/.git/.gic resources (real Aurora toolset area
///     resource types 2012/2023/2078), deliberately excluding module.ifo -- an ERF meant for the
///     Aurora toolset's File -> Import must not carry module-only resources -- the "ERF " erf-type
///     header (nwn_erf's own default).
///
/// nwn_erf already writes a spec-correct ERF V1.0 container (160-byte header, 24-byte key entries,
/// 8-byte resource entries, resource type codes from its own built-in extension table) for whichever
/// --erf-type is requested, so this wraps that tool rather than re-implementing the binary format:
/// one less place for the container layout to drift from a real NWN implementation. Since both
/// containers are built from the identical staged files, an .are payload packed into either one is
/// byte-for-byte identical.
/// </summary>
internal static class ErfPacker
{
    /// <summary>Packs every file in <paramref name="stage"/> as a "MOD " container -- the toolset-
    /// reviewable review module, including module.ifo.</summary>
    public static void PackMod(string stage, string erfTool, string outPath) =>
        Pack(stage, erfTool, outPath, "MOD", extensions: null);

    /// <summary>Packs only the .are/.git/.gic files in <paramref name="stage"/> as an "ERF " container
    /// -- the standalone-area export, with no module.ifo.</summary>
    public static void PackErf(string stage, string erfTool, string outPath) =>
        Pack(stage, erfTool, outPath, "ERF", extensions: AreaResourceExtensions);

    private static readonly string[] AreaResourceExtensions = { ".are", ".git", ".gic" };

    private static void Pack(string stage, string erfTool, string outputPath, string erfType, string[] extensions)
    {
        if (File.Exists(outputPath))
            File.Delete(outputPath);

        var files = Directory.GetFiles(stage)
            .Where(f => extensions == null || extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        if (files.Count == 0)
            throw new InvalidOperationException($"no matching staged resources to pack into '{outputPath}'.");

        // Pass bare filenames (not the full staged temp-directory paths) and run nwn_erf with its
        // working directory set to the stage folder. The review module's area count has grown across
        // every onboarding wave (base-game pilot/Wave-2/Wave-3 exterior/Wave-4 fcx01, plus one showcase
        // per PaletteVariant profile); by this wave the full-path entry list crossed the ~32K-character
        // Windows command-line limit and Process.Start began failing with Win32 error 206
        // ("filename or extension is too long") even though every prior wave packed fine -- the temp
        // stage directory's own absolute path was repeated once per file, dwarfing the actual filenames.
        // Relative filenames keep this well under the limit regardless of how many more tilesets/
        // showcases get added later.
        var entries = string.Join(" ", files.Select(f => $"\"{Path.GetFileName(f)}\""));
        Run(erfTool, $"-e {erfType} -c -f \"{outputPath}\" {entries}", workingDirectory: stage);
    }

    /// <summary>Shared external-tool runner for both the erf packer above and Program.cs's
    /// JSON-to-GFF conversion step (nwn_gff.exe) -- one process-launch helper for the whole tool.</summary>
    public static void Run(string exe, string arguments, string workingDirectory = null)
    {
        var psi = new ProcessStartInfo(exe, arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        if (workingDirectory != null)
            psi.WorkingDirectory = workingDirectory;
        using var proc = Process.Start(psi);
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"{Path.GetFileName(exe)} failed: {stderr}");
    }
}
