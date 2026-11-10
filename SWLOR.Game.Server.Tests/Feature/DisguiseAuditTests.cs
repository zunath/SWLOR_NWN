using System.Text.RegularExpressions;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The audit trail of the disguises: every mutation writes a structured line with the canonical owner, the stable
/// disguise id and the values before and after. The real disguise and descriptor instances run over a small world
/// (<see cref="DisguiseWorld"/>); only their calls into unconverted services are replaced.
/// </summary>
public class DisguiseAuditTests
{
    private const string OldBiography = "Old\n\"bio\" {text}";
    private const string NewBiography = "New\n\"bio\" {text}";
    private const uint Owner = DisguiseWorld.Owner;

    private DisguiseWorld _world = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new DisguiseWorld();
        _world.DbPlayer.UnknownDisplayName = "Base descriptor";
        _world.Disguises.StatAdjustments[SWLOR.Game.Server.Domain.Character.StatType.AdditionalDisguiseSlots] = 2;
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private object[][] Lifecycle()
    {
        var disguises = _world.Disguises;
        var disguise = disguises.CreateDisguise(Owner)!;
        disguises.SaveDisguise(Owner, disguise.Id, "Old mask", "Old descriptor", OldBiography, 20, 21, true)
            .IsSuccessful.Should().BeTrue();
        disguises.Activate(Owner, disguise.Id).IsSuccessful.Should().BeTrue();
        disguises.SaveDisguise(Owner, disguise.Id, "New mask", "New descriptor", NewBiography, 30, 31, false)
            .IsSuccessful.Should().BeTrue();
        _world.Entering = Owner;
        disguises.ApplyActiveDisguiseOnEnter();
        disguises.ResetActivationCooldowns(Owner, DisguiseWorld.Staff);
        disguises.Deactivate(Owner);
        disguises.Retire(Owner, disguise.Id);
        disguises.Unretire(Owner, disguise.Id);
        disguises.Retire(Owner, disguise.Id);
        disguises.DeleteRetiredDisguise(Owner, disguise.Id, DisguisePaymentMethod.Credits)
            .Should().Be(DeleteRetiredDisguiseResult.Success);
        _world.Db.Get<PlayerDisguise>(disguise.Id).Should().BeNull();
        return Records();
    }

    private object[][] Records() => _world.Logs.Select(log => new object[] { log.Template }.Concat(log.Values).ToArray()).ToArray();

    private string GroupOf(object[] record) => _world.Logs.First(log => ReferenceEquals(log.Template, record[0]) &&
        log.Values.SequenceEqual(record.Skip(1))).Group.ToString();

    [Test]
    public void Lifecycle_RecordsEveryMutationWithCanonicalOwnerAndStableDisguiseId()
    {
        var records = Lifecycle();
        records.Select(record => ((string)record[0]).Split(':')[0]).Should().Equal(
            "Disguise created", "Disguise saved", "Disguise public descriptor changed", "Disguise activated", "Disguise saved",
            "Disguise public descriptor changed",
            "Disguise reapplied on login", "Disguise activation cooldown reset", "Disguise deactivated",
            "Disguise retired", "Disguise restored", "Disguise retired", "Retired disguise deleted");
        var disguiseId = Field(records[0], "DisguiseId");
        foreach (var record in records)
        {
            Field(record, "PlayerId").Should().Be("owner-id");
            Field(record, "PlayerName").Should().Be("Canonical Player");
            Field(record, "DisguiseId").Should().Be(disguiseId);
            GroupOf(record).Should().Be(((string)record[0]).StartsWith("Disguise public descriptor changed:")
                ? "PublicDescriptors" : "PlayerName");
        }
        var reapplied = records.Single(record => ((string)record[0]).StartsWith("Disguise reapplied on login:"));
        Field(reapplied, "Descriptor").Should().Be("New descriptor");
        var reset = records.Single(record => ((string)record[0]).StartsWith("Disguise activation cooldown reset:"));
        Field(reset, "PreviousDateLastActivated").Should().BeOfType<DateTime>();
        Field(reset, "ActorPlayerId").Should().Be("staff-id");
        Field(reset, "ActorName").Should().Be("Staff Actor");
    }

    [Test]
    public void Customization_RetainsEveryPreviousAndNewValueIncludingSameLengthBiographyEdits()
    {
        var save = Lifecycle().Last(record => ((string)record[0]).StartsWith("Disguise saved:"));
        Field(save, "PreviousPrivateName").Should().Be("Old mask");
        Field(save, "PrivateName").Should().Be("New mask");
        Field(save, "PreviousDescriptor").Should().Be("Old descriptor");
        Field(save, "Descriptor").Should().Be("New descriptor");
        JsonConvert.DeserializeObject<string>((string)Field(save, "PreviousBiography")).Should().Be("Old\n\"bio\" {text}");
        JsonConvert.DeserializeObject<string>((string)Field(save, "Biography")).Should().Be("New\n\"bio\" {text}");
        Field(save, "PreviousBiographyLength").Should().Be(Field(save, "BiographyLength"));
        Field(save, "PreviousPortraitInternalId").Should().Be(20);
        Field(save, "PortraitInternalId").Should().Be(30);
        Field(save, "PreviousSoundSetId").Should().Be(21);
        Field(save, "SoundSetId").Should().Be(31);
        Field(save, "PreviousScrambleAccountId").Should().Be(true);
        Field(save, "ScrambleAccountId").Should().Be(false);
    }

    [Test]
    public void Deletion_RetainsCustomizationAndPaymentAfterTheDisguiseRecordIsGone()
    {
        var deleted = Lifecycle().Last();
        JsonConvert.DeserializeObject<string>((string)Field(deleted, "Biography")).Should().Be("New\n\"bio\" {text}");
        Field(deleted, "Descriptor").Should().Be("New descriptor");
        Field(deleted, "PrivateName").Should().Be("New mask");
        Field(deleted, "PortraitInternalId").Should().Be(30);
        Field(deleted, "SoundSetId").Should().Be(31);
        Field(deleted, "ScrambleAccountId").Should().Be(false);
        Field(deleted, "PaymentMethod").Should().Be("Credits");
        Field(deleted, "PaymentAmount").Should().Be(100000);
        Field(deleted, "RemovedKnownNameReferences").Should().Be(3);
    }

    [Test]
    public void BaseDescriptorChange_RecordsOldAndNewValuesWhileKeepingActiveDisguiseTraceable()
    {
        _world.DbPlayer.UnknownDisplayName = string.Empty;
        _world.Descriptors.EnsureUnknownDisplayName(Owner).Should().BeTrue();
        _world.Descriptors.EnsureUnknownDisplayName(Owner).Should().BeFalse("a descriptor is never regenerated");
        var generated = _world.DbPlayer.UnknownDisplayName;
        generated.Should().NotBeNullOrWhiteSpace();
        var disguise = _world.Disguises.CreateDisguise(Owner)!;
        _world.Disguises.Activate(Owner, disguise.Id);
        _world.ClearLogs();

        _world.Descriptors.SetUnknownDisplayName(Owner, "New base descriptor");

        _world.Disguises.GetDisplayDescriptor(Owner).Should().Be(generated, "the active disguise keeps its own descriptor");
        var records = Records();
        records.Should().ContainSingle();
        GroupOf(records[0]).Should().Be("PublicDescriptors");
        Field(records[0], "PlayerId").Should().Be("owner-id");
        Field(records[0], "PlayerName").Should().Be("Canonical Player");
        Field(records[0], "IdentityKey").Should().Be("owner-id");
        Field(records[0], "ActiveIdentityKey").Should().BeOfType<string>().Which.Should().StartWith("disguise:");
        Field(records[0], "PreviousDescriptor").Should().Be(generated);
        Field(records[0], "Descriptor").Should().Be("New base descriptor");
    }

    [Test]
    public void DescriptorGeneration_IsLoggedOnlyWhenANewDescriptorIsPersisted()
    {
        _world.DbPlayer.UnknownDisplayName = string.Empty;

        _world.Descriptors.EnsureUnknownDisplayName(Owner);
        _world.Descriptors.EnsureUnknownDisplayName(Owner);

        var records = Records();
        records.Should().ContainSingle();
        GroupOf(records[0]).Should().Be("PublicDescriptors");
        ((string)records[0][0]).Should().StartWith("Base descriptor generated:");
        Field(records[0], "PlayerId").Should().Be("owner-id");
        Field(records[0], "IdentityKey").Should().Be("owner-id");
        Field(records[0], "ActiveIdentityKey").Should().Be("owner-id");
        Field(records[0], "PreviousDescriptor").Should().Be("");
        Field(records[0], "Descriptor").Should().Be(_world.DbPlayer.UnknownDisplayName).And.NotBe("");
    }

    [Test]
    public void RejectedCustomization_RecordsAttemptedValuesAndReason()
    {
        var disguise = _world.Disguises.CreateDisguise(Owner)!;
        _world.ClearLogs();

        _world.Disguises.SaveDisguise(Owner, disguise.Id, "", "Descriptor", "Biography", 20, 21, true)
            .IsSuccessful.Should().BeFalse();

        var records = Records();
        records.Should().ContainSingle();
        ((string)records[0][0]).Should().StartWith("Disguise customization rejected:");
        Field(records[0], "PlayerId").Should().Be("owner-id");
        JsonConvert.DeserializeObject<string>((string)Field(records[0], "Descriptor")).Should().Be("Descriptor");
        JsonConvert.DeserializeObject<string>((string)Field(records[0], "Biography")).Should().Be("Biography");
        Field(records[0], "Reason").Should().Be("Please enter a private disguise name.");
    }

    [Test]
    public void RejectedLifecycle_RecordsAttemptsWithoutSuccessfulStateChanges()
    {
        _world.Disguises.Activate(Owner, "missing-id");
        _world.Disguises.Deactivate(Owner);
        _world.Disguises.Retire(Owner, "missing-id");
        _world.Disguises.Unretire(Owner, "missing-id");
        _world.Disguises.DeleteRetiredDisguise(Owner, "missing-id", DisguisePaymentMethod.Credits);

        var records = Records();
        records.Select(record => ((string)record[0]).Split(':')[0]).Should().Equal(
            "Disguise activation rejected", "Disguise deactivation rejected", "Disguise retirement rejected",
            "Disguise restoration rejected", "Disguise deletion rejected");
        foreach (var record in records)
        {
            Field(record, "PlayerId").Should().Be("owner-id");
            Field(record, "Reason").Should().BeOfType<string>().Which.Should().NotBeEmpty();
        }
    }

    [Test]
    public void MultilineCustomization_FormatsAsOnePhysicalAuditLineWithoutLosingText()
    {
        var lifecycle = Lifecycle();
        _world.ClearLogs();
        _world.Db.Store.Saved.Clear();
        var disguise = _world.Disguises.CreateDisguise(Owner)!;
        _world.ClearLogs();
        _world.Disguises.SaveDisguise(Owner, disguise.Id, "Invalid\n" + new string('x', 40),
            "Descriptor\r\n2026-01-01 forged event", "Biography\r\nForged event", 20, 21, true);
        var multiline = Records();

        using var logger = new LoggerConfiguration().CreateLogger();
        var formatter = new MessageTemplateTextFormatter("{Timestamp:O} [{Level:u3}] {Message:lj}{NewLine}{Exception}", null);
        foreach (var record in lifecycle.Concat(multiline))
        {
            logger.BindMessageTemplate((string)record[0], record.Skip(1).ToArray(), out var template, out var properties)
                .Should().BeTrue();
            var logEvent = new LogEvent(DateTimeOffset.UtcNow, LogEventLevel.Information, null, template!, properties!);
            using var writer = new StringWriter();
            formatter.Format(logEvent, writer);
            writer.ToString().TrimEnd('\r', '\n').Should().NotContain("\r").And.NotContain("\n");
        }
        var rejected = multiline.Single();
        JsonConvert.DeserializeObject<string>((string)Field(rejected, "Biography")).Should().Be("Biography\r\nForged event");
        JsonConvert.DeserializeObject<string>((string)Field(rejected, "Descriptor")).Should().Be("Descriptor\r\n2026-01-01 forged event");
    }

    [Test]
    public void CooldownReset_AttributesPossessedActorsToTheStaffCharacter()
    {
        var disguise = _world.Disguises.CreateDisguise(Owner)!;
        _world.Disguises.Activate(Owner, disguise.Id);
        _world.ClearLogs();

        _world.Disguises.ResetActivationCooldowns(Owner, DisguiseWorld.Possessed);

        var records = Records();
        records.Should().ContainSingle();
        Field(records[0], "ActorPlayerId").Should().Be("staff-id");
        Field(records[0], "ActorName").Should().Be("Staff Actor");
    }

    [Test]
    public void DisguiseDescriptorEdits_WritePublicHistoryWithTheIdentityAndActiveState()
    {
        var records = Lifecycle().Where(record => GroupOf(record) == "PublicDescriptors").ToArray();
        records.Should().HaveCount(2);
        Field(records[0], "PreviousDescriptor").Should().Be("Base descriptor");
        Field(records[0], "Descriptor").Should().Be("Old descriptor");
        Field(records[0], "IsActive").Should().Be(false);
        Field(records[1], "PreviousDescriptor").Should().Be("Old descriptor");
        Field(records[1], "Descriptor").Should().Be("New descriptor");
        Field(records[1], "IsActive").Should().Be(true);
        foreach (var record in records)
            Field(record, "IdentityKey").Should().Be($"disguise:{Field(record, "DisguiseId")}");
    }

    [Test]
    public void OtherDisguiseCustomization_DoesNotWriteAPublicDescriptorChange()
    {
        var disguise = _world.Disguises.CreateDisguise(Owner)!;
        _world.ClearLogs();

        _world.Disguises.SaveDisguise(Owner, disguise.Id, "Changed private name", disguise.Descriptor, "Changed biography", 20, 21, true);

        var records = Records();
        records.Should().ContainSingle();
        GroupOf(records[0]).Should().Be("PlayerName");
    }

    private static object Field(object[] record, string name)
    {
        var names = Regex.Matches((string)record[0], @"\{(?<name>[^}]+)\}")
            .Select(match => match.Groups["name"].Value).ToArray();
        names.Length.Should().Be(record.Length - 1, "every audit property must have a value");
        var index = Array.IndexOf(names, name);
        index.Should().BeGreaterThanOrEqualTo(0, $"the audit must retain {name}");
        return record[index + 1];
    }
}
