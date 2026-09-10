using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.AnimationDrafts;
using SWLOR.NWN.Formats.Mdl;
using SWLOR.NWN.Formats;
using SWLOR.Toolset.Domain.Animation;

namespace SWLOR.Toolset.Tests;

public class InstalledMotionLibraryTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp() => _directory = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), "swlor-installed-library-" + Guid.NewGuid().ToString("N"))).FullName;

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, true);

    private (string Repository, string MountedOverlay) MountedFixture()
    {
        var repository = Directory.CreateDirectory(Path.Combine(_directory, "repository")).FullName;
        Directory.CreateDirectory(Path.Combine(repository, "Build"));
        var mounted = Directory.CreateDirectory(Path.Combine(repository, "SWLOR_Haks", "models")).FullName;
        File.WriteAllText(Path.Combine(repository, "SWLOR.Game.Server.sln"), "");
        File.WriteAllText(Path.Combine(repository, "Build", "hakbuilder.json"),
            "{\"HakList\":[{\"Path\":\"../SWLOR_Haks/models\"}]}");
        var overlay = Path.Combine(mounted, "bank.mdl");
        File.WriteAllText(overlay, "mounted bank");
        return (repository, overlay);
    }

    [Test]
    public void DirectlyMountedOverlayFindsItsRepository()
    {
        var fixture = MountedFixture();
        InstalledMotionLibrary.FindMountedRepository(fixture.MountedOverlay).Should().Be(fixture.Repository);
    }

    [Test]
    public void RepositoryArtifactDoesNotResolveThroughASameNamedMountedBank()
    {
        var fixture = MountedFixture();
        var artifacts = Directory.CreateDirectory(Path.Combine(fixture.Repository, "artifacts")).FullName;
        var overlay = Path.Combine(artifacts, "bank.mdl");
        File.WriteAllText(overlay, "standalone bank");
        InstalledMotionLibrary.FindMountedRepository(overlay).Should().BeNull();
    }

    [Test]
    public void SubdirectoryOfMountedLayerIsNotItselfMounted()
    {
        var fixture = MountedFixture();
        var nested = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(fixture.MountedOverlay)!, "nested")).FullName;
        var overlay = Path.Combine(nested, "bank.mdl");
        File.WriteAllText(overlay, "unindexed bank");
        InstalledMotionLibrary.FindMountedRepository(overlay).Should().BeNull();
    }

    [Test]
    public void OutsideRepositoryOverlayRemainsStandalone()
    {
        MountedFixture();
        var overlay = Path.Combine(_directory, "bank.mdl");
        File.WriteAllText(overlay, "standalone bank");
        InstalledMotionLibrary.FindMountedRepository(overlay).Should().BeNull();
    }

    [Test]
    public void RemainingReadBudgetIsCheckedWithoutChangingTheCounterOnFailure()
    {
        var path = Path.Combine(_directory, "bank.mdl");
        File.WriteAllBytes(path, [1, 2]);
        long loaded = AnimationInstall.MaximumInputBytes - 1;
        var before = loaded;
        Action read = () => InstalledMotionLibrary.ReadBank(path, ref loaded);
        read.Should().Throw<InvalidDataException>();
        loaded.Should().Be(before);
    }

    [Test]
    public void SuccessfulReadConsumesOnlyTheBytesActuallyReturned()
    {
        var path = Path.Combine(_directory, "bank.mdl");
        File.WriteAllBytes(path, [1, 2, 3]);
        long loaded = 5;
        InstalledMotionLibrary.ReadBank(path, ref loaded).Should().Equal(1, 2, 3);
        loaded.Should().Be(8);
    }

    [Test]
    public void SmallAliasedParentBanksCannotEachClaimAnIndependentDecodedAllowance()
    {
        var budget = new MdlReadBudget(64L * 1024 * 1024);
        var root = new MdlReader().Parse(AliasedBank("bank0", "bank1"), budget);
        var loads = new List<string>();
        var library = new InstalledMotionLibrary(root, name =>
        {
            loads.Add(name);
            var index = int.Parse(name[4..]);
            return AliasedBank(name, "bank" + (index + 1));
        }, budget);
        Action resolve = () => library.Resolve("sw_missing");
        resolve.Should().Throw<NwnFormatException>().WithMessage("*MDL model chain cumulative allocation budget*");
        loads.Should().Equal(new[] { "bank1", "bank2" },
            "the shared budget must stop retention long before the 32-bank depth limit or 128 MiB raw limit");
        budget.ReservedBytes.Should().BeLessThanOrEqualTo(64L * 1024 * 1024);
    }

    [Test]
    public void ReusingACachedDecodedParentDoesNotChargeTheSharedBudgetAgain()
    {
        var budget = new MdlReadBudget();
        byte[] Bytes(string name, string parent, string clip) => Encoding.ASCII.GetBytes(
            $"newmodel {name}\nsetsupermodel {name} {parent}\nbeginmodelgeom {name}\nnode dummy {name}\nparent NULL\nendnode\nendmodelgeom {name}\n" +
            $"newanim {clip} {name}\nlength 1\nnode dummy {name}\nparent NULL\npositionkey 1\n0 0 0 0\nendnode\ndoneanim {clip} {name}\ndonemodel {name}\n");
        var root = new MdlReader().Parse(Bytes("bank0", "bank1", "sw_first"), budget);
        var loads = 0;
        var library = new InstalledMotionLibrary(root, _ => { loads++; return Bytes("bank1", "NULL", "sw_second"); }, budget);
        library.Resolve("sw_second").Owner.Name.Should().Be("bank1");
        var reserved = budget.ReservedBytes;
        library.Resolve("SW_SECOND").Owner.Name.Should().Be("bank1");
        library.Resolve("sw_first").Owner.Name.Should().Be("bank0");
        budget.ReservedBytes.Should().Be(reserved);
        loads.Should().Be(1);
    }

    private static byte[] AliasedBank(string name, string parent)
    {
        const int meshCount = 3, faceCount = 32767, rootPointer = 232, childPointers = rootPointer + 112;
        const int meshNodes = childPointers + meshCount * 4;
        const int facePointer = meshNodes + meshCount * (112 + 512);
        var bytes = new byte[12 + facePointer + faceCount * 32];
        void UInt(int offset, int value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(offset, 4), (uint)value);
        UInt(4, bytes.Length - 12);
        Encoding.ASCII.GetBytes(name).CopyTo(bytes, 20);
        Encoding.ASCII.GetBytes(parent).CopyTo(bytes, 180);
        UInt(12 + 72, rootPointer);
        UInt(12 + rootPointer + 72, childPointers);
        UInt(12 + rootPointer + 76, meshCount);
        for (var i = 0; i < meshCount; i++)
        {
            var pointer = meshNodes + i * (112 + 512);
            UInt(12 + childPointers + i * 4, pointer);
            UInt(12 + pointer + 108, 0x20);
            UInt(12 + pointer + 112 + 8, facePointer);
            UInt(12 + pointer + 112 + 12, faceCount);
        }
        return bytes;
    }

    private static MdlModel Model(string name, string parent, params string[] animations)
    {
        var model = new MdlModel { Name = name, SuperModel = parent };
        model.Animations.AddRange(animations.Select(animation => new MdlAnimation { Name = animation }));
        return model;
    }

    [Test]
    public void ResolvesLaterBankAndReturnsItsOwningModel()
    {
        var root = Model("first", "second", "sw_old");
        var second = Model("second", "third");
        var third = Model("third", "NULL", "sw_fishing6");
        var models = new Dictionary<string, MdlModel> { [second.Name] = second, [third.Name] = third };
        var result = new InstalledMotionLibrary(root, name => models.GetValueOrDefault(name)).Resolve("sw_fishing6");
        result.Owner.Should().BeSameAs(third);
        result.Animation.Should().BeSameAs(third.Animations.Single());
    }

    [Test]
    public void ChildClipWinsWithoutLoadingShadowedParent()
    {
        var root = Model("first", "second", "sw_shared");
        var library = new InstalledMotionLibrary(root, _ => throw new AssertionException("A matching child must stop traversal."));
        library.Resolve("SW_SHARED").Animation.Should().BeSameAs(root.Animations.Single());
    }

    [Test]
    public void LoadedBanksAreReusedAcrossDifferentClipQueries()
    {
        var root = Model("first", "second");
        var second = Model("second", "third", "sw_second");
        var third = Model("third", "NULL", "sw_third");
        var loads = new List<string>();
        var library = new InstalledMotionLibrary(root, name =>
        {
            loads.Add(name);
            return name == second.Name ? second : third;
        });
        library.Resolve("sw_third").Owner.Should().BeSameAs(third);
        library.Resolve("sw_second").Owner.Should().BeSameAs(second);
        library.Resolve("SW_THIRD").Owner.Should().BeSameAs(third);
        loads.Should().Equal("second", "third");
    }

    [Test]
    public void MissingClipFailsAtTheEndOfAValidChain()
    {
        var library = new InstalledMotionLibrary(Model("first", "NULL", "sw_existing"), _ => null);
        Action resolve = () => library.Resolve("sw_missing");
        resolve.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void MissingParentCannotSilentlyProduceAnIncompletePreview()
    {
        var library = new InstalledMotionLibrary(Model("first", "missing"), _ => null);
        Action resolve = () => library.Resolve("sw_missing");
        resolve.Should().Throw<InvalidDataException>();
    }

    [Test]
    public void CaseVariantCycleFailsWithoutRepeatedLoading()
    {
        var root = Model("first", "second");
        var second = Model("second", "FIRST");
        var loads = 0;
        var library = new InstalledMotionLibrary(root, _ => { loads++; return second; });
        Action resolve = () => library.Resolve("sw_missing");
        resolve.Should().Throw<InvalidDataException>();
        loads.Should().BeLessThanOrEqualTo(2);
    }

    [Test]
    public void ExcessiveDepthStopsBeforeLoadingAnUnboundedChain()
    {
        var loads = 0;
        var library = new InstalledMotionLibrary(Model("bank0", "bank1"), name =>
        {
            loads++;
            var number = int.Parse(name[4..]);
            return Model(name, "bank" + (number + 1));
        });
        Action resolve = () => library.Resolve("sw_missing");
        resolve.Should().Throw<InvalidDataException>();
        loads.Should().BeLessThanOrEqualTo(32);
    }
}
