using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Animation;
using SWLOR.NWN.Formats.Mdl;

namespace SWLOR.Toolset.Tests;

public class AnimationBankRebalanceTests
{
    private string _root = null!;
    private string _models = null!;
    [SetUp] public void Setup()
    {
        _root = Path.Combine(Path.GetTempPath(), "swlor-rebalance-" + Guid.NewGuid().ToString("N"));
        _models = Path.Combine(_root, "SWLOR_Haks", "models");
        Directory.CreateDirectory(_models);
        Directory.CreateDirectory(Path.Combine(_root, "Build"));
        Directory.CreateDirectory(Path.Combine(_root, "design", "animations"));
        File.WriteAllText(Path.Combine(_root, "Build", "hakbuilder.json"), "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
        File.WriteAllText(Path.Combine(_models, "a_ba.mdl"), Model("a_ba", "an_a_ba", ""));
        File.WriteAllText(Path.Combine(_models, "an_a_ba.mdl"), Model("an_a_ba", "NULL", Triplet("sw_one") + Triplet("sw_two")));
        File.WriteAllText(Path.Combine(_root, "design", "animations", "registry.json"), JsonSerializer.Serialize(new[]
        {
            new AnimationRegistration("One", "sw_one", 1, ["SWLOR_Haks/models/a_ba.mdl"]),
            new AnimationRegistration("Two", "sw_two", 1, ["SWLOR_Haks/models/a_ba.mdl"])
        }));
    }
    [TearDown] public void Teardown() => Directory.Delete(_root, true);
    private static string Model(string name, string super, string blocks) =>
        $"# SWLOR authored animations for a_ba\nnewmodel {name}\nsetsupermodel {name} {super}\nclassification character\nsetanimationscale 1\nbeginmodelgeom {name}\nnode dummy {name}\n parent NULL\nendnode\nendmodelgeom {name}\n{blocks}donemodel {name}\n";
    private static string Triplet(string name) => string.Concat(new[] { name, name + "_in", name + "_out" }.Select(key =>
        $"newanim {key} an_a_ba\n length 1\n transtime 0.1\n animroot an_a_ba\nnode dummy an_a_ba\n parent NULL\n orientationkey 2\n 0 0 1 0 0.25\n 1 0 1 0 0.75\nendnode\ndoneanim {key} an_a_ba\n"));
    private AnimationInstallPlan Prepare() => AnimationBankRebalance.Prepare(_root, "a_ba", 1024 * 1024, 1);

    [Test]
    public void CompleteTripletsKeepTheirKeyPayloadAndNativeRigWhileNewBanksAvoidCollisions()
    {
        File.WriteAllText(Path.Combine(_models, "ab_a_ba_001.mdl"), Model("ab_a_ba_001", "NULL", ""));
        var beforeRig = File.ReadAllBytes(Path.Combine(_models, "a_ba.mdl"));
        var plan = Prepare();
        plan.Changes.Should().HaveCount(2);
        var first = new MdlReader().Parse(plan.Changes[0].After);
        first.SuperModel.Should().Be("ab_a_ba_002");
        var last = new MdlReader().Parse(plan.Changes[1].After);
        last.SuperModel.Should().BeNullOrEmpty();
        first.Animations.Select(a => a.Name).Should().BeEquivalentTo(["sw_one", "sw_one_in", "sw_one_out"]);
        last.Animations.Select(a => a.Name).Should().BeEquivalentTo(["sw_two", "sw_two_in", "sw_two_out"]);
        Encoding.UTF8.GetString(plan.Changes[0].After).Should().Contain(Triplet("sw_one"));
        Encoding.UTF8.GetString(plan.Changes[1].After).Replace("ab_a_ba_002", "an_a_ba")
            .Should().Contain(Triplet("sw_two"));
        plan.Apply();
        File.ReadAllBytes(Path.Combine(_models, "a_ba.mdl")).Should().Equal(beforeRig);
        Prepare().Changes.Should().BeEmpty("rebalancing is idempotent");
    }

    [Test]
    public void AChangedSourceAfterPreviewIsNeverOverwritten()
    {
        var plan = Prepare();
        var bank = Path.Combine(_models, "an_a_ba.mdl");
        File.AppendAllText(bank, "# concurrent edit\n");
        var changed = File.ReadAllBytes(bank);
        ((Action)(() => plan.Apply())).Should().Throw<IOException>();
        File.ReadAllBytes(bank).Should().Equal(changed);
        File.Exists(Path.Combine(_models, "ab_a_ba_001.mdl")).Should().BeFalse();
    }

    [Test]
    public void FailureDuringPublicationRollsBackTheWholeChain()
    {
        var plan = Prepare();
        var bank = Path.Combine(_models, "an_a_ba.mdl");
        var before = File.ReadAllBytes(bank);
        ((Action)(() => plan.Apply(index => { if (index == 1) throw new IOException("injected failure"); })))
            .Should().Throw<IOException>();
        File.ReadAllBytes(bank).Should().Equal(before);
        File.Exists(Path.Combine(_models, "ab_a_ba_001.mdl")).Should().BeFalse();
    }

    [Test]
    public void MissingExitPhaseRejectsBeforeAnyPublication()
    {
        var bank = Path.Combine(_models, "an_a_ba.mdl");
        File.WriteAllText(bank, File.ReadAllText(bank).Replace("sw_two_out", "sw_unknown"));
        ((Action)(() => Prepare())).Should().Throw<InvalidDataException>();
    }

    private (AnimationInstallPlan Plan, string Source) CompiledFixture()
    {
        var repository = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (repository != null && !Directory.Exists(Path.Combine(repository.FullName, "SWLOR.Toolset.Tests", "Fixtures", "Animation"))) repository = repository.Parent;
        var fixtures = Path.Combine(repository!.FullName, "SWLOR.Toolset.Tests", "Fixtures", "Animation");
        var binary = File.ReadAllBytes(Path.Combine(fixtures, "CompiledBank.mdl"));
        var text = File.ReadAllBytes(Path.Combine(fixtures, "CompiledBank.txt"));
        File.WriteAllText(Path.Combine(_models, "hero.mdl"), Model("hero", "an_hero", ""));
        var bank = Path.Combine(_models, "an_hero.mdl");
        File.WriteAllBytes(bank, binary);
        var source = AnimationBankSource.PathFor(Path.Combine(_root, "SWLOR_Haks"), bank);
        Directory.CreateDirectory(Path.GetDirectoryName(source)!);
        File.WriteAllBytes(source, AnimationBankSource.Encode(text, binary));
        File.WriteAllText(Path.Combine(_root, "design", "animations", "registry.json"), JsonSerializer.Serialize(new[]
        { new AnimationRegistration("Wave", "sw_wave", 1, ["SWLOR_Haks/models/hero.mdl"]) }));
        return (AnimationBankRebalance.Prepare(_root, "hero"), source);
    }

    [Test]
    public void ChangedCompiledCompanionAfterPreviewRejectsTheTransaction()
    {
        var (plan, source) = CompiledFixture();
        File.AppendAllText(source, "# external edit\n");
        ((Action)(() => plan.Apply())).Should().Throw<IOException>();
        ((Action)(() => AnimationBankRebalance.Prepare(_root, "hero"))).Should().Throw<InvalidDataException>()
            .WithMessage("*do not match*");
    }

    [Test]
    public void CompiledCompanionRemainsLeasedThroughoutPublication()
    {
        var (plan, source) = CompiledFixture();
        var output = Path.Combine(_root, "probe.json");
        File.WriteAllBytes(output, [1]);
        var transaction = new AnimationInstallPlan
        {
            AnimationName = plan.AnimationName, ConstantName = plan.ConstantName,
            Inputs = plan.Inputs, AbsentInputs = plan.AbsentInputs,
            Changes = [new(output, [1], [2])]
        };
        transaction.Apply(_ => ((Action)(() => File.WriteAllText(source, "concurrent writer"))).Should().Throw<IOException>());
        File.ReadAllBytes(output).Should().Equal(2);
        AnimationBankRebalance.Prepare(_root, "hero").Changes.Should().BeEmpty();
    }

    [Test]
    public void InheritedForeignRigBanksDoNotRetainUnrelatedEditableCompanions()
    {
        var registry = Path.Combine(_root, "design", "animations", "registry.json");
        var before = File.ReadAllBytes(registry);
        var (_, foreignSource) = CompiledFixture();
        File.WriteAllBytes(registry, before);
        File.WriteAllText(foreignSource, "This unrelated companion must never be read.");
        var bank = Path.Combine(_models, "an_a_ba.mdl");
        File.WriteAllText(bank, File.ReadAllText(bank).Replace("setsupermodel an_a_ba NULL", "setsupermodel an_a_ba an_hero"));
        var plan = Prepare();
        plan.Inputs.Should().NotContainKey(foreignSource);
        new MdlReader().Parse(plan.Changes[^1].After).SuperModel.Should().Be("an_hero");
    }

    [Test]
    public void OwnedBankOutsideHakRootIsRejected()
    {
        var outside = Path.Combine(_root, "outside");
        Directory.CreateDirectory(outside);
        File.Move(Path.Combine(_models, "an_a_ba.mdl"), Path.Combine(outside, "an_a_ba.mdl"));
        File.WriteAllText(Path.Combine(_root, "Build", "hakbuilder.json"), "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"},{\"Path\":\"../outside\"}]}");
        ((Action)(() => Prepare())).Should().Throw<InvalidDataException>().WithMessage("*outside SWLOR_Haks*");
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(129)]
    public void ClipLimitRejectsValuesOutsideTheSupportedRange(int clips)
        => ((Action)(() => AnimationBankRebalance.Prepare(_root, "a_ba", clips))).Should()
            .Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("clipsPerBank");

    [Test]
    public void SmallerExplicitClipLimitCreatesHeadroomWithoutChangingTheDefault()
    {
        AnimationBankRebalance.Prepare(_root, "a_ba").Changes.Should().BeEmpty();
        AnimationBankRebalance.Prepare(_root, "a_ba", 1).Changes.Should().HaveCount(2);
    }

    [Test]
    public void ExactBankSelectionDoesNotReadOtherOwnedCompanionsButLeasesTheirBinaries()
    {
        var (_, unselectedSource) = CompiledFixture();
        var unselectedBank = Path.Combine(_models, "an_hero.mdl");
        File.WriteAllText(unselectedSource, "Deliberately invalid: this unselected companion must not be decoded.");
        File.WriteAllText(Path.Combine(_models, "hero.mdl"), Model("hero", "ab_hero_001", ""));
        var selected = Path.Combine(_models, "ab_hero_001.mdl");
        File.WriteAllText(selected, Model("ab_hero_001", "an_hero", Triplet("sw_one") + Triplet("sw_two"))
            .Replace("an_a_ba", "ab_hero_001").Replace("authored animations for a_ba", "authored animations for hero"));
        File.WriteAllText(Path.Combine(_root, "design", "animations", "registry.json"), JsonSerializer.Serialize(new[]
        {
            new AnimationRegistration("Wave", "sw_wave", 1, ["SWLOR_Haks/models/hero.mdl"]),
            new AnimationRegistration("One", "sw_one", 1, ["SWLOR_Haks/models/hero.mdl"]),
            new AnimationRegistration("Two", "sw_two", 1, ["SWLOR_Haks/models/hero.mdl"])
        }));
        var plan = AnimationBankRebalance.Prepare(_root, "hero", 1, "ab_hero_001");
        plan.Inputs.Should().NotContainKey(unselectedSource).And.ContainKey(unselectedBank);
        plan.Changes.Should().HaveCount(2).And.NotContain(change => change.Path == unselectedBank);
        new MdlReader().Parse(plan.Changes[^1].After).SuperModel.Should().Be("an_hero");
        File.AppendAllText(unselectedBank, "external binary edit");
        ((Action)(() => plan.Apply())).Should().Throw<IOException>();
        File.ReadAllText(selected).Should().Contain("sw_two");
    }

    [TestCase("an_absent")]
    [TestCase("a_ba")]
    public void ExactBankSelectorRejectsMissingOrNonOwnedModels(string bank)
        => ((Action)(() => AnimationBankRebalance.Prepare(_root, "a_ba", 1, bank)))
            .Should().Throw<InvalidDataException>().WithMessage("*Selected bank*");

    [Test]
    public void ExactBankSelectionStillRejectsOrphanPhasesInAnUnselectedBank()
    {
        File.WriteAllText(Path.Combine(_models, "an_a_ba.mdl"), Model("an_a_ba", "ab_a_ba_001", Triplet("sw_one")));
        File.WriteAllText(Path.Combine(_models, "ab_a_ba_001.mdl"),
            Model("ab_a_ba_001", "NULL", Triplet("sw_two").Replace("sw_two_out", "sw_orphan").Replace("an_a_ba", "ab_a_ba_001")));
        ((Action)(() => AnimationBankRebalance.Prepare(_root, "a_ba", 1, "an_a_ba")))
            .Should().Throw<InvalidDataException>().WithMessage("*phases*");
    }

    [Test]
    public void SplitBanksSortNativeLookupTablesWithoutRewritingAnimationPayloads()
    {
        File.WriteAllText(Path.Combine(_models, "an_a_ba.mdl"), Model("an_a_ba", "NULL", Triplet("sw_two") + Triplet("sw_one") + Triplet("sw_three")));
        var registryPath = Path.Combine(_root, "design", "animations", "registry.json");
        var registry = JsonSerializer.Deserialize<List<AnimationRegistration>>(File.ReadAllText(registryPath))!;
        registry.Add(new("Three", "sw_three", 1, ["SWLOR_Haks/models/a_ba.mdl"]));
        File.WriteAllText(registryPath, JsonSerializer.Serialize(registry));
        var plan = AnimationBankRebalance.Prepare(_root, "a_ba", 2);
        foreach (var change in plan.Changes)
            new MdlReader().Parse(change.After).Animations.Select(animation => animation.Name.ToLowerInvariant())
                .Should().BeInAscendingOrder(StringComparer.Ordinal);
        Encoding.UTF8.GetString(plan.Changes[0].After).Should().Contain(Triplet("sw_one")).And.Contain(Triplet("sw_two"));
    }
}
