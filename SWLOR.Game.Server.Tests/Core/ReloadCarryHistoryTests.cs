using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Keeps a hot reload from an earlier build from losing carried state when a field moves or is renamed: every key some build has
/// carried (<c>Snapshots/ReloadCarryHistory.txt</c>) is still carried, named by a <see cref="ReloadCarriedFromAttribute"/> on the
/// field that holds it now, or recorded as dropped. The old build writes each class's fields under its own class name, so a moved
/// field the new build does not map is lost without a word.
/// </summary>
[TestFixture]
public class ReloadCarryHistoryTests
{
    private const string History = "SWLOR.Game.Server.Tests/Core/Snapshots/ReloadCarryHistory.txt";
    private const string DroppedMarker = "  dropped:";
    private const string RegenerateCommand =
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Game.Server.Tests --no-build --filter \"FullyQualifiedName~ReloadCarryHistoryTests\"" +
        "   (PowerShell: $env:SWLOR_UPDATE_SNAPSHOTS='1'; dotnet test ...; Remove-Item Env:SWLOR_UPDATE_SNAPSHOTS)";

    private static readonly Assembly Game = ScriptHandlerTestSupport.GameAssembly;

    [Test]
    public void Every_carried_field_is_in_the_history()
    {
        var lines = ReadLines();
        var known = Entries(lines).Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
        var missing = CurrentKeys().Where(key => !known.Contains(key)).OrderBy(key => key, StringComparer.Ordinal).ToArray();
        if (missing.Length == 0)
            return;

        if (Environment.GetEnvironmentVariable(SnapshotComparison.UpdateVariable) == "1")
        {
            foreach (var key in missing)
                Insert(lines, key);
            File.WriteAllText(HistoryPath(), string.Concat(lines.Select(line => line + "\n")), new UTF8Encoding(false));
            TestContext.Progress.WriteLine($"{SnapshotComparison.UpdateVariable}=1: added {missing.Length} keys to {History}. Review the diff and commit it.");
            return;
        }

        Assert.Fail($"{History} does not list these carried fields: {string.Join(", ", missing)}.{Environment.NewLine}" +
                    $"Add them (it never removes a line) with:{Environment.NewLine}  {RegenerateCommand}{Environment.NewLine}and commit the file.");
    }

    [Test]
    public void Every_key_an_earlier_build_carried_is_still_carried_moved_or_dropped()
    {
        var current = CurrentKeys().ToHashSet(StringComparer.Ordinal);
        var named = CarriedFromKeys(ReloadStateCarrier.CarriedClasses(Game).SelectMany(entry => entry.Fields))
            .Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

        var lost = Entries(ReadLines())
            .Where(entry => entry.Dropped == null && !current.Contains(entry.Key) && !named.Contains(entry.Key))
            .Select(entry => entry.Key)
            .ToArray();

        lost.Should().BeEmpty(
            $"a reload from a build that carried these keys would lose them; mark the field that holds each one now with " +
            $"[ReloadCarriedFrom(\"<former class>\", \"<former field>\")] (the field name only when it changed), or end its line in {History} " +
            $"with \"{DroppedMarker} <reason>\" when it is no longer carried on purpose");
    }

    [Test]
    public void Every_former_owner_names_a_key_in_the_history_from_a_carried_field_once()
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var known = Entries(ReadLines()).Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);
        var marked = Game.GetTypes().SelectMany(type => type.GetFields(flags)).ToArray();
        var problems = new List<string>();

        foreach (var (key, field) in CarriedFromKeys(marked))
        {
            var name = $"{field.DeclaringType!.FullName}.{field.Name}";
            if (ReloadDigestHasher.StateOf(field) != ReloadStateKind.Carried)
                problems.Add($"{name} names a former owner but is not carried; only carried fields are read back");
            if (!known.Contains(key))
                problems.Add($"{name} reads {key}, which no build carried according to {History}");
        }

        problems.AddRange(CarriedFromKeys(marked)
            .GroupBy(entry => (entry.Field.DeclaringType, entry.Key))
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key.DeclaringType!.FullName} reads {group.Key.Key} into " +
                             string.Join(" and ", group.Select(entry => entry.Field.Name))));

        problems.Should().BeEmpty("each former key is read into one field of a class, from a key some build carried");
    }

    private static IEnumerable<string> CurrentKeys()
    {
        return ReloadStateCarrier.CarriedClasses(Game)
            .SelectMany(entry => entry.Fields)
            .Select(field => $"{field.DeclaringType!.FullName}.{field.Name}");
    }

    /// <summary>Each former key (<c>owner.field</c>) a <see cref="ReloadCarriedFromAttribute"/> of <paramref name="fields"/> reads.</summary>
    private static IEnumerable<(string Key, FieldInfo Field)> CarriedFromKeys(IEnumerable<FieldInfo> fields)
    {
        return fields.SelectMany(field => field.GetCustomAttributes<ReloadCarriedFromAttribute>(false)
            .Select(from => ($"{from.FormerOwner}.{from.FormerField ?? field.Name}", field)));
    }

    private static IEnumerable<(string Key, string Dropped)> Entries(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                continue;

            var marker = line.IndexOf(DroppedMarker, StringComparison.Ordinal);
            if (marker < 0)
            {
                yield return (line.Trim(), null);
                continue;
            }

            var reason = line.Substring(marker + DroppedMarker.Length).Trim();
            yield return (line.Substring(0, marker).Trim(), reason.Length == 0 ? null : reason);
        }
    }

    /// <summary>Puts <paramref name="key"/> before the first entry that sorts after it, so a sorted history stays sorted.</summary>
    private static void Insert(List<string> lines, string key)
    {
        var index = lines.FindIndex(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#", StringComparison.Ordinal) &&
                                            string.CompareOrdinal(Entries(new[] { line }).Single().Key, key) > 0);
        if (index < 0)
            lines.Add(key);
        else
            lines.Insert(index, key);
    }

    private static List<string> ReadLines()
    {
        var path = HistoryPath();
        File.Exists(path).Should().BeTrue($"{History} lists every key some build has carried");
        return File.ReadAllText(path).Replace("\r\n", "\n").TrimEnd('\n').Split('\n').ToList();
    }

    private static string HistoryPath()
    {
        return Path.Combine(SnapshotComparison.FindRepositoryRoot().FullName, History.Replace('/', Path.DirectorySeparatorChar));
    }
}
