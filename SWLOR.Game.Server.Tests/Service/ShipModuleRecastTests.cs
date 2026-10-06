using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipModuleRecastTests
{
    [Test]
    public void ActiveSlots_HaveIndependentGroupsAndPassiveConfigurationHasNone()
    {
        var slots = ShipModuleFeat.GetAll();
        var active = slots.Where(x => (int)x.Key >= (int)FeatType.ShipModule1 && (int)x.Key <= (int)FeatType.ShipModule20).Select(x => x.Value).ToArray();
        active.Should().HaveCount(20);
        active.Select(x => x.RecastGroup).Should().OnlyHaveUniqueItems().And.NotContain(RecastGroup.Invalid);
        foreach (var slot in active)
        {
            var label = typeof(RecastGroup).GetField(slot.RecastGroup.ToString())!.GetCustomAttribute<RecastGroupAttribute>()!;
            label.ShortName.Length.Should().BeLessThanOrEqualTo(14);
        }
        slots.Where(x => (int)x.Key >= (int)FeatType.ShipModule21).Select(x => x.Value.RecastGroup).Should().OnlyContain(x => x == RecastGroup.Invalid);
    }

    [Test]
    public void EveryActiveModule_HasAllSixNativeCooldownTextures()
    {
        var textures = typeof(IShipModuleListDefinition).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(x))
            .SelectMany(x => ((IShipModuleListDefinition)Activator.CreateInstance(x)!).BuildShipModules().Values)
            .Where(x => x.CalculateRecastAction != null)
            .Select(x => x.Texture).Distinct().ToArray();
        textures.Should().NotBeEmpty();
        foreach (var texture in textures)
        for (var stage = 0; stage <= AbilityCooldownVisual.MaximumCooldownStage; stage++)
        {
            var resref = AbilityCooldownVisual.GetCooldownTextureName(texture, stage);
            resref.Should().NotBeNull($"{texture} must support cooldown artwork");
            var path = Path.Combine(RepositoryRoot(), "SWLOR_Haks", "sw_ability", resref + ".tga");
            File.Exists(path).Should().BeTrue(path);
            var data = File.ReadAllBytes(path);
            BitConverter.ToUInt16(data, 12).Should().Be(32, path);
            BitConverter.ToUInt16(data, 14).Should().Be(32, path);
            data[17].Should().Be(8, "NWN icons use eight alpha bits and bottom-left origin");
        }
    }
    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository.");
    }
}
