using System.Text;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>The difference between a debt measure as it is now and its checked-in baseline.</summary>
internal sealed class DebtComparison
{
    private DebtComparison(
        IReadOnlyList<DebtEntry> added,
        IReadOnlyList<(DebtEntry Entry, int Baseline)> grown,
        IReadOnlyList<(DebtEntry Entry, int Baseline)> shrunk,
        IReadOnlyList<(string Key, int Baseline)> gone)
    {
        Added = added;
        Grown = grown;
        Shrunk = shrunk;
        Gone = gone;
    }

    /// <summary>Entries that are not in the baseline: new debt.</summary>
    public IReadOnlyList<DebtEntry> Added { get; }

    /// <summary>Entries whose count is above the baseline's: the debt grew.</summary>
    public IReadOnlyList<(DebtEntry Entry, int Baseline)> Grown { get; }

    /// <summary>Entries whose count is below the baseline's: debt paid, the baseline must follow.</summary>
    public IReadOnlyList<(DebtEntry Entry, int Baseline)> Shrunk { get; }

    /// <summary>Baseline entries that no longer exist: debt paid, the baseline must follow.</summary>
    public IReadOnlyList<(string Key, int Baseline)> Gone { get; }

    public bool IsClean => Added.Count == 0 && Grown.Count == 0 && Shrunk.Count == 0 && Gone.Count == 0;

    public static DebtComparison Compare(IReadOnlyDictionary<string, int> baseline, IEnumerable<DebtEntry> actual)
    {
        var current = actual.OrderBy(entry => entry.Key, StringComparer.Ordinal).ToList();
        var currentKeys = current.Select(entry => entry.Key).ToHashSet(StringComparer.Ordinal);

        var added = current.Where(entry => !baseline.ContainsKey(entry.Key)).ToList();
        var grown = current
            .Where(entry => baseline.TryGetValue(entry.Key, out var count) && entry.Count > count)
            .Select(entry => (entry, baseline[entry.Key]))
            .ToList();
        var shrunk = current
            .Where(entry => baseline.TryGetValue(entry.Key, out var count) && entry.Count < count)
            .Select(entry => (entry, baseline[entry.Key]))
            .ToList();
        var gone = baseline
            .Where(pair => !currentKeys.Contains(pair.Key))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => (pair.Key, pair.Value))
            .ToList();
        return new DebtComparison(added, grown, shrunk, gone);
    }

    /// <summary>A failure message that names every change and says what to do about it.</summary>
    public string Describe(string title, string baselineName, string unit, string regenerateCommand)
    {
        var text = new StringBuilder();
        text.AppendLine($"{title}: the code no longer matches {baselineName}.");

        if (Added.Count > 0 || Grown.Count > 0)
        {
            text.AppendLine($"New or larger debt ({Added.Count} new, {Grown.Count} grown). Phase 5 only shrinks this measure:");
            foreach (var entry in Added)
                text.AppendLine($"  + {entry.Key} | {entry.Count} {unit}{Detail(entry)}");
            foreach (var (entry, baseline) in Grown)
                text.AppendLine($"  > {entry.Key} | {baseline} -> {entry.Count} {unit}{Detail(entry)}");
            text.AppendLine("Change the code so it stays within the baseline. Only a change the owner approved (or a master sync that brings");
            text.AppendLine("the debt in) may regenerate the baseline; say why in the pull request.");
        }

        if (Shrunk.Count > 0 || Gone.Count > 0)
        {
            text.AppendLine($"Debt paid ({Shrunk.Count} smaller, {Gone.Count} gone). Regenerate the baseline in this pull request so it cannot come back:");
            foreach (var (entry, baseline) in Shrunk)
                text.AppendLine($"  < {entry.Key} | {baseline} -> {entry.Count} {unit}");
            foreach (var (key, baseline) in Gone)
                text.AppendLine($"  - {key} | {baseline} {unit}");
        }

        text.AppendLine("Regenerate (this also rewrites DebtReport.md) and review the diff:");
        text.AppendLine($"  {regenerateCommand}");
        return text.ToString();
    }

    private static string Detail(DebtEntry entry) => entry.Detail.Length == 0 ? string.Empty : $"   [{entry.Detail}]";
}
