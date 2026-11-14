using System.Text;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Compares text produced by a test against a snapshot checked into this test project, and rewrites the snapshot
/// when <c>SWLOR_UPDATE_SNAPSHOTS=1</c> is set. A mismatch fails with a line diff and the command that regenerates the file.
/// </summary>
public static class SnapshotComparison
{
    public const string UpdateVariable = "SWLOR_UPDATE_SNAPSHOTS";

    private const int ContextLines = 3;
    private const int MaxDiffLines = 150;

    /// <param name="repositoryRelativePath">Snapshot path from the repository root, with forward slashes.</param>
    /// <param name="actual">The text the code under test produced.</param>
    /// <param name="regenerateCommand">Shown in the failure message.</param>
    public static void AssertMatches(string repositoryRelativePath, string actual, string regenerateCommand)
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, repositoryRelativePath.Replace('/', Path.DirectorySeparatorChar));
        actual = Normalize(actual);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual, new UTF8Encoding(false));
            TestContext.Progress.WriteLine($"{UpdateVariable}=1: wrote {repositoryRelativePath}. Review the diff and commit it.");
            return;
        }

        if (!File.Exists(path))
        {
            Assert.Fail(
                $"The snapshot {repositoryRelativePath} does not exist.{Environment.NewLine}" +
                $"Create it with: {regenerateCommand}");
        }

        var expected = Normalize(File.ReadAllText(path));
        if (expected == actual)
            return;

        Assert.Fail(
            $"{repositoryRelativePath} no longer matches what the code produces.{Environment.NewLine}" +
            $"If the change is intended, regenerate the snapshot with:{Environment.NewLine}  {regenerateCommand}{Environment.NewLine}" +
            $"and commit the new file. Differences ('-' snapshot, '+' now):{Environment.NewLine}" +
            Diff(expected.Split('\n'), actual.Split('\n')));
    }

    private static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n");
    }

    private static string Diff(string[] expected, string[] actual)
    {
        // Longest common subsequence over the lines between the shared head and tail.
        var head = 0;
        while (head < expected.Length && head < actual.Length && expected[head] == actual[head])
            head++;

        var tail = 0;
        while (tail < expected.Length - head && tail < actual.Length - head &&
               expected[expected.Length - 1 - tail] == actual[actual.Length - 1 - tail])
            tail++;

        var a = expected.Skip(head).Take(expected.Length - head - tail).ToArray();
        var b = actual.Skip(head).Take(actual.Length - head - tail).ToArray();
        var lcs = new int[a.Length + 1, b.Length + 1];
        for (var i = a.Length - 1; i >= 0; i--)
        for (var j = b.Length - 1; j >= 0; j--)
            lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        // Each entry: (marker, text).
        var script = new List<(char Marker, string Text)>();
        for (var i = 0; i < head; i++)
            script.Add((' ', expected[i]));

        int x = 0, y = 0;
        while (x < a.Length || y < b.Length)
        {
            if (x < a.Length && y < b.Length && a[x] == b[y])
            {
                script.Add((' ', a[x]));
                x++;
                y++;
            }
            else if (y < b.Length && (x == a.Length || lcs[x, y + 1] >= lcs[x + 1, y]))
            {
                script.Add(('+', b[y]));
                y++;
            }
            else
            {
                script.Add(('-', a[x]));
                x++;
            }
        }

        for (var i = 0; i < tail; i++)
            script.Add((' ', expected[expected.Length - tail + i]));

        // Name the nearest enclosing section (a line that starts in column 0) at the top of each hunk.
        var section = new string[script.Count];
        var current = string.Empty;
        for (var i = 0; i < script.Count; i++)
        {
            var text = script[i].Text;
            if (text.Length > 0 && text[0] != ' ' && text[0] != '#')
                current = text;
            section[i] = current;
        }

        var show = new bool[script.Count];
        for (var i = 0; i < script.Count; i++)
        {
            if (script[i].Marker == ' ')
                continue;
            for (var k = Math.Max(0, i - ContextLines); k <= Math.Min(script.Count - 1, i + ContextLines); k++)
                show[k] = true;
        }

        var diff = new StringBuilder();
        var written = 0;
        var inHunk = false;
        for (var i = 0; i < script.Count; i++)
        {
            if (!show[i])
            {
                inHunk = false;
                continue;
            }

            if (written >= MaxDiffLines)
            {
                diff.Append("... (diff truncated; regenerate the snapshot and read `git diff`)\n");
                break;
            }

            if (!inHunk)
            {
                var name = section[i];
                var comment = name.IndexOf("  #", StringComparison.Ordinal);
                if (comment >= 0)
                    name = name.Substring(0, comment);
                diff.Append("@@ ").Append(name.Length == 0 ? "(file header)" : name).Append(" @@\n");
                inHunk = true;
            }

            diff.Append(script[i].Marker).Append(' ').Append(script[i].Text).Append('\n');
            written++;
        }

        return diff.ToString();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
