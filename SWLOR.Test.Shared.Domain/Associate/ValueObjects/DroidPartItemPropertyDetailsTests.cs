using NUnit.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Associate.ValueObjects;

namespace SWLOR.Test.Shared.Domain.Associate.ValueObjects
{
    [TestFixture]
    public class DroidPartItemPropertyDetailsTests
    {
        [Test]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var details = new DroidPartItemPropertyDetails();

            // Assert
            Assert.That(details.PartType, Is.EqualTo(ItemPropertyDroidPartSubType.Invalid));
            Assert.That(details.Tier, Is.EqualTo(0));
            Assert.That(details.Level, Is.EqualTo(0));
            Assert.That(details.HP, Is.EqualTo(0));
            Assert.That(details.STM, Is.EqualTo(0));
            Assert.That(details.AISlots, Is.EqualTo(0));
            Assert.That(details.AGI, Is.EqualTo(0));
            Assert.That(details.MGT, Is.EqualTo(0));
            Assert.That(details.PER, Is.EqualTo(0));
            Assert.That(details.SOC, Is.EqualTo(0));
            Assert.That(details.VIT, Is.EqualTo(0));
            Assert.That(details.WIL, Is.EqualTo(0));
            Assert.That(details.OneHanded, Is.EqualTo(0));
            Assert.That(details.TwoHanded, Is.EqualTo(0));
            Assert.That(details.MartialArts, Is.EqualTo(0));
            Assert.That(details.Ranged, Is.EqualTo(0));
        }

        [Test]
        public void Properties_ShouldBeSettable()
        {
            // Arrange
            var details = new DroidPartItemPropertyDetails();

            // Act
            details.PartType = ItemPropertyDroidPartSubType.Head;
            details.Tier = 3;
            details.Level = 8;
            details.HP = 75;
            details.STM = 40;
            details.AISlots = 2;
            details.AGI = 12;
            details.MGT = 10;
            details.PER = 15;
            details.SOC = 6;
            details.VIT = 18;
            details.WIL = 11;
            details.OneHanded = 4;
            details.TwoHanded = 2;
            details.MartialArts = 6;
            details.Ranged = 3;

            // Assert
            Assert.That(details.PartType, Is.EqualTo(ItemPropertyDroidPartSubType.Head));
            Assert.That(details.Tier, Is.EqualTo(3));
            Assert.That(details.Level, Is.EqualTo(8));
            Assert.That(details.HP, Is.EqualTo(75));
            Assert.That(details.STM, Is.EqualTo(40));
            Assert.That(details.AISlots, Is.EqualTo(2));
            Assert.That(details.AGI, Is.EqualTo(12));
            Assert.That(details.MGT, Is.EqualTo(10));
            Assert.That(details.PER, Is.EqualTo(15));
            Assert.That(details.SOC, Is.EqualTo(6));
            Assert.That(details.VIT, Is.EqualTo(18));
            Assert.That(details.WIL, Is.EqualTo(11));
            Assert.That(details.OneHanded, Is.EqualTo(4));
            Assert.That(details.TwoHanded, Is.EqualTo(2));
            Assert.That(details.MartialArts, Is.EqualTo(6));
            Assert.That(details.Ranged, Is.EqualTo(3));
        }
    }
}
