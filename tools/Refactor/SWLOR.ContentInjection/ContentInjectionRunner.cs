namespace SWLOR.ContentInjection;

/// <summary>
/// Runs the tool over a tree (what the command line and the tests call). Apply and check need the branch: a tree without
/// <c>SWLOR.Game.Server/Core/IContentFactory.cs</c> (master, the parity copy) is left alone. Reverse runs on any folder.
/// </summary>
internal static class ContentInjectionRunner
{
    /// <summary>Success (and, for a check, nothing to change).</summary>
    public const int Ok = 0;

    /// <summary>A check found something <see cref="RunMode.Apply"/> would change.</summary>
    public const int Differs = 1;

    /// <summary>The input could not be processed; nothing was written.</summary>
    public const int Failed = 2;

    /// <summary>Something needs a person (the refusal list); every file without a refusal was converted.</summary>
    public const int Refused = 3;

    /// <summary>The branch marker: a tree without it is not the branch, and apply and check change nothing there.</summary>
    public const string MarkerPath = "SWLOR.Game.Server/Core/IContentFactory.cs";

    public static int Run(RunOptions options, TextWriter log)
    {
        try
        {
            return options.Mode == RunMode.Reverse ? Reverse(options, log) : ApplyOrCheck(options, log);
        }
        catch (ContentInjectionException exception)
        {
            log.WriteLine("Nothing was written: " + exception.Message);
            return Failed;
        }
    }

    private static int ApplyOrCheck(RunOptions options, TextWriter log)
    {
        var root = options.Root;
        if (!File.Exists(Path.Combine(root, MarkerPath)))
        {
            log.WriteLine($"Skipped: this tree has no {MarkerPath}, so it is not the branch (master, or the parity copy of it).");
            return Ok;
        }

        var data = ContentInjectionData.Load(options.DataPath);
        var names = NameTable.Load(root);
        var files = SourceTree.Load(root);
        var converted = files.Where(file => data.IsConverted(file.RelativePath)).ToList();
        var analysis = ContentAnalyzer.Analyze(converted, names);
        var refusals = new List<Refusal>(analysis.Refusals);
        TypeDirectory? directory = null;
        TypeDirectory Types() => directory ??= new TypeDirectory(files.Where(file => file.Kind == ProjectKind.Server));

        var changes = new List<(string Path, byte[] Bytes)>();
        var texts = new Dictionary<TreeFile, string>();
        if (converted.Count > 0 || names.RegisteredSingletons.Count > 0)
        {
            foreach (var file in files)
            {
                var result = ContentRewriter.Apply(file, analysis, converted.Contains(file), Types);
                refusals.AddRange(result.Refusals);
                if (string.Equals(result.Text, file.Text, StringComparison.Ordinal))
                    continue;

                texts[file] = result.Text;
                changes.Add((Path.Combine(root, file.RelativePath), file.Source.Encode(result.Text)));
                log.WriteLine($"{file.RelativePath}: {result.Edits} edit(s)");
            }
        }

        var singletons = analysis.Singletons.Where(type => type.Refusals.Count == 0).Select(type => type.FullName)
            .Concat(names.RegisteredSingletons.Where(name => analysis.Classes.Any(type => type.FullName == name && type.Refusals.Count > 0)))
            .ToList();
        var signatures = files.Where(file => file.Kind == ProjectKind.Server)
            .SelectMany(file => RegistrationsWriter.Signatures(file, texts.TryGetValue(file, out var text) ? text : file.Text, analysis, Types, refusals))
            .ToList();
        var registrationsPath = Path.Combine(root, NameTable.RegistrationsPath);
        var registrations = File.Exists(registrationsPath) ? SourceFile.Read(registrationsPath) : new SourceFile(string.Empty, string.Empty);
        var written = RegistrationsWriter.Write(singletons, signatures, registrations.Text.Length == 0 ? "\n" : registrations.LineEnding);
        if (!string.Equals(written, registrations.Text, StringComparison.Ordinal))
        {
            changes.Add((registrationsPath, registrations.Encode(written)));
            log.WriteLine($"{NameTable.RegistrationsPath}: {singletons.Count} singleton(s), {signatures.Distinct().Count()} signature(s)");
        }

        if (options.Mode == RunMode.Apply)
        {
            foreach (var (path, bytes) in changes)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, bytes);
            }
        }

        foreach (var refusal in refusals.Distinct().OrderBy(refusal => refusal.Path, StringComparer.Ordinal).ThenBy(refusal => refusal.Line))
            log.WriteLine("REFUSED " + refusal);

        var verb = options.Mode == RunMode.Apply ? "Changed" : "Would change";
        log.WriteLine($"{verb} {changes.Count} file(s) for {converted.Count} converted file(s), {singletons.Count} content singleton(s), " +
                      $"{analysis.MadeWithServices.Count} made type(s) with services; {refusals.Distinct().Count()} refusal(s).");
        if (refusals.Count > 0)
            return Refused;
        return options.Mode == RunMode.Check && changes.Count > 0 ? Differs : Ok;
    }

    private static int Reverse(RunOptions options, TextWriter log)
    {
        if (!Directory.Exists(options.Root))
            throw new ContentInjectionException($"The folder {options.Root} does not exist.");

        var files = 0;
        var rewrites = 0;
        foreach (var path in SourceTree.Files(options.Root))
        {
            var source = SourceFile.Read(path);
            var (text, count) = ReverseRewriter.Apply(source.Text);
            if (count == 0)
                continue;

            File.WriteAllBytes(path, source.Encode(text));
            files++;
            rewrites += count;
            log.WriteLine($"{SourceTree.Relative(options.Root, path)}: {count} creation site(s) and fixture propert(ies) reversed");
        }

        log.WriteLine($"Reversed {files} file(s): {rewrites} change(s).");
        return Ok;
    }
}
