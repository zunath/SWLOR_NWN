using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.GuiDefinition;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Picture names must match tools/appearance_previews (tools/tests/test_appearance_previews.py
/// checks the Python side against this file's source formats).
/// </summary>
[TestFixture]
public class AppearancePreviewTests
{
    [TestCase("pfh0_chest001", AppearancePreviewView.Front, "apfh0_chest001f")]
    [TestCase("pfh0_chest001", AppearancePreviewView.Back, "apfh0_chest001b")]
    [TestCase("pfh0_chest001", AppearancePreviewView.Angle, "apfh0_chest001a")]
    [TestCase("PMH0_Head012", AppearancePreviewView.Front, "apmh0_head012f")]
    [TestCase("helm_007", AppearancePreviewView.Back, "ahelm_007b")]
    [TestCase("pfh0_pelvis123", AppearancePreviewView.Angle, "apfh0_pelvis123a")]
    public void ResrefIsAKeyAndViewLetter(string key, AppearancePreviewView view, string expected)
    {
        var resref = AppearancePreview.Resref(key, view);
        resref.Should().Be(expected);
        resref.Length.Should().BeLessThanOrEqualTo(AppearancePreview.MaxResrefLength);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("pfh0_pelvis1000")] // 17 characters with "a" and the view letter
    [TestCase("pfh0_bicepr1234")]
    public void ResrefIsEmptyWithoutAKeyOrPastTheSixteenCharacterLimit(string key)
    {
        foreach (var view in Enum.GetValues<AppearancePreviewView>())
            AppearancePreview.Resref(key, view).Should().BeEmpty();
    }

    [Test]
    public void SixteenCharacterResrefsAreKept()
    {
        AppearancePreview.MaxResrefLength.Should().Be(16, "NWN resrefs are at most 16 characters");
        AppearancePreview.Resref("pfh0_bicepr001", AppearancePreviewView.Front).Should().HaveLength(16);
    }

    [Test]
    public void ViewLettersMatchTheGeneratorViews()
    {
        AppearancePreview.ViewLetter(AppearancePreviewView.Front).Should().Be('f');
        AppearancePreview.ViewLetter(AppearancePreviewView.Back).Should().Be('b');
        AppearancePreview.ViewLetter(AppearancePreviewView.Angle).Should().Be('a');
        AppearancePreview.ViewLetter((AppearancePreviewView)99).Should().Be('f');
        Enum.GetValues<AppearancePreviewView>().Select(view => (int)view).Should().Equal(new[] { 0, 1, 2 },
            "the browser's view toggles select by position");
    }

    [TestCase(Gender.Female, "H", "pfh0")]
    [TestCase(Gender.Male, "h", "pmh0")]
    [TestCase(Gender.Male, "E", "pme0")]
    [TestCase(Gender.Female, "z", "pfz0")]
    public void BodyPrefixUsesGenderRaceAndNormalPhenotype(Gender gender, string race, string expected) =>
        AppearancePreview.BodyPrefix(gender, race).Should().Be(expected);

    [TestCase(null)]
    [TestCase("")]
    [TestCase(" ")]
    [TestCase("****")]
    [TestCase("HE")]
    public void BodyPrefixIsEmptyWithoutASingleRaceLetter(string race) =>
        AppearancePreview.BodyPrefix(Gender.Female, race).Should().BeEmpty();

    [TestCase(AppearanceEditorSlot.Neck, "neck")]
    [TestCase(AppearanceEditorSlot.Chest, "chest")]
    [TestCase(AppearanceEditorSlot.Belt, "belt")]
    [TestCase(AppearanceEditorSlot.Pelvis, "pelvis")]
    [TestCase(AppearanceEditorSlot.Robe, "robe")]
    [TestCase(AppearanceEditorSlot.RightShoulder, "shor")]
    [TestCase(AppearanceEditorSlot.LeftShoulder, "shol")]
    [TestCase(AppearanceEditorSlot.RightBicep, "bicepr")]
    [TestCase(AppearanceEditorSlot.LeftBicep, "bicepl")]
    [TestCase(AppearanceEditorSlot.RightForearm, "forer")]
    [TestCase(AppearanceEditorSlot.LeftForearm, "forel")]
    [TestCase(AppearanceEditorSlot.RightHand, "handr")]
    [TestCase(AppearanceEditorSlot.LeftHand, "handl")]
    [TestCase(AppearanceEditorSlot.RightThigh, "legr")]
    [TestCase(AppearanceEditorSlot.LeftThigh, "legl")]
    [TestCase(AppearanceEditorSlot.RightShin, "shinr")]
    [TestCase(AppearanceEditorSlot.LeftShin, "shinl")]
    [TestCase(AppearanceEditorSlot.RightFoot, "footr")]
    [TestCase(AppearanceEditorSlot.LeftFoot, "footl")]
    public void ArmorPartKeyUsesTheModelStemAndThreeDigitPart(AppearanceEditorSlot slot, string stem)
    {
        AppearancePreview.ModelStem(slot).Should().Be(stem);
        AppearancePreview.ArmorPartKey("pfh0", slot, 7).Should().Be($"pfh0_{stem}007");
        AppearancePreview.ArmorPartKey("pmh0", slot, 123).Should().Be($"pmh0_{stem}123");
        AppearancePreview.ArmorPartKey("pfh0", slot, 0).Should().BeEmpty("part 0 is no part");
        AppearancePreview.ArmorPartKey("pfh0", slot, -1).Should().BeEmpty();
        AppearancePreview.ArmorPartKey(string.Empty, slot, 7).Should().BeEmpty("an unknown body has no pictures");
        AppearancePreview.Resref(AppearancePreview.ArmorPartKey("pfh0", slot, 999), AppearancePreviewView.Front)
            .Should().NotBeEmpty("every stem fits the resref limit up to part 999");
    }

    [TestCase(AppearanceEditorSlot.Head)]
    [TestCase(AppearanceEditorSlot.Cloak)]
    public void HeadAndCloakAreNotArmorParts(AppearanceEditorSlot slot)
    {
        AppearancePreview.ModelStem(slot).Should().BeNull();
        AppearancePreview.ArmorPartKey("pfh0", slot, 5).Should().BeEmpty();
        AppearancePreview.ToArmorPart(slot).Should().Be(AppearanceArmor.Invalid);
    }

    [Test]
    public void WeaponPartKeysNameTheEnginePartModels()
    {
        AppearancePreview.WeaponPartKey("WSwLs", AppearanceWeapon.Top, 101).Should().Be("wswls_t_011");
        AppearancePreview.WeaponPartKey("wswls", AppearanceWeapon.Middle, 325).Should().Be("wswls_m_253");
        AppearancePreview.WeaponPartKey("wswls", AppearanceWeapon.Bottom, 908).Should().Be("wswls_b_089");
        AppearancePreview.WeaponPartKey("wswls", AppearanceWeapon.Invalid, 101).Should().BeEmpty();
        AppearancePreview.WeaponPartKey("wswls", AppearanceWeapon.Top, 0).Should().BeEmpty();
        AppearancePreview.WeaponPartKey("", AppearanceWeapon.Top, 101).Should().BeEmpty();
        AppearancePreview.Resref(AppearancePreview.WeaponPartKey("wswls", AppearanceWeapon.Top, 101), AppearancePreviewView.Angle)
            .Should().Be("awswls_t_011a");
        AppearancePreview.SimpleWeaponKey("WPLGLV", 7).Should().Be("wplglv_007");
        AppearancePreview.SimpleWeaponKey("wplglv", 0).Should().BeEmpty();
    }

    [Test]
    public void HeadHelmetAndCloakKeys()
    {
        AppearancePreview.HeadKey("pmh0", 7).Should().Be("pmh0_head007");
        AppearancePreview.HeadKey("pfe0", 150).Should().Be("pfe0_head150");
        AppearancePreview.HeadKey("pmh0", 0).Should().BeEmpty();
        AppearancePreview.HeadKey(string.Empty, 7).Should().BeEmpty();

        AppearancePreview.HelmetKey(12).Should().Be("helm_012");
        AppearancePreview.HelmetKey(321).Should().Be("helm_321");
        AppearancePreview.HelmetKey(0).Should().BeEmpty();

        AppearancePreview.CloakKey("pfh0", 3).Should().Be("pfh0_clk003");
        AppearancePreview.CloakKey("pmz0", 217).Should().Be("pmz0_clk217");
        AppearancePreview.CloakKey("pfh0", 0).Should().BeEmpty();
        AppearancePreview.CloakKey(string.Empty, 3).Should().BeEmpty();
    }

    [Test]
    public void ArmorPartsRoundTripThroughSlots()
    {
        var parts = Enum.GetValues<AppearanceEditorSlot>()
            .Where(slot => slot is not (AppearanceEditorSlot.Head or AppearanceEditorSlot.Cloak))
            .Select(slot => (Slot: slot, Part: AppearancePreview.ToArmorPart(slot))).ToArray();
        parts.Should().HaveCount((int)AppearanceArmor.Num);
        parts.Select(entry => entry.Part).Should().OnlyHaveUniqueItems().And.NotContain(AppearanceArmor.Invalid);
        foreach (var (slot, part) in parts)
            AppearancePreview.FromArmorPart(part).Should().Be(slot);
    }

    [TestCase(0, "A1")]
    [TestCase(7, "H1")]
    [TestCase(15, "P1")]
    [TestCase(16, "A2")]
    [TestCase(17, "B2")]
    [TestCase(160, "A11")]
    [TestCase(175, "P11")]
    public void PaletteCodeIsColumnLetterAndRowNumber(int colorId, string expected) =>
        AppearancePreview.PaletteCode(colorId).Should().Be(expected);

    [TestCase(-1)]
    [TestCase(176)]
    [TestCase(255)]
    public void PaletteCodeIsEmptyOutsideThePalette(int colorId) =>
        AppearancePreview.PaletteCode(colorId).Should().BeEmpty();

    [Test]
    public void BodyCategoriesRoundTripThroughTheFifteenBodySlots()
    {
        var categories = Enum.GetValues<AppearanceEditorSlot>()
            .Select(slot => (Slot: slot, Category: AppearancePreview.BodyCategory(slot)))
            .Where(entry => entry.Category >= 0).ToArray();
        categories.Select(entry => entry.Category).Should().BeEquivalentTo(Enumerable.Range(0, 15),
            "the appearance tab lists fifteen body part categories");
        foreach (var (slot, category) in categories)
            AppearancePreview.FromBodyCategory(category).Should().Be(slot);

        AppearancePreview.BodyCategory(AppearanceEditorSlot.Head).Should().Be(0);
        AppearancePreview.BodyCategory(AppearanceEditorSlot.Chest).Should().Be(1);
        foreach (var slot in new[]
                 {
                     AppearanceEditorSlot.Neck, AppearanceEditorSlot.Belt, AppearanceEditorSlot.Robe, AppearanceEditorSlot.Cloak,
                     AppearanceEditorSlot.RightShoulder, AppearanceEditorSlot.LeftShoulder
                 })
            AppearancePreview.BodyCategory(slot).Should().Be(-1, $"{slot} is armor only");

        AppearancePreview.FromBodyCategory(15).Should().Be(AppearanceEditorSlot.Head);
    }

    // ---------------------------------------------------------------- view model helpers

    // width, height, x, y, screen width, screen height, scale -> width, height
    [TestCase(600f, 400f, 40f, 30f, 0f, 0f, 1f, 600f, 400f)]
    [TestCase(600f, 400f, 40f, 30f, 1920f, 1080f, 1f, 600f, 400f)]
    [TestCase(1240f, 720f, 40f, 0f, 1280f, 720f, 1f, 1240f, 720f)]
    [TestCase(1800f, 900f, 300f, 200f, 1280f, 720f, 1f, 1280f, 720f)]
    [TestCase(800f, 500f, 1900f, 1000f, 2400f, 1350f, 1.2f, 800f, 500f)]
    [TestCase(2000f, 1000f, 0f, 0f, 1600f, 900f, 0.8f, 2000f, 1000f)]
    [TestCase(2400f, 1400f, 0f, 0f, 1600f, 900f, 0.8f, 2000f, 1125f)]
    public void WindowsAreCappedToTheScreenButNeverGrown(float width, float height, float x, float y, float screenWidth,
        float screenHeight, float scale, float expectedWidth, float expectedHeight)
    {
        var geometry = new GuiRectangle(x, y, width, height);
        var fitted = FitWindowToScreen(geometry, screenWidth, screenHeight, scale);
        fitted.Width.Should().BeApproximately(expectedWidth, 0.01f);
        fitted.Height.Should().BeApproximately(expectedHeight, 0.01f);
        if (screenWidth > 0f)
        {
            // Positions are physical pixels; the window covers its size times the GUI scale.
            fitted.X.Should().BeInRange(0f, screenWidth - fitted.Width * scale + 0.01f);
            fitted.Y.Should().BeInRange(0f, screenHeight - fitted.Height * scale + 0.01f);
        }
    }

    [Test]
    public void AWindowThatAlreadyFitsKeepsItsGeometry()
    {
        var geometry = new GuiRectangle(100f, 50f, 500f, 300f);
        FitWindowToScreen(geometry, 1920f, 1080f, 1f).Should().BeSameAs(geometry);
        FitWindowToScreen(geometry, 0f, 0f, 1f).Should().BeSameAs(geometry);
    }

    [TestCase(AppearanceEditorSlot.LeftShoulder, "Left Shoulder")]
    [TestCase(AppearanceEditorSlot.Chest, "Chest")]
    [TestCase(AppearanceEditorSlot.RightFoot, "Right Foot")]
    public void PartColourRowsNameTheSelectedPart(AppearanceEditorSlot slot, string expected) =>
        ((string)typeof(AppearanceEditorViewModel)
            .GetMethod("PartColorsTitle", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { slot })!).Should().Be(expected);

    // editor x, screen width, scale -> browser x
    [TestCase(100f, 1920f, 1f, 800f)]
    [TestCase(1200f, 1920f, 1f, 600f)]
    [TestCase(100f, 1280f, 1f, 680f)]
    [TestCase(0f, 1280f, 1f, 680f)]
    [TestCase(600f, 1280f, 1f, 0f)]
    [TestCase(100f, 0f, 1f, 800f)]
    [TestCase(100f, 1920f, 1.5f, 1020f)]
    [TestCase(1000f, 1920f, 1.5f, 100f)]
    public void ThePartBrowserOpensBesideTheEditor(float editorX, float screenWidth, float scale, float expectedX)
    {
        var editor = new GuiRectangle(editorX, 50f, AppearanceEditorDefinition.EditorWidth, AppearanceEditorDefinition.EditorHeight);
        var placed = PlaceBeside(editor, AppearancePartBrowserDefinition.WindowWidth,
            AppearancePartBrowserDefinition.WindowHeight, screenWidth, 1080f, scale);
        placed.X.Should().BeApproximately(expectedX, 0.01f,
            "right of the editor when it fits, else left, else against the edge that covers less of the editor");
        placed.Y.Should().Be(50f, "the browser lines up with the editor's top");
        placed.Width.Should().Be(AppearancePartBrowserDefinition.WindowWidth);
        placed.Height.Should().Be(AppearancePartBrowserDefinition.WindowHeight);
        PlaceBeside(null, 600f, 570f, 1920f, 1080f, 1f).Should().BeNull();
    }

    private static GuiRectangle PlaceBeside(GuiRectangle editor, float width, float height,
        float screenWidth, float screenHeight, float scale) =>
        (GuiRectangle)typeof(AppearanceEditorViewModel)
            .GetMethod("PlaceBeside", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { editor, width, height, screenWidth, screenHeight, scale });

    private static GuiRectangle FitWindowToScreen(GuiRectangle geometry, float screenWidth, float screenHeight, float scale) =>
        (GuiRectangle)typeof(AppearanceEditorViewModel)
            .GetMethod("FitWindowToScreen", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { geometry, screenWidth, screenHeight, scale })!;

    [TestCase("pme0_chest148", "pmh0_chest148")]
    [TestCase("pfd0_clk012", "pfh0_clk012")]
    [TestCase("pfz0_chest001", "pfh0_chest001")]
    [TestCase("pmh0_chest148", null)]
    [TestCase("helm_001", null)]
    public void NonHumanPartsHaveAHumanTwin(string key, string expected) =>
        AppearancePreview.HumanTwinKey(key).Should().Be(expected);

    [Test]
    public void MissingSpeciesPictureFallsBackToTheHumanPicture()
    {
        bool Exists(string resref) => resref is "apmh0_chest148f" or "apme0_belt003f";
        AppearancePreview.Picture("pme0_chest148", AppearancePreviewView.Front, Exists).Should().Be("apmh0_chest148f");
        AppearancePreview.Picture("pme0_belt003", AppearancePreviewView.Front, Exists).Should().Be("apme0_belt003f");
        AppearancePreview.Picture("pme0_neck009", AppearancePreviewView.Front, Exists).Should().Be(AppearancePreview.EmptyPicture);
        AppearancePreview.Picture(string.Empty, AppearancePreviewView.Front, Exists).Should().Be(AppearancePreview.EmptyPicture);
        AppearancePreview.Picture("helm_002", AppearancePreviewView.Back, Exists).Should().Be(AppearancePreview.EmptyPicture);
        AppearancePreview.Picture("helm_002", AppearancePreviewView.Back, resref => resref == "ahelm_002b").Should().Be("ahelm_002b");
    }
}
