using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Dueling Ring's structure, its module resources, and the engine glue that the pure rules tests cannot reach: the
/// damage hook that runs before a blow lands, and the death handler that turns a duelist's fall into a subdual.
/// </summary>
public class DuelingRingWiringTests
{
    private const int DuelingRingId = 5101;

    // The structure

    [Test]
    public void TheRing_IsACityOnlyStructureWithTheReservedId()
    {
        ((int)StructureType.DuelingRing).Should().Be(DuelingRingId);

        var ring = StructureType.DuelingRing.GetAttribute<StructureType, StructureAttribute>();
        ring.Name.Should().Be("Dueling Ring");
        ring.IsActive.Should().BeTrue();
        ring.RestrictedPropertyTypes.Should().Be(PropertyType.City, "only a city, or a station, can have a ring");
        ring.LayoutType.Should().Be(PropertyLayoutType.Invalid, "the ring is a placeable, not a building with an interior");
        ring.CanBeRetrieved.Should().BeTrue();
    }

    [Test]
    public void TheRing_KeepsHostileSpawnsAndResourceNodesAwayLikeAnyBuilding()
    {
        SpawnExclusionRules.IsQualifyingStructure(StructureType.DuelingRing).Should().BeTrue();
    }

    [Test]
    public void TheRing_StandsOnExistingPlaceables()
    {
        var ring = StructureType.DuelingRing.GetAttribute<StructureType, StructureAttribute>();

        File.Exists(Path.Combine(Root, "Module", "utp", $"{ring.Resref}.utp.json"))
            .Should().BeTrue($"the ring terminal blueprint '{ring.Resref}' must exist in the module");
        File.Exists(Path.Combine(Root, "Module", "utp", "_mdrn_pl_tilec01.utp.json"))
            .Should().BeTrue("the marked ring under the terminal must exist in the module");
    }

    [Test]
    public void TheRingsItem_IsTheStructureItemOfItsId()
    {
        var path = Path.Combine(Root, "Module", "uti", $"structure_{DuelingRingId}.uti.json");
        File.Exists(path).Should().BeTrue();

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var item = document.RootElement;
        Text(item, "Tag").Should().Be($"structure_{DuelingRingId}");
        Text(item, "TemplateResRef").Should().Be($"structure_{DuelingRingId}");
        item.GetProperty("LocalizedName").GetProperty("value").GetProperty("0").GetString().Should().Be("Dueling Ring");
        item.GetProperty("Description").GetProperty("value").GetProperty("0").GetString()
            .Should().MatchRegex("^[\\x20-\\x7E]+$", "player-facing text is plain ASCII");

        File.ReadAllText(Path.Combine(Root, "Module", "itp", "itempalcus.itp.json"))
            .Should().Contain($"\"structure_{DuelingRingId}\"", "the toolset palette lists the item");
    }

    [Test]
    public void TheRing_IsWiredIntoPlacementAndRetrieval()
    {
        var actions = ReadServerSource("Service", "PropertyService", "StructureChangedAction.cs");

        actions.Should().Contain("Assign(StructureType.DuelingRing, StructureChangeType.PositionChanged, Duels.OnRingPlaced);");
        actions.Should().Contain("Assign(StructureType.DuelingRing, StructureChangeType.Retrieved, Duels.OnRingRetrieved);");
    }

    [Test]
    public void TheRingTerminal_UsesAScriptNameTheEngineCanLoad()
    {
        ScriptName.OnDuelingRingUsed.Length.Should().BeLessThanOrEqualTo(16, "NWN script names are at most 16 characters");
        ReadServerSource("Service", "Duels.cs").Should().Contain("[NWNEventHandler(ScriptName.OnDuelingRingUsed)]");
    }

    [Test]
    public void TheMenus_HaveAMainPage()
    {
        new DuelingRingDialog().Build().DefaultPageId.Should().Be("MAIN_PAGE");
        new DuelChallengeDialog().Build().DefaultPageId.Should().Be("MAIN_PAGE");
    }

    // Before the damage lands, before Death runs

    [Test]
    public void DamageToADuelist_IsSeenBeforeTheDamageLands()
    {
        var combat = ReadServerSource("Service", "Combat.cs");
        var method = combat.IndexOf("public static int ApplyDamageTakenModifiers(", StringComparison.Ordinal);
        var hook = combat.IndexOf("Duels.RecordDamage(defender, attacker);", method, StringComparison.Ordinal);
        var fatalSave = combat.IndexOf("TryPreventFatalDamageAndGrantTemporaryHP(defender, damage, restoreToOneHP: false)", method, StringComparison.Ordinal);

        method.Should().BeGreaterThan(0);
        hook.Should().BeGreaterThan(method);
        hook.Should().BeLessThan(fatalSave, "the damage pipeline runs the hook before the hit is applied");
        combat.Substring(hook - 60, 60).Should().Contain("if (isLandedAttack)", "a swing the engine discards must not void a duel");
        combat.Split("Duels.RecordDamage(").Length.Should().Be(2, "the damage pipeline is the one hook for weapons, abilities and effects");
    }

    [Test]
    public void ATrapThatSkipsTheDamagePipeline_StillReportsItsBlow()
    {
        ReadServerSource("Service", "Traps.cs").Should().Contain("Duels.RecordDamage(victim, record.Owner);");
    }

    [Test]
    public void ADuelistsFall_IsJudgedAfterTheShipDeathAndBeforeTheDeathPanel()
    {
        var death = ReadServerSource("Service", "Death.cs");
        var space = death.IndexOf("Space.ApplyDeath(player)", StringComparison.Ordinal);
        var spaceReturn = death.IndexOf("return;", space, StringComparison.Ordinal);
        var duel = death.IndexOf("Duels.TakeFall(player, hostile)", StringComparison.Ordinal);
        var panel = death.IndexOf("PopUpDeathGUIPanel", StringComparison.Ordinal);
        var stockSubdual = death.IndexOf("IsSubdualModeEnabled", StringComparison.Ordinal);

        space.Should().BeGreaterThan(0);
        spaceReturn.Should().BeLessThan(duel, "a ship death still ends the handler first");
        duel.Should().BeLessThan(stockSubdual, "a duel subdues whatever either player's settings are");
        duel.Should().BeLessThan(panel);
    }

    [Test]
    public void TheDuelSubdual_IsTheSameSubdualAsTheSettingBasedOne()
    {
        var death = ReadServerSource("Service", "Death.cs");

        death.Should().Contain("ApplySubdual(player, PvpCombat.GetControllingPlayer(hostile));");
        death.Should().Contain("ApplySubdual(player, hostile);");
        death.Split("EffectResurrection()").Length.Should().Be(3, "the subdual and the respawn are the only resurrections");
        death.Should().Contain("typeof(KnockdownStatusEffect), 60f");
        death.Should().Contain("typeof(SubdualPenaltyStatusEffect), 300f");
    }

    private static string Text(JsonElement item, string key) => item.GetProperty(key).GetProperty("value").GetString();

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        }
    }

    private static string ReadServerSource(params string[] path)
    {
        var full = Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(path).ToArray());
        return File.ReadAllText(full).Replace("\r\n", "\n");
    }
}
