using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The skill reads run on every swing (the attack skill of the weapon) and every XP award, so a read through
/// <see cref="GameServices.Skill"/> must not allocate: the property reads
/// a per-service slot and the instance reads its own tables. These tests count the bytes the
/// current thread allocates over many calls, through the accessor and on the instance, over the real catalogs. The reads
/// that ask the engine about a creature or an item (a creature's rank, the skill of an equipped weapon) are not here: the
/// recording engine fake allocates a record per call, so their cost says nothing about the service.
/// </summary>
[TestFixture]
[NonParallelizable]
public class SkillFacadeAllocationTests
{
    private const int Calls = 20000;

    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private ISkillService _instance = null!;

    [OneTimeSetUp]
    public void BuildContainer()
    {
        // Caching the catalog signals an event on the module, so it runs once under the engine fakes.
        _container = new FacadeContainer();
        _instance = _container.Get<ISkillService>();
        using (_container.Install())
        using (new PlayerEngine())
        using (EventsPluginRuntime.Override(new FakeEventsPlugin()))
        {
            _instance.CacheData();
            _instance.LoadMappings();
            _instance.CacheXPChartData();
        }
    }

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [SetUp]
    public void SetUp() => _installed = _container.Install();

    [TearDown]
    public void TearDown() => _installed.Dispose();

    private static long Allocated(Action call)
    {
        for (var i = 0; i < 100; i++)
            call();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Calls; i++)
            call();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Test]
    public void ReadingTheAccessorAllocatesNothingAndAnswersTheInstalledInstance()
    {
        ISkillService skill = null!;

        Allocated(() => skill = Skill).Should().Be(0);

        skill.Should().BeSameAs(_instance);
    }

    [Test]
    public void TheCatalogAndChartReadsAllocateNothingThroughTheAccessorOrOnTheInstance()
    {
        var sink = 0;

        foreach (var (name, throughAccessor, onInstance) in new (string, Action, Action)[]
                 {
                     ("GetSkillDetails", () => sink += Skill.GetSkillDetails(SkillType.Vibroblade).MaxRank, () => sink += _instance.GetSkillDetails(SkillType.Vibroblade).MaxRank),
                     ("GetSkillCategoryDetails", () => sink += Skill.GetSkillCategoryDetails(SkillCategoryType.Combat).Sequence, () => sink += _instance.GetSkillCategoryDetails(SkillCategoryType.Combat).Sequence),
                     ("GetSkillTypeByBaseItem", () => sink += (int)Skill.GetSkillTypeByBaseItem(BaseItem.Longsword), () => sink += (int)_instance.GetSkillTypeByBaseItem(BaseItem.Longsword)),
                     ("GetSkillTypeByBaseItem (unmapped)", () => sink += (int)Skill.GetSkillTypeByBaseItem(BaseItem.Torch), () => sink += (int)_instance.GetSkillTypeByBaseItem(BaseItem.Torch)),
                     ("GetAttackSkillTypeByBaseItem", () => sink += (int)Skill.GetAttackSkillTypeByBaseItem(BaseItem.Gloves), () => sink += (int)_instance.GetAttackSkillTypeByBaseItem(BaseItem.Gloves)),
                     ("GetRequiredXP", () => sink += Skill.GetRequiredXP(10), () => sink += _instance.GetRequiredXP(10)),
                     ("GetTotalRequiredXP", () => sink += Skill.GetTotalRequiredXP(10), () => sink += _instance.GetTotalRequiredXP(10)),
                     ("GetDeltaXP", () => sink += Skill.GetDeltaXP(2), () => sink += _instance.GetDeltaXP(2)),
                     ("APCap", () => sink += Skill.APCap, () => sink += _instance.APCap),
                 })
        {
            var accessor = Allocated(throughAccessor);
            var direct = Allocated(onInstance);

            accessor.Should().Be(0, name);
            direct.Should().Be(0, name);
        }

        GC.KeepAlive(sink);
    }
}
