using System.Globalization;
using System.Text;

namespace SWLOR.Toolset.Domain.GameData.GameCode
{
    /// <summary>One documented VFX screenshot and the metadata that helps a builder choose it.</summary>
    public sealed record VisualEffectReferenceInfo(
        int Value,
        string Group,
        string EnumName,
        string VisualTags,
        string Location,
        string Colors,
        string SelectionHint,
        string SourcePage,
        string ImageUrl);

    internal static class VisualEffectReferenceReader
    {
        private const string RelativePath = "Readmes/VisualEffectReference.csv";

        public static IReadOnlyDictionary<int, VisualEffectReferenceInfo> Read(string? gameServerSourceRoot)
        {
            if (string.IsNullOrWhiteSpace(gameServerSourceRoot))
                return new Dictionary<int, VisualEffectReferenceInfo>();

            try
            {
                var path = Path.Combine(gameServerSourceRoot, RelativePath);
                if (!File.Exists(path))
                    return new Dictionary<int, VisualEffectReferenceInfo>();

                var result = new Dictionary<int, VisualEffectReferenceInfo>();
                foreach (var line in File.ReadLines(path).Skip(1))
                {
                    var cells = ParseCsvRow(line);
                    if (cells.Count < 10 ||
                        !int.TryParse(cells[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    {
                        continue;
                    }

                    result[value] = new VisualEffectReferenceInfo(
                        value,
                        cells[0],
                        cells[2],
                        cells[4],
                        cells[5],
                        cells[6],
                        cells[7],
                        cells[8],
                        cells[9]);
                }

                return result;
            }
            catch (IOException)
            {
                return new Dictionary<int, VisualEffectReferenceInfo>();
            }
            catch (UnauthorizedAccessException)
            {
                return new Dictionary<int, VisualEffectReferenceInfo>();
            }
        }

        private static IReadOnlyList<string> ParseCsvRow(string line)
        {
            var result = new List<string>();
            var cell = new StringBuilder();
            var quoted = false;

            for (var index = 0; index < line.Length; index++)
            {
                var character = line[index];
                if (character == '"')
                {
                    if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                    {
                        cell.Append('"');
                        index++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (character == ',' && !quoted)
                {
                    result.Add(cell.ToString());
                    cell.Clear();
                }
                else
                {
                    cell.Append(character);
                }
            }

            result.Add(cell.ToString());
            return result;
        }
    }
}
