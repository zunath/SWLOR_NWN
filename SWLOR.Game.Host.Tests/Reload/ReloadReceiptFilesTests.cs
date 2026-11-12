using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Host.Reload;

namespace SWLOR.Game.Host.Tests.Reload;

/// <summary>Receipt files: written whole (a reader never sees half a receipt), read back, and described in one line.</summary>
[TestFixture]
public class ReloadReceiptFilesTests
{
    private static readonly Guid Id = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");
    private static readonly DateTimeOffset Written = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private string _root = null!;

    [SetUp]
    public void CreateRoot()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-receipts", Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void DeleteRoot()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    [Test]
    public void A_receipt_is_written_into_a_new_folder_and_read_back_whole()
    {
        var path = Path.Combine(_root, "receipts", "one.json");
        var receipt = new ReloadReceipt(ReloadReceipt.CurrentFormatVersion, Id, ReloadReceiptStatus.Applied, "applied", Written)
        {
            ActiveGeneration = 2,
            ValidationMilliseconds = 12.5,
            PauseMilliseconds = 340,
            PausedFrame = 99
        };

        ReloadReceiptFiles.Write(path, receipt);

        ReloadReceiptFiles.Read(path).Should().Be(receipt);
        Directory.GetFiles(Path.GetDirectoryName(path)!).Should().Equal(path);
        File.ReadAllText(path).Should().Contain("\"Status\": \"Applied\"", "statuses are written by name for the developer reading the file");
    }

    [Test]
    public void A_later_receipt_replaces_the_earlier_one()
    {
        var path = Path.Combine(_root, "one.json");
        ReloadReceiptFiles.Write(path, new ReloadReceipt(1, Id, ReloadReceiptStatus.Accepted, "validating", Written));

        ReloadReceiptFiles.Write(path, new ReloadReceipt(1, Id, ReloadReceiptStatus.Rejected, "validation failed", Written));

        var read = ReloadReceiptFiles.Read(path)!;
        read.Status.Should().Be(ReloadReceiptStatus.Rejected);
        read.IsFinal.Should().BeTrue();
        Directory.GetFiles(_root).Should().ContainSingle();
    }

    [Test]
    public void There_is_no_receipt_before_the_host_writes_one()
    {
        ReloadReceiptFiles.Read(Path.Combine(_root, "none.json")).Should().BeNull();
    }

    [TestCase("not json")]
    [TestCase("{\"FormatVersion\":2,\"GenerationId\":\"0f8fad5b-d9cb-469f-a165-70867728950e\",\"Status\":\"Applied\",\"Detail\":\"x\",\"WrittenUtc\":\"2026-11-12T10:00:00+00:00\"}")]
    [TestCase("{\"FormatVersion\":1,\"GenerationId\":\"00000000-0000-0000-0000-000000000000\",\"Status\":\"Applied\",\"Detail\":\"x\",\"WrittenUtc\":\"2026-11-12T10:00:00+00:00\"}")]
    [TestCase("{\"FormatVersion\":1,\"GenerationId\":\"0f8fad5b-d9cb-469f-a165-70867728950e\",\"Status\":\"Applied\",\"WrittenUtc\":\"2026-11-12T10:00:00+00:00\"}")]
    [TestCase("{\"FormatVersion\":1,\"GenerationId\":\"0f8fad5b-d9cb-469f-a165-70867728950e\",\"Status\":7,\"Detail\":\"x\",\"WrittenUtc\":\"2026-11-12T10:00:00+00:00\"}")]
    [TestCase("null")]
    public void A_file_that_is_not_a_receipt_of_this_format_is_refused(string text)
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, "bad.json");
        File.WriteAllText(path, text);

        var read = () => ReloadReceiptFiles.Read(path);

        read.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void Only_an_accepted_receipt_is_not_final()
    {
        foreach (var status in Enum.GetValues<ReloadReceiptStatus>())
            new ReloadReceipt(1, Id, status, "", Written).IsFinal.Should().Be(status != ReloadReceiptStatus.Accepted);
    }

    [Test]
    public void A_long_detail_is_shortened_for_the_file()
    {
        ReloadReceiptFiles.LimitDetail("short").Should().Be("short");
        var limited = ReloadReceiptFiles.LimitDetail(new string('x', ReloadReceiptFiles.MaximumDetailLength + 10));
        limited.Should().HaveLength(ReloadReceiptFiles.MaximumDetailLength + 3).And.EndWith("...");
    }

    [Test]
    public void A_receipt_is_described_in_one_line_with_the_numbers_it_has()
    {
        var applied = new ReloadReceipt(1, Id, ReloadReceiptStatus.Applied, "running", Written)
        {
            ActiveGeneration = 3,
            ValidationMilliseconds = 1200.4,
            PauseMilliseconds = 850.6,
            PausedFrame = 4321
        };
        var accepted = new ReloadReceipt(1, Id, ReloadReceiptStatus.Accepted, "validating", Written);

        ReloadReceiptFiles.Describe(applied).Should()
            .Be("generation=0f8fad5bd9cb469fa16570867728950e status=Applied active=3 validateMs=1200 pauseMs=851 frame=4321: running");
        ReloadReceiptFiles.Describe(accepted).Should().Be("generation=0f8fad5bd9cb469fa16570867728950e status=Accepted: validating");
    }

    [Test]
    public void Writing_and_reading_need_a_path_and_a_receipt()
    {
        var receipt = new ReloadReceipt(1, Id, ReloadReceiptStatus.Accepted, "", Written);

        ((Action)(() => ReloadReceiptFiles.Write(" ", receipt))).Should().Throw<ArgumentException>();
        ((Action)(() => ReloadReceiptFiles.Write(Path.Combine(_root, "x.json"), null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => ReloadReceiptFiles.Read(""))).Should().Throw<ArgumentException>();
        ((Action)(() => ReloadReceiptFiles.Describe(null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => ReloadReceiptFiles.LimitDetail(null!))).Should().Throw<ArgumentNullException>();
    }
}
