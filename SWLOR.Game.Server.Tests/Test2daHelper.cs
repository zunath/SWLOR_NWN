namespace SWLOR.Game.Server.Tests;

internal static class Test2daHelper
{
    public static Dictionary<int, Dictionary<string, string>> Read2da(FileInfo file)
    {
        var lines = File.ReadAllLines(file.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        var headers = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var rows = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var parts = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !int.TryParse(parts[0], out var rowNumber))
                continue;

            var row = new Dictionary<string, string>();
            for (var index = 0; index < headers.Length && index + 1 < parts.Length; index++)
            {
                row[headers[index]] = parts[index + 1];
            }

            rows[rowNumber] = row;
        }

        return rows;
    }
}
