using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    /// <summary>Covers the Angler builds the fishing study plays: their numbers come from the game's perk definitions at the rank they are played at.</summary>
    [TestFixture]
    public class AnglerBuildTests
    {
        [Test]
        public void ABuildTakesItsNumbersFromThePerkDefinitionsAtTheRankItIsPlayedAt()
        {
            AnglerBuild.Unperked.EffectsAt(50).Should().Be(LineAnglerEffects.None);
            AnglerBuild.PatientLine.EffectsAt(4).Should().Be(LineAnglerEffects.None);
            AnglerBuild.PatientLine.EffectsAt(5).Should().Be(new LineAnglerEffects(TensionCapBonus: 2));
            AnglerBuild.PatientLine.EffectsAt(19).Should().Be(new LineAnglerEffects(TensionCapBonus: 2, SetHookDrainBonus: 1, ForecastDepth: 2, ForecastTurns: 1));
            AnglerBuild.PatientLine.EffectsAt(20).GiveLineDistanceReduction.Should().Be(1, "the style opens at Agriculture 20");
            AnglerBuild.AggressiveLine.EffectsAt(20).ReelDistanceBonus.Should().Be(1);
            AnglerBuild.PatientLine.EffectsAt(49).RareFishRungReduction.Should().Be(0);
            AnglerBuild.PatientLine.EffectsAt(50).Should().Be(new LineAnglerEffects(
                TensionCapBonus: 8, SetHookDrainBonus: 3, ForecastDepth: 2, ForecastTurns: 3, GiveLineDistanceReduction: 1, RareFishRungReduction: 4));
            AnglerBuild.PatientBelowCapstone.EffectsAt(50).RareFishRungReduction.Should().Be(0);
        }

        [Test]
        public void AFullLineOwnsOneStyleOnly_AndAnAloneBuildOwnsOnePurchase()
        {
            AnglerBuild.PatientLine.Perks.Should().Contain(PerkType.PatientAngler).And.NotContain(PerkType.AggressiveAngler);
            AnglerBuild.AggressiveLine.Perks.Should().Contain(PerkType.AggressiveAngler).And.NotContain(PerkType.PatientAngler);
            AnglerBuild.PatientBelowCapstone.Perks.Should().NotContain(PerkType.TrophyCatch);
            AnglerBuild.Alone(PerkType.StrongLine, 2).Perks.Should().Equal(PerkType.StrongLine);
        }

        [Test]
        public void APurchaseAloneIsPlayedAtTheLevelNamed_NotAtTheHighestLevelTheRankAllows()
        {
            AnglerBuild.Alone(PerkType.StrongLine, 1).EffectsAt(50).TensionCapBonus.Should().Be(2);
            AnglerBuild.Alone(PerkType.StrongLine, 2).EffectsAt(50).TensionCapBonus.Should().Be(5);
            AnglerBuild.Alone(PerkType.StrongLine, 3).EffectsAt(50).TensionCapBonus.Should().Be(8);
            AnglerBuild.Alone(PerkType.StrongLine, 3).EffectsAt(30).TensionCapBonus.Should().Be(0, "its rank is not reached");
            AnglerBuild.Alone(PerkType.StrongLine, 2).Name.Should().Be("Strong Line II");
            AnglerBuild.Alone(PerkType.TrophyCatch, 1).Name.Should().Be("Trophy Catch");
        }

        [Test]
        public void ThePurchasesOfTheLineAreTenRowsInRankOrder_TheStyleCountedTwice()
        {
            var purchases = AnglerPerkStudy.Purchases();

            purchases.Should().HaveCount(11);
            purchases.Select(purchase => purchase.Name).Should().Contain(new[] { "Patient Angler", "Aggressive Angler", "Trophy Catch", "Fish Sense II" });
        }
    }
}
