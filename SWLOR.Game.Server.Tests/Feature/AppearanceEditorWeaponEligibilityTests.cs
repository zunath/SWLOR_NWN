using System.Reflection;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class AppearanceEditorWeaponEligibilityTests
{
    [TestCase(BaseItem.Lightsaber, "saber_train_1")]
    [TestCase(BaseItem.Saberstaff, "trn_saberstaff_1")]
    [TestCase(BaseItem.Longsword, "longsword")]
    public void CursedItemsAreBlockedRegardlessOfPlotFlag(BaseItem type, string resref)
    {
        AllowsFlags(type, false, true, resref).Should().BeFalse();
        AllowsFlags(type, true, true, resref).Should().BeFalse();
        AllowsFlags(type, false, false, resref).Should().BeTrue();
    }

    [TestCase(BaseItem.Lightsaber, "saber_train_1", true)]
    [TestCase(BaseItem.Lightsaber, "SABER_TRAIN_5", true)]
    [TestCase(BaseItem.Saberstaff, "trn_saberstaff_1", true)]
    [TestCase(BaseItem.Lightsaber, "darkhung_l1", false)]
    [TestCase(BaseItem.Saberstaff, "darkhung_w1", false)]
    [TestCase(BaseItem.Lightsaber, "trn_saberstaff_1", false)]
    [TestCase(BaseItem.Longsword, "saber_train_1", false)]
    [TestCase(BaseItem.Lightsaber, null, false)]
    public void PlotExceptionIsLimitedToTrainingSabers(BaseItem type, string resref, bool allowed)
    {
        AllowsFlags(type, true, false, resref).Should().Be(allowed);
    }

    // The training catalog must match the module: every listed blueprint is a Plot saber of the right base
    // type, and every other plot saber or saberstaff blueprint with the training look stays out of it.
    [Test]
    public void TrainingCatalogMatchesModuleBlueprints()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        var uti = Path.Combine(directory!.FullName, "Module", "uti");

        TrainingSaberCatalog.LightsaberResrefs.Should().HaveCount(9);
        TrainingSaberCatalog.SaberstaffResrefs.Should().HaveCount(9);

        foreach (var (resrefs, baseItem) in new[]
                 {
                     (TrainingSaberCatalog.LightsaberResrefs, (int)BaseItem.Lightsaber),
                     (TrainingSaberCatalog.SaberstaffResrefs, (int)BaseItem.Saberstaff)
                 })
        {
            foreach (var resref in resrefs)
            {
                var blueprint = JObject.Parse(File.ReadAllText(Path.Combine(uti, resref + ".uti.json")));
                ((int)blueprint["BaseItem"]!["value"]!).Should().Be(baseItem, resref);
                ((int)blueprint["Plot"]!["value"]!).Should().Be(1, resref);
                ((string)blueprint["Tag"]!["value"]!).Should().Be(resref);
                ((string)blueprint["TemplateResRef"]!["value"]!).Should().Be(resref);
            }
        }

        // The other plot blueprints that share the default training look are not training weapons.
        foreach (var (resref, type) in new[]
                 {
                     ("darkhung_l1", BaseItem.Lightsaber), ("darkhung_l2", BaseItem.Lightsaber),
                     ("eclipse_l1", BaseItem.Lightsaber), ("guardmst_l1", BaseItem.Lightsaber),
                     ("lightstand_l1", BaseItem.Lightsaber), ("sabstorm_l1", BaseItem.Lightsaber),
                     ("darkhung_w1", BaseItem.Saberstaff), ("eclipse_w1", BaseItem.Saberstaff),
                     ("guardmst_w1", BaseItem.Saberstaff), ("lightstand_w1", BaseItem.Saberstaff),
                     ("sabstorm_w1", BaseItem.Saberstaff), ("infconduit_w1", BaseItem.Saberstaff)
                 })
        {
            TrainingSaberCatalog.IsTrainingSaber(type, resref).Should().BeFalse(resref);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void TrainingPartsRequireEveryModelAndColorToBeAllowed(bool staff)
    {
        WeaponAppearanceBaseDefinition appearance = staff
            ? new SaberstaffAppearanceDefinition() : new LightsaberAppearanceDefinition();
        var tops = staff ? new[] { 102, 107, 111 } : new[] { 114, 108, 102 };
        var bottoms = staff ? new[] { 107, 207, 307 } : new[] { 115, 215, 315 };
        appearance.TopParts.Should().BeEquivalentTo(tops);
        appearance.MiddleParts.Should().Equal(101);
        appearance.BottomParts.Should().BeEquivalentTo(bottoms);

        foreach (var top in tops)
        foreach (var bottom in bottoms)
        {
            AllowsParts(appearance, top, 101, bottom).Should().BeTrue();
            // Allowlists encode model + color * 100; reject unsupported models and colors independently.
            AllowsParts(appearance, top + 100, 101, bottom).Should().BeFalse();
            AllowsParts(appearance, 199, 101, bottom).Should().BeFalse();
            AllowsParts(appearance, top, 102, bottom).Should().BeFalse();
            AllowsParts(appearance, top, 201, bottom).Should().BeFalse();
            AllowsParts(appearance, top, 101, 199).Should().BeFalse();
            AllowsParts(appearance, top, 101, bottom + 300).Should().BeFalse();
        }
    }

    private static bool AllowsFlags(BaseItem type, bool plot, bool cursed, string resref) =>
        (bool)typeof(AppearanceEditorViewModel).GetMethod("AllowsItemFlags", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { type, plot, cursed, resref })!;

    private static bool AllowsParts(WeaponAppearanceBaseDefinition appearance, int top, int middle, int bottom) =>
        (bool)typeof(AppearanceEditorViewModel).GetMethod("AllowsWeaponParts", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { appearance, top, middle, bottom })!;
}
