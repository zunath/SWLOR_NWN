#nullable enable
using System.Linq;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SWLOR.Game.Server.EngineTests.Characterization.Support;

/// <summary>
/// Renders a <see cref="SnapshotTable"/> as JSON with a fixed layout: a short header, then one row per line.
/// The layout never changes with the data, so snapshots diff row by row and stay readable in a pull request.
/// </summary>
internal static class SnapshotJson
{
    private static readonly JsonSerializerOptions StringOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Render(SnapshotTable table)
    {
        var text = new StringBuilder();
        text.Append("{\n");
        text.Append("  \"function\": ").Append(Quote(table.Function)).Append(",\n");
        text.Append("  \"description\": ").Append(Quote(table.Description)).Append(",\n");
        text.Append("  \"seed\": ").Append(table.Seed is { } seed ? seed.ToString(CultureInfo.InvariantCulture) : "null").Append(",\n");
        text.Append("  \"inputs\": ").Append(Quote(table.Inputs)).Append(",\n");
        text.Append("  \"rowCount\": ").Append(table.Rows.Count.ToString(CultureInfo.InvariantCulture)).Append(",\n");
        text.Append("  \"columns\": [");
        text.Append(string.Join(", ", table.InputColumns.Select(Quote).Concat(table.OutputColumns.Select(name => Quote("=> " + name)))));
        text.Append("],\n");
        text.Append("  \"rows\": [");
        for (var i = 0; i < table.Rows.Count; i++)
        {
            text.Append(i == 0 ? "\n    " : ",\n    ");
            AppendArray(text, table.Rows[i]);
        }

        text.Append(table.Rows.Count == 0 ? "]\n" : "\n  ]\n");
        text.Append("}\n");
        return text.ToString();
    }

    /// <summary>Renders one cell the way it appears in a row.</summary>
    public static string RenderCell(object? cell)
    {
        var text = new StringBuilder();
        AppendCell(text, cell);
        return text.ToString();
    }

    private static void AppendArray(StringBuilder text, IEnumerable cells)
    {
        text.Append('[');
        var first = true;
        foreach (var cell in cells)
        {
            if (!first)
                text.Append(", ");
            first = false;
            AppendCell(text, cell);
        }

        text.Append(']');
    }

    private static void AppendCell(StringBuilder text, object? cell)
    {
        switch (cell)
        {
            case null:
                text.Append("null");
                break;
            case bool value:
                text.Append(value ? "true" : "false");
                break;
            case string value:
                text.Append(Quote(value));
                break;
            case Enum value:
                text.Append(Quote(value.ToString()));
                break;
            case float value:
                text.Append(FormatFloatingPoint(float.IsFinite(value), value.ToString("R", CultureInfo.InvariantCulture)));
                break;
            case double value:
                text.Append(FormatFloatingPoint(double.IsFinite(value), value.ToString("R", CultureInfo.InvariantCulture)));
                break;
            case IFormattable value when cell is sbyte or byte or short or ushort or int or uint or long or ulong:
                text.Append(value.ToString(null, CultureInfo.InvariantCulture));
                break;
            case IEnumerable values:
                AppendArray(text, values);
                break;
            default:
                throw new NotSupportedException($"Snapshot cells of type {cell.GetType().Name} are not supported.");
        }
    }

    // JSON has no NaN or Infinity; record them as strings so a regression to one is still visible.
    private static string FormatFloatingPoint(bool isFinite, string text) => isFinite ? text : Quote(text);

    private static string Quote(string value) => JsonSerializer.Serialize(value, StringOptions);
}
