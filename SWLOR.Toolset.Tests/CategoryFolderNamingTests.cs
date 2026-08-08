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

        /// <summary>
        /// The strict constructor is for names this build invents; names read out of a file or a base-game
        /// palette go through <see cref="CategoryFolder.Sanitize"/> first, because refusing those means
        /// refusing to open the module at all.
        /// </summary>
        [Test]
        public void SanitizeRepairsANameTheConstructorWouldRefuse()
        {
            var repaired = CategoryFolder.Sanitize("Skin/Hide");

            repaired.Should().Be("Skin-Hide");
            var act = () => new CategoryFolder(repaired!);
            act.Should().NotThrow();
        }

        [Test]
        public void SanitizeLeavesALegalNameAloneApartFromTrimming()
        {
            CategoryFolder.Sanitize("  Weapons  ").Should().Be("Weapons");
        }

        [Test]
        public void SanitizeReportsANameWithNothingUsableInIt()
        {
            CategoryFolder.Sanitize(null).Should().BeNull();
            CategoryFolder.Sanitize("   ").Should().BeNull("a folder with no name cannot be shown or addressed");
        }

        /// <summary>
        /// The other half of <see cref="CategoryFolder.Sanitize"/>'s job. A name out of a file gets
        /// repaired because nobody is there to be told; a name someone typed gets reported because they
        /// are, and neither wants the constructor's throw - a command handler cannot catch it anywhere
        /// useful, and it reached the builder as a crash rather than as a message.
        /// </summary>
        [Test]
        public void NameProblemReportsATypedNameHoldingThePathSeparator()
        {
            CategoryFolder.NameProblem("Weapons/Melee").Should().Contain("cannot contain");
        }

        [Test]
        public void NameProblemReportsATypedNameWithNothingInIt()
        {
            CategoryFolder.NameProblem("   ").Should().NotBeNullOrEmpty();
            CategoryFolder.NameProblem(null).Should().NotBeNullOrEmpty();
        }

        [Test]
        public void NameProblemPassesALegalTypedName()
        {
            CategoryFolder.NameProblem("Weapons").Should().BeNull();
        }

        [Test]
        public void SurroundingWhitespaceStillDoesNotCreateADistinctSibling()
        {
            var parent = new CategoryFolder("Weapons");
            parent.AddChild("Melee");

            var act = () => parent.AddChild("  Melee  ");

            act.Should().Throw<ArgumentException>("names are trimmed before they are compared");
        }

        [Test]
        public void RootFolderNamesAreUniqueIgnoringCase()
        {
            var section = new CategorySection();
            section.AddFolder("Weapons");

            var act = () => section.AddFolder("weapons");

            act.Should().Throw<ArgumentException>().WithMessage("*already has*");
            section.Folders.Should().ContainSingle();
        }

        [Test]
        public void RenamingCannotCollideWithASibling()
        {
            var section = new CategorySection();
            var weapons = section.AddFolder("Weapons");
            section.AddFolder("Props");

            section.TryRenameFolder(weapons, "props").Should().BeFalse();
            weapons.Name.Should().Be("Weapons");
        }

        [Test]
        public void NestedRenameChecksItsOwnParentAndRepathsPins()
        {
            var section = new CategorySection();
            var weapons = section.AddFolder("Weapons");
            var melee = weapons.AddChild("Melee");
            weapons.AddChild("Ranged");
            section.Pin(section.PathKey(melee));

            section.TryRenameFolder(melee, "RANGED").Should().BeFalse();
            section.TryRenameFolder(melee, "Blades").Should().BeTrue();

            melee.Name.Should().Be("Blades");
            section.Pinned.Should().Equal("Weapons/Blades");
        }
    }
}
