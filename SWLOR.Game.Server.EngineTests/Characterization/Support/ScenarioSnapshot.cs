#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using SWLOR.Game.Server.EngineTests.Framework;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>
/// Output locking for engine scenarios: compares a <see cref="SnapshotTable"/> with the JSON file checked into
/// <c>SWLOR.Game.Server.EngineTests/Characterization/Snapshots/&lt;area&gt;/&lt;name&gt;.json</c> and fails with a readable
/// row diff when they differ. The file format is the one of <c>SWLOR.Game.Server.CharacterizationTests</c>.
/// </summary>
/// <remarks>
/// <para><b>Where the expected files live.</b> The server runs in a container that cannot see the source tree, so the
/// snapshot files are embedded in this assembly at build time. Rebuild after changing a snapshot file.</para>
/// <para><b>Regenerating.</b> Run the scenarios with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> in the shell
/// (<c>$env:SWLOR_UPDATE_SNAPSHOTS = "1"; scripts/run-engine-tests.ps1 -Filter Characterization</c>). The server then
/// writes every snapshot that differs to <c>&lt;results directory&gt;/characterization/</c>, and the runner script copies
/// those files into the Snapshots folder of this project. Review the diff. Regenerate only for an intended, approved
/// behaviour change; never to make a failing scenario pass.</para>
/// <para><b>Rules.</b> Scenarios freeze <c>Clock</c>, seed <c>Random</c>, and record only values that do not depend on
/// object ids, wall-clock time or frame timing. Keep a snapshot small enough to review.</para>
/// </remarks>
public static class ScenarioSnapshot
{
    public const string UpdateVariable = "SWLOR_UPDATE_SNAPSHOTS";

    /// <summary>The folder, below the engine-test results directory, that update runs write to.</summary>
    public const string OutputFolder = "characterization";

    private const string ResourcePrefix = "Characterization/Snapshots/";
    private const int MaximumDiffLines = 15;

    public static bool IsUpdating => Environment.GetEnvironmentVariable(UpdateVariable) == "1";

    /// <summary>Checks the table against <c>Snapshots/&lt;area&gt;/&lt;name&gt;.json</c> (or writes the actual output when updating).</summary>
    public static void Verify(string area, string name, SnapshotTable table)
    {
        if (table.Rows.Count == 0)
            throw new EngineTestAssertionException($"{table.Function}: the snapshot has no rows.");

        RejectConstantColumns(table);

        var actual = SnapshotJson.Render(table);
        var relativePath = $"{area}/{name}.json";
        var expected = ReadExpected(relativePath);

        if (IsUpdating)
        {
            if (expected == actual)
                return;

            var path = Path.Combine(GetOutputDirectory(), area, name + ".json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual, new UTF8Encoding(false));
            Console.WriteLine($"[ENGINE_TESTS] Snapshot written: {relativePath} ({table.Rows.Count} rows) -> {path}");
            return;
        }

        if (expected == null)
            throw new EngineTestAssertionException($"Snapshot {relativePath} does not exist.{RegenerateHelp()}");

        if (expected == actual)
            return;

        throw new EngineTestAssertionException(Describe(relativePath, expected, actual));
    }

    private static string GetOutputDirectory()
    {
        var settings = ApplicationSettings.Get();
        var directory = settings.EngineTestResultsDirectory;
        if (string.IsNullOrWhiteSpace(directory))
            directory = "./engine_tests/";

        return Path.Combine(directory, OutputFolder);
    }

    private static string? ReadExpected(string relativePath)
    {
        var assembly = typeof(ScenarioSnapshot).Assembly;
        var wanted = ResourcePrefix + relativePath;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => string.Equals(name.Replace('\\', '/'), wanted, StringComparison.Ordinal));
        if (resource == null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }

    private static void RejectConstantColumns(SnapshotTable table)
    {
        if (table.AllowConstantOutputs || table.Rows.Count < 2)
            return;

        for (var column = 0; column < table.OutputColumns.Count; column++)
        {
            var index = table.InputColumns.Count + column;
            var values = table.Rows.Select(row => SnapshotJson.RenderCell(row[index])).Distinct().Count();
            if (values < 2)
                throw new EngineTestAssertionException(
                    $"{table.Function}: output column '{table.OutputColumns[column]}' is the same in all {table.Rows.Count} rows, " +
                    "so the scenario probably did not exercise it. Fix the fixture, or set AllowConstantOutputs if this is genuine.");
        }
    }

    private static string Describe(string relativePath, string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var removed = LinesMissingFrom(expectedLines, actualLines);
        var added = LinesMissingFrom(actualLines, expectedLines);

        var text = new StringBuilder();
        text.Append($"Snapshot mismatch: {relativePath}. ");
        text.Append($"{removed.Count} line(s) only in the snapshot (-), {added.Count} line(s) only in the actual output (+). ");
        AppendLines(text, "-", removed);
        AppendLines(text, "+", added);
        text.Append(RegenerateHelp());
        return text.ToString();
    }

    private static List<(int Line, string Text)> LinesMissingFrom(string[] source, string[] other)
    {
        var available = new Dictionary<string, int>();
        foreach (var line in other)
            available[line] = available.TryGetValue(line, out var count) ? count + 1 : 1;

        var missing = new List<(int, string)>();
        for (var i = 0; i < source.Length; i++)
        {
            if (available.TryGetValue(source[i], out var count) && count > 0)
                available[source[i]] = count - 1;
            else
                missing.Add((i + 1, source[i]));
        }

        return missing;
    }

    private static void AppendLines(StringBuilder text, string marker, List<(int Line, string Text)> lines)
    {
        foreach (var (line, content) in lines.Take(MaximumDiffLines))
            text.Append($"[{marker} line {line}: {content.Trim()}] ");

        if (lines.Count > MaximumDiffLines)
            text.Append($"[{marker} ... and {lines.Count - MaximumDiffLines} more] ");
    }

    private static string RegenerateHelp()
    {
        return " Regenerate only for an intended, approved behaviour change: set SWLOR_UPDATE_SNAPSHOTS=1 and run " +
               "scripts/run-engine-tests.ps1 -Filter Characterization, then review the diff of the snapshot files.";
    }
}
