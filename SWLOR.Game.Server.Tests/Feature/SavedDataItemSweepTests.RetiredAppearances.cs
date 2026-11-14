#nullable enable
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.Tests.Feature;

public partial class SavedDataItemSweepTests
{
    private static object Part(string label, int value) =>
        new[] { Scalar(0, label, value <= byte.MaxValue ? value : 0), Scalar(2, ("x" + label).Length > 16 ? ("x" + label)[..16] : "x" + label, value) };

    private static object[] Parts(params (string Label, int Value)[] parts) =>
        parts.SelectMany(part => (object[])Part(part.Label, part.Value)).ToArray();

    private static (int, int) PartFields(GffStruct node, string label) =>
        (Int(node, label), Int(node, ("x" + label).Length > 16 ? ("x" + label)[..16] : "x" + label));

    [Test]
    public void RetiredAppearances_MoveArmorPartsToTheBodyPartOrNoneAndKeepTheRest()
    {
        var original = Encode(Item("custom_armor", BaseItem.Armor, null, Parts(
                ("ArmorPart_Robe", 160), ("ArmorPart_Neck", 299), ("ArmorPart_Torso", 300), ("ArmorPart_LShoul", 255),
                ("ArmorPart_RBicep", 18), ("ArmorPart_LBicep", 17), ("ArmorPart_Belt", 120), ("ArmorPart_RFoot", 13),
                ("ArmorPart_LShin", 52), ("ArmorPart_Pelvis", 34))
            .Concat(new[] { Scalar(2, "StackSize", 1), Text("UUID", "retired-armor"), Scalar(0, "Cloth1Color", 23) }).ToArray()));

        EnzymeBaseItemMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        PartFields(root, "ArmorPart_Robe").Should().Be((0, 0), "a retired robe becomes no robe");
        PartFields(root, "ArmorPart_Neck").Should().Be((0, 0), "the editor's None shows the wearer's own neck");
        PartFields(root, "ArmorPart_Torso").Should().Be((0, 0));
        PartFields(root, "ArmorPart_LShoul").Should().Be((0, 0));
        PartFields(root, "ArmorPart_RBicep").Should().Be((0, 0));
        PartFields(root, "ArmorPart_Belt").Should().Be((0, 0));
        PartFields(root, "ArmorPart_RFoot").Should().Be((0, 0));
        PartFields(root, "ArmorPart_LShin").Should().Be((1, 1), "the shin list has no None, so 1 is its bare option");
        PartFields(root, "ArmorPart_LBicep").Should().Be((17, 17), "bicep 17 still ships");
        PartFields(root, "ArmorPart_Pelvis").Should().Be((34, 34));
        Value(root, "UUID").Should().Be("retired-armor");
        (Int(root, "StackSize"), Int(root, "Cloth1Color")).Should().Be((1, 23));

        EnzymeBaseItemMigration.MigrateSavedObject(migrated, out var rerun).Should().BeFalse("nothing retired is left");
        rerun.Should().BeSameAs(migrated);
    }

    [Test]
    public void RetiredAppearances_MoveHelmetsAndCloaksAndLeaveOtherItemsAlone()
    {
        EnzymeBaseItemMigration.MigrateSavedObject(Encode(Item("custom_helmet", BaseItem.Helmet, 297)), out var helmet).Should().BeTrue();
        (Int(Read(helmet), "ModelPart1"), Int(Read(helmet), "xModelPart1")).Should().Be((1, 1));
        EnzymeBaseItemMigration.MigrateSavedObject(Encode(Item("custom_cloak", BaseItem.Cloak, 33)), out var cloak).Should().BeTrue();
        (Int(Read(cloak), "ModelPart1"), Int(Read(cloak), "xModelPart1")).Should().Be((1, 1));

        EnzymeBaseItemMigration.MigrateSavedObject(Encode(Item("custom_helmet", BaseItem.Helmet, 298)), out _).Should().BeFalse();
        EnzymeBaseItemMigration.MigrateSavedObject(Encode(Item("custom_cloak", BaseItem.Cloak, 34)), out _).Should().BeFalse();
        EnzymeBaseItemMigration.MigrateSavedObject(Encode(Item("custom_ring", BaseItem.Ring, 297)), out _)
            .Should().BeFalse("only helmets and cloaks use these model numbers");
    }

    [Test]
    public void RetiredAppearances_MoveACreaturesHeadBodyPartsAndCarriedItems()
    {
        var helmet = Item("custom_helmet", BaseItem.Helmet, 256);
        var original = Encode(Node(0, Parts(("BodyPart_LBicep", 18), ("BodyPart_RBicep", 1), ("ArmorPart_RFoot", 13),
                ("BodyPart_LShoul", 255), ("Appearance_Head", 258))
            .Concat(new[]
            {
                Scalar(2, "Appearance_Type", (int)AppearanceType.Human), Scalar(0, "Gender", 1),
                List("ItemList", helmet),
            }).ToArray()), "BIC ");

        EnzymeBaseItemMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        RacialAppearanceRegistry.TryGet(AppearanceType.Human, out var human).Should().BeTrue();
        PartFields(root, "Appearance_Head").Should().Be((human.FemaleHeads[0], human.FemaleHeads[0]),
            "a retired head becomes the first head the race offers");
        PartFields(root, "BodyPart_LBicep").Should().Be((1, 1));
        PartFields(root, "BodyPart_RBicep").Should().Be((1, 1));
        PartFields(root, "ArmorPart_RFoot").Should().Be((1, 1), "character files save the right foot body part here");
        PartFields(root, "BodyPart_LShoul").Should().Be((0, 0));
        var carried = Children(root, "ItemList").Single();
        Int(carried, "ModelPart1").Should().Be(1);

        EnzymeBaseItemMigration.MigrateSavedObject(migrated, out _).Should().BeFalse();
    }

    [Test]
    public void RetiredAppearances_LeaveHeadsAloneOnBodiesThatStillShipThem()
    {
        // Male human head 258 still ships; only the female one was retired.
        var male = Encode(Node(0, Parts(("Appearance_Head", 258)).Concat(new[]
            { Scalar(2, "Appearance_Type", (int)AppearanceType.Human), Scalar(0, "Gender", 0) }).ToArray()), "BIC ");
        EnzymeBaseItemMigration.MigrateSavedObject(male, out _).Should().BeFalse();
        // A non-parts appearance has no head model to retire.
        var creature = Encode(Node(0, Parts(("Appearance_Head", 258)).Concat(new[]
            { Scalar(2, "Appearance_Type", 2143), Scalar(0, "Gender", 1) }).ToArray()), "UTC ");
        EnzymeBaseItemMigration.MigrateSavedObject(creature, out _).Should().BeFalse();
    }

    [Test]
    public void RetiredAppearances_KeepOutfitPartNumbersInStepWithTheGarment()
    {
        var outfit = new PlayerOutfit { RobeId = 160, NeckId = 299, LeftShoulderId = 255, LeftBicepId = 17, HelmetId = 297, TorsoId = 186 };
        RetiredAppearanceMigration.UpdateOutfitIds(outfit).Should().BeTrue();
        (outfit.RobeId, outfit.NeckId, outfit.LeftShoulderId, outfit.LeftBicepId, outfit.HelmetId, outfit.TorsoId)
            .Should().Be((0, 0, 0, 17, 1, 186));
        RetiredAppearanceMigration.UpdateOutfitIds(outfit).Should().BeFalse();
    }

    [Test]
    public void RetiredAppearances_ReplacementsStillShip()
    {
        foreach (var (part, styles) in RetiredAppearances.ArmorParts)
        foreach (var style in styles)
        {
            RetiredAppearances.ArmorParts[part].Should().NotContain(RetiredAppearanceMigration.ReplaceArmorPart(part, style), part.ToString());
            RetiredAppearances.ArmorParts[part].Should().NotContain(RetiredAppearanceMigration.ReplaceBodyPart(part, style), part.ToString());
        }
        RetiredAppearances.Helmets.Should().NotContain(RetiredAppearanceMigration.HelmetReplacement);
        RetiredAppearances.Cloaks.Should().NotContain(RetiredAppearanceMigration.CloakReplacement);
        foreach (var (appearance, letter) in RetiredAppearanceMigration.ModelLetters)
        foreach (var gender in new[] { 0, 1 })
        {
            var prefix = $"p{(gender == 1 ? 'f' : 'm')}{letter}";
            if (!RetiredAppearances.Heads.TryGetValue(prefix, out var retired)) continue;
            foreach (var head in retired)
                retired.Should().NotContain(RetiredAppearanceMigration.ReplaceHead(appearance, gender, head), $"{appearance} {prefix}");
        }
    }

    [Test]
    public void RetiredAppearances_AreNeverOffered()
    {
        var general = new GeneralArmorAppearanceDefinition();
        var offered = new Dictionary<RetiredArmorPart, int[]>
        {
            [RetiredArmorPart.Torso] = general.Torso, [RetiredArmorPart.Neck] = general.Neck, [RetiredArmorPart.Belt] = general.Belt,
            [RetiredArmorPart.Pelvis] = general.Pelvis, [RetiredArmorPart.Robe] = general.Robe, [RetiredArmorPart.Shoulder] = general.Shoulder,
            [RetiredArmorPart.Bicep] = general.Bicep, [RetiredArmorPart.Forearm] = general.Forearm, [RetiredArmorPart.Hand] = general.Hand,
            [RetiredArmorPart.Thigh] = general.Thigh, [RetiredArmorPart.Shin] = general.Shin, [RetiredArmorPart.Foot] = general.Foot,
        };
        foreach (var (part, styles) in offered)
            styles.Should().NotIntersectWith(RetiredAppearances.ArmorParts[part], part.ToString());
        general.Helmet.Should().NotIntersectWith(RetiredAppearances.Helmets);
        general.Cloak.Should().NotIntersectWith(RetiredAppearances.Cloaks);

        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            RacialAppearanceRegistry.TryGet(appearance, out var race).Should().BeTrue();
            var letter = RetiredAppearanceMigration.ModelLetters[(int)appearance];
            foreach (var (gender, heads) in new[] { ('m', race.MaleHeads), ('f', race.FemaleHeads) })
                if (RetiredAppearances.Heads.TryGetValue($"p{gender}{letter}", out var retired))
                    heads.Should().NotIntersectWith(retired, $"{appearance} {gender}");
            foreach (var (part, styles) in new[]
                     {
                         (RetiredArmorPart.Torso, race.Torsos), (RetiredArmorPart.Pelvis, race.Pelvis),
                         (RetiredArmorPart.Bicep, race.LeftBicep.Concat(race.RightBicep).ToArray()),
                         (RetiredArmorPart.Forearm, race.LeftForearm.Concat(race.RightForearm).ToArray()),
                         (RetiredArmorPart.Hand, race.LeftHand.Concat(race.RightHand).ToArray()),
                         (RetiredArmorPart.Thigh, race.LeftThigh.Concat(race.RightThigh).ToArray()),
                         (RetiredArmorPart.Shin, race.LeftShin.Concat(race.RightShin).ToArray()),
                         (RetiredArmorPart.Foot, race.LeftFoot.Concat(race.RightFoot).ToArray()),
                     })
                styles.Should().NotIntersectWith(RetiredAppearances.ArmorParts[part], $"{appearance} {part}");
        }
    }

    [Test]
    public void RetiredAppearances_ModelLettersMatchAppearance2da()
    {
        var lines = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "appearance.2da"));
        static string[] Tokens(string line) => Regex.Matches(line, "\"[^\"]*\"|\\S+").Select(match => match.Value).ToArray();
        var header = Tokens(lines[2]).ToList();
        var race = header.IndexOf("RACE") + 1;
        var modelType = header.IndexOf("MODELTYPE") + 1;
        var rows = lines.Skip(3).Select(Tokens).Where(columns => columns.Length > race && int.TryParse(columns[0], out _))
            .ToDictionary(columns => int.Parse(columns[0]));
        foreach (var (appearance, letter) in RetiredAppearanceMigration.ModelLetters)
        {
            rows[appearance][race].ToLowerInvariant().Should().Be(letter.ToString(), appearance.ToString());
            rows[appearance][modelType].Should().Be("P", $"{appearance} is parts based");
        }
        RacialAppearanceRegistry.GetAppearanceTypes().Select(type => (int)type)
            .Should().BeSubsetOf(RetiredAppearanceMigration.ModelLetters.Keys);
    }

    [Test]
    public void RetiredAppearances_AreRetiredInTheirTwoDas()
    {
        var files = new Dictionary<RetiredArmorPart, string>
        {
            [RetiredArmorPart.Torso] = "chest", [RetiredArmorPart.Neck] = "neck", [RetiredArmorPart.Belt] = "belt",
            [RetiredArmorPart.Pelvis] = "pelvis", [RetiredArmorPart.Robe] = "robe", [RetiredArmorPart.Shoulder] = "shoulder",
            [RetiredArmorPart.Bicep] = "bicep", [RetiredArmorPart.Forearm] = "forearm", [RetiredArmorPart.Hand] = "hand",
            [RetiredArmorPart.Thigh] = "legs", [RetiredArmorPart.Shin] = "shin", [RetiredArmorPart.Foot] = "foot",
        };
        Dictionary<int, string[]> Rows(string name) =>
            File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", name)).Skip(3)
                .Select(line => Regex.Matches(line, "\"[^\"]*\"|\\S+").Select(match => match.Value).ToArray())
                .Where(columns => columns.Length > 1 && int.TryParse(columns[0], out _))
                .ToDictionary(columns => int.Parse(columns[0]));
        foreach (var (part, file) in files)
        {
            var rows = Rows($"parts_{file}.2da");
            foreach (var style in RetiredAppearances.ArmorParts[part].Where(rows.ContainsKey))
                rows[style].Skip(1).Should().OnlyContain(value => value == "****", $"parts_{file}.2da row {style}");
        }
        var cloaks = Rows("cloakmodel.2da");
        foreach (var row in RetiredAppearances.Cloaks)
            cloaks[row].Skip(1).Should().OnlyContain(value => value == "****", $"cloakmodel.2da row {row}");
    }
}
