using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class AnimationInstallBatchTests
{
    private string _folder = null!;
    [SetUp] public void Setup() { _folder = Path.Combine(Path.GetTempPath(), "swlor-animation-batch-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_folder); }
    [TearDown] public void Teardown() { Directory.Delete(_folder, true); }

    private AnimationInstallPlan Change(string path, byte[] bytes) => new()
    {
        AnimationName = "sw_test", ConstantName = "Test", Inputs = new Dictionary<string, byte[]>(),
        Changes = [new(path, File.Exists(path) ? File.ReadAllBytes(path) : null, bytes)]
    };

    private AnimationInstallPlan WithFingerprint(string dependency, params AnimationFileChange[] changes) => new()
    {
        AnimationName = "sw_test", ConstantName = "Test", Inputs = new Dictionary<string, byte[]>(),
        ReadOnlyInputs = new Dictionary<string, AnimationInputFingerprint>
        { [dependency] = AnimationInputFingerprint.FromBytes(File.ReadAllBytes(dependency)) },
        Changes = changes
    };

    [Test]
    public void ReadOnlyFingerprintIsLeasedDuringPublicationAndRejectsStalePreview()
    {
        var dependency = Path.Combine(_folder, "native.mdl"); File.WriteAllBytes(dependency, [1, 2]);
        var output = Path.Combine(_folder, "result.json");
        var plan = WithFingerprint(dependency, new AnimationFileChange(output, null, [3]));
        plan.Apply(_ => ((Action)(() => File.WriteAllBytes(dependency, [9]))).Should().Throw<IOException>());
        File.WriteAllBytes(dependency, [9, 9]);
        ((Action)(() => plan.Apply())).Should().Throw<IOException>();
    }

    [Test]
    public void ReadOnlyFingerprintChangeBetweenStepsRollsBackOnlyPublishedOutputs()
    {
        var dependency = Path.Combine(_folder, "native.mdl"); File.WriteAllBytes(dependency, [1, 2]);
        var output = Path.Combine(_folder, "result.json");
        ((Action)(() => AnimationInstallBatch.Apply([
            () => WithFingerprint(dependency, new AnimationFileChange(output, null, [3])),
            () => { File.WriteAllBytes(dependency, [9, 9]); return Change(output, [4]); }
        ]))).Should().Throw<IOException>();
        File.ReadAllBytes(dependency).Should().Equal(9, 9);
        File.Exists(output).Should().BeFalse();
    }

    [Test]
    public void FingerprintedDependencyCanBecomeAnIntentionalBatchOutput()
    {
        var dependency = Path.Combine(_folder, "native.mdl"); File.WriteAllBytes(dependency, [1, 2]);
        AnimationInstallBatch.Apply([
            () => WithFingerprint(dependency),
            () => Change(dependency, [3, 4]),
            () => WithFingerprint(dependency)
        ]);
        File.ReadAllBytes(dependency).Should().Equal(3, 4);
    }

    [Test]
    public void ReadOnlyFingerprintsDoNotConsumeTheRollbackPayloadBudget()
    {
        var dependency = Path.Combine(_folder, "native.mdl"); File.WriteAllBytes(dependency, new byte[4096]);
        var output = Path.Combine(_folder, "result.json");
        AnimationInstallBatch.Apply([() => WithFingerprint(dependency, new AnimationFileChange(output, null, [3]))], 2);
        File.ReadAllBytes(output).Should().Equal(3);
    }

    [Test]
    public void LaterPreparationFailureRestoresOriginalFilesAndRemovesNewBanks()
    {
        var existing = Path.Combine(_folder, "registry.json"); File.WriteAllBytes(existing, [1]);
        var created = Path.Combine(_folder, "created.mdl");
        Action apply = () => AnimationInstallBatch.Apply([
            () => Change(existing, [2]), () => Change(created, [3]), () => Change(existing, [4]), () => Change(created, [5]),
            () => throw new InvalidDataException("Later animation is invalid.")]);
        apply.Should().Throw<InvalidDataException>();
        File.ReadAllBytes(existing).Should().Equal(1);
        File.Exists(created).Should().BeFalse();
        Directory.GetFiles(_folder).Should().Equal(existing);
    }

    [Test]
    public void LaterApplyFailureRollsBackAllEarlierSuccessfulPlans()
    {
        var existing = Path.Combine(_folder, "registry.json"); File.WriteAllBytes(existing, [1]);
        var blocked = Path.Combine(_folder, "blocked"); Directory.CreateDirectory(blocked);
        Action apply = () => AnimationInstallBatch.Apply([() => Change(existing, [2]), () => Change(blocked, [3])]);
        apply.Should().Throw<Exception>();
        File.ReadAllBytes(existing).Should().Equal(1);
        Directory.Exists(blocked).Should().BeTrue();
        Directory.GetFiles(_folder).Should().Equal(existing);
    }

    [Test]
    public void ConcurrentWriterIsPreservedAndOtherBatchFilesAreRestored()
    {
        var first = Path.Combine(_folder, "first.json"); File.WriteAllBytes(first, [1]);
        var second = Path.Combine(_folder, "second.json"); File.WriteAllBytes(second, [2]);
        Action apply = () => AnimationInstallBatch.Apply([() => Change(first, [3]), () => Change(second, [4]), () =>
        {
            File.WriteAllBytes(first, [9]);
            return Change(first, [5]);
        }]);
        apply.Should().Throw<AggregateException>();
        File.ReadAllBytes(first).Should().Equal(9);
        File.ReadAllBytes(second).Should().Equal(2);
        Directory.GetFiles(_folder).Should().BeEquivalentTo([first, second]);
    }

    [Test]
    public void SnapshotLimitRejectsLaterStepAndRestoresEarlierSteps()
    {
        var first = Path.Combine(_folder, "first.json"); File.WriteAllBytes(first, [1]);
        var second = Path.Combine(_folder, "second.json"); File.WriteAllBytes(second, [2]);
        Action apply = () => AnimationInstallBatch.Apply([() => Change(first, [3]), () => Change(second, [4, 5, 6])], 5);
        apply.Should().Throw<InvalidDataException>().WithMessage("*snapshot budget*");
        File.ReadAllBytes(first).Should().Equal(1);
        File.ReadAllBytes(second).Should().Equal(2);
    }

    [Test]
    public void ConcurrentEditOfAnUnchangedSourceFailsAndRollsBackGeneratedOutputs()
    {
        var source = Path.Combine(_folder, "source.swlanim"); File.WriteAllBytes(source, [1]);
        var bank = Path.Combine(_folder, "bank.mdl"); File.WriteAllBytes(bank, [2]);
        var later = Path.Combine(_folder, "later.mdl");
        Action apply = () => AnimationInstallBatch.Apply([
            () => new AnimationInstallPlan
            {
                AnimationName = "sw_test", ConstantName = "Test", Inputs = new Dictionary<string, byte[]>(),
                Changes = [new(source, [1], [1]), new(bank, [2], [3])]
            },
            () => { File.WriteAllBytes(source, [9]); return Change(later, [4]); }
        ]);
        apply.Should().Throw<IOException>().WithMessage("*changed during the animation batch*");
        File.ReadAllBytes(source).Should().Equal(9);
        File.ReadAllBytes(bank).Should().Equal(2);
        File.Exists(later).Should().BeFalse();
        Directory.GetFiles(_folder).Should().BeEquivalentTo([source, bank]);
    }

    [Test]
    public void VerificationOnlySourcesConsumeOnlyOneCopyOfTheSnapshotBudget()
    {
        var source = Path.Combine(_folder, "source.swlanim"); File.WriteAllBytes(source, [1, 2, 3]);
        AnimationInstallBatch.Apply([() => Change(source, [1, 2, 3])], 3).Should().BeEmpty();
        File.ReadAllBytes(source).Should().Equal(1, 2, 3);
    }

    [Test]
    public void NoOpAfterAChangedOutputStillRestoresTheOriginalOnFailure()
    {
        var path = Path.Combine(_folder, "bank.mdl"); File.WriteAllBytes(path, [1]);
        Action apply = () => AnimationInstallBatch.Apply([() => Change(path, [2]), () => Change(path, [2]),
            () => throw new InvalidDataException("Later failure")]);
        apply.Should().Throw<InvalidDataException>();
        File.ReadAllBytes(path).Should().Equal(1);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReadOnlyInputChangesFailEvenWhenTheInputIsNotAnOutput(bool reusedByLaterPlan)
    {
        var source = Path.Combine(_folder, "hakbuilder.json"); File.WriteAllBytes(source, [1]);
        var bank = Path.Combine(_folder, "bank.mdl"); File.WriteAllBytes(bank, [2]);
        AnimationInstallPlan Plan() => new()
        {
            AnimationName = "sw_test", ConstantName = "Test",
            Inputs = new Dictionary<string, byte[]> { [source] = File.ReadAllBytes(source) },
            Changes = [new(bank, File.ReadAllBytes(bank), [3])]
        };
        Action apply = () => AnimationInstallBatch.Apply([Plan, () =>
        {
            File.WriteAllBytes(source, [9]);
            return reusedByLaterPlan ? Plan() : Change(bank, [4]);
        }]);
        apply.Should().Throw<IOException>();
        File.ReadAllBytes(source).Should().Equal(9);
        File.ReadAllBytes(bank).Should().Equal(2);
    }

    [Test]
    public void LaterBatchOwnedUpdatesAdvanceTheExpectedInputSnapshot()
    {
        var source = Path.Combine(_folder, "source.json"); File.WriteAllBytes(source, [1]);
        AnimationInstallPlan Read() => new()
        {
            AnimationName = "sw_test", ConstantName = "Test", Changes = [],
            Inputs = new Dictionary<string, byte[]> { [source] = File.ReadAllBytes(source) }
        };
        AnimationInstallBatch.Apply([Read, () => Change(source, [2]), Read]).Should().BeEmpty();
        File.ReadAllBytes(source).Should().Equal(2);
        Action fail = () => AnimationInstallBatch.Apply([Read, () => Change(source, [3]), Read,
            () => throw new InvalidDataException("Failure")]);
        fail.Should().Throw<InvalidDataException>();
        File.ReadAllBytes(source).Should().Equal(2);
    }

    [Test]
    public void APreviouslyAbsentDependencyCannotAppearExternallyDuringTheBatch()
    {
        var source = Path.Combine(_folder, "missing.json");
        var bank = Path.Combine(_folder, "bank.json");
        Action apply = () => AnimationInstallBatch.Apply([
            () => new AnimationInstallPlan { AnimationName = "sw_test", ConstantName = "Test",
                Inputs = new Dictionary<string, byte[]>(), AbsentInputs = [source], Changes = [new(bank, null, [1])] },
            () => { File.WriteAllBytes(source, [9]); return Change(bank, [2]); }
        ]);
        apply.Should().Throw<IOException>();
        File.ReadAllBytes(source).Should().Equal(9);
        File.Exists(bank).Should().BeFalse();
    }

    [Test]
    public void SuccessfulBatchPublishesTheFinalStateOfRepeatedPaths()
    {
        var path = Path.Combine(_folder, "registry.json"); File.WriteAllBytes(path, [1]);
        AnimationInstallBatch.Apply([() => Change(path, [2]), () => Change(path, [3])]).Should().BeEmpty();
        File.ReadAllBytes(path).Should().Equal(3);
    }
}
