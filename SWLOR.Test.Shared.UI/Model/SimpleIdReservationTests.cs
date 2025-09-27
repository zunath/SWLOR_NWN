using NUnit.Framework;
using SWLOR.Shared.UI.Model;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleIdReservationTests
    {
        [Test]
        public void Constructor_WithDefaultValues_SetsProperties()
        {
            // Arrange & Act
            var idReservation = new IdReservation();

            // Assert
            Assert.That(idReservation.Count, Is.EqualTo(0));
            Assert.That(idReservation.StartId, Is.EqualTo(0));
            Assert.That(idReservation.EndId, Is.EqualTo(0));
        }

        [Test]
        public void SetCount_WithValidValue_SetsCount()
        {
            // Arrange
            var idReservation = new IdReservation();

            // Act
            idReservation.Count = 10;

            // Assert
            Assert.That(idReservation.Count, Is.EqualTo(10));
        }

        [Test]
        public void SetStartId_WithValidValue_SetsStartId()
        {
            // Arrange
            var idReservation = new IdReservation();

            // Act
            idReservation.StartId = 100;

            // Assert
            Assert.That(idReservation.StartId, Is.EqualTo(100));
        }

        [Test]
        public void SetEndId_WithValidValue_SetsEndId()
        {
            // Arrange
            var idReservation = new IdReservation();

            // Act
            idReservation.EndId = 200;

            // Assert
            Assert.That(idReservation.EndId, Is.EqualTo(200));
        }
    }
}
