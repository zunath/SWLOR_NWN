using SWLOR.Shared.UI.Component;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiRectangleTests
    {
        [Test]
        public void Constructor_WithDefaultValues_SetsProperties()
        {
            // Arrange & Act
            var rectangle = new GuiRectangle(0, 0, 0, 0);

            // Assert
            Assert.That(rectangle.X, Is.EqualTo(0));
            Assert.That(rectangle.Y, Is.EqualTo(0));
            Assert.That(rectangle.Width, Is.EqualTo(0));
            Assert.That(rectangle.Height, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithValues_SetsProperties()
        {
            // Arrange & Act
            var rectangle = new GuiRectangle(10, 20, 100, 50);

            // Assert
            Assert.That(rectangle.X, Is.EqualTo(10));
            Assert.That(rectangle.Y, Is.EqualTo(20));
            Assert.That(rectangle.Width, Is.EqualTo(100));
            Assert.That(rectangle.Height, Is.EqualTo(50));
        }

        [Test]
        public void SetX_WithValidValue_SetsX()
        {
            // Arrange
            var rectangle = new GuiRectangle(0, 0, 0, 0);

            // Act
            rectangle.X = 15;

            // Assert
            Assert.That(rectangle.X, Is.EqualTo(15));
        }

        [Test]
        public void SetY_WithValidValue_SetsY()
        {
            // Arrange
            var rectangle = new GuiRectangle(0, 0, 0, 0);

            // Act
            rectangle.Y = 25;

            // Assert
            Assert.That(rectangle.Y, Is.EqualTo(25));
        }

        [Test]
        public void SetWidth_WithValidValue_SetsWidth()
        {
            // Arrange
            var rectangle = new GuiRectangle(0, 0, 0, 0);

            // Act
            rectangle.Width = 150;

            // Assert
            Assert.That(rectangle.Width, Is.EqualTo(150));
        }

        [Test]
        public void SetHeight_WithValidValue_SetsHeight()
        {
            // Arrange
            var rectangle = new GuiRectangle(0, 0, 0, 0);

            // Act
            rectangle.Height = 75;

            // Assert
            Assert.That(rectangle.Height, Is.EqualTo(75));
        }
    }
}
