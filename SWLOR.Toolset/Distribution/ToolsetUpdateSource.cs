using Velopack;
using Velopack.Sources;

namespace SWLOR.Toolset.Distribution;

internal static class ToolsetUpdateSource
{
    public static GithubSource Create(string? installedVersion) => new(
        "https://github.com/zunath/SWLOR_NWN", null,
        installedVersion != null && SemanticVersion.Parse(installedVersion).IsPrerelease);
}
