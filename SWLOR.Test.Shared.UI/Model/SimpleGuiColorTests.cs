using SWLOR.Shared.Abstractions.Models;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiColorTests
    {
        [Test]
        public void Constructor_WithDefaultValues_SetsProperties()
        {
            // Arrange & Act
            var color = new GuiColor(0, 0, 0, 0);

            // Assert
            Assert.That(color.R, Is.EqualTo(0));
            Assert.That(color.G, Is.EqualTo(0));
            Assert.That(color.B, Is.EqualTo(0));
            Assert.That(color.Alpha, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithRgbaValues_SetsProperties()
        {
            // Arrange & Act
            var color = new GuiColor(255, 128, 64, 200);

            // Assert
            Assert.That(color.R, Is.EqualTo(255));
            Assert.That(color.G, Is.EqualTo(128));
            Assert.That(color.B, Is.EqualTo(64));
            Assert.That(color.Alpha, Is.EqualTo(200));
        }

        [Test]
        public void SetR_WithValidValue_SetsR()
        {
            // Arrange
            var color = new GuiColor(0, 0, 0, 0);

            // Act
            color.R = 100;

            // Assert
            Assert.That(color.R, Is.EqualTo(100));
        }

        [Test]
        public void SetG_WithValidValue_SetsG()
        {
            // Arrange
            var color = new GuiColor(0, 0, 0, 0);

            // Act
            color.G = 150;

            // Assert
            Assert.That(color.G, Is.EqualTo(150));
        }

        [Test]
        public void SetB_WithValidValue_SetsB()
        {
            // Arrange
            var color = new GuiColor(0, 0, 0, 0);

            // Act
            color.B = 200;

            // Assert
            Assert.That(color.B, Is.EqualTo(200));
        }

        [Test]
        public void SetAlpha_WithValidValue_SetsAlpha()
        {
            // Arrange
            var color = new GuiColor(0, 0, 0, 0);

            // Act
            color.Alpha = 255;

            // Assert
            Assert.That(color.Alpha, Is.EqualTo(255));
        }
    }
}
