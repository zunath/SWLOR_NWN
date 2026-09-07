using System.Text;
using System.Text.Json;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.Toolset.Domain.Render;

namespace SWLOR.Toolset.Tests;

public class AnimationFileSafetyTests
{
    private string _folder = null!;
    [SetUp] public void Setup() { _folder = Path.Combine(Path.GetTempPath(), "swlor-animation-files-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_folder); }
    [TearDown] public void Teardown() { Directory.Delete(_folder, true); }

    private (AnimationProject Project, string Target) InstallationFixture()
    {
        var project = new AnimationProject { Name = "Wave", ModelName = "hero", AnimationRoot = "rootdummy",
            Joints = [new("hero", -1, new(Vector3.Zero, Quaternion.Identity, 1)),
                new("rootdummy", 0, new(Vector3.UnitZ, Quaternion.Identity, 1))] };
        var target = Path.Combine(_folder, "SWLOR_Haks", "models", "hero.mdl");
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.WriteAllText(target, "newmodel hero\nsetsupermodel hero NULL\n" + AnimationMdl.ExportGeometry(project) + "donemodel hero\n");
        Directory.CreateDirectory(Path.Combine(_folder, "Build"));
        File.WriteAllText(Path.Combine(_folder, "Build", "hakbuilder.json"), "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
        return (project, target);
    }

    [Test]
    public void CompletedBankSizeAllocatesANewBankAndRemainsEditable()
    {
        var (project, target) = InstallationFixture();
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        var native = File.ReadAllBytes(target);
        var first = Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl");
        var budget = File.ReadAllBytes(first).Length + 200;
        project.Name = "Other";
        var install = AnimationInstall.Prepare(_folder, project, [target], AnimationInstall.MaximumInputBytes, bankBudget: budget);
        install.Apply();
        var firstModel = new MdlReader().Parse(File.ReadAllBytes(first));
        var second = Path.Combine(Path.GetDirectoryName(target)!, firstModel.SuperModel + ".mdl");
        firstModel.Animations.Select(a => a.Name).Should().Contain("sw_wave").And.NotContain(install.AnimationName);
        new MdlReader().Parse(File.ReadAllBytes(second)).Animations.Should().Contain(a => a.Name == install.AnimationName);
        File.ReadAllBytes(first).Length.Should().BeLessThanOrEqualTo(budget);
        File.ReadAllBytes(second).Length.Should().BeLessThanOrEqualTo(budget);
        File.ReadAllBytes(target).Should().Equal(native);
        project.Duration = 2;
        AnimationInstall.Prepare(_folder, project, [target], AnimationInstall.MaximumInputBytes, bankBudget: budget).Apply();
        new MdlReader().Parse(File.ReadAllBytes(second)).Animations.Single(a => a.Name == install.AnimationName).Length.Should().Be(2);
    }

    [TestCase(false)] [TestCase(true)]
    public void OversizedNewAndReplacementBanksAreRejectedBeforeWriting(bool replacement)
    {
        var (project, target) = InstallationFixture();
        var budget = 100;
        if (replacement)
        {
            AnimationInstall.Prepare(_folder, project, [target]).Apply();
            budget = File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")).Length + 200;
            for (var i = 0; i <= 100; i++)
            {
                var pose = project.Joints.Select(j => j.Rest).ToArray();
                pose[1] = pose[1] with { Position = new Vector3(i / 100f, 0, 1) };
                project.SetKey(i / 100f, pose);
            }
        }
        var before = Directory.GetFiles(_folder, "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllBytes);
        Action prepare = () => AnimationInstall.Prepare(_folder, project, [target], AnimationInstall.MaximumInputBytes, bankBudget: budget);
        prepare.Should().Throw<InvalidDataException>().WithMessage("*bank exceeds its input size limit*");
        Directory.GetFiles(_folder, "*", SearchOption.AllDirectories).Should().BeEquivalentTo(before.Keys);
        foreach (var file in before) File.ReadAllBytes(file.Key).Should().Equal(file.Value);
    }

    [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)]
    public void OccupiedShortBankNamesAllocateAndRetainAFreeName(bool otherLayer, bool inherited)
    {
        var (project, target) = InstallationFixture();
        var collisionFolder = otherLayer ? Path.Combine(_folder, "SWLOR_Haks", "other") : Path.GetDirectoryName(target)!;
        Directory.CreateDirectory(collisionFolder);
        var collision = Path.Combine(collisionFolder, "an_hero.mdl");
        File.WriteAllText(collision, File.ReadAllText(target).Replace("hero", "an_hero"));
        var original = File.ReadAllBytes(collision);
        if (otherLayer) File.WriteAllText(Path.Combine(_folder, "Build", "hakbuilder.json"),
            "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"},{\"Path\":\"../SWLOR_Haks/other\"}]}");
        if (inherited) File.WriteAllText(target, File.ReadAllText(target).Replace("setsupermodel hero NULL", "setsupermodel hero an_hero"));

        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        var bank = new MdlReader().Parse(File.ReadAllBytes(target)).SuperModel;
        bank.Should().NotBe("an_hero"); bank.Length.Should().BeLessThanOrEqualTo(16);
        var overlayPath = Path.Combine(Path.GetDirectoryName(target)!, bank + ".mdl");
        new MdlReader().Parse(File.ReadAllBytes(overlayPath)).SuperModel.Should().Be(inherited ? "an_hero" : "");
        File.ReadAllBytes(collision).Should().Equal(original);
        if (!inherited) File.Delete(collision); // Freeing the default must not change an installed bank's identity.
        project.Duration = 2;
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        new MdlReader().Parse(File.ReadAllBytes(target)).SuperModel.Should().Be(bank);
        new MdlReader().Parse(File.ReadAllBytes(overlayPath)).Animations.Single(a => a.Name == "sw_wave").Length.Should().Be(2);
        if (inherited) File.ReadAllBytes(collision).Should().Equal(original);
    }

    [Test]
    public void InstallationKeepsBendsInTheTargetsLocalJointFrame()
    {
        var (project, target) = InstallationFixture();
        var targetRig = project.Clone();
        targetRig.Joints[1] = targetRig.Joints[1] with { Rest = targetRig.Joints[1].Rest with
            { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2) } };
        File.WriteAllText(target, "newmodel hero\nsetsupermodel hero NULL\n" + AnimationMdl.ExportGeometry(targetRig) + "donemodel hero\n");
        project.Joints[1] = project.Joints[1] with { Rest = project.Joints[1].Rest with
            { Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 2) } };
        var start = project.Sample(0); var finish = (PosedNode[])start.Clone();
        finish[1] = finish[1] with { Orientation = start[1].Orientation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2) };
        project.SetKey(0, start); project.SetKey(1, finish);
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        var bank = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(target)!, "an_hero.mdl")));
        var motion = bank.Animations.Single(a => a.Name == "sw_wave");
        foreach (var time in new[] { 0f, .5f, 1f })
        {
            var rotation = MdlAnimationPose.Sample(motion, time)["rootdummy"].Orientation;
            // A local Y bend under a target rest rolled 90 degrees around X sweeps X toward Y.
            var expected = new Vector3(MathF.Cos(time * MathF.PI / 2), MathF.Sin(time * MathF.PI / 2), 0);
            Vector3.Distance(Vector3.Transform(Vector3.UnitX, rotation), expected).Should().BeLessThan(1e-5f);
            Vector3.Distance(Vector3.Transform(Vector3.UnitY, rotation), Vector3.UnitZ).Should().BeLessThan(1e-5f);
        }
    }

    [TestCase(null, false)] [TestCase("social", false)] [TestCase("social/greetings", false)] [TestCase("social/greetings", true)]
    public void InstallationKeepsOneSourceAndReusesItsRegisteredCategory(string? category, bool bom)
    {
        var (project, target) = InstallationFixture();
        var categorized = category != null;
        var relative = $"design/animations/{category ?? "uncategorized"}/Wave.swlanim";
        var source = Path.Combine(_folder, relative);
        var original = Encoding.UTF8.GetBytes(project.Serialize().Replace("\r\n", "\n").Replace("\n", "\r\n") + "\r\n");
        if (bom) original = new UTF8Encoding(true).GetPreamble().Concat(original).ToArray();
        if (categorized)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(source)!);
            File.WriteAllBytes(source, original);
        }
        var plan = AnimationInstall.Prepare(_folder, project, [target], categorized ? source : null);
        plan.Apply();
        if (categorized) File.ReadAllBytes(source).Should().Equal(original, "installation must preserve the provenance hash");
        var registryPath = Path.Combine(_folder, "design/animations/registry.json");
        JsonSerializer.Deserialize<AnimationRegistration[]>(File.ReadAllText(registryPath))!.Single().ProjectPath.Should().Be(relative);
        project.Duration = 2;
        AnimationInstall.Prepare(_folder, project, [target], Path.Combine(_folder, "elsewhere/Wave.swlanim")).Apply();
        Directory.GetFiles(Path.Combine(_folder, "design/animations"), "*.swlanim", SearchOption.AllDirectories).Select(Path.GetFullPath).Should().Equal(Path.GetFullPath(source));
        AnimationProject.Deserialize(File.ReadAllText(source)).Duration.Should().Be(2);
    }

    [Test]
    public async Task ByteBackedProjectLoadingAcceptsAUtf8Bom()
    {
        var (project, _) = InstallationFixture();
        var path = Path.Combine(_folder, "bom.swlanim");
        var bytes = new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(project.Serialize())).ToArray();
        File.WriteAllBytes(path, bytes);
        var loadedBytes = await AnimationProject.ReadFileBytesAsync(path);
        AnimationProject.Deserialize(Encoding.UTF8.GetString(loadedBytes)).Serialize().Should().Be(project.Serialize());
        loadedBytes.Should().Equal(bytes, "external-change detection must retain the exact disk bytes");
    }

    [Test]
    public void LegacyRegistryWithoutSourcePathKeepsItsExistingProjectInPlace()
    {
        var (project, target) = InstallationFixture();
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        var legacy = Path.Combine(_folder, "design/animations/Wave.swlanim");
        File.Move(Path.Combine(_folder, "design/animations/uncategorized/Wave.swlanim"), legacy);
        var registryPath = Path.Combine(_folder, "design/animations/registry.json");
        var entries = JsonSerializer.Deserialize<AnimationRegistration[]>(File.ReadAllText(registryPath))!;
        File.WriteAllText(registryPath, JsonSerializer.Serialize(entries.Select(e => new { e.Name, e.AnimationName, e.Duration, e.Targets })));
        project.Duration = 2;
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        Directory.GetFiles(Path.Combine(_folder, "design/animations"), "*.swlanim", SearchOption.AllDirectories).Select(Path.GetFullPath).Should().Equal(Path.GetFullPath(legacy));
        AnimationProject.Deserialize(File.ReadAllText(legacy)).Duration.Should().Be(2);
    }

    [TestCase("../Wave.swlanim")]
    [TestCase("design/animations/social/../../Wave.swlanim")]
    [TestCase("design/animations/social/Other.swlanim")]
    [TestCase("SWLOR.Game.Server/Wave.swlanim")]
    public void RegistryCannotRedirectSourceWritesOutsideItsNamedProject(string path)
    {
        var (project, target) = InstallationFixture();
        AnimationInstall.Prepare(_folder, project, [target]).Apply();
        var registryPath = Path.Combine(_folder, "design/animations/registry.json");
        var entries = JsonSerializer.Deserialize<AnimationRegistration[]>(File.ReadAllText(registryPath))!;
        File.WriteAllText(registryPath, JsonSerializer.Serialize(entries.Select(e => e with { ProjectPath = path })));
        Action prepare = () => AnimationInstall.Prepare(_folder, project, [target]);
        prepare.Should().Throw<InvalidDataException>().WithMessage("*Invalid source project path*");
    }

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

    [TestCase(false)] [TestCase(true)]
    public void InstallationReservesMissingResolutionPathsUntilCommitOrRollbackCompletes(bool fail)
    {
        var missing = Path.Combine(_folder, "higher-priority", "inherited.mdl");
        var first = Path.Combine(_folder, "first.mdl"); var second = Path.Combine(_folder, "second.mdl");
        var checkedReservation = false;
        var changes = new CommitConflictChanges([new(first, null, [3]), new(second, null, [4])], () =>
        {
            File.Exists(first).Should().BeTrue();
            Action competingWriter = () => File.WriteAllBytes(missing, [5]);
            competingWriter.Should().Throw<IOException>("a higher-priority resource must not appear between output commits");
            checkedReservation = true;
            if (fail) throw new IOException("Simulated later commit failure");
        });
        var plan = new AnimationInstallPlan { AnimationName = "sw_test", ConstantName = "Test",
            Inputs = new Dictionary<string, byte[]>(), AbsentInputs = [missing, first, second], Changes = changes };
        Action install = plan.Apply;
        if (fail) install.Should().Throw<IOException>(); else install.Should().NotThrow();
        checkedReservation.Should().BeTrue(); File.Exists(first).Should().Be(!fail); File.Exists(second).Should().Be(!fail);
        File.Exists(missing).Should().BeFalse("delete-on-close must remove every reservation on every exit");
        File.WriteAllBytes(missing, [5]); File.ReadAllBytes(missing).Should().Equal(5);
    }

    [Test] public void TooManyAbsentDependenciesFailBeforeStagingOrOpeningReservations()
    {
        var output = Path.Combine(_folder, "outputs", "created.mdl");
        var missing = Enumerable.Range(0, AnimationInstall.MaximumAbsentReservations + 1)
            .Select(i => Path.Combine(_folder, "missing", "model" + i + ".mdl")).ToArray();
        var plan = new AnimationInstallPlan { AnimationName = "sw_test", ConstantName = "Test", Changes = [new(output, null, [1])],
            Inputs = new Dictionary<string, byte[]>(), AbsentInputs = missing };
        Action apply = plan.Apply;
        apply.Should().Throw<InvalidDataException>().WithMessage("*too many missing model dependencies*");
        Directory.GetFileSystemEntries(_folder).Should().BeEmpty();
    }

    [Test] public void ReservationLimitCountsUniqueNonOutputPathsOnly()
    {
        var output = Path.Combine(_folder, "created.mdl");
        var missing = Enumerable.Range(0, AnimationInstall.MaximumAbsentReservations)
            .Select(i => Path.Combine(_folder, "model" + i + ".mdl")).ToArray();
        var plan = new AnimationInstallPlan { AnimationName = "sw_test", ConstantName = "Test", Changes = [new(output, null, [1])],
            Inputs = new Dictionary<string, byte[]>(), AbsentInputs = missing.Concat(missing).Append(output).ToArray() };
        plan.GetAbsentReservationPaths().Should().HaveCount(AnimationInstall.MaximumAbsentReservations);
        Directory.GetFileSystemEntries(_folder).Should().BeEmpty();
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
