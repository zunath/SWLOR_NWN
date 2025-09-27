using SWLOR.Shared.UI.Entity;

namespace SWLOR.Test.Shared.UI.Entity
{
    [TestFixture]
    public class SimplePlayerWindowGeometryTests
    {
        [Test]
        public void Constructor_WithDefaultValues_SetsProperties()
        {
            // Arrange & Act
            var geometry = new PlayerWindowGeometry();

            // Assert
            Assert.That(geometry.Id, Is.Not.Null); // EntityBase generates a GUID
            Assert.That(geometry.WindowGeometries, Is.Not.Null);
            Assert.That(geometry.WindowGeometries.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithPlayerId_SetsId()
        {
            // Arrange & Act
            var geometry = new PlayerWindowGeometry("player123");

            // Assert
            Assert.That(geometry.Id, Is.EqualTo("player123"));
            Assert.That(geometry.WindowGeometries, Is.Not.Null);
        }

        [Test]
        public void SetId_WithValidId_SetsId()
        {
            // Arrange
            var geometry = new PlayerWindowGeometry();

            // Act
            geometry.Id = "new_player";

            // Assert
            Assert.That(geometry.Id, Is.EqualTo("new_player"));
        }

        [Test]
        public void WindowGeometries_CanBeModified()
        {
            // Arrange
            var geometry = new PlayerWindowGeometry();

            // Act
            geometry.WindowGeometries.Add(SWLOR.Shared.Abstractions.Enums.GuiWindowType.CharacterSheet, new SWLOR.Shared.UI.Component.GuiRectangle(0, 0, 100, 50));

            // Assert
            Assert.That(geometry.WindowGeometries.Count, Is.EqualTo(1));
        }
    }
}
