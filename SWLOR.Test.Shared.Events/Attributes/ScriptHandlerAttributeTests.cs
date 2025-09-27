using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Test.Shared.Events.Attributes
{
    [TestFixture]
    public class ScriptHandlerAttributeTests
    {
        [Test]
        public void Constructor_WithValidScript_SetsScript()
        {
            // Arrange
            var scriptName = "test_script";

            // Act
            var attribute = new ScriptHandlerAttribute(scriptName);

            // Assert
            Assert.That(attribute.Script, Is.EqualTo(scriptName));
        }

        [Test]
        public void Constructor_WithNullScript_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ScriptHandlerAttribute(null));
        }

        [Test]
        public void Constructor_WithEmptyScript_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ScriptHandlerAttribute(string.Empty));
        }

        [Test]
        public void Constructor_WithWhitespaceScript_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ScriptHandlerAttribute("   "));
        }

        [Test]
        public void GenericConstructor_WithValidEventType_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ScriptHandlerAttribute<OnModuleLoad>());
        }

        [Test]
        public void GenericConstructor_WithEventType_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new ScriptHandlerAttribute<OnModuleLoad>());
        }
    }
}