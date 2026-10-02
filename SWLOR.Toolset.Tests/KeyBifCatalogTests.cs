using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

public sealed class KeyBifCatalogTests
{
    [Test]
    public void Load_UsesLaterSelectedKeyArchive()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "swlor-key-catalog", Guid.NewGuid().ToString("N"));
        var dataDirectory = Path.Combine(installRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        try
        {
            WriteArchive(dataDirectory, "nwn_base", [1, 2, 3, 4]);
            WriteArchive(dataDirectory, "nwn_retail", [5, 6, 7, 8]);

            var catalog = KeyBifCatalog.Load(dataDirectory);
            var identity = new ResourceIdentity("sample", ResourceIdentity.TypeFromExtension("mdl"));

            catalog.ResourceCount.Should().Be(1);
            catalog.Contains(identity).Should().BeTrue();
            Action tooSmallRead = () => catalog.TryGetBytes(identity, out _, maximumBytes: 2);
            tooSmallRead.Should().Throw<FormatException>().WithMessage("*configured limit*");
            catalog.TryGetBytes(identity, out var bytes).Should().BeTrue();
            bytes.Should().Equal(5, 6, 7, 8);
        }
        finally
        {
            Directory.Delete(installRoot, recursive: true);
        }
    }

    [Test]
    public void Contains_DoesNotOpenBifUntilBytesAreRequested()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "swlor-key-catalog", Guid.NewGuid().ToString("N"));
        var dataDirectory = Path.Combine(installRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        try
        {
            WriteArchive(dataDirectory, "nwn_base", [1, 2, 3, 4]);
            var catalog = KeyBifCatalog.Load(dataDirectory);
            var identity = new ResourceIdentity("sample", ResourceIdentity.TypeFromExtension("mdl"));
            File.Delete(Path.Combine(dataDirectory, "nwn_base.bif"));

            catalog.Contains(identity).Should().BeTrue("index lookup must not reopen or extract the BIF");
            catalog.TryGetBytes(identity, out var missingBytes).Should().BeFalse();
            missingBytes.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(installRoot, recursive: true);
        }
    }

    [Test]
    public void Load_FallsBackToBifStoredDirectlyUnderDataDirectory()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "swlor-key-catalog", Guid.NewGuid().ToString("N"));
        var dataDirectory = Path.Combine(installRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        try
        {
            WriteArchive(dataDirectory, "nwn_base", [9, 8, 7, 6], useDataPrefix: false);
            var catalog = KeyBifCatalog.Load(dataDirectory);
            var identity = new ResourceIdentity("sample", ResourceIdentity.TypeFromExtension("mdl"));

            catalog.TryGetBytes(identity, out var bytes).Should().BeTrue();
            bytes.Should().Equal(9, 8, 7, 6);
        }
        finally
        {
            Directory.Delete(installRoot, recursive: true);
        }
    }

    [Test]
    public void Load_PreservesRawUnknownResourceTypeCodes()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "swlor-key-catalog", Guid.NewGuid().ToString("N"));
        var dataDirectory = Path.Combine(installRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        try
        {
            WriteArchive(dataDirectory, "nwn_base", [3, 1, 4], resourceType: 62000);
            var catalog = KeyBifCatalog.Load(dataDirectory);
            var identity = new ResourceIdentity("sample", 62000);

            catalog.Contains(identity).Should().BeTrue();
            catalog.TryGetBytes(identity, out var bytes).Should().BeTrue();
            bytes.Should().Equal(3, 1, 4);
        }
        finally
        {
            Directory.Delete(installRoot, recursive: true);
        }
    }

    [Test]
    public void Load_UsesExplicitStockInstallDataRoot()
    {
        var dataDirectory = Environment.GetEnvironmentVariable("SWLOR_TEST_NWN_DATA_ROOT");
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            Assert.Ignore("Set SWLOR_TEST_NWN_DATA_ROOT to the explicit stock NWN data directory for this corpus check.");
            return;
        }

        var catalog = KeyBifCatalog.Load(dataDirectory);
        catalog.ResourceCount.Should().BeGreaterThan(1000);
        var modelType = ResourceIdentity.TypeFromExtension("mdl");
        var identity = new[] { "c_bear", "c_badger", "nw_chicken", "plc_chest1" }
            .Select(resRef => new ResourceIdentity(resRef, modelType))
            .FirstOrDefault(catalog.Contains);
        identity.Should().NotBe(default);
        catalog.TryGetBytes(identity, out var bytes).Should().BeTrue();
        bytes.Should().NotBeEmpty();
    }

    private static void WriteArchive(
        string dataDirectory,
        string archiveName,
        byte[] payload,
        bool useDataPrefix = true,
        ushort resourceType = 2002)
    {
        var bifName = archiveName + ".bif";
        File.WriteAllBytes(Path.Combine(dataDirectory, bifName), BuildBif(payload, resourceType));
        File.WriteAllBytes(Path.Combine(dataDirectory, archiveName + ".key"), BuildKey(bifName, useDataPrefix, resourceType));
    }

    private static byte[] BuildKey(string bifName, bool useDataPrefix, ushort resourceType)
    {
        var encodedFilename = Encoding.UTF8.GetBytes(useDataPrefix ? "data\\" + bifName : bifName);
        const int bifTableOffset = 64;
        const int resourceTableOffset = bifTableOffset + 12;
        var filenameOffset = resourceTableOffset + 22;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("KEY "u8);
        writer.Write("V1  "u8);
        writer.Write(1u);
        writer.Write(1u);
        writer.Write((uint)bifTableOffset);
        writer.Write((uint)resourceTableOffset);
        writer.Write((uint)filenameOffset);
        writer.Write((uint)1);
        writer.Write(new byte[32]);
        writer.Write(40u);
        writer.Write((uint)filenameOffset);
        writer.Write((ushort)encodedFilename.Length);
        writer.Write((ushort)1);
        writer.Write(Encoding.ASCII.GetBytes("sample"));
        writer.Write(new byte[10]);
        writer.Write(resourceType);
        writer.Write(0u);
        writer.Write(encodedFilename);
        return stream.ToArray();
    }

    private static byte[] BuildBif(byte[] payload, ushort resourceType)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);
        writer.Write("BIFF"u8);
        writer.Write("V1  "u8);
        writer.Write(1u);
        writer.Write(0u);
        writer.Write(20u);
        writer.Write(0u);
        writer.Write(36u);
        writer.Write((uint)payload.Length);
        writer.Write((uint)resourceType);
        writer.Write(payload);
        return stream.ToArray();
    }
}
