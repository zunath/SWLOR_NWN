using NUnit.Framework;
using SWLOR.CLI;
using SWLOR.CLI.Model;

namespace SWLOR.CLI.Tests;

[TestFixture]
public sealed class HakBuilderTests
{
    private const long Limit = 2L * 1024 * 1024 * 1024;

    [Test]
    public void ArchiveJustBelowLimitIsAccepted()
    {
        Assert.DoesNotThrow(() => HakBuilder.ValidateArchiveSize("test", new[] { Limit - 193 }));
    }

    [TestCase(2147483456L)]
    [TestCase(long.MaxValue)]
    public void OversizedArchiveIsRejectedWithoutArithmeticOverflow(long resourceSize)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            HakBuilder.ValidateArchiveSize("large", new[] { resourceSize }));
        Assert.That(error!.Message, Does.Contain("large").And.Contain("Split").And.Contain("2 GiB"));
    }

    [Test]
    public void MultipleResourcesIncludeEveryTableEntry()
    {
        Assert.Throws<InvalidOperationException>(() =>
            HakBuilder.ValidateArchiveSize("large", new[] { Limit - 225, 1L }));
    }

    [Test]
    public void InvalidInputFailsBeforeDeletingExistingOutputs()
    {
        var root = Path.Combine(Path.GetTempPath(), "SWLOR hak preflight", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "hak"));
        Directory.CreateDirectory(Path.Combine(root, "tlk"));
        var archive = Path.Combine(root, "hak", "test.hak");
        var tlk = Path.Combine(root, "tlk", "test.tlk");
        File.WriteAllText(archive, "previous archive");
        File.WriteAllText(tlk, "previous tlk");
        try
        {
            Assert.Throws<DirectoryNotFoundException>(() => new HakBuilder().Process(new HakBuilderConfig
            {
                OutputPath = root + Path.DirectorySeparatorChar,
                TlkPath = "test.tlk",
                EnableChecksumChecking = false,
                HakList = new() { new() { Name = "test", Path = Path.Combine(root, "missing") } }
            }));
            Assert.That(File.ReadAllText(archive), Is.EqualTo("previous archive"));
            Assert.That(File.ReadAllText(tlk), Is.EqualTo("previous tlk"));
        }
        finally { Directory.Delete(root, true); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BuiltArchiveIncludesTheLodResourceTypeAndExactRedirectPayload(bool legacyCache)
    {
        var root = Path.Combine(Path.GetTempPath(), "SWLOR lod packing", Guid.NewGuid().ToString("N"));
        var source = Path.Combine(root, "source");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "proxy.lod"), "geometry\n");
        File.WriteAllText(Path.Combine(source, "geometry.mdl"), "model payload");
        try
        {
            var output = Path.Combine(root, "output", "hak");
            var archive = Path.Combine(output, "parts.hak");
            var sidecar = Path.Combine(output, "parts.md5");
            if (legacyCache)
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(archive, "legacy archive without LOD resources");
                ChecksumUtil.WriteChecksumFile(sidecar, ChecksumUtil.ChecksumFolder(source));
            }
            var config = new HakBuilderConfig
            {
                OutputPath = Path.Combine(root, "output") + Path.DirectorySeparatorChar,
                TlkPath = Path.Combine(root, "unused.tlk"),
                EnableChecksumChecking = legacyCache,
                HakList = new() { new() { Name = "parts", Path = source } }
            };
            new HakBuilder().Process(config);
            if (legacyCache)
            {
                Assert.That(ChecksumUtil.ReadChecksumFile(sidecar), Is.Not.EqualTo(ChecksumUtil.ChecksumFolder(source)),
                    "the new cache must include packer options as well as source contents");
                var previousBuild = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                File.SetLastWriteTimeUtc(archive, previousBuild);
                new HakBuilder().Process(config);
                Assert.That(File.GetLastWriteTimeUtc(archive), Is.EqualTo(previousBuild),
                    "an archive built with the current options must still be reused");
            }
            using var reader = new BinaryReader(File.OpenRead(archive));
            reader.BaseStream.Position = 16;
            var count = reader.ReadUInt32();
            reader.BaseStream.Position = 24;
            var keys = reader.ReadUInt32();
            var resources = reader.ReadUInt32();
            Assert.That(count, Is.EqualTo(2));
            var found = false;
            for (var index = 0; index < count; index++)
            {
                reader.BaseStream.Position = keys + index * 24;
                var name = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(16)).TrimEnd('\0');
                var resource = reader.ReadUInt32();
                var type = reader.ReadUInt16();
                if (name != "proxy") continue;
                Assert.That(type, Is.EqualTo(2078));
                reader.BaseStream.Position = resources + resource * 8;
                var offset = reader.ReadUInt32();
                var size = reader.ReadUInt32();
                reader.BaseStream.Position = offset;
                Assert.That(System.Text.Encoding.ASCII.GetString(reader.ReadBytes((int)size)), Is.EqualTo("geometry\n"));
                found = true;
            }
            Assert.That(found, Is.True, "nwn_erf must not silently omit .lod files");
        }
        finally { Directory.Delete(root, true); }
    }
}
