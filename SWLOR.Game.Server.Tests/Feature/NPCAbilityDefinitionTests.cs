using System.IO.Compression;
using System.Globalization;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AIDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class NPCAbilityDefinitionTests
{
    [Test]
    public void NPCAbilities_MatchBibleRecastAndResourceCosts()
    {
        var abilities = BuildNPCAbilities();
        var expected = ExpectedAbilities();

        abilities.Keys.Should().BeEquivalentTo(expected.Keys);

        foreach (var (feat, expectedAbility) in expected)
        {
            abilities.Should().ContainKey(feat);

            var ability = abilities[feat];
            ability.Name.Should().Be(expectedAbility.Name);
            ability.RecastGroup.Should().Be(expectedAbility.RecastGroup);
            ability.RecastDelay.Should().NotBeNull();
            ability.RecastDelay(0).Should().Be(expectedAbility.RecastSeconds);

            ability.Requirements
                .OfType<AbilityRequirementStamina>()
                .Should()
                .ContainSingle()
                .Which
                .RequiredSTM
                .Should()
                .Be(expectedAbility.StaminaCost);

            if (expectedAbility.FPCost.HasValue)
            {
                ability.Requirements
                    .OfType<AbilityRequirementFP>()
                    .Should()
                    .ContainSingle()
                    .Which
                    .RequiredFP
                    .Should()
                    .Be(expectedAbility.FPCost.Value);
            }
            else
            {
                ability.Requirements.OfType<AbilityRequirementFP>().Should().BeEmpty();
            }
        }
    }

    [Test]
    public void NPCAbilities_AreDocumentedInBibleNpcAbilitiesTab()
    {
        var root = FindRepositoryRoot();
        using var archive = ZipFile.OpenRead((root / "design" / "bible" / "SWLOR Design Bible - Combat Upgrade.xlsx").FullName);
        var worksheet = ReadWorksheetByName(archive, "NPC Abilities");
        var sharedStrings = ReadSharedStrings(archive);
        var documentedRows = ReadNpcAbilityBibleRows(worksheet, sharedStrings);
        var expectedAbilities = BuildAllNPCAbilities();

        documentedRows
            .Select(row => row.Ability)
            .Should()
            .OnlyHaveUniqueItems("each NPC ability should have exactly one source-of-truth Bible row");

        var documentedByAbility = documentedRows.ToDictionary(row => row.Ability);

        documentedByAbility.Keys
            .Should()
            .BeEquivalentTo(
                expectedAbilities.Values.Select(ability => ability.Ability.Name),
                "the NPC Abilities Bible tab should document every NPC ability definition, including generated signature abilities");

        foreach (var (feat, expectedAbility) in expectedAbilities)
        {
            documentedByAbility.Should().ContainKey(expectedAbility.Ability.Name);
            var row = documentedByAbility[expectedAbility.Ability.Name];

            row.Feat.Should().Be($"FeatType.{feat}");
            row.SourceFile.Should().Be(expectedAbility.SourceFile);
            row.Targeting.Should().NotBeNullOrWhiteSpace();
            row.Hostile.Should().NotBeNullOrWhiteSpace();
            row.Area.Should().NotBeNullOrWhiteSpace();
            row.RequiresTarget.Should().NotBeNullOrWhiteSpace();
            row.MaxRange.Should().NotBeNullOrWhiteSpace();
            row.ActivationDelay.Should().NotBeNullOrWhiteSpace();
            row.RecastGroup.Should().NotBeNullOrWhiteSpace();
            row.Recast.Should().NotBeNullOrWhiteSpace();
            row.Stamina.Should().NotBeNullOrWhiteSpace();
            row.DamageResistance.Should().NotBeNullOrWhiteSpace();
            row.StatusEffect.Should().NotBeNullOrWhiteSpace();
            row.Duration.Should().NotBeNullOrWhiteSpace();
            row.Notes.Should().NotBeNullOrWhiteSpace();

            var detail = expectedAbility.Ability;
            row.Hostile.Should().Be(detail.IsHostileAbility ? "Yes" : "No");
            row.Area.Should().Be(detail.IsAreaAbility ? "Yes" : "No");
            row.RequiresTarget.Should().Be(detail.RequiresTarget ? "Yes" : "No");
            row.ActivationDelay.Should().Be($"{FormatNumber(detail.ActivationDelay?.Invoke(0, 0, detail.AbilityLevel) ?? 0f)}s");
            row.RecastGroup.Should().Be(detail.RecastGroup.ToString());
            row.Recast.Should().Be($"{FormatNumber(detail.RecastDelay?.Invoke(0) ?? 0f)}s");

            var stamina = detail.Requirements.OfType<AbilityRequirementStamina>().Single().RequiredSTM;
            row.Stamina.Should().Be($"{stamina} STM");
        }
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    [Test]
    public void NPCAbilities_HaveMatchingFeat2daRows()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");

        foreach (var feat in ExpectedAbilities().Keys)
        {
            featRows.Should().ContainKey((int)feat, $"{feat} must have a usable feat.2da row");
            featRows[(int)feat]["LABEL"].Should().Be(feat.ToString());
        }
    }

    [Test]
    public void NewResistanceThreatAbilities_UseFeatOnly2daRowsWithSharedNpcSpell()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");

        spellRows[904]["Label"].Should().Be("Bite", "NPC innate abilities reuse the shared feat-activation spell row");

        foreach (var feat in NewResistanceThreatAbilities())
        {
            featRows[(int)feat]["LABEL"].Should().Be(feat.ToString());
            featRows[(int)feat]["SPELLID"].Should().Be("904");
            spellRows.Values.Should().NotContain(
                row => row.ContainsKey("Label") && row["Label"] == feat.ToString(),
                $"{feat} should not require a separate spells.2da row");
        }
    }

    [Test]
    public void NPCAbilities_AreAvailableToDefaultNpcAI()
    {
        Ability.CacheData();
        var profiles = new DefaultAIProfileDefinition().BuildProfiles();
        var genericAbilityFeats = profiles[AIProfileType.Generic].Actions
            .Where(action => action.Type == AIActionType.Ability)
            .Select(action => action.Feat)
            .ToHashSet();

        foreach (var feat in ExpectedAbilities().Keys)
        {
            Ability.IsFeatRegistered(feat).Should().BeTrue($"{feat} must be registered before AI can execute it");
            genericAbilityFeats.Should().Contain(feat, $"{feat} should be exposed through the Generic NPC AI profile");
        }
    }

    [Test]
    public void ForceSunder_AppliesBeamFromCasterAfterSuccessfulHit()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "NPC" / "ForceSunderAbilityDefinition.cs").FullName);
        var normalizedSource = source.Replace("\r\n", "\n");

        source.Should().Contain("afterSuccessfulHit: ApplyForceSunderBeam");
        source.Should().Contain("EffectBeam(VisualEffect.Vfx_Beam_Drain, activator, BodyNode.Hand)");
        normalizedSource.Should().NotContain("ResistanceType.Disruption,\n                VisualEffect.Vfx_Beam_Drain,");
    }

    [Test]
    public void ConcussiveChallenge_UsesDamageAndControlInsteadOfEnmityOnlyPressure()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(
            (root / "SWLOR.Game.Server" / "Feature" / "AbilityDefinition" / "NPC" / "ConcussiveChallengeAbilityDefinition.cs").FullName);
        var normalizedSource = source.Replace("\r\n", "\n");

        normalizedSource.Should().Contain("5,\n                30,\n                6,\n                typeof(DazedStatusEffect)");
        source.Should().Contain("CombatDamageType.Sonic");
        source.Should().Contain("ResistanceType.Mind");
        source.Should().NotContain("enmityBonus");
    }

    private static Dictionary<FeatType, AbilityDetail> BuildNPCAbilities()
    {
        return BuildAllNPCAbilities()
            .Where(entry => !IsGeneratedNPCSignatureFeat(entry.Key))
            .ToDictionary(entry => entry.Key, entry => entry.Value.Ability);
    }

    private static Dictionary<FeatType, BuiltNPCAbility> BuildAllNPCAbilities()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var npcAbilityNamespace = typeof(ArcPulseAbilityDefinition).Namespace;
        var abilities = new Dictionary<FeatType, BuiltNPCAbility>();

        var definitions = typeof(ArcPulseAbilityDefinition)
            .Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                type.Namespace == npcAbilityNamespace &&
                definitionType.IsAssignableFrom(type))
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!)
            .ToList();

        foreach (var definition in definitions)
        {
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                abilities.Add(feat, new BuiltNPCAbility(ability, $"{definition.GetType().Name}.cs"));
            }
        }

        return abilities;
    }

    private static bool IsGeneratedNPCSignatureFeat(FeatType feat)
    {
        return (int)feat >= (int)FeatType.BraceBreaker &&
               (int)feat <= (int)FeatType.ApexCollapse;
    }

    private static Dictionary<FeatType, ExpectedNPCAbility> ExpectedAbilities()
    {
        return new Dictionary<FeatType, ExpectedNPCAbility>
        {
            [FeatType.RendingBite] = new("Rending Bite", RecastGroup.RendingBite, 14f, 3),
            [FeatType.CripplingTalons] = new("Crippling Talons", RecastGroup.CripplingTalons, 10f, 3),
            [FeatType.PiercingQuills] = new("Piercing Quills", RecastGroup.PiercingQuills, 18f, 5),
            [FeatType.ToxicSpit] = new("Toxic Spit", RecastGroup.ToxicSpit, 18f, 3),
            [FeatType.ScorchingBreath] = new("Scorching Breath", RecastGroup.ScorchingBreath, 22f, 5),
            [FeatType.InfernoBlast] = new("Inferno Blast", RecastGroup.InfernoBlast, 34f, 8),
            [FeatType.SeismicSlam] = new("Seismic Slam", RecastGroup.SeismicSlam, 28f, 6),
            [FeatType.RupturingQuake] = new("Rupturing Quake", RecastGroup.RupturingQuake, 48f, 10),
            [FeatType.TerrifyingBellow] = new("Terrifying Bellow", RecastGroup.TerrifyingBellow, 20f, 4),
            [FeatType.DisorientingScreech] = new("Disorienting Screech", RecastGroup.DisorientingScreech, 36f, 7),
            [FeatType.IronCarapace] = new("Iron Carapace", RecastGroup.IronCarapace, 32f, 4),
            [FeatType.MaulingBite] = new("Mauling Bite", RecastGroup.MaulingBite, 13f, 4),
            [FeatType.BonecrusherBite] = new("Bonecrusher Bite", RecastGroup.BonecrusherBite, 16f, 5),
            [FeatType.RakingClaws] = new("Raking Claws", RecastGroup.RakingClaws, 11f, 3),
            [FeatType.PouncingStrike] = new("Pouncing Strike", RecastGroup.PouncingStrike, 18f, 5),
            [FeatType.TailSweep] = new("Tail Sweep", RecastGroup.TailSweep, 18f, 5),
            [FeatType.GoringCharge] = new("Goring Charge", RecastGroup.GoringCharge, 22f, 6),
            [FeatType.BarbedVolley] = new("Barbed Volley", RecastGroup.BarbedVolley, 17f, 5),
            [FeatType.VenomSpray] = new("Venom Spray", RecastGroup.VenomSpray, 18f, 5),
            [FeatType.ToxicCloud] = new("Toxic Cloud", RecastGroup.ToxicCloud, 24f, 7),
            [FeatType.FrostSpit] = new("Frost Spit", RecastGroup.FrostSpit, 24f, 2),
            [FeatType.StaticBurst] = new("Static Burst", RecastGroup.StaticBurst, 20f, 6),
            [FeatType.SavageRoar] = new("Savage Roar", RecastGroup.SavageRoar, 21f, 4),
            [FeatType.SonicShriek] = new("Sonic Shriek", RecastGroup.SonicShriek, 19f, 5),
            [FeatType.ChitinGuard] = new("Chitin Guard", RecastGroup.ChitinGuard, 32f, 5),
            [FeatType.PrecisionShot] = new("Precision Shot", RecastGroup.PrecisionShot, 15f, 4),
            [FeatType.SuppressingShot] = new("Suppressing Shot", RecastGroup.SuppressingShot, 17f, 4),
            [FeatType.GrenadeBurst] = new("Grenade Burst", RecastGroup.GrenadeBurst, 22f, 6),
            [FeatType.SerratedSlash] = new("Serrated Slash", RecastGroup.SerratedSlash, 15f, 4),
            [FeatType.BrutalBash] = new("Brutal Bash", RecastGroup.BrutalBash, 16f, 4),
            [FeatType.TacticalMark] = new("Tactical Mark", RecastGroup.TacticalMark, 20f, 4),
            [FeatType.OverloadShot] = new("Overload Shot", RecastGroup.OverloadShot, 17f, 5),
            [FeatType.ArcPulse] = new("Arc Pulse", RecastGroup.ArcPulse, 20f, 6),
            [FeatType.IonBurst] = new("Ion Burst", RecastGroup.IonBurst, 18f, 5),
            [FeatType.TargetLock] = new("Target Lock", RecastGroup.TargetLock, 20f, 3),
            [FeatType.ShrapnelBurst] = new("Shrapnel Burst", RecastGroup.ShrapnelBurst, 20f, 6),
            [FeatType.ForceRend] = new("Force Rend", RecastGroup.ForceRend, 16f, 5),
            [FeatType.MindSpike] = new("Mind Spike", RecastGroup.MindSpike, 18f, 4),
            [FeatType.DarkShock] = new("Dark Shock", RecastGroup.DarkShock, 24f, 7),
            [FeatType.DreadWave] = new("Dread Wave", RecastGroup.DreadWave, 24f, 6),
            [FeatType.GlacialSlime] = new("Glacial Slime", RecastGroup.GlacialSlime, 17f, 4),
            [FeatType.HoarfrostGlob] = new("Hoarfrost Glob", RecastGroup.HoarfrostGlob, 16f, 4),
            [FeatType.PermafrostRupture] = new("Permafrost Rupture", RecastGroup.PermafrostRupture, 26f, 7),
            [FeatType.RimePounce] = new("Rime Pounce", RecastGroup.RimePounce, 15f, 5),
            [FeatType.CryoBile] = new("Cryo Bile", RecastGroup.CryoBile, 24f, 8),
            [FeatType.CapacitorSurge] = new("Capacitor Surge", RecastGroup.CapacitorSurge, 20f, 5),
            [FeatType.StaticWeb] = new("Static Web", RecastGroup.StaticWeb, 22f, 6),
            [FeatType.ForceSunder] = new("Force Sunder", RecastGroup.ForceSunder, 18f, 6),
            [FeatType.NullShock] = new("Null Shock", RecastGroup.NullShock, 24f, 7),
            [FeatType.RendingCarve] = new("Rending Carve", RecastGroup.RendingCarve, 18f, 5),
            [FeatType.StimCanister] = new("Stim Canister", RecastGroup.StimCanister, 24f, 6),
            [FeatType.BloodFrenzyFlurry] = new("Blood Frenzy Flurry", RecastGroup.BloodFrenzyFlurry, 20f, 6),
            [FeatType.ConcussiveChallenge] = new("Concussive Challenge", RecastGroup.ConcussiveChallenge, 24f, 5),
        };
    }

    private static FeatType[] NewResistanceThreatAbilities()
    {
        return new[]
        {
            FeatType.GlacialSlime,
            FeatType.HoarfrostGlob,
            FeatType.PermafrostRupture,
            FeatType.RimePounce,
            FeatType.CryoBile,
            FeatType.CapacitorSurge,
            FeatType.StaticWeb,
            FeatType.ForceSunder,
            FeatType.NullShock,
            FeatType.RendingCarve,
            FeatType.StimCanister,
            FeatType.BloodFrenzyFlurry,
            FeatType.ConcussiveChallenge
        };
    }

    private sealed record ExpectedNPCAbility(
        string Name,
        RecastGroup RecastGroup,
        float RecastSeconds,
        int StaminaCost,
        int? FPCost = null);

    private sealed record BuiltNPCAbility(
        AbilityDetail Ability,
        string SourceFile);

    private sealed record NPCAbilityBibleRow(
        string Ability,
        string Feat,
        string Targeting,
        string Hostile,
        string Area,
        string RequiresTarget,
        string MaxRange,
        string ActivationDelay,
        string RecastGroup,
        string Recast,
        string Stamina,
        string DamageResistance,
        string StatusEffect,
        string Duration,
        string Notes,
        string SourceFile);

    private static Dictionary<int, Dictionary<string, string>> Read2daRows(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static XDocument ReadWorksheetByName(ZipArchive archive, string sheetName)
    {
        var workbook = ReadWorkbookXml(archive, "xl/workbook.xml");
        var relationships = ReadWorkbookXml(archive, "xl/_rels/workbook.xml.rels");
        XNamespace workbookNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelationshipNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        var sheet = workbook
            .Descendants(workbookNs + "sheet")
            .Single(candidate => candidate.Attribute("name")?.Value == sheetName);
        var relationshipId = sheet.Attribute(relationshipNs + "id")?.Value;
        relationshipId.Should().NotBeNullOrWhiteSpace($"{sheetName} should have a workbook relationship id");

        var target = relationships
            .Descendants(packageRelationshipNs + "Relationship")
            .Single(candidate => candidate.Attribute("Id")?.Value == relationshipId)
            .Attribute("Target")?
            .Value
            .Replace('\\', '/');
        target.Should().NotBeNullOrWhiteSpace($"{sheetName} should resolve to a worksheet XML target");

        var entryName = target!.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : $"xl/{target}";
        return ReadWorkbookXml(archive, entryName);
    }

    private static XDocument ReadWorkbookXml(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        entry.Should().NotBeNull($"{entryName} should exist in the combat Bible workbook");

        using var stream = entry!.Open();
        return XDocument.Load(stream);
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null)
            return Array.Empty<string>();

        var sharedStrings = ReadWorkbookXml(archive, "xl/sharedStrings.xml");
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        return sharedStrings
            .Descendants(ns + "si")
            .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static IReadOnlyList<NPCAbilityBibleRow> ReadNpcAbilityBibleRows(
        XDocument worksheet,
        IReadOnlyList<string> sharedStrings)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<NPCAbilityBibleRow>();

        foreach (var row in worksheet.Descendants(ns + "row"))
        {
            if (!int.TryParse(row.Attribute("r")?.Value, out var rowNumber) || rowNumber <= 1)
                continue;

            var cells = row
                .Elements(ns + "c")
                .ToDictionary(
                    cell => GetWorkbookColumn(cell.Attribute("r")?.Value ?? string.Empty),
                    cell => GetWorkbookCellText(cell, sharedStrings));

            var ability = GetWorkbookRowValue(cells, "A");
            if (string.IsNullOrWhiteSpace(ability))
                continue;

            rows.Add(new NPCAbilityBibleRow(
                ability,
                GetWorkbookRowValue(cells, "B"),
                GetWorkbookRowValue(cells, "C"),
                GetWorkbookRowValue(cells, "D"),
                GetWorkbookRowValue(cells, "E"),
                GetWorkbookRowValue(cells, "F"),
                GetWorkbookRowValue(cells, "G"),
                GetWorkbookRowValue(cells, "H"),
                GetWorkbookRowValue(cells, "I"),
                GetWorkbookRowValue(cells, "J"),
                GetWorkbookRowValue(cells, "K"),
                GetWorkbookRowValue(cells, "L"),
                GetWorkbookRowValue(cells, "M"),
                GetWorkbookRowValue(cells, "N"),
                GetWorkbookRowValue(cells, "O"),
                GetWorkbookRowValue(cells, "P")));
        }

        return rows;
    }

    private static string GetWorkbookColumn(string address)
    {
        return new string(address.TakeWhile(char.IsLetter).ToArray());
    }

    private static string GetWorkbookRowValue(IReadOnlyDictionary<string, string> cells, string column)
    {
        return cells.TryGetValue(column, out var value)
            ? value
            : string.Empty;
    }

    private static string GetWorkbookCellText(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var type = cell.Attribute("t")?.Value;
        if (type == "inlineStr")
            return string.Concat(cell.Descendants(ns + "t").Select(text => text.Value));

        var value = cell.Element(ns + "v")?.Value;
        if (type == "s" && int.TryParse(value, out var index))
            return sharedStrings[index];

        return value ?? string.Empty;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
