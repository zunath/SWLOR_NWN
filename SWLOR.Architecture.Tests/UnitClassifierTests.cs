using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

[TestFixture]
public class UnitClassifierTests
{
    [TestCase("SWLOR.Game.Server.Service", "Combat", "Combat")]
    [TestCase("SWLOR.Game.Server.Service", "CombatPoint", "CombatPoint")]
    [TestCase("SWLOR.Game.Server.Service.CombatService", "HitResult", "Combat")]
    [TestCase("SWLOR.Game.Server.Service.GuiService.Component", "GuiButton", "Gui")]
    [TestCase("SWLOR.Game.Server.Service.NPCService", "NPCGroupType", "NPC")]
    [TestCase("SWLOR.Game.Server.Service.ChatPortraitTellService", "ChatPortraitClick", "ChatPortraitTell")]
    [TestCase("SWLOR.Game.Server.Service.Animation", "Folder", "Animation")]
    [TestCase("SWLOR.Game.Server.Feature", "Auditing", "Feature.Auditing")]
    [TestCase("SWLOR.Game.Server.Feature.PerkDefinition", "ArmorPerkDefinition", "Feature/PerkDefinition")]
    [TestCase("SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition", "Egg", "Feature/BeastDefinition")]
    [TestCase("SWLOR.Game.Server.Core", "Closure", "Platform/Core")]
    [TestCase("SWLOR.Game.Server.Core.Beamdog", "Wrapper", "Platform/Core")]
    [TestCase("SWLOR.Game.Server.Native", "Hook", "Platform/Native")]
    [TestCase("SWLOR.Game.Server.Entity", "Player", "Platform/Entity")]
    [TestCase("SWLOR.Game.Server", "ApplicationSettings", "Platform/Root")]
    public void Classifies_a_type_by_its_namespace(string @namespace, string name, string expectedUnit)
    {
        DependencyUnit.TryParse(expectedUnit, out var expected).Should().BeTrue();

        UnitClassifier.Classify(new TypeKey(@namespace, name)).Should().Be(expected);
    }

    private const string DomainMapJson = """
        {
          "platformDomain": "Platform",
          "featureTypeDomain": "Gameplay",
          "unrestrictedTargetDomains": ["Core", "Platform"],
          "domains": {
            "Core": { "services": ["DB"], "featureFolders": [] },
            "Character": { "services": ["Achievement", "Stat"], "featureFolders": [] },
            "Space": { "services": ["Space", "SpaceModuleRecast"], "featureFolders": [] },
            "Gameplay": { "services": [], "featureFolders": [] }
          }
        }
        """;

    [TestCase("SWLOR.Game.Server.Domain.Character", "IAchievementService", "Achievement")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "AchievementService", "Achievement")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "AchievementNotification", "Achievement")]
    [TestCase("SWLOR.Game.Server.Domain.Character.Achievement", "AchievementWindow", "Achievement")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "IStatService", "Stat")]
    [TestCase("SWLOR.Game.Server.Domain.Core", "IDBService", "DB")]
    [TestCase("SWLOR.Game.Server.Domain.Space", "SpaceService", "Space")]
    [TestCase("SWLOR.Game.Server.Domain.Space", "ISpaceModuleRecastService", "SpaceModuleRecast")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "AchievementsHelper", "Platform/Domain.Character")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "Unrelated", "Platform/Domain.Character")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "IAmbient", "Platform/Domain.Character")]
    [TestCase("SWLOR.Game.Server.Domain.Unknown", "IAchievementService", "Platform/Domain.Unknown")]
    public void Classifies_the_types_of_a_converted_service_as_the_service(string @namespace, string name, string expectedUnit)
    {
        DependencyUnit.TryParse(expectedUnit, out var expected).Should().BeTrue();

        UnitClassifier.Classify(new TypeKey(@namespace, name), DomainMap.Parse(DomainMapJson)).Should().Be(expected);
    }

    [Test]
    public void Without_a_map_the_types_of_a_domain_namespace_are_platform_code()
    {
        UnitClassifier.Classify(new TypeKey("SWLOR.Game.Server.Domain.Character", "IAchievementService")).Should().Be(
            new DependencyUnit(UnitKind.Platform, "Domain.Character"));
    }

    [TestCase("", "<Module>")]
    [TestCase("", "<>f__AnonymousType0")]
    [TestCase("System.Runtime.CompilerServices", "IsExternalInit")]
    [TestCase("SWLOR.Game.ServerOther", "Look")]
    [TestCase("SWLOR.NWN.API.NWScript", "NWScript")]
    public void Types_outside_the_server_belong_to_no_unit(string @namespace, string name)
    {
        UnitClassifier.Classify(new TypeKey(@namespace, name)).Should().BeNull();
    }

    [TestCase("Combat")]
    [TestCase("Feature/PerkDefinition")]
    [TestCase("Feature.Auditing")]
    [TestCase("Platform/Native")]
    public void A_unit_reads_back_from_its_text_form(string text)
    {
        DependencyUnit.TryParse(text, out var unit).Should().BeTrue();

        unit.ToString().Should().Be(text);
    }

    [TestCase("")]
    [TestCase("Feature/")]
    [TestCase("Feature.")]
    [TestCase("Platform/")]
    [TestCase("Combat Stat")]
    [TestCase("Unknown/Thing")]
    public void Text_that_is_not_a_unit_is_rejected(string text)
    {
        DependencyUnit.TryParse(text, out _).Should().BeFalse();
    }
}
