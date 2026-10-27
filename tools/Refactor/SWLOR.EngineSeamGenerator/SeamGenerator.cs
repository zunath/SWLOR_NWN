namespace SWLOR.EngineSeamGenerator;

/// <summary>
/// Builds the whole engine seam in memory: the interface, the pass-through implementation holding the original bodies, the
/// runtime, the recording fake, and the rewritten static wrappers. Nothing is written here; <see cref="Apply"/> and
/// <see cref="Compare"/> do that, and only when <see cref="GeneratorOutput.Errors"/> is empty.
/// </summary>
internal static class SeamGenerator
{
    public static GeneratorOutput Generate(
        IGeneratorFileSystem sourceFiles,
        IGeneratorFileSystem previousOutput,
        SeamDefinition definition,
        Action<Dictionary<string, string>>? beforeVerify = null)
    {
        var output = new GeneratorOutput();
        output.GeneratedRoots.AddRange(new[] { definition.InterfaceRoot, definition.PassThroughRoot, definition.FakeRoot });

        var sources = ReadSources(sourceFiles, definition, output.Errors);
        var existing = ReadPreviousPassThrough(previousOutput, definition, output.Errors);
        if (output.Errors.Count > 0) return output;

        var plan = Planner.Plan(sources, existing, definition, output.Errors);
        if (output.Errors.Count > 0) return output;

        foreach (var source in sources)
        {
            var text = StaticWrapperRewriter.Rewrite(source, definition);
            output.Files[source.Path] = text;
            if (text != source.Text) output.RewrittenWrappers.Add(source.Path);

            if (source.Members.Count == 0) continue;

            output.Files[definition.PartPath(definition.InterfaceRoot, source.Path)] = InterfaceEmitter.Emit(source, definition);
            output.Files[definition.PartPath(definition.PassThroughRoot, source.Path)] = PassThroughEmitter.Emit(source, plan, definition);
            output.Files[definition.PartPath(definition.FakeRoot, source.Path)] = FakeEmitter.Emit(source, definition);
        }

        output.Files[definition.RuntimeFile] = RuntimeEmitter.Emit(definition);

        beforeVerify?.Invoke(output.Files);
        RoundTripVerifier.Verify(plan, output.Files, definition, output.Errors);
        if (output.Errors.Count == 0) WriteReport(plan, output, definition);
        return output;
    }

    /// <summary>Writes every changed file and deletes stale generated parts. Does nothing when the output has errors.</summary>
    public static int Apply(GeneratorOutput output, IGeneratorFileSystem target)
    {
        if (output.Errors.Count > 0) throw new InvalidOperationException("Refusing to write: the generator found errors.");

        var changed = 0;
        foreach (var (path, content) in output.Files.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            var current = target.ReadAllText(path);
            if (current != null && WrapperParser.Normalize(current) == WrapperParser.Normalize(content)) continue;

            target.WriteAllText(path, content);
            changed++;
        }

        foreach (var stale in FindStale(output, target))
        {
            target.Delete(stale);
            changed++;
        }

        return changed;
    }

    /// <summary>Lists the files that differ from the output, are missing, or are stale. Empty means the target is current.</summary>
    public static List<string> Compare(GeneratorOutput output, IGeneratorFileSystem target)
    {
        var differences = new List<string>();
        foreach (var (path, content) in output.Files.OrderBy(f => f.Key, StringComparer.Ordinal))
        {
            var current = target.ReadAllText(path);
            if (current == null) differences.Add($"missing: {path}");
            else if (WrapperParser.Normalize(current) != WrapperParser.Normalize(content)) differences.Add($"differs: {path}");
        }

        differences.AddRange(FindStale(output, target).Select(path => $"stale: {path}"));
        return differences;
    }

    private static List<string> FindStale(GeneratorOutput output, IGeneratorFileSystem target)
    {
        return output.GeneratedRoots
            .SelectMany(root => target.ListFiles(root, recursive: true))
            .Where(path => !output.Files.ContainsKey(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private static List<WrapperSourceFile> ReadSources(IGeneratorFileSystem files, SeamDefinition definition, List<string> errors)
    {
        var paths = definition.SourceDirectories
            .SelectMany(directory => files.ListFiles(directory, recursive: false))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        if (paths.Count == 0)
        {
            errors.Add($"No wrapper sources found in {string.Join(", ", definition.SourceDirectories)}.");
        }

        var sources = new List<WrapperSourceFile>();
        foreach (var path in paths)
        {
            var text = files.ReadAllText(path) ?? throw new GeneratorException($"{path} could not be read.");
            sources.Add(WrapperParser.Parse(path, text, definition, errors));
        }

        return sources;
    }

    private static Dictionary<string, GeneratedMember> ReadPreviousPassThrough(IGeneratorFileSystem files, SeamDefinition definition, List<string> errors)
    {
        var members = new Dictionary<string, GeneratedMember>(StringComparer.Ordinal);
        foreach (var path in files.ListFiles(definition.PassThroughRoot, recursive: true))
        {
            var text = files.ReadAllText(path) ?? throw new GeneratorException($"{path} could not be read.");
            foreach (var member in GeneratedTypeReader.Read(path, text, definition.PassThroughName, errors))
            {
                if (!members.TryAdd(member.Key, member))
                {
                    errors.Add($"{path}: {member.Key} appears in more than one previous {definition.PassThroughName} part.");
                }
            }
        }

        return members;
    }

    private static void WriteReport(GenerationPlan plan, GeneratorOutput output, SeamDefinition definition)
    {
        var members = plan.Sources.SelectMany(s => s.Members).ToList();
        var methods = members.Count(m => m.Kind == MemberKind.Method);
        var properties = members.Count(m => m.Kind == MemberKind.Property);
        var constants = plan.Sources.SelectMany(s => s.Constants.Select(c => (s.Path, Name: c))).ToList();
        var withMembers = plan.Sources.Count(s => s.Members.Count > 0);

        output.Report.Add($"Wrapper files: {plan.Sources.Count} ({withMembers} with members).");
        output.Report.Add($"Members behind {definition.InterfaceName}: {members.Count} ({methods} methods, {properties} properties).");
        output.Report.Add($"Left static: {constants.Count} public constants (an interface cannot hold them): " +
                          string.Join(", ", constants.Select(c => c.Name)) + ".");
        output.Report.Add($"Bodies moved by this run: {plan.MovedThisRun.Count}; already moved: {members.Count - plan.MovedThisRun.Count}; " +
                          $"dropped (no wrapper any more): {plan.Dropped.Count}" +
                          (plan.Dropped.Count > 0 ? " (" + string.Join(", ", plan.Dropped) + ")" : string.Empty) + ".");
        output.Report.Add($"Generated parts: {withMembers} each of {definition.InterfaceName}, {definition.PassThroughName} and {definition.FakeName}, plus {definition.RuntimeName}.");
        output.Report.Add($"Wrapper files rewritten: {output.RewrittenWrappers.Count}.");
    }
}
