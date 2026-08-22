using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Documents;

namespace SWLOR.Toolset.Tests
{
    /// <summary>Verifies ItpDocument's recursive palette-node traversal against a real corpus file.</summary>
    public class ItpDocumentTests
    {
        private static string CreaturePalettePath =>
            Path.Combine(CorpusLocator.ModuleDirectory, "itp", "creaturepalcus.itp.json");

        [Test]
        public void CreaturePalette_TopLevelNodes_MatchCorpus()
        {
            var document = ItpDocument.Load(CreaturePalettePath);

            document.Nodes.Should().HaveCount(6);
            document.Nodes[0].Name.Should().Be("Modern");
            document.Nodes[0].StrRef.Should().BeNull();
            document.Nodes[0].Children.Should().HaveCount(15);

            document.Nodes[1].StrRef.Should().Be(6693u);
            document.Nodes[1].Name.Should().BeNull();
            document.Nodes[1].Children.Should().HaveCount(16);

            document.Nodes[5].Name.Should().Be("World Creatures");
        }

        [Test]
        public void CreaturePalette_LeafNode_HasResRefFactionAndCr()
        {
            var document = ItpDocument.Load(CreaturePalettePath);

            // Nodes[0] ("Modern") -> Children[0] (an ID/STRREF subcategory) -> Children[0] (the
            // actual creature leaf, with RESREF/FACTION/CR/NAME).
            var subcategory = document.Nodes[0].Children[0];
            subcategory.StrRef.Should().Be(63235u);

            var leaf = subcategory.Children[0];
            leaf.ResRef.Should().Be("riktasha");
            leaf.Name.Should().Be("Riktasha");
            leaf.Faction.Should().Be("Hostile");
            leaf.ChallengeRating.Should().Be(0.125f);
        }

        [Test]
        public void CreaturePalette_UnobservedMembers_ReturnNull()
        {
            // CC and DELETE_ME were named in the original brief but never observed in this
            // repository's .itp corpus; verify the defensive accessors degrade to null rather
            // than throwing.
            var document = ItpDocument.Load(CreaturePalettePath);
            var node = document.Nodes[0];

            node.Cc.Should().BeNull();
            node.DeleteMe.Should().BeNull();
        }
    }
}
