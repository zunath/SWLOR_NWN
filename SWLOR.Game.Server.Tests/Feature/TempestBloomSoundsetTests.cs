using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class TempestBloomSoundsetTests
{
    // soundset.2da rows: 903 AssassinDroid, 910 Droid, 924 MedicalDroid, 935 TankDroid, 944 HK47,
    // 448 Troglodyte_Warrior (creature growls) and 413 Valen_Shadowbreath (NWN2 voice, no ssf in the haks).
    private static readonly int[] UnsuitableSoundsets = { 903, 910, 924, 935, 944, 448, 413 };
    private const int RepSoldier = 926;

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

    [TestCase("cp_tempbloom_sp")]
    [TestCase("cp_tempbloom_ad")]
    [TestCase("cp_tempbloom_ic")]
    [TestCase("cp_tempbloom_ms")]
    [TestCase("cp_tempbloom_wd")]
    public void TempestBloomCreatures_UseHumanoidSoldierVoice(string blueprint)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "utc", $"{blueprint}.utc.json")));
        var soundset = document.RootElement.GetProperty("SoundSetFile").GetProperty("value").GetInt32();

        UnsuitableSoundsets.Should().NotContain(soundset);
        soundset.Should().Be(RepSoldier);
    }
}
