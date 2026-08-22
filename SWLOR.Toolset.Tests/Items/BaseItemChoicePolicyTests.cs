using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors.Items;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests.Items
{
    /// <summary>Which baseitems.2da rows the Base Type list offers, both in isolation and against the real 2da.</summary>
    [TestFixture]
    public class BaseItemChoicePolicyTests
    {
        [TestCase(null, "AArCl", false)]
        [TestCase("", "AArCl", false)]
        [TestCase("   ", "AArCl", false)]
        [TestCase("DELETED", "AArCl", false)]
        [TestCase("deleted", "AArCl", false)]
        [TestCase("Deleted_Shortsword", "AArCl", false, TestName = "ContainsDeletedAnywhere")]
        [TestCase("bio_reserved", "AArCl", false)]
        [TestCase("BIO_RESERVED", "AArCl", false)]
        [TestCase("cep_reserved", "AArCl", false)]
        [TestCase("Padding", "AArCl", false)]
        [TestCase("PADDING", "AArCl", false)]
        [TestCase("Shortsword", null, false, TestName = "BlankItemClassIsRejected")]
        [TestCase("Shortsword", "", false)]
        [TestCase("USER", "AArCl", false)]
        [TestCase("Unused", "AArCl", false)]
        [TestCase("INVALID_ITEM", "AArCl", false)]
        [TestCase("NULL6", "AArCl", false)]
        [TestCase("Shortsword", "WSwSbre", true)]
        public void IsOffered_MatchesExpectation(string? label, string? itemClass, bool expected)
        {
            BaseItemChoicePolicy.IsOffered(label, itemClass).Should().Be(expected);
        }

        [TestCase("Bad Strref", false)]
        [TestCase("bad strref", false)]
        [TestCase("BAD STRREF", false)]
        [TestCase("  Bad Strref  ", false)]
        [TestCase("bio_reserved", false)]
        [TestCase("BIO_RESERVED", false)]
        [TestCase("cep_reserved", false)]
        [TestCase("DELETED", false)]
        [TestCase("Padding", false)]
        [TestCase("USER", false)]
        [TestCase("Unused", false)]
        [TestCase("INVALID_ITEM", false)]
        [TestCase("NULL6", false)]
        [TestCase("", false)]
        [TestCase("   ", false)]
        [TestCase("Shortsword", true)]
        public void IsOffered_RejectsABrokenTlkResolutionByDisplayText(string display, bool expected)
        {
            // Label/ItemClass are otherwise perfectly valid - only the resolved display text (what
            // a broken base-game dialog.tlk strref hands back for this row's Name column) decides.
            BaseItemChoicePolicy.IsOffered("Shortsword", "WSwSbre", display).Should().Be(expected);
        }

        [Test]
        public void IsOffered_WithNoDisplaySuppliedSkipsTheDisplayCheck()
        {
            // A caller with only the raw label/ItemClass (no resolved display available) still gets
            // the rest of the policy applied rather than being rejected for a check it can't run.
            BaseItemChoicePolicy.IsOffered("Shortsword", "WSwSbre").Should().BeTrue();
        }

        [Test]
        public void RealBaseItemsTable_ExcludesDeletedAndPaddingRowsButKeepsAHealthyNumber()
        {
            var sw2Da = Path.Combine(CorpusLocator.RepositoryRoot, "SWLOR_Haks", "sw_2da");
            var twoDa = new TwoDaService(sw2Da);
            var table = twoDa.GetTable("baseitems");

            var offered = 0;
            for (var row = 0; row < table.RowCount; row++)
            {
                var label = table.GetString(row, "label");
                var itemClass = table.GetString(row, "ItemClass");

                if (!BaseItemChoicePolicy.IsOffered(label, itemClass))
                    continue;

                var lowered = label!.Trim().ToLowerInvariant();
                lowered.Should().NotContain("deleted", $"row {row} should not have been offered");
                lowered.Should().NotContain("reserved", $"row {row} should not have been offered");
                lowered.Should().NotBe("padding", $"row {row} should not have been offered");
                lowered.Should().NotBe("user", $"row {row} should not have been offered");
                offered++;
            }

            offered.Should().BeGreaterThan(150, "the base item list still needs a healthy number of real rows");
        }
    }
}
