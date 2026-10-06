using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.ShipDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.Formats.Tlk;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipFittingTests
{
    private static readonly ShipFittingCatalog Catalog = ShipFittingCatalog.Default;

    [Test]
    public void PlayerHullDefinitions_HaveAllApprovedProfiles()
    {
        var ships = new PlayerShipDefinition().BuildShips();
        ships.Keys.Should().BeEquivalentTo(Catalog.Hulls.Keys);
        foreach (var (id, detail) in ships) detail.FittingProfile.Should().Be(Catalog.Hulls[id]);
        Catalog.Hulls.Should().HaveCount(26);
        Catalog.Modules.Should().HaveCount(47);
        Catalog.Variants.Should().HaveCount(207);
    }

    [Test]
    public void EveryVariant_MatchesApprovedNumericalSpecification()
    {
        var source = Specification();
        foreach (var row in source["module_variants"]!)
        {
            var variant = Catalog.GetVariant((string)row["design"]!, (string)row["calibration"]!);
            variant.Power.Should().Be((int)row["power"]!);
            variant.Capacitor.Should().Be((int)row["capacitor"]!);
            variant.Output.Should().BeApproximately((double)row["output"]!, 1e-9);
            variant.Cycle.Should().BeApproximately((double)row["cycle"]!, 1e-9);
            variant.Tracking.Should().BeApproximately((double)row["tracking"]!, 1e-9);
            variant.Range.Should().BeApproximately((double)row["range"]!, 1e-9);
            variant.RecoveryFraction.Should().BeApproximately((double)row["recovery_fraction"]!, 1e-9);
            variant.EngineeringRank.Should().Be((int)row["engineering"]!);
            ShipModuleTuning.CapacitorCost(Catalog.Modules[variant.Design].Capacitor,
                variant.CapacitorMultiplier).Should().Be(variant.Capacitor);
        }
    }

    [Test]
    public void EveryApprovedBuild_HasLegalSlotsMountsPowerAndOperatingRanks()
    {
        foreach (var build in Specification()["builds"]!)
        {
            var skills = ((JObject)build["skills"]!).Properties().ToDictionary(
                p => Enum.Parse<SkillType>(p.Name.Replace(" ", "")), p => (int)p.Value);
            var fittings = build["modules"]!.Select(id => new ShipFittingModule((string)id!));
            var result = ShipFittingCalculator.Calculate((string)build["hull"]!, fittings,
                skills, (string)build["configuration"]!);
            result.Errors.Should().BeEmpty((string)build["name"]!);
            result.IsLegal.Should().BeTrue();
        }
    }

    [Test]
    public void StartingFreighter_SupportsTheAdvancedCompactIndustryFitAt37Power()
    {
        var fits = new[] { "compact_drill", "deep_scanner", "hull_repair" }
            .Select(id => new ShipFittingModule(id, "Compact"));
        var ranks = new Dictionary<SkillType, int> {
            [SkillType.Piloting] = 1, [SkillType.SpaceIndustry] = 20 };
        var result = ShipFittingCalculator.Calculate("ShipDeedLightFreighter", fits, ranks);
        result.Errors.Should().BeEmpty();
        result.PowerUsed.Should().Be(37);
        result.Hull.Power.Should().Be(40);
    }

    [Test]
    public void ManufacturingRank_IsSeparateFromOperatingRank()
    {
        var ranks = new Dictionary<SkillType, int> { [SkillType.Piloting] = 1 };
        var result = ShipFittingCalculator.Calculate("ShipDeedLightEscort",
            new[] { new ShipFittingModule("tracking_laser") }, ranks);
        result.IsLegal.Should().BeTrue();
        Catalog.Modules["tracking_laser"].EngineeringRank.Should().Be(5);
        Catalog.Modules["tracking_laser"].OperatorRank.Should().Be(0);
    }

    [Test]
    public void UnrelatedSkills_DoNotUnlockDemandingIndustryHardware()
    {
        var ranks = new Dictionary<SkillType, int> {
            [SkillType.Piloting] = 50, [SkillType.Gunnery] = 50, [SkillType.Engineering] = 50 };
        var result = ShipFittingCalculator.Calculate("ShipDeedLightFreighter",
            new[] { new ShipFittingModule("compact_drill", "Compact") }, ranks);
        result.Errors.Should().Contain(x => x.Contains("SpaceIndustry rank 20"));
    }

    [Test]
    public void CompactCalibration_PreservesMountRestrictions()
    {
        var result = ShipFittingCalculator.Calculate("ShipDeedLightEscort",
            new[] { new ShipFittingModule("sustained_beam", "Compact") },
            new Dictionary<SkillType, int> { [SkillType.Piloting] = 50 });
        result.Errors.Should().Contain(x => x.Contains("Standard mount"));
    }

    [Test]
    public void PowerAndSlots_AreValidatedIndependently()
    {
        var result = ShipFittingCalculator.Calculate("ShipDeedLightEscort",
            Enumerable.Repeat(new ShipFittingModule("tracking_laser", "High Output"), 4),
            new Dictionary<SkillType, int> { [SkillType.Piloting] = 1 });
        result.PowerUsed.Should().Be(40);
        result.Errors.Should().Contain(x => x.StartsWith("Fitting power"));
        result.Errors.Should().Contain(x => x.StartsWith("High slots"));
    }

    [Test]
    public void UniqueModuleLimit_PreventsDuplicateProtectedHolds()
    {
        var result = ShipFittingCalculator.Calculate("ShipDeedLightFreighter",
            Enumerable.Repeat(new ShipFittingModule("protected_hold"), 2),
            new Dictionary<SkillType, int> { [SkillType.Piloting] = 1 });
        result.Errors.Should().Contain(x => x.Contains("at most 1"));
    }

    [Test]
    public void MissingMountMetadata_FailsClosed()
    {
        var json = JObject.Parse(File.ReadAllText(Path.Combine(Root(), "SWLOR.Game.Server", "Data", "ShipFitting.json")));
        ((JObject)json["modules"]![0]!).Remove("mount");
        Action load = () => ShipFittingCatalog.Load(json.ToString());
        load.Should().Throw<JsonSerializationException>();
    }

    [TestCase(SkillType.Piloting, "Piloting")]
    [TestCase(SkillType.Gunnery, "Gunnery")]
    [TestCase(SkillType.ShipSystems, "Ship Systems")]
    [TestCase(SkillType.Astrometrics, "Astrometrics")]
    [TestCase(SkillType.SpaceIndustry, "Space Industry")]
    public void OperatingSkills_AreAvailableUnderTheSharedRankCap(SkillType skill, string name)
    {
        var detail = skill.GetAttribute<SkillType, SkillAttribute>();
        detail.Name.Should().Be(name);
        detail.MaxRank.Should().Be(50);
        detail.IsActive.Should().BeTrue();
        detail.ContributesToSkillCap.Should().BeTrue();
        detail.CharacterTypeRestriction.Should().Be(SWLOR.Game.Server.Enumeration.CharacterType.Invalid);
        detail.IsShownInCraftMenu.Should().BeFalse();
    }

    [TestCase(SkillType.Gunnery, "Gunnery", 6198)]
    [TestCase(SkillType.ShipSystems, "Ship Systems", 6199)]
    [TestCase(SkillType.Astrometrics, "Astrometrics", 6200)]
    [TestCase(SkillType.SpaceIndustry, "Space Industry", 6201)]
    public void OperatingSkillLabels_Match2DaAndCompiledTlk(SkillType skill, string name, int tlkId)
    {
        var row = File.ReadLines(Path.Combine(Root(), "SWLOR_Haks", "sw_2da", "iprp_skill.2da"))
            .Select(x => x.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            .Single(x => x.Length >= 3 && x[0] == ((int)skill).ToString());
        int.Parse(row[1]).Should().Be(16777216 + tlkId);
        // Verify the generated binary, not only its authoring JSON.
        var tlk = TlkReader.Read(File.ReadAllBytes(Path.Combine(Root(), "SWLOR_Haks", "sw_tlk", "sw_tlk.tlk")));
        tlk.Entries[tlkId].Text.Should().Be(name);
    }

    internal static JObject Specification() => JObject.Parse(
        File.ReadAllText(Path.Combine(Root(), "design", "space", "space-balance.json")));

    internal static string Root()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository.");
    }
}
