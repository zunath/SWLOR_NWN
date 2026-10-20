using System.Text.Json;

namespace SWLOR.ContributorSetup;

internal sealed record NativeToolConfiguration(
    string Version, string LinuxUrl, string LinuxSha256, string[] NativeNames, string[] WindowsFiles)
{
    public static NativeToolConfiguration Load(string root) =>
        JsonSerializer.Deserialize<NativeToolConfiguration>(File.ReadAllText(Path.Combine(root, "tools", "native-tools.json")))
        ?? throw new InvalidDataException("Missing native tool configuration.");
}
