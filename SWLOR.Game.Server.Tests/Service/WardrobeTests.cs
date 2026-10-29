using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class WardrobeTests
{
    [Test]
    public void OutfitsKeepOneOrderWhateverTheSearchReturns()
    {
        var names = new[] { "Outfit #10", "outfit #2", "Formal", "Outfit #1", "Battle Dress", "Outfit #2" };
        var outfits = names.Select(name => new PlayerOutfit { Name = name }).ToList();

        var forward = Wardrobe.SortForDisplay(outfits).Select(outfit => outfit.Id).ToList();
        var reversed = Wardrobe.SortForDisplay(Enumerable.Reverse(outfits)).Select(outfit => outfit.Id).ToList();

        reversed.Should().Equal(forward, "the same outfits always list in the same order");
        var sorted = Wardrobe.SortForDisplay(outfits).Select(outfit => outfit.Name).ToList();
        sorted.Take(3).Should().Equal("Battle Dress", "Formal", "Outfit #1");
        sorted.Skip(3).Take(2).Should().BeEquivalentTo(new[] { "outfit #2", "Outfit #2" }, "case is ignored");
        sorted.Last().Should().Be("Outfit #10", "numbers compare by value, so #10 follows #2");
    }

    [Test]
    public void OutfitsWithoutNamesStillSort()
    {
        var outfits = new[] { new PlayerOutfit { Name = null }, new PlayerOutfit { Name = "Outfit #1" } };

        Wardrobe.SortForDisplay(outfits).Select(outfit => outfit.Name).Should().Equal(null, "Outfit #1");
    }
}
