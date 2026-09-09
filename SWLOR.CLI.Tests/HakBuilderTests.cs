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
}
