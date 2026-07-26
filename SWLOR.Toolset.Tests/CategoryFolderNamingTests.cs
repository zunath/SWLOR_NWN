using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Categories;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// Category names have to keep their path keys unambiguous.
    /// </summary>
    /// <remarks>
    /// A pin is stored as a path key built by joining folder names with '/', and resolved by splitting
    /// on it again. Two things broke that. A name containing '/' produced a key that split into segments
    /// that never existed, so the pin resolved to nothing or to another branch. Two siblings with the
    /// same name shared a key outright, so pinning or locating the second found the first and toggling
    /// either could unpin the other.
    /// </remarks>
    [TestFixture]
    public class CategoryFolderNamingTests
    {
        [Test]
        public void ANameCannotContainThePathSeparator()
        {
            var act = () => new CategoryFolder("Weapons/Melee");

            act.Should().Throw<ArgumentException>().WithMessage("*cannot contain*");
        }

        [Test]
        public void RenamingToAPathSeparatorIsRefused()
        {
            var folder = new CategoryFolder("Weapons");

            var act = () => folder.Rename("Props/Industrial");

            act.Should().Throw<ArgumentException>();
            folder.Name.Should().Be("Weapons", "a refused rename must leave the folder alone");
        }

        [Test]
        public void AddingAChildWithThePathSeparatorIsRefused()
        {
            var parent = new CategoryFolder("Weapons");

            var act = () => parent.AddChild("Melee/Blades");

            act.Should().Throw<ArgumentException>();
            parent.Children.Should().BeEmpty();
        }

        [Test]
        public void TwoSiblingsCannotShareAName()
        {
            var parent = new CategoryFolder("Weapons");
            parent.AddChild("Melee");

            var act = () => parent.AddChild("Melee");

            act.Should().Throw<ArgumentException>().WithMessage("*already has*");
            parent.Children.Should().ContainSingle();
        }

        [Test]
        public void SiblingNamesClashRegardlessOfCase()
        {
            var parent = new CategoryFolder("Weapons");
            parent.AddChild("Melee");

            var act = () => parent.AddChild("melee");

            act.Should().Throw<ArgumentException>("path keys are resolved case-insensitively");
        }

        [Test]
        public void TheSameNameUnderDifferentParentsIsFine()
        {
            var weapons = new CategoryFolder("Weapons");
            var props = new CategoryFolder("Props");

            weapons.AddChild("Rare");
            var act = () => props.AddChild("Rare");

            act.Should().NotThrow("their full paths differ, so their keys differ");
        }

        [Test]
        public void AFolderMayKeepItsOwnNameWhenChecked()
        {
            var parent = new CategoryFolder("Weapons");
            var child = parent.AddChild("Melee");

            parent.IsNameAvailable("Melee", except: child).Should().BeTrue("renaming to its own name is a no-op");
            parent.IsNameAvailable("Melee").Should().BeFalse();
        }

        [Test]
        public void SurroundingWhitespaceStillDoesNotCreateADistinctSibling()
        {
            var parent = new CategoryFolder("Weapons");
            parent.AddChild("Melee");

            var act = () => parent.AddChild("  Melee  ");

            act.Should().Throw<ArgumentException>("names are trimmed before they are compared");
        }
    }
}
