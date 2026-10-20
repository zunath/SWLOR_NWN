using System.Text.Json;

namespace SWLOR.NWN.Formats.Common;

/// <summary>Adapts unsigned structure IDs to the native converter's signed representation.</summary>
public sealed class NativeGffInput : IDisposable
{
    public string InputPath { get; }
    private readonly bool _temporary;

    private NativeGffInput(string path, bool temporary) => (InputPath, _temporary) = (path, temporary);

    public static NativeGffInput? PrepareForTool(string executable, string workingDirectory, string[] arguments)
    {
        if (!OperatingSystem.IsLinux() || NativeToolPath.ExecutableName(Path.GetFileName(executable), false) != "nwn_gff")
            return null;
        var format = Array.IndexOf(arguments, "-l");
        var input = Array.IndexOf(arguments, "-i");
        if (format < 0 || format + 1 >= arguments.Length || arguments[format + 1] != "json" || input < 0 || input + 1 >= arguments.Length)
            return null;
        var staged = Prepare(Path.GetFullPath(arguments[input + 1], workingDirectory));
        arguments[input + 1] = staged.InputPath;
        return staged;
    }

    public static NativeGffInput Prepare(string sourcePath)
    {
        var source = File.ReadAllBytes(sourcePath);
        var reader = new Utf8JsonReader(source, new JsonReaderOptions { MaxDepth = 1024 });
        using var output = new MemoryStream();
        var copied = 0;
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || !reader.ValueTextEquals("__struct_id"u8)) continue;
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number ||
                !reader.TryGetUInt32(out var id) || id <= int.MaxValue) continue;
            var start = checked((int)reader.TokenStartIndex);
            output.Write(source, copied, start - copied);
            // Patch only the numeric token. Old Nim JSON may contain raw binary string bytes.
            output.Write(System.Text.Encoding.ASCII.GetBytes(unchecked((int)id).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            copied = checked((int)reader.BytesConsumed);
        }
        if (copied == 0) return new NativeGffInput(sourcePath, false);
        output.Write(source, copied, source.Length - copied);
        var temporary = Path.Combine(Path.GetTempPath(), "swlor_gff_" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllBytes(temporary, output.ToArray());
            return new NativeGffInput(temporary, true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }

    public void Dispose()
    {
        if (_temporary) File.Delete(InputPath);
    }
}
