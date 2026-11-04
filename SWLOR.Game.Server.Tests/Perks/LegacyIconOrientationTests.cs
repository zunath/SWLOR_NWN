using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// The legacy gameplay icon art was drawn upside down. The May 2026 icon pass moved it to bottom-left origin without changing the
/// decoded image, so these icons showed upside down in the Perks window and the classic hotbar. The fixture lists the decoded
/// pixel hash, top row first, of each icon as it was before the fix. A regeneration or origin pass that turns one upside down
/// again decodes to its old hash and fails here. New art for one of these icons passes as long as it is not the old upside-down
/// copy.
/// </summary>
public class LegacyIconOrientationTests
{
    [Test]
    public void LegacyIcons_AreNotUpsideDownAgain()
    {
        var root = PassivePerkIconTests.FindRepositoryRoot().FullName;
        var iconRoot = Path.Combine(root, "SWLOR_Haks", "sw_ability");
        var fixture = Path.Combine(root, "SWLOR.Game.Server.Tests", "Fixtures", "IconOrientation", "LegacyUpsideDownIconHashes.csv");
        var upsideDownHashes = File.ReadLines(fixture)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(','))
            .ToDictionary(fields => fields[0], fields => fields[1], StringComparer.OrdinalIgnoreCase);
        var files = Directory.GetFiles(iconRoot, "*.tga")
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        upsideDownHashes.Should().HaveCountGreaterThan(1000);
        foreach (var (icon, upsideDown) in upsideDownHashes)
        {
            if (!files.TryGetValue(icon, out var path))
                continue;

            var actual = Convert.ToHexString(SHA256.HashData(CraftingPerkIconOrientationTests.DecodeTopRowFirst(File.ReadAllBytes(path))))
                .ToLowerInvariant();
            if (actual == upsideDown)
                failures.Add($"{icon}: its pixels are the old upside-down copy; flip the pixel rows and keep the bottom-left origin");
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }
}
