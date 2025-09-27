using NUnit.Framework;
using SWLOR.Shared.UI.Component;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiComboEntryTests
    {
        [Test]
        public void Constructor_WithDefaultValues_SetsProperties()
        {
            // Arrange & Act
            var entry = new GuiComboEntry("", 0);

            // Assert
            Assert.That(entry.Label, Is.EqualTo(""));
            Assert.That(entry.Value, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithLabelAndValue_SetsProperties()
        {
            // Arrange & Act
            var entry = new GuiComboEntry("Option 1", 1);

            // Assert
            Assert.That(entry.Label, Is.EqualTo("Option 1"));
            Assert.That(entry.Value, Is.EqualTo(1));
        }

        [Test]
        public void SetLabel_WithValidLabel_SetsLabel()
        {
            // Arrange
            var entry = new GuiComboEntry("", 0);

            // Act
            entry.Label = "New Label";

            // Assert
            Assert.That(entry.Label, Is.EqualTo("New Label"));
        }

        [Test]
        public void SetValue_WithValidValue_SetsValue()
        {
            // Arrange
            var entry = new GuiComboEntry("", 0);

            // Act
            entry.Value = 5;

            // Assert
            Assert.That(entry.Value, Is.EqualTo(5));
        }
    }
}
