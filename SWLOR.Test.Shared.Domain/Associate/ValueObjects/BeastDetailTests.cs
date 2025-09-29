using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class BeastDetailTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var beastDetail = new BeastDetail();

            // Assert
            Assert.That(beastDetail.Name, Is.Null);
            Assert.That(beastDetail.Appearance, Is.EqualTo(AppearanceType.Dwarf));
            Assert.That(beastDetail.AppearanceScale, Is.EqualTo(1f));
            Assert.That(beastDetail.PortraitId, Is.EqualTo(0));
            Assert.That(beastDetail.SoundSetId, Is.EqualTo(0));
            Assert.That(beastDetail.Role, Is.EqualTo(BeastRoleType.Invalid));
            Assert.That(beastDetail.AccuracyStat, Is.EqualTo(AbilityType.Might));
            Assert.That(beastDetail.DamageStat, Is.EqualTo(AbilityType.Might));
            Assert.That(beastDetail.Levels, Is.Not.Null);
            Assert.That(beastDetail.Levels, Is.Empty);
            Assert.That(beastDetail.PossibleMutations, Is.Not.Null);
            Assert.That(beastDetail.PossibleMutations, Is.Empty);
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var beastDetail = new BeastDetail();
            var mutation = new MutationDetail(BeastType.Aradile);

            // Act
            beastDetail.Name = "Test Beast";
            beastDetail.Appearance = AppearanceType.Human;
            beastDetail.AppearanceScale = 1.5f;
            beastDetail.PortraitId = 123;
            beastDetail.SoundSetId = 456;
            beastDetail.Role = BeastRoleType.Tank;
            beastDetail.AccuracyStat = AbilityType.Perception;
            beastDetail.DamageStat = AbilityType.Might;
            beastDetail.Levels.Add(1, new BeastLevel());
            beastDetail.PossibleMutations.Add(mutation);

            // Assert
            Assert.That(beastDetail.Name, Is.EqualTo("Test Beast"));
            Assert.That(beastDetail.Appearance, Is.EqualTo(AppearanceType.Human));
            Assert.That(beastDetail.AppearanceScale, Is.EqualTo(1.5f));
            Assert.That(beastDetail.PortraitId, Is.EqualTo(123));
            Assert.That(beastDetail.SoundSetId, Is.EqualTo(456));
            Assert.That(beastDetail.Role, Is.EqualTo(BeastRoleType.Tank));
            Assert.That(beastDetail.AccuracyStat, Is.EqualTo(AbilityType.Perception));
            Assert.That(beastDetail.DamageStat, Is.EqualTo(AbilityType.Might));
            Assert.That(beastDetail.Levels.Count, Is.EqualTo(1));
            Assert.That(beastDetail.PossibleMutations.Count, Is.EqualTo(1));
        }

        [Test]
        public void Levels_ShouldBeMutable()
        {
            // Arrange
            var beastDetail = new BeastDetail();
            var level1 = new BeastLevel { HP = 100 };
            var level2 = new BeastLevel { HP = 200 };

            // Act
            beastDetail.Levels.Add(1, level1);
            beastDetail.Levels.Add(2, level2);

            // Assert
            Assert.That(beastDetail.Levels.Count, Is.EqualTo(2));
            Assert.That(beastDetail.Levels[1].HP, Is.EqualTo(100));
            Assert.That(beastDetail.Levels[2].HP, Is.EqualTo(200));
        }

        [Test]
        public void PossibleMutations_ShouldBeMutable()
        {
            // Arrange
            var beastDetail = new BeastDetail();
            var mutation1 = new MutationDetail(BeastType.Aradile);
            var mutation2 = new MutationDetail(BeastType.CrystalSpider);

            // Act
            beastDetail.PossibleMutations.Add(mutation1);
            beastDetail.PossibleMutations.Add(mutation2);

            // Assert
            Assert.That(beastDetail.PossibleMutations.Count, Is.EqualTo(2));
            Assert.That(beastDetail.PossibleMutations, Contains.Item(mutation1));
            Assert.That(beastDetail.PossibleMutations, Contains.Item(mutation2));
        }
    }
}
