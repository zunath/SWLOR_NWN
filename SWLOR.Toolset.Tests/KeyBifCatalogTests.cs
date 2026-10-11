using System.Text;
using System.Security.Cryptography;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Resources;

namespace SWLOR.Toolset.Tests;

public sealed class KeyBifCatalogTests
{
    [Test]
    [Category("LicensedCorpus")]
    public void InstalledXp3TilesetMatchesItsIndependentHashThroughBoundedStockReads()
    {
        var dataDirectory = StockDataDirectory();
        dataDirectory.Should().NotBeNullOrWhiteSpace("the actual stock corpus must be selected explicitly");
        new FileInfo(Path.Combine(dataDirectory!, "xp3.bif")).Length.Should().Be(683611953);
        var catalog = KeyBifCatalog.Load(dataDirectory!);
        var identity = ResourceIdentity.FromFileName("tbw01.set");
        var before = GC.GetAllocatedBytesForCurrentThread();

        ((Action)(() => catalog.TryGetBytes(identity, out _, maximumBytes: 62200))).Should()
            .Throw<FormatException>().WithMessage("*configured limit*");
        catalog.TryGetBytes(identity, out var bytes, maximumBytes: 62201).Should().BeTrue();

        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(4 * 1024 * 1024,
            "the real 683 MiB archive retains only metadata and the requested tileset");
        bytes.Should().HaveCount(62201);
        Convert.ToHexStringLower(SHA256.HashData(bytes)).Should()
            .Be("13499381c11c7fe3848e25c289da953e0d7a0209014ec2f99d6b087bb1faaada");
    }

    [Test]
    public void LargeStockArchiveReadsOnlyItsPayloadAndStillRefusesTheSourceFileLimit()
    {
        var installRoot = Path.Combine(Path.GetTempPath(), "swlor-key-catalog", Guid.NewGuid().ToString("N"));
        var dataDirectory = Path.Combine(installRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        try
        {
            WriteArchive(dataDirectory, "nwn_base", [1, 2, 3, 4]);
            var path = Path.Combine(dataDirectory, "nwn_base.bif");
            using (var stream = File.OpenWrite(path)) stream.SetLength(683611953);
            var catalog = KeyBifCatalog.Load(dataDirectory);
            var identity = new ResourceIdentity("sample", ResourceIdentity.TypeFromExtension("mdl"));
            var before = GC.GetAllocatedBytesForCurrentThread();

            catalog.TryGetBytes(identity, out var bytes, maximumBytes: 4).Should().BeTrue();

            (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(1024 * 1024,
                "a shipped-size stock archive is streamed without loading its unused bytes");
            bytes.Should().Equal(1, 2, 3, 4);
            using (var stream = File.OpenWrite(path)) stream.SetLength(1024L * 1024 * 1024 + 1);
            ((Action)(() => catalog.TryGetBytes(identity, out _, maximumBytes: 4))).Should()
                .Throw<FormatException>().WithMessage("*configured limit*");
        }
        finally
        {
            Directory.Delete(installRoot, recursive: true);
        }
    }

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
        var dataDirectory = StockDataDirectory();
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

    /// <summary>SWLOR_TEST_NWN_DATA_ROOT selects an explicit stock data directory; otherwise the located install's.</summary>
    private static string? StockDataDirectory()
    {
        var configured = Environment.GetEnvironmentVariable("SWLOR_TEST_NWN_DATA_ROOT");
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        return NwnInstallLocator.Locate(Environment.GetEnvironmentVariable("NWN_INSTALL_PATH")) is { } install
            ? Path.Combine(install, "data")
            : null;
    }
}
