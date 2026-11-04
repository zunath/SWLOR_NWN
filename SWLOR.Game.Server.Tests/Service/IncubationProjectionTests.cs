using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Tests.Service;

// Guards the incubator's outcome odds and recipe progress, which the incubator window shows to players
// and the mutation roll relies on.
public class IncubationProjectionTests
{
    private static MutationDetail EnzymeMutation(BeastType target, int weight, Action<MutationRequirementEnzyme> configure)
    {
        var enzyme = new MutationRequirementEnzyme();
        configure(enzyme);
        return new MutationDetail(target)
        {
            Weight = weight,
            Requirements = new List<IMutationRequirement> { enzyme }
        };
    }

    private static BeastDetail Beast(params MutationDetail[] mutations)
    {
        return new BeastDetail { Name = "Test beast", PossibleMutations = mutations.ToList() };
    }

    private static IncubationJob Job(int stage, int mutationChance, Action<IncubationJob> configure = null)
    {
        var job = new IncubationJob
        {
            BeastDNAType = BeastType.Blastail,
            CurrentStage = stage,
            MutationChance = mutationChance
        };
        configure?.Invoke(job);
        return job;
    }

    [Test]
    public void GetOutcomes_SplitsMutationChanceByWeight()
    {
        var beast = Beast(
            EnzymeMutation(BeastType.ArmourbackSpineguard, 90, e => e.LyaseEnzymeColors[EnzymeColorType.Orange] = 1),
            EnzymeMutation(BeastType.MustardlashSlime, 10, e => e.LyaseEnzymeColors[EnzymeColorType.Orange] = 1));
        var job = Job(3, 200, j => j.LyaseColors[EnzymeColorType.Orange] = 1);

        var outcomes = IncubationProjection.GetOutcomes(beast, job, true);

        outcomes.Should().HaveCount(3);
        outcomes.Single(x => x.Type == BeastType.ArmourbackSpineguard).Percent.Should().BeApproximately(18f, 0.001f);
        outcomes.Single(x => x.Type == BeastType.MustardlashSlime).Percent.Should().BeApproximately(2f, 0.001f);
        outcomes.Single(x => x.Type == BeastType.Blastail).Percent.Should().BeApproximately(80f, 0.001f);
    }

    [Test]
    public void GetOutcomes_MutationNotAllowed_KeepsTheDnaBeast()
    {
        var beast = Beast(EnzymeMutation(BeastType.ArmourbackSpineguard, 90, e => e.LyaseEnzymeColors[EnzymeColorType.Orange] = 1));
        var job = Job(3, 380, j => j.LyaseColors[EnzymeColorType.Orange] = 1);

        var outcomes = IncubationProjection.GetOutcomes(beast, job, false);

        outcomes.Should().ContainSingle().Which.Should().Be(new IncubationOutcome(BeastType.Blastail, 100f));
    }

    [Test]
    public void GetOutcomes_NoRecipeMet_KeepsTheDnaBeast()
    {
        var beast = Beast(EnzymeMutation(BeastType.ArmourbackSpineguard, 90, e => e.LyaseEnzymeColors[EnzymeColorType.Orange] = 2));
        var job = Job(3, 380, j => j.LyaseColors[EnzymeColorType.Orange] = 1);

        var outcomes = IncubationProjection.GetOutcomes(beast, job, true);

        outcomes.Should().ContainSingle().Which.Percent.Should().Be(100f);
    }

    [Test]
    public void Evaluate_CountsTheEnzymeWaitingInTheSlot()
    {
        var mutation = EnzymeMutation(BeastType.ArmourbackSpineguard, 90, e => e.LyaseEnzymeColors[EnzymeColorType.Orange] = 2);
        var job = Job(1, 0, j => j.LyaseColors[EnzymeColorType.Orange] = 1);

        var projection = IncubationProjection.Create(job, EnzymeColorType.Orange, EnzymeColorType.Invalid, EnzymeColorType.Invalid, 0, true);
        var report = projection.Evaluate(mutation, new DateTime(2026, 11, 2));

        report.IsReady.Should().BeTrue();
        report.Lines.Should().ContainSingle().Which.Should().Be(new MutationRequirementLine("orange lyase 2/2", true, false));
        report.Summary.Should().Be("Requirements met");
    }

    [Test]
    public void Evaluate_ListsWhatIsMissingWhileStagesRemain()
    {
        var mutation = EnzymeMutation(BeastType.ArmourbackSpineguard, 90, e => e.IsomeraseEnzymeColors[EnzymeColorType.Yellow] = 2);
        var job = Job(1, 0, j => j.IsomeraseColors[EnzymeColorType.Yellow] = 1);

        var projection = IncubationProjection.Create(job, EnzymeColorType.Invalid, EnzymeColorType.Invalid, EnzymeColorType.Invalid, 0, true);
        var report = projection.Evaluate(mutation, new DateTime(2026, 11, 2));

        report.IsReady.Should().BeFalse();
        report.Summary.Should().Be("1 more enzyme needed");
    }

    [Test]
    public void Evaluate_TooFewStagesLeft_IsOutOfReach()
    {
        var mutation = EnzymeMutation(BeastType.ArmourbackSpineguard, 90, e => e.IsomeraseEnzymeColors[EnzymeColorType.Yellow] = 3);
        var job = Job(2, 0);

        var projection = IncubationProjection.Create(job, EnzymeColorType.Invalid, EnzymeColorType.Invalid, EnzymeColorType.Invalid, 0, true);
        var report = projection.Evaluate(mutation, new DateTime(2026, 11, 2));

        report.Summary.Should().Be("Out of reach for this job");
    }

    [Test]
    public void Evaluate_WrongDay_IsWaitableNotMissing()
    {
        var mutation = new MutationDetail(BeastType.CoppercoilMirelisk)
        {
            Weight = 90,
            Requirements = new List<IMutationRequirement> { new MutationRequirementDayOfWeek(DayOfWeek.Thursday, DayOfWeek.Wednesday) }
        };
        var monday = new DateTime(2026, 11, 2);

        var report = IncubationProjection
            .Create(Job(3, 100), EnzymeColorType.Invalid, EnzymeColorType.Invalid, EnzymeColorType.Invalid, 100, false)
            .Evaluate(mutation, monday);

        monday.DayOfWeek.Should().Be(DayOfWeek.Monday);
        report.IsReady.Should().BeFalse();
        report.Lines.Should().ContainSingle()
            .Which.Should().Be(new MutationRequirementLine("Complete Wed or Thu (UTC)", false, true));
        report.Summary.Should().Be("Enzymes met - wait for the day");
    }

    [Test]
    public void DayOfWeekRequirement_SaysServerTime()
    {
        new MutationRequirementDayOfWeek(DayOfWeek.Monday).GetRequirementDescription()
            .Should().Be("Only on Monday (server time, UTC)");
    }

    [Test]
    public void IncubationJob_SavedBeforeTheOptionExisted_StillAllowsMutation()
    {
        new IncubationJob().IsMutationSuppressed.Should().BeFalse();
    }

    [Test]
    public void DayOfWeekRequirement_ListsDaysInWeekOrder()
    {
        new MutationRequirementDayOfWeek(DayOfWeek.Thursday, DayOfWeek.Wednesday).DescribeDays()
            .Should().Be("Wednesday or Thursday");
    }
}
