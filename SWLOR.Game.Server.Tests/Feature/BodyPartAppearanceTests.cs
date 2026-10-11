using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Tests.Feature;

public class BodyPartAppearanceTests
{
    [TestCase(CreaturePart.LeftHand)]
    [TestCase(CreaturePart.RightHand)]
    [TestCase(CreaturePart.LeftBicep)]
    [TestCase(CreaturePart.RightBicep)]
    public void OnlyMatchingExplicitOutfitPartsFollowBodyChanges(CreaturePart part)
    {
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(part, 1, 1), Is.True);
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(part, 251, 251), Is.True);
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(part, 1, 0), Is.False,
            "Model zero already follows the naked body.");
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(part, 1, 151), Is.False,
            "A distinct glove or sleeve must retain its outfit model.");
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(part, 0, 0), Is.False);
    }

    [Test]
    public void UnrelatedOutfitPartsDoNotFollowBodyChanges()
    {
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(CreaturePart.Torso, 1, 1), Is.False);
        Assert.That(BodyPartAppearance.ShouldUpdateArmorPart(CreaturePart.Head, 1, 1), Is.False);
    }

    [Test]
    public void HumanModelSpeciesAndOutfitsOfferTheRestoredBicep()
    {
        foreach (var type in RacialAppearanceRegistry.GetAppearanceTypes())
        {
            RacialAppearanceRegistry.TryGet(type, out var definition);
            if (definition is not HumanModelRacialAppearanceBaseDefinition)
                continue;
            Assert.That(definition.LeftBicep, Does.Contain(251), type.ToString());
            Assert.That(definition.RightBicep, Does.Contain(251), type.ToString());
        }
        Assert.That(new GeneralArmorAppearanceDefinition().Bicep, Does.Contain(251));
        Assert.That(new HumanRacialAppearanceDefinition().LeftBicep, Does.Contain(1).And.Contain(2));
        Assert.That(new DroidRacialAppearanceDefinition().LeftBicep, Does.Not.Contain(251));
    }

    [TestCase("parts_hand", 260)]
    [TestCase("parts_hand", 261)]
    [TestCase("parts_hand", 262)]
    [TestCase("parts_hand", 263)]
    [TestCase("parts_bicep", 251)]
    public void SelectableModelsHaveValidNativePartRows(string table, int model)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "SWLOR_Haks")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
        var row = File.ReadLines(Path.Combine(root!.FullName, "SWLOR_Haks", "sw_2da", table + ".2da"))
            .Select(line => line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries))
            .Single(columns => columns.Length > 0 && columns[0] == model.ToString());
        Assert.That(row[1], Is.EqualTo("0"));
        Assert.That(row[2], Is.EqualTo("0"));
    }
}
