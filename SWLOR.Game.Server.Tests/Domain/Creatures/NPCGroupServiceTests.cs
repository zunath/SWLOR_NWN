using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.NPCService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Creatures;

/// <summary>
/// Unit tests of the NPC group instance. The cache is filled by the module handler from the attributes of
/// <see cref="NPCGroupType"/>, so no engine is needed; the facade tests prove the container holds one instance.
/// </summary>
[TestFixture]
public class NPCGroupServiceTests
{
    private NPCGroupService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new NPCGroupService();
    }

    private static NPCGroupAttribute DeclaredAttribute(NPCGroupType type) =>
        typeof(NPCGroupType).GetField(type.ToString())!.GetCustomAttribute<NPCGroupAttribute>()!;

    [Test]
    public void GetNPCGroup_BeforeTheCacheLoads_HasNoGroups()
    {
        var read = () => _service.GetNPCGroup(NPCGroupType.Invalid);

        read.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void CacheData_CachesTheDeclaredDetailsOfEveryGroup()
    {
        _service.CacheData();

        foreach (var type in Enum.GetValues<NPCGroupType>())
        {
            var cached = _service.GetNPCGroup(type);
            cached.Should().NotBeNull($"{type} is cached");
            cached.Name.Should().Be(DeclaredAttribute(type).Name, $"{type} keeps its declared name");
        }

        _service.GetNPCGroup(NPCGroupType.Invalid).Name.Should().Be("Invalid");
        _service.GetNPCGroup(NPCGroupType.CZ220_Mynocks).Name.Should().Be("Mynocks");
    }

    [Test]
    public void CacheData_ReportsHowManyGroupsItLoaded()
    {
        var original = Console.Out;
        var captured = new StringWriter();
        Console.SetOut(captured);
        try
        {
            _service.CacheData();
        }
        finally
        {
            Console.SetOut(original);
        }

        captured.ToString().Should().Contain($"Loaded {Enum.GetValues<NPCGroupType>().Length} NPC groups.");
    }

    [Test]
    public void CacheData_RunTwice_ReplacesTheEntriesWithoutDuplicatingThem()
    {
        _service.CacheData();
        var first = _service.GetNPCGroup(NPCGroupType.CZ220_Mynocks);

        _service.CacheData();

        _service.GetNPCGroup(NPCGroupType.CZ220_Mynocks).Name.Should().Be(first.Name);
    }

    [Test]
    public void EachInstanceKeepsItsOwnCache()
    {
        var other = new NPCGroupService();
        _service.CacheData();

        var read = () => other.GetNPCGroup(NPCGroupType.CZ220_Mynocks);

        read.Should().Throw<KeyNotFoundException>("the cache is instance state, not shared between instances");
    }

    [Test]
    public void TheFacadeLoadsAndReadsTheContainersInstance()
    {
        using var container = new FacadeContainer();
        using (container.Install())
        {
            NPCGroup.CacheData();

            NPCGroup.GetNPCGroup(NPCGroupType.CZ220_Mynocks).Name.Should().Be("Mynocks");
        }

        container.Get<INPCGroupService>().GetNPCGroup(NPCGroupType.CZ220_Mynocks).Name.Should().Be("Mynocks");
    }
}
