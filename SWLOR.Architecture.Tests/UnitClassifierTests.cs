using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <remarks>
/// The <c>Service.&lt;Name&gt;Service</c> cases use a made-up service (<c>Widget</c>): <c>tools/Refactor/Move-SupportNamespaces.ps1</c>
/// rewrites every text file of the repository, and a real service name here would be rewritten when master brings back its
/// support folder.
/// </remarks>
[TestFixture]
public class UnitClassifierTests
{
    [TestCase("SWLOR.Game.Server.Domain.Combat", "CombatService", "Domain/Combat")]
    [TestCase("SWLOR.Game.Server.Domain.Combat", "ICombatRecorderService", "Domain/Combat")]
    [TestCase("SWLOR.Game.Server.Domain.Combat", "HitResult", "Domain/Combat")]
    [TestCase("SWLOR.Game.Server.Domain.UI.Component", "GuiButton", "Domain/UI")]
    [TestCase("SWLOR.Game.Server.Domain.Crafting.Minigame.Search", "SearchSetup", "Domain/Crafting")]
    [TestCase("SWLOR.Game.Server.Domain.Foundation", "IDBService", "Domain/Foundation")]
    [TestCase("SWLOR.Game.Server.Domain.Unknown", "Anything", "Domain/Unknown")]
    [TestCase("SWLOR.Game.Server.Service.WidgetService", "WidgetResult", "Widget")]
    [TestCase("SWLOR.Game.Server.Service.WidgetService.Topic", "WidgetPart", "Widget")]
    [TestCase("SWLOR.Game.Server.Service.Animation", "Folder", "Animation")]
    [TestCase("SWLOR.Game.Server.Feature", "Auditing", "Feature.Auditing")]
    [TestCase("SWLOR.Game.Server.Feature.PerkDefinition", "ArmorPerkDefinition", "Feature/PerkDefinition")]
    [TestCase("SWLOR.Game.Server.Feature.BeastDefinition.IncubationBeastDefinition", "Egg", "Feature/BeastDefinition")]
    [TestCase("SWLOR.Game.Server.Core", "Closure", "Platform/Core")]
    [TestCase("SWLOR.Game.Server.Core.Beamdog", "Wrapper", "Platform/Core")]
    [TestCase("SWLOR.Game.Server.Native", "Hook", "Platform/Native")]
    [TestCase("SWLOR.Game.Server.Entity", "Player", "Platform/Entity")]
    [TestCase("SWLOR.Game.Server.Domain", "Loose", "Platform/Domain")]
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
          "unrestrictedTargetDomains": ["Foundation", "Platform"],
          "domains": {
            "Foundation": { "services": ["DB"], "featureFolders": [] },
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
    [TestCase("SWLOR.Game.Server.Domain.Foundation", "IDBService", "DB")]
    [TestCase("SWLOR.Game.Server.Domain.Space", "SpaceService", "Space")]
    [TestCase("SWLOR.Game.Server.Domain.Space", "ISpaceModuleRecastService", "SpaceModuleRecast")]
    [TestCase("SWLOR.Game.Server.Service.WidgetService", "Part", "Widget")]
    public void Names_the_service_a_type_is_named_after(string @namespace, string name, string expectedService)
    {
        UnitClassifier.ServiceOf(new TypeKey(@namespace, name), DomainMap.Parse(DomainMapJson)).Should().Be(expectedService);
    }

    [TestCase("SWLOR.Game.Server.Domain.Character", "AchievementsHelper")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "Unrelated")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "IAmbient")]
    [TestCase("SWLOR.Game.Server.Domain.Character", "ISpaceService")]
    [TestCase("SWLOR.Game.Server.Domain.Unknown", "IAchievementService")]
    [TestCase("SWLOR.Game.Server.Feature", "AchievementService")]
    [TestCase("SWLOR.Game.Server.Core", "StatService")]
    [TestCase("SWLOR.NWN.API", "StatService")]
    [TestCase("SWLOR.Game.Server.Service", "Widget")]
    public void A_type_that_names_no_service_of_its_domain_has_none(string @namespace, string name)
    {
        UnitClassifier.ServiceOf(new TypeKey(@namespace, name), DomainMap.Parse(DomainMapJson)).Should().BeNull();
    }

    [Test]
    public void A_domain_unit_belongs_to_its_domain_when_the_map_declares_it()
    {
        var map = DomainMap.Parse(DomainMapJson);

        map.DomainOf(new DependencyUnit(UnitKind.Domain, "Character")).Should().Be("Character");
        map.DomainOf(new DependencyUnit(UnitKind.Domain, "Foundation")).Should().Be("Foundation");
        map.DomainOf(new DependencyUnit(UnitKind.Domain, "Unknown")).Should().BeNull("a Domain namespace that is not a declared domain is unmapped");
        map.DomainOf(new DependencyUnit(UnitKind.Domain, "Platform")).Should().BeNull();
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

    [Test]
    public void A_type_directly_in_Service_belongs_to_no_unit()
    {
        // P2-Z3 left nothing directly in Service; the namespace guard fails a new type there, so no unit is made for it.
        UnitClassifier.Classify(new TypeKey("SWLOR.Game.Server.Service", "Widget")).Should().BeNull();
    }

    [TestCase("Domain/Combat")]
    [TestCase("Widget")]
    [TestCase("Feature/PerkDefinition")]
    [TestCase("Feature.Auditing")]
    [TestCase("Platform/Native")]
    public void A_unit_reads_back_from_its_text_form(string text)
    {
        DependencyUnit.TryParse(text, out var unit).Should().BeTrue();

        unit.ToString().Should().Be(text);
    }

    [TestCase("")]
    [TestCase("Domain/")]
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
