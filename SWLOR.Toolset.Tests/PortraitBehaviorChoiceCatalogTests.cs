using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Domain.GameData.Lookups;

namespace SWLOR.Toolset.Tests
{
    [TestFixture]
    public class PortraitBehaviorChoiceCatalogTests
    {
        [Test]
        public void PortraitsCarryTheSameAuthoritativeFiltersIntoEverySharedGallery()
        {
            var choices = PortraitBehaviorChoiceCatalog.Build(
                [
                    new PortraitRow(12, "hu_f_01_", "hu_f_01_", 1, 6, null),
                    new PortraitRow(305, "plc_chestb_", "plc_chestb_", 4, null, 1),
                    new PortraitRow(999, "custom_", "custom_", 99, 999, null)
                ],
                new Dictionary<int, string> { [1] = "Female", [4] = "None" },
                new Dictionary<int, string> { [6] = "Human" });

            choices.Should().HaveCount(3);
            choices[0].Display.Should().Be("hu_f_01_ (12)");
            choices[0].ImageResRef.Should().Be("po_hu_f_01_m");
            choices[0].ImageCrop.Should().Be(BehaviorChoiceImageCrop.NeverwinterPortrait);
            choices[0].GalleryFacets.Should().ContainEquivalentOf(
                new BehaviorChoiceFacet("gender", "Gender", "1", "Female", 1));
            choices[0].GalleryFacets.Should().ContainEquivalentOf(
                new BehaviorChoiceFacet("race", "Race", "6", "Human"));
            choices[0].GalleryFacets.Should().ContainEquivalentOf(
                new BehaviorChoiceFacet("subject", "Subject", "creature", "Creature"));

            choices[1].GalleryFacets.Should().ContainEquivalentOf(
                new BehaviorChoiceFacet("subject", "Subject", "inanimate", "Inanimate", 1));
            choices[2].GalleryFacets.Where(facet =>
                    facet.GroupKey is "gender" or "race")
                .Should().OnlyContain(facet => facet.Display == "Unspecified");
        }
    }
}
