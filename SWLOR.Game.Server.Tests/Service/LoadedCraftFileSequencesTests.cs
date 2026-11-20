using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Domain.Crafting.Research;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Covers the craft file sequence kept outside the creature's locals at login. A player who logs back in before a restart gets the
/// locals of their earlier login copied over the new creature, which put back a stale loaded sequence and discarded every research job
/// started since then.
/// </summary>
public class LoadedCraftFileSequencesTests
{
    private static readonly LoadedCraftFileSequences _loads = new();

    private static readonly DateTime Now = new(2026, 11, 19, 3, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp() => _loads.Clear();

    [TearDown]
    public void TearDown() => _loads.Clear();

    [Test]
    public void ARecordedLoad_IsTakenOnce()
    {
        _loads.Record("pc-7", 9, Now);

        _loads.TryTake("pc-7", out var sequence).Should().BeTrue();
        sequence.Should().Be(9);
        _loads.TryTake("pc-7", out _).Should().BeFalse();
    }

    [Test]
    public void ACreatureWithNoRecordedLoad_HasNothingToTake()
    {
        _loads.TryTake("pc-7", out var sequence).Should().BeFalse();
        sequence.Should().Be(0);
    }

    [Test]
    public void ALaterLoadIntoTheSameCreature_ReplacesTheEarlierOne()
    {
        _loads.Record("pc-7", 2, Now);
        _loads.Record("pc-7", 5, Now.AddSeconds(1));

        _loads.TryTake("pc-7", out var sequence).Should().BeTrue();
        sequence.Should().Be(5);
    }

    [Test]
    public void ALoadNoLoginClaimed_IsDroppedAfterItsLifetime()
    {
        _loads.Record("pc-7", 4, Now);
        _loads.Record("pc-8", 6, Now + LoadedCraftFileSequences.Lifetime + TimeSpan.FromSeconds(1));

        _loads.TryTake("pc-7", out _).Should().BeFalse();
        _loads.TryTake("pc-8", out var sequence).Should().BeTrue();
        sequence.Should().Be(6);
    }

    [Test]
    public void AFileWithoutACharacterId_IsNotRecorded()
    {
        _loads.Record(string.Empty, 4, Now);

        _loads.TryTake(string.Empty, out _).Should().BeFalse();
    }

    [Test]
    public void AFileWithoutASequence_LoadsAsZero()
    {
        _loads.Record("pc-7", -3, Now);

        _loads.TryTake("pc-7", out var sequence).Should().BeTrue();
        sequence.Should().Be(0);
    }

    [Test]
    public void Resolve_TheFileIsWhatWasLoadedAndExported_AndTheCounterNeverGoesBack()
    {
        LoadedCraftFileSequences.Resolve(5, 3).Should().Be((5, 5, 5));
        LoadedCraftFileSequences.Resolve(5, 8).Should().Be((8, 5, 5));
    }

    [Test]
    public void Apply_ReplacesTheStaleLocalsOfAnEarlierLogin()
    {
        // The earlier login loaded a file with no sequence; three research jobs then took sequences 1 to 3 and the logout saved 3.
        var locals = new Dictionary<string, int>
        {
            [CraftSessionFileSequence.Variable] = 3,
            [CraftSessionFileSequence.ExportedVariable] = 3,
            [CraftSessionFileSequence.LoadedVariable] = 0,
        };
        _loads.Record("pc-7", 3, Now);

        Apply("pc-7", locals);

        locals[CraftSessionFileSequence.Variable].Should().Be(3);
        locals[CraftSessionFileSequence.ExportedVariable].Should().Be(3);
        locals[CraftSessionFileSequence.LoadedVariable].Should().Be(3);
    }

    [Test]
    public void Apply_DoesNothingTheSecondTime()
    {
        var locals = new Dictionary<string, int>();
        _loads.Record("pc-7", 3, Now);
        Apply("pc-7", locals);
        locals[CraftSessionFileSequence.Variable] = 6;
        locals[CraftSessionFileSequence.ExportedVariable] = 6;

        Apply("pc-7", locals);

        locals[CraftSessionFileSequence.Variable].Should().Be(6);
        locals[CraftSessionFileSequence.ExportedVariable].Should().Be(6);
        locals[CraftSessionFileSequence.LoadedVariable].Should().Be(3);
    }

    [Test]
    public void ResearchJobsStartedBeforeARelog_SurviveTheLoginCheck()
    {
        var locals = new Dictionary<string, int>
        {
            [CraftSessionFileSequence.Variable] = 8,
            [CraftSessionFileSequence.ExportedVariable] = 8,
            [CraftSessionFileSequence.LoadedVariable] = 5,
        };
        var jobs = new[]
        {
            new ResearchJob { StartSequence = 6 },
            new ResearchJob { StartSequence = 7 },
            new ResearchJob { StartSequence = 8 },
        };

        // Without the recorded load, the stale value from the earlier login discards all three.
        foreach (var job in jobs)
            ResearchJobFileRules.AtLogin(job, locals[CraftSessionFileSequence.LoadedVariable]).Should().Be(ResearchJobFileState.StartNotInFile);

        _loads.Record("pc-7", 8, Now);
        Apply("pc-7", locals);

        foreach (var job in jobs)
            ResearchJobFileRules.AtLogin(job, locals[CraftSessionFileSequence.LoadedVariable]).Should().Be(ResearchJobFileState.Current);
    }

    [Test]
    public void AJobTheFileReallyPredates_IsStillDiscarded()
    {
        // A crash lost the export after the start: the file carries 5 though the job was stamped 6.
        var locals = new Dictionary<string, int>();
        _loads.Record("pc-7", 5, Now);
        Apply("pc-7", locals);

        ResearchJobFileRules.AtLogin(new ResearchJob { StartSequence = 6 }, locals[CraftSessionFileSequence.LoadedVariable])
            .Should().Be(ResearchJobFileState.StartNotInFile);
    }

    private static void Apply(string characterId, Dictionary<string, int> locals) =>
        _loads.Apply(characterId, name => locals.GetValueOrDefault(name), (name, value) => locals[name] = value);
}
