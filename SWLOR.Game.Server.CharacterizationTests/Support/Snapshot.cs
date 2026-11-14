using System.Text;
using NUnit.Framework;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>
/// Output locking: compares a <see cref="SnapshotTable"/> with the JSON file checked into
/// <c>Snapshots/&lt;area&gt;/&lt;name&gt;.json</c> and fails with a readable row diff when they differ.
/// </summary>
/// <remarks>
/// <para><b>Adding a snapshot.</b> Build a <see cref="SnapshotTable"/> from a deterministic grid, call
/// <see cref="Verify"/>, run the test once with <c>SWLOR_UPDATE_SNAPSHOTS=1</c> to create the file, then read the file:
/// the rows must look plausible (no column that is all zero, boundary rows present) before it is committed.</para>
/// <para><b>When a test fails.</b> The failure lists the rows that exist only in the snapshot and only in the actual
/// output. A mismatch means the game's calculation changed. Regenerate only for an intended, approved behaviour
/// change; never to make a failing test pass.</para>
/// <para><b>Regenerating.</b> Set <c>SWLOR_UPDATE_SNAPSHOTS=1</c> and run the tests again. The same variable
/// is used by the boot-order test in <c>SWLOR.Game.Server.Tests</c>. PowerShell:
/// <c>$env:SWLOR_UPDATE_SNAPSHOTS = "1"; dotnet test SWLOR.Game.Server.CharacterizationTests --no-build</c>.</para>
/// <para><b>Where files live.</b> Snapshots are read and written in the project's source folder, found by walking up from
/// the test binaries. <c>SWLOR_SNAPSHOT_DIRECTORY</c> overrides the <c>Snapshots</c> folder, which lets a parity run
/// write the reference output of another checkout somewhere else and compare the two folders.</para>
/// <para><b>Randomness.</b> A test that rolls dice wraps the calls in <see cref="SeededRandom"/>, records the seed in
/// the table, and its fixture is <c>[NonParallelizable]</c> because the game's <c>Random</c> is one global generator.</para>
/// <para><b>Rules.</b> Use only public APIs that also exist on <c>master</c>, so the same project can run against it.
/// Keep a snapshot small enough to review: a coarse grid plus the boundary values, one row per input combination.</para>
/// </remarks>
public static class Snapshot
{
    public const string UpdateVariable = "SWLOR_UPDATE_SNAPSHOTS";
    public const string DirectoryVariable = "SWLOR_SNAPSHOT_DIRECTORY";

    private const string ProjectFileName = "SWLOR.Game.Server.CharacterizationTests.csproj";
    private const int MaximumDiffLines = 15;

    public static bool IsUpdating
    {
        get
        {
            return Environment.GetEnvironmentVariable(UpdateVariable) == "1";
        }
    }

    /// <summary>The folder that holds one sub-folder per area.</summary>
    public static string Directory
    {
        get
        {
            var overridePath = Environment.GetEnvironmentVariable(DirectoryVariable);
            if (!string.IsNullOrWhiteSpace(overridePath))
                return Path.GetFullPath(overridePath);

            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, ProjectFileName)))
                    return Path.Combine(folder.FullName, "Snapshots");
            }

            throw new InvalidOperationException(
                $"Could not find {ProjectFileName} above {AppContext.BaseDirectory}. Set {DirectoryVariable}.");
        }
    }

    /// <summary>Checks the table against <c>Snapshots/&lt;area&gt;/&lt;name&gt;.json</c> (or rewrites the file when updating).</summary>
    public static void Verify(string area, string name, SnapshotTable table)
    {
        RejectUnusableTable(table);

        var actual = SnapshotJson.Render(table);
        var path = Path.Combine(Directory, area, name + ".json");
        var relativePath = $"Snapshots/{area}/{name}.json";

        if (IsUpdating)
        {
            if (File.Exists(path) && Normalize(File.ReadAllText(path)) == actual)
                return;

            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual, new UTF8Encoding(false));
            TestContext.Progress.WriteLine($"Snapshot written: {relativePath} ({table.Rows.Count} rows)");
            return;
        }

        if (!File.Exists(path))
            Assert.Fail($"Snapshot {relativePath} does not exist.{RegenerateHelp()}");

        var expected = Normalize(File.ReadAllText(path));
        if (expected == actual)
            return;

        Assert.Fail(Describe(relativePath, expected, actual));
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n");

    private static void RejectUnusableTable(SnapshotTable table)
    {
        if (table.Rows.Count == 0)
            Assert.Fail($"{table.Function}: the snapshot has no rows.");

        if (table.AllowConstantOutputs || table.Rows.Count < 2)
            return;

        for (var column = 0; column < table.OutputColumns.Count; column++)
        {
            var index = table.InputColumns.Count + column;
            var values = table.Rows.Select(row => SnapshotJson.RenderCell(row[index])).Distinct().Count();
            if (values < 2)
                Assert.Fail(
                    $"{table.Function}: output column '{table.OutputColumns[column]}' is the same in all {table.Rows.Count} rows, " +
                    "so the function was probably not exercised. Fix the inputs, or set AllowConstantOutputs if this is genuine.");
        }
    }

    private static string Describe(string relativePath, string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var removed = LinesMissingFrom(expectedLines, actualLines);
        var added = LinesMissingFrom(actualLines, expectedLines);

        var text = new StringBuilder();
        text.AppendLine($"Snapshot mismatch: {relativePath}");
        text.AppendLine($"{removed.Count} line(s) only in the snapshot (-), {added.Count} line(s) only in the actual output (+).");
        text.AppendLine("A row is one line: the inputs, then the outputs after the columns marked with =>.");
        AppendLines(text, "-", removed);
        AppendLines(text, "+", added);
        text.Append(RegenerateHelp());
        return text.ToString();
    }

    private static List<(int Line, string Text)> LinesMissingFrom(string[] source, string[] other)
    {
        var available = new Dictionary<string, int>();
        foreach (var line in other)
            available[line] = available.GetValueOrDefault(line) + 1;

        var missing = new List<(int, string)>();
        for (var i = 0; i < source.Length; i++)
        {
            if (available.GetValueOrDefault(source[i]) > 0)
                available[source[i]]--;
            else
                missing.Add((i + 1, source[i]));
        }

        return missing;
    }

    private static void AppendLines(StringBuilder text, string marker, List<(int Line, string Text)> lines)
    {
        foreach (var (line, content) in lines.Take(MaximumDiffLines))
            text.AppendLine($"  {marker} line {line}: {content.Trim()}");

        if (lines.Count > MaximumDiffLines)
            text.AppendLine($"  {marker} ... and {lines.Count - MaximumDiffLines} more");
    }

    private static string RegenerateHelp()
    {
        var fixture = TestContext.CurrentContext.Test.ClassName;
        var filter = string.IsNullOrEmpty(fixture) ? string.Empty : $" --filter \"FullyQualifiedName~{fixture}\"";
        return Environment.NewLine +
               "Regenerate only for an intended, approved behaviour change:" + Environment.NewLine +
               $"  PowerShell: $env:{UpdateVariable} = \"1\"; dotnet test SWLOR.Game.Server.CharacterizationTests --no-build{filter}" + Environment.NewLine +
               $"  bash:       {UpdateVariable}=1 dotnet test SWLOR.Game.Server.CharacterizationTests --no-build{filter}" + Environment.NewLine +
               "then review the diff of the snapshot file.";
    }
}
