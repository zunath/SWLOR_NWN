using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Test.Shared.Events.Attributes
{
    [TestFixture]
    public class ScriptHandlerAttributeTests
    {
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