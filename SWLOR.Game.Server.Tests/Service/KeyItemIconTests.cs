using System.Reflection;
using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Service;

public class KeyItemIconTests
{
    [Test]
    public void DefaultIcon_HasValidResrefAndExpectedTgaFormat()
    {
        KeyItemIcon.Default.Should().NotBeNullOrWhiteSpace();
        KeyItemIcon.Default.Length.Should().BeLessThanOrEqualTo(16,
            "NWN resource names are limited to 16 characters");

        var root = FindRepositoryRoot();
        var path = Path.Combine(root.FullName, "SWLOR_Haks", "sw_item", $"{KeyItemIcon.Default}.tga");

        File.Exists(path).Should().BeTrue($"the Key Items empty state should reference a packaged icon at {path}");
        AssertIconTga(path, "Default Key Item icon");
    }

    [Test]
    public void ActiveKeyItems_HaveValidIconResrefs()
    {
        var entries = GetActiveEntries();
        var uniqueIcons = entries
            .Where(x => x.Detail.Category is KeyItemCategoryType.QuestItems or
                KeyItemCategoryType.Documents or KeyItemCategoryType.Keys)
            .ToList();
        var maps = entries
            .Where(x => x.Detail.Category == KeyItemCategoryType.Maps)
            .ToList();
        var fieldNotes = entries
            .Where(x => x.Detail.Category == KeyItemCategoryType.FieldNotes)
            .ToList();

        entries.Should().HaveCount(414);
        uniqueIcons.Should().HaveCount(198);
        maps.Should().HaveCount(77);
        fieldNotes.Should().HaveCount(139);

        entries.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.Resref));
        entries.Should().OnlyContain(x => x.Resref.Length <= 16,
            "NWN resource names are limited to 16 characters");

        uniqueIcons.Select(x => x.Resref).Should().OnlyHaveUniqueItems(
            "every Key, Quest Item, and Document entry has unique artwork");

        var mapResrefs = maps.Select(x => x.Resref).ToHashSet();
        mapResrefs.Should().BeEquivalentTo(KeyItemIcon.MapIconResrefs,
            "Maps intentionally reuse the six approved map categories");

        var fieldNoteResrefs = fieldNotes.Select(x => x.Resref).ToHashSet();
        fieldNoteResrefs.Should().BeEquivalentTo(KeyItemIcon.FieldNoteIconResrefs,
            "Field Notes intentionally reuse the six approved document categories");
    }

    [Test]
    public void ActiveKeyItemIcons_ExistAndHaveExpectedTgaFormat()
    {
        var root = FindRepositoryRoot();
        var iconDirectory = Path.Combine(root.FullName, "SWLOR_Haks", "sw_item");
        var entries = GetActiveEntries();
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries.DistinctBy(x => x.Resref))
        {
            var path = Path.Combine(iconDirectory, $"{entry.Resref}.tga");
            File.Exists(path).Should().BeTrue($"{entry.Type} should reference a packaged icon at {path}");
            var pixels = AssertIconTga(path, entry.Type.ToString());

            var hash = Convert.ToHexString(SHA256.HashData(pixels));
            hashes.Should().NotContainKey(hash,
                $"{entry.Type} should not reuse the pixels of {hashes.GetValueOrDefault(hash)}");
            hashes[hash] = entry.Type.ToString();
        }
    }

    private static List<KeyItemIconEntry> GetActiveEntries()
    {
        return Enum.GetValues<KeyItemType>()
            .Select(type => new
            {
                Type = type,
                Detail = typeof(KeyItemType)
                    .GetField(type.ToString())!
                    .GetCustomAttribute<KeyItemAttribute>()!,
            })
            .Where(x => x.Detail.IsActive)
            .Select(x => new KeyItemIconEntry(
                x.Type,
                x.Detail,
                KeyItemIcon.GetIconResref(x.Type)))
            .ToList();
    }

    private static byte[] AssertIconTga(string path, string label)
    {
        var bytes = File.ReadAllBytes(path);
        bytes.Should().HaveCountGreaterThan(18, $"{label} should be a valid TGA file");
        bytes[1].Should().Be(0, $"{label} should not use a color map");
        bytes[2].Should().Be(2, $"{label} should be an uncompressed true-color TGA");

        var width = bytes[12] + (bytes[13] << 8);
        var height = bytes[14] + (bytes[15] << 8);
        width.Should().Be(64, $"{label} should be 64 pixels wide");
        height.Should().Be(64, $"{label} should be 64 pixels tall");
        bytes[16].Should().BeOneOf(new byte[] { 24, 32 }, $"{label} should be 24-bit or 32-bit");

        var pixelOffset = 18 + bytes[0];
        var pixelLength = checked(width * height * (bytes[16] / 8));
        bytes.Length.Should().BeGreaterThanOrEqualTo(
            pixelOffset + pixelLength,
            $"{label} should contain its complete pixel payload");

        return bytes.AsSpan(pixelOffset, pixelLength).ToArray();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull("repository root should be discoverable from the test directory");
        return directory!;
    }

    private sealed record KeyItemIconEntry(
        KeyItemType Type,
        KeyItemAttribute Detail,
        string Resref);
}
