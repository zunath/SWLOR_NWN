using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Combat.Enums;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class IncubationJobTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var job = new IncubationJob();

            // Assert
            Assert.That(job.ParentPropertyId, Is.Null);
            Assert.That(job.PlayerId, Is.Null);
            Assert.That(job.CurrentStage, Is.EqualTo(0));
            Assert.That(job.BeastDNAType, Is.EqualTo(BeastType.Invalid));
            Assert.That(job.MutationChance, Is.EqualTo(0));
            Assert.That(job.AttackPurity, Is.EqualTo(0));
            Assert.That(job.AccuracyPurity, Is.EqualTo(0));
            Assert.That(job.EvasionPurity, Is.EqualTo(0));
            Assert.That(job.LearningPurity, Is.EqualTo(0));
            Assert.That(job.XPPenalty, Is.EqualTo(0));
            Assert.That(job.DateStarted, Is.EqualTo(DateTime.MinValue));
            Assert.That(job.DateCompleted, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void Constructor_ShouldInitializeDictionaries()
        {
            // Act
            var job = new IncubationJob();

            // Assert
            Assert.That(job.DefensePurities, Is.Not.Null);
            Assert.That(job.DefensePurities, Is.Empty);
            Assert.That(job.SavingThrowPurities, Is.Not.Null);
            Assert.That(job.SavingThrowPurities, Is.Empty);
            Assert.That(job.LyaseColors, Is.Not.Null);
            Assert.That(job.LyaseColors.Count, Is.EqualTo(8));
            Assert.That(job.IsomeraseColors, Is.Not.Null);
            Assert.That(job.IsomeraseColors.Count, Is.EqualTo(8));
            Assert.That(job.HydrolaseColors, Is.Not.Null);
            Assert.That(job.HydrolaseColors.Count, Is.EqualTo(8));
        }

        [Test]
        public void Constructor_ShouldInitializeEnzymeColorDictionariesWithZeroValues()
        {
            // Act
            var job = new IncubationJob();

            // Assert
            Assert.That(job.LyaseColors[EnzymeColorType.Blue], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.Orange], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.Red], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.Purple], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.White], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.Green], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.Yellow], Is.EqualTo(0));
            Assert.That(job.LyaseColors[EnzymeColorType.Black], Is.EqualTo(0));
            
            Assert.That(job.IsomeraseColors[EnzymeColorType.Blue], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Orange], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Red], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Purple], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.White], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Green], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Yellow], Is.EqualTo(0));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Black], Is.EqualTo(0));
            
            Assert.That(job.HydrolaseColors[EnzymeColorType.Blue], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.Orange], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.Red], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.Purple], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.White], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.Green], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.Yellow], Is.EqualTo(0));
            Assert.That(job.HydrolaseColors[EnzymeColorType.Black], Is.EqualTo(0));
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var job = new IncubationJob();
            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(1);

            // Act
            job.ParentPropertyId = "parent123";
            job.PlayerId = "player456";
            job.CurrentStage = 3;
            job.BeastDNAType = BeastType.JuvenileRancor;
            job.MutationChance = 25;
            job.AttackPurity = 80;
            job.AccuracyPurity = 70;
            job.EvasionPurity = 60;
            job.LearningPurity = 90;
            job.XPPenalty = 10;
            job.DateStarted = startDate;
            job.DateCompleted = endDate;

            // Assert
            Assert.That(job.ParentPropertyId, Is.EqualTo("parent123"));
            Assert.That(job.PlayerId, Is.EqualTo("player456"));
            Assert.That(job.CurrentStage, Is.EqualTo(3));
            Assert.That(job.BeastDNAType, Is.EqualTo(BeastType.JuvenileRancor));
            Assert.That(job.MutationChance, Is.EqualTo(25));
            Assert.That(job.AttackPurity, Is.EqualTo(80));
            Assert.That(job.AccuracyPurity, Is.EqualTo(70));
            Assert.That(job.EvasionPurity, Is.EqualTo(60));
            Assert.That(job.LearningPurity, Is.EqualTo(90));
            Assert.That(job.XPPenalty, Is.EqualTo(10));
            Assert.That(job.DateStarted, Is.EqualTo(startDate));
            Assert.That(job.DateCompleted, Is.EqualTo(endDate));
        }

        [Test]
        public void DefensePurities_ShouldBeMutable()
        {
            // Arrange
            var job = new IncubationJob();

            // Act
            job.DefensePurities.Add(CombatDamageType.Physical, 5);
            job.DefensePurities.Add(CombatDamageType.Force, 3);

            // Assert
            Assert.That(job.DefensePurities.Count, Is.EqualTo(2));
            Assert.That(job.DefensePurities[CombatDamageType.Physical], Is.EqualTo(5));
            Assert.That(job.DefensePurities[CombatDamageType.Force], Is.EqualTo(3));
        }

        [Test]
        public void SavingThrowPurities_ShouldBeMutable()
        {
            // Arrange
            var job = new IncubationJob();

            // Act
            job.SavingThrowPurities.Add(SavingThrowCategoryType.Fortitude, 4);
            job.SavingThrowPurities.Add(SavingThrowCategoryType.Will, 2);

            // Assert
            Assert.That(job.SavingThrowPurities.Count, Is.EqualTo(2));
            Assert.That(job.SavingThrowPurities[SavingThrowCategoryType.Fortitude], Is.EqualTo(4));
            Assert.That(job.SavingThrowPurities[SavingThrowCategoryType.Will], Is.EqualTo(2));
        }

        [Test]
        public void LyaseColors_ShouldBeMutable()
        {
            // Arrange
            var job = new IncubationJob();

            // Act
            job.LyaseColors[EnzymeColorType.Blue] = 5;
            job.LyaseColors[EnzymeColorType.Red] = 3;

            // Assert
            Assert.That(job.LyaseColors[EnzymeColorType.Blue], Is.EqualTo(5));
            Assert.That(job.LyaseColors[EnzymeColorType.Red], Is.EqualTo(3));
        }

        [Test]
        public void IsomeraseColors_ShouldBeMutable()
        {
            // Arrange
            var job = new IncubationJob();

            // Act
            job.IsomeraseColors[EnzymeColorType.Green] = 7;
            job.IsomeraseColors[EnzymeColorType.Yellow] = 2;

            // Assert
            Assert.That(job.IsomeraseColors[EnzymeColorType.Green], Is.EqualTo(7));
            Assert.That(job.IsomeraseColors[EnzymeColorType.Yellow], Is.EqualTo(2));
        }

        [Test]
        public void HydrolaseColors_ShouldBeMutable()
        {
            // Arrange
            var job = new IncubationJob();

            // Act
            job.HydrolaseColors[EnzymeColorType.Purple] = 6;
            job.HydrolaseColors[EnzymeColorType.White] = 1;

            // Assert
            Assert.That(job.HydrolaseColors[EnzymeColorType.Purple], Is.EqualTo(6));
            Assert.That(job.HydrolaseColors[EnzymeColorType.White], Is.EqualTo(1));
        }
    }
}
