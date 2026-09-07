using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class AnimationFileSafetyTests
{
    private string _folder = null!;
    [SetUp] public void Setup() { _folder = Path.Combine(Path.GetTempPath(), "swlor-animation-files-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_folder); }
    [TearDown] public void Teardown() { Directory.Delete(_folder, true); }

    [TestCase(false)] [TestCase(true)]
    public async Task AnotherWriterCreatingTheDestinationAtCommitIsNeverOverwritten(bool existing)
    {
        var path = Path.Combine(_folder, "project.swlanim");
        var original = Encoding.UTF8.GetBytes("accepted version");
        if (existing) File.WriteAllBytes(path, original);
        Func<Task> save = () => AnimationProjectFile.SaveAsync(path, Encoding.UTF8.GetBytes("my edit"), existing ? original : null,
            () => File.WriteAllText(path, "other writer's newer edit"));
        var failure = await save.Should().ThrowAsync<IOException>();
        File.ReadAllText(path).Should().Be("other writer's newer edit");
        var backups = Directory.GetFiles(_folder, "*.bak");
        if (existing)
        {
            backups.Should().HaveCount(1); File.ReadAllBytes(backups[0]).Should().Equal(original);
            failure.Which.Message.Should().Contain(backups[0]);
        }
        else backups.Should().BeEmpty();
        Directory.GetFiles(_folder, "*.tmp").Should().BeEmpty();
    }

    [Test] public async Task ChangedCapturedBytesAreRestoredWithoutCommitting()
    {
        var path = Path.Combine(_folder, "project.swlanim");
        File.WriteAllText(path, "external edit");
        var checkedCommit = false;
        Func<Task> save = () => AnimationProjectFile.SaveAsync(path, [1], Encoding.UTF8.GetBytes("old version"), () => checkedCommit = true);
        await save.Should().ThrowAsync<IOException>();
        checkedCommit.Should().BeFalse(); File.ReadAllText(path).Should().Be("external edit");
        Directory.GetFiles(_folder).Should().Equal(path);
    }

    [Test] public async Task CapturedFileStaysExclusivelyLeasedThroughPublication()
    {
        var path = Path.Combine(_folder, "project.swlanim");
        File.WriteAllBytes(path, [1, 2, 3]);
        var backup = await AnimationProjectFile.SaveAsync(path, [4, 5], [1, 2, 3], () =>
        {
            var captured = Directory.GetFiles(_folder, "*.bak").Single();
            Action write = () => { using var other = File.OpenWrite(captured); };
            write.Should().Throw<IOException>();
        });
        backup.Should().BeNull(); File.ReadAllBytes(path).Should().Equal(4, 5);
        Directory.GetFiles(_folder).Should().Equal(path);
    }

    [Test] public async Task CommitGuardFailureRestoresTheOriginalFile()
    {
        var path = Path.Combine(_folder, "project.swlanim");
        File.WriteAllBytes(path, [1, 2, 3]);
        Func<Task> save = () => AnimationProjectFile.SaveAsync(path, [4, 5], [1, 2, 3], () => throw new InvalidOperationException("Module locked"));
        await save.Should().ThrowAsync<InvalidOperationException>().WithMessage("Module locked");
        File.ReadAllBytes(path).Should().Equal(1, 2, 3); Directory.GetFiles(_folder).Should().Equal(path);
    }

    [Test] public void GltfRejectsTheSecondExternalBufferBeforeAllocatingBeyondTheAggregateBudget()
    {
        foreach (var name in new[] { "first.bin", "second.bin" })
            using (var stream = File.Create(Path.Combine(_folder, name))) stream.SetLength(120L * 1024 * 1024);
        var path = Path.Combine(_folder, "oversized.gltf");
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            asset = new { version = "2.0" },
            buffers = new[] { new { uri = "first.bin", byteLength = 1 }, new { uri = "second.bin", byteLength = 1 } }
        }));
        var before = GC.GetAllocatedBytesForCurrentThread();
        Action load = () => GltfAnimationSource.Load(path);
        load.Should().Throw<InvalidDataException>();
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(132L * 1024 * 1024,
            "the second 120 MiB file must be rejected using the remaining 8 MiB budget before allocation");
    }

    [Test] public void ConditionalRemovalPreservesAnotherWritersReplacement()
    {
        var path = Path.Combine(_folder, "created.mdl"); File.WriteAllBytes(path, [1, 2]);
        AnimationProjectFile.CommitStaged(path, null, [1, 2], () => File.WriteAllBytes(path, [3, 4]));
        File.ReadAllBytes(path).Should().Equal(3, 4);
        Directory.GetFiles(_folder).Should().Equal(path);
    }

    [Test] public void ConditionalRemovalRestoresAChangedCapturedFile()
    {
        var path = Path.Combine(_folder, "created.mdl"); File.WriteAllBytes(path, [3, 4]);
        Action remove = () => AnimationProjectFile.CommitStaged(path, null, [1, 2], () => { });
        remove.Should().Throw<IOException>();
        File.ReadAllBytes(path).Should().Equal(3, 4); Directory.GetFiles(_folder).Should().Equal(path);
    }

    [TestCase(false)] [TestCase(true)]
    public void InstallationRollsBackOnlyItsOwnOutputAfterALaterCommitConflict(bool competingEdit)
    {
        var first = Path.Combine(_folder, "first.mdl"); var second = Path.Combine(_folder, "second.mdl");
        File.WriteAllBytes(first, [1]); File.WriteAllBytes(second, [2]);
        var changes = new CommitConflictChanges([new(first, [1], [3]), new(second, [2], [4])], () =>
        {
            if (competingEdit) File.WriteAllBytes(first, [5]);
            File.WriteAllBytes(second, [6]);
        });
        var plan = new AnimationInstallPlan { AnimationName = "sw_test", ConstantName = "Test", Inputs = new Dictionary<string, byte[]>(), Changes = changes };
        Action install = plan.Apply;
        if (competingEdit) install.Should().Throw<AggregateException>();
        else install.Should().Throw<IOException>();
        File.ReadAllBytes(first).Should().Equal(competingEdit ? (byte)5 : (byte)1);
        File.ReadAllBytes(second).Should().Equal(6);
        Directory.GetFiles(_folder, "*.tmp").Should().BeEmpty();
        Directory.GetFiles(_folder, "*.bak").Should().BeEmpty();
    }

    private sealed class CommitConflictChanges(AnimationFileChange[] changes, Action conflict) : IReadOnlyList<AnimationFileChange>
    {
        private int _iterations;
        public int Count => changes.Length;
        public AnimationFileChange this[int index] => changes[index];
        public IEnumerator<AnimationFileChange> GetEnumerator()
        {
            var committing = ++_iterations == 3;
            yield return changes[0];
            if (committing) conflict();
            yield return changes[1];
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [TestCase(false)] [TestCase(true)]
    public void InstallationLeasesDependenciesThroughCommitAndReleasesThemAfterSuccessOrFailure(bool fail)
    {
        var input = Path.Combine(_folder, "inherited.mdl"); File.WriteAllBytes(input, [1]);
        var first = Path.Combine(_folder, "first.mdl"); var second = Path.Combine(_folder, "second.mdl");
        var checkedLease = false;
        var changes = new CommitConflictChanges([new(first, null, [3]), new(second, null, [4])], () =>
        {
            File.ReadAllBytes(first).Should().Equal(new byte[] { 3 }, "the first output has already been committed");
            Action rewrite = () => File.WriteAllBytes(input, [2]);
            rewrite.Should().Throw<IOException>("the dependency must remain stable between output commits");
            Action replace = () => File.Move(input, input + ".moved");
            replace.Should().Throw<IOException>("the input lease must also prevent replacement or deletion");
            checkedLease = true;
            if (fail) throw new IOException("Simulated later commit failure");
        });
        var plan = new AnimationInstallPlan { AnimationName = "sw_test", ConstantName = "Test",
            Inputs = new Dictionary<string, byte[]> { [input] = [1] }, Changes = changes };
        Action install = plan.Apply;
        if (fail) install.Should().Throw<IOException>(); else install.Should().NotThrow();
        checkedLease.Should().BeTrue(); File.Exists(first).Should().Be(!fail); File.Exists(second).Should().Be(!fail);
        File.ReadAllBytes(input).Should().Equal(1);
        File.WriteAllBytes(input, [2]); File.ReadAllBytes(input).Should().Equal(new byte[] { 2 }, "all leases must be released on every exit");
    }

    [Test] public void GltfRejectsAnOversizedDeclaredBufferBeforeOpeningItsFile()
    {
        var path = Path.Combine(_folder, "oversized.gltf");
        File.WriteAllText(path, JsonSerializer.Serialize(new { asset = new { version = "2.0" },
            buffers = new[] { new { uri = "does-not-exist.bin", byteLength = 128 * 1024 * 1024 + 1 } } }));
        Action load = () => GltfAnimationSource.Load(path);
        load.Should().Throw<InvalidDataException>().WithMessage("*oversized glTF buffers*");
    }
}
