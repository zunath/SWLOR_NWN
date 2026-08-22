using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Behaviors;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>
    /// Every item-editor choice list (Base Types, Palette Categories, Spells, and every subtype set)
    /// must come back alphabetical by display text. <see cref="EditorService"/> needs a live
    /// workspace/session to construct, so this reaches its private <c>SortByDisplay</c> helper
    /// directly through reflection rather than standing up the whole service - the same static
    /// method every BuildItemChoices branch is routed through.
    /// </summary>
    [TestFixture]
    public class ItemChoiceOrderingTests
    {
        private static IReadOnlyList<BehaviorChoice> SortByDisplay(IReadOnlyList<BehaviorChoice> choices)
        {
            var method = typeof(EditorService).GetMethod(
                "SortByDisplay", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull("EditorService must still expose its central sort helper");

            return (IReadOnlyList<BehaviorChoice>)method!.Invoke(null, new object[] { choices })!;
        }

        [Test]
        public void BaseTypeDisplaysComeBackAlphabetical()
        {
            var unordered = new[]
            {
                new BehaviorChoice(1, "Shortsword"),
                new BehaviorChoice(2, "amulet"),
                new BehaviorChoice(3, "Bastard Sword"),
                new BehaviorChoice(4, "Zweihander"),
                new BehaviorChoice(5, "boots")
            };

            var sorted = SortByDisplay(unordered);

            sorted.Select(choice => choice.Display).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
            sorted.Select(choice => choice.Display).Should().Equal(
                "amulet", "Bastard Sword", "boots", "Shortsword", "Zweihander");
        }

        [Test]
        public void SubtypeSetDisplaysComeBackAlphabeticalIncludingHierarchicalCategoryStrings()
        {
            // Category displays are hierarchical ("Armor > Clothing"); a plain ordinal sort of the
            // whole string is what "alphabetical" means for those too - no special-casing the ">".
            var unordered = new[]
            {
                new BehaviorChoice(1, "Fire Resistance"),
                new BehaviorChoice(2, "Armor > Clothing"),
                new BehaviorChoice(3, "Disruption Resistance"),
                new BehaviorChoice(4, "Armor > Heavy"),
                new BehaviorChoice(5, "Electrical Resistance")
            };

            var sorted = SortByDisplay(unordered);

            sorted.Select(choice => choice.Display).Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
            sorted.Select(choice => choice.Display).Should().Equal(
                "Armor > Clothing", "Armor > Heavy", "Disruption Resistance",
                "Electrical Resistance", "Fire Resistance");
        }
    }
}
