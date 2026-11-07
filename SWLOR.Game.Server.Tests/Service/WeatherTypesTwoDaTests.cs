using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Guards sw_2da/weathertypes.2da, which fixes rain and snow that start while players are in the area.
/// </summary>
/// <remarks>
/// Every weather start the server sends restarts a fixed 6000 ms fade on the client. Each frame of that
/// fade draws the precipitation at (FadeTime - remaining) / FadeTime of full strength, where FadeTime is
/// read from the row of the weather the client is currently showing. The base game's 6000 makes the
/// first frame 0%, and precipitation that starts that faint stays a light drizzle until the player
/// re-enters the area. A long FadeTime makes the first frame nearly full strength.
/// </remarks>
public class WeatherTypesTwoDaTests
{
    private const int ClientFadeMilliseconds = 6000;
    private const double MinimumFirstFrameStrength = 0.99;

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Module directory not found.");
        }
    }

    private static List<string[]> Rows()
    {
        var path = Path.Combine(Root, "SWLOR_Haks", "sw_2da", "weathertypes.2da");
        if (!File.Exists(path))
            Assert.Fail($"{path} is missing; initialize the SWLOR_Haks submodule (sw_2da).");

        var lines = File.ReadAllLines(path);
        lines[0].Should().StartWith("2DA V2.0");
        lines[2].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Should()
            .Equal("Label", "Model", "Animation", "ModelLightning", "FadeTime");
        return lines.Skip(3)
            .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Where(columns => columns.Length > 0)
            .ToList();
    }

    [Test]
    public void WeatherTypes_KeepTheBaseGameRowsAndModels()
    {
        var rows = Rows();
        rows.Select(row => string.Join(' ', row.Take(5))).Should().Equal(
            "0 Clear **** **** ****",
            "1 Rain fx_rain rain fx_lightning01",
            "2 Snow fx_snow snow fx_lightning01");
    }

    [Test]
    public void EveryWeatherType_StartsPrecipitationAtNearlyFullStrength()
    {
        // Clear is the row used when rain or snow starts from clear skies; Rain and Snow are used when
        // the client is already showing precipitation (rain to snow, or a repeated start).
        foreach (var row in Rows())
        {
            row.Should().HaveCount(6, $"row {row[0]} needs a FadeTime");
            int.TryParse(row[5], out var fadeTime).Should().BeTrue($"{row[1]} FadeTime must be a number, not {row[5]}");
            var firstFrame = (fadeTime - ClientFadeMilliseconds) / (double)fadeTime;
            firstFrame.Should().BeGreaterThanOrEqualTo(MinimumFirstFrameStrength,
                $"{row[1]} rain and snow must not start as a drizzle that only re-entering the area fixes");
        }
    }
}
