namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Runs the generator against a repository root for one <see cref="GeneratorMode"/>. This is what the command line and the
/// "checked-in output is current" test both call.
/// </summary>
internal static class GeneratorRunner
{
    /// <summary>Success.</summary>
    public const int Ok = 0;

    /// <summary>The working tree differs from the generator's output (check and verify modes).</summary>
    public const int Differs = 1;

    /// <summary>The input could not be processed or the round trip failed; nothing was written.</summary>
    public const int Failed = 2;

    public static int Run(string root, GeneratorMode mode, string? revision, TextWriter log)
    {
        var definition = SeamDefinition.NWScript;
        var disk = new DiskFileSystem(root);

        try
        {
            GeneratorOutput output;
            if (mode == GeneratorMode.VerifyOrigin)
            {
                // The wrapper files as committed at the revision, with no previous output: the pristine hand-written code.
                output = SeamGenerator.Generate(new GitFileSystem(root, revision ?? throw new GeneratorException("A git revision is required.")),
                    new MemoryFileSystem(), definition);
            }
            else
            {
                output = SeamGenerator.Generate(disk, disk, definition);
            }

            if (output.Errors.Count == 0)
            {
                output.Errors.AddRange(CheckFixedPoint(output, definition));
            }

            if (output.Errors.Count > 0)
            {
                log.WriteLine("The generator found problems; nothing was written:");
                foreach (var error in output.Errors) log.WriteLine("  " + error);
                return Failed;
            }

            foreach (var line in output.Report) log.WriteLine(line);

            if (mode == GeneratorMode.Apply)
            {
                var changed = SeamGenerator.Apply(output, disk);
                log.WriteLine(changed == 0 ? "Nothing to change: the output is current." : $"Wrote or deleted {changed} files.");
                return Ok;
            }

            var differences = SeamGenerator.Compare(output, disk);
            if (differences.Count == 0)
            {
                log.WriteLine(mode == GeneratorMode.Check
                    ? "Check passed: the checked-in output is exactly what the generator produces."
                    : $"Verified: the working tree is exactly what the generator produces from {revision}.");
                return Ok;
            }

            log.WriteLine(mode == GeneratorMode.Check
                ? $"The checked-in output is out of date ({differences.Count} files). Run tools/Refactor/Apply-EngineSeam.ps1."
                : $"The working tree differs from what the generator produces from {revision} ({differences.Count} files).");
            foreach (var difference in differences.Take(50)) log.WriteLine("  " + difference);
            if (differences.Count > 50) log.WriteLine($"  ... and {differences.Count - 50} more");
            return Differs;
        }
        catch (GeneratorException exception)
        {
            log.WriteLine("The generator stopped: " + exception.Message);
            return Failed;
        }
    }

    /// <summary>
    /// Applies the output to an in-memory copy and runs the generator again on it. A correct run is a fixed point: the second
    /// run must reproduce the same files, otherwise a later run on the real tree would change it again.
    /// </summary>
    private static IEnumerable<string> CheckFixedPoint(GeneratorOutput output, SeamDefinition definition)
    {
        var memory = new MemoryFileSystem(output.Files);
        var second = SeamGenerator.Generate(memory, memory, definition);
        if (second.Errors.Count > 0)
        {
            return second.Errors.Select(e => "second pass: " + e);
        }

        return SeamGenerator.Compare(second, memory).Select(d => "second pass is not a fixed point: " + d);
    }
}
