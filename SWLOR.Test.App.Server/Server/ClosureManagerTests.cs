using NSubstitute;
using SWLOR.App.Server.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.App.Server.Server
{
    /// <summary>
    /// Unit tests for ClosureManager class.
    /// Tests closure management, OBJECT_SELF handling, and error scenarios.
    /// </summary>
    [TestFixture]
    public class ClosureManagerTests
    {
        private ILogger _mockLogger;
        private ClosureManager _manager;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
            _manager = new ClosureManager(_mockLogger);
        }

        [Test]
        public void Constructor_WithValidLogger_ShouldCreateInstance()
        {
            // Arrange & Act
            var manager = new ClosureManager(_mockLogger);

            // Assert
            Assert.That(manager, Is.Not.Null);
        }

        [Test]
        public void ObjectSelf_DefaultValue_ShouldBeObjectInvalid()
        {
            // Arrange
            const uint expectedInvalid = 0x7F000000;

            // Act
            var result = _manager.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(expectedInvalid), 
                "Default ObjectSelf should be OBJECT_INVALID (0x7F000000)");
        }

        [Test]
        public void ObjectSelf_SetAndGet_ShouldReturnSetValue()
        {
            // Arrange
            const uint testValue = 12345u;

            // Act
            _manager.ObjectSelf = testValue;
            var result = _manager.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(testValue));
        }

        [Test]
        public void ObjectSelf_SetMultipleTimes_ShouldReturnLastValue()
        {
            // Arrange
            const uint value1 = 100u;
            const uint value2 = 200u;
            const uint value3 = 300u;

            // Act
            _manager.ObjectSelf = value1;
            _manager.ObjectSelf = value2;
            _manager.ObjectSelf = value3;
            var result = _manager.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(value3));
        }

        [Test]
        public void ObjectSelf_SetToZero_ShouldAcceptValue()
        {
            // Arrange
            const uint zeroValue = 0u;

            // Act
            _manager.ObjectSelf = zeroValue;
            var result = _manager.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(zeroValue));
        }

        [Test]
        public void ObjectSelf_SetToMaxValue_ShouldAcceptValue()
        {
            // Arrange
            const uint maxValue = uint.MaxValue;

            // Act
            _manager.ObjectSelf = maxValue;
            var result = _manager.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(maxValue));
        }

        [Test]
        public void ICoreFunctionHandler_ObjectSelf_ShouldMatchPublicProperty()
        {
            // Arrange
            const uint testValue = 54321u;
            _manager.ObjectSelf = testValue;

            // Act
            var publicValue = _manager.ObjectSelf;
            var interfaceValue = ((global::NWN.Core.ICoreFunctionHandler)_manager).ObjectSelf;

            // Assert
            Assert.That(interfaceValue, Is.EqualTo(publicValue), 
                "Interface implementation should match public property");
        }

        [Test]
        public void Constructor_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new ClosureManager(_mockLogger));
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldNotThrowDuringConstruction()
        {
            // Arrange & Act & Assert
            // Constructor itself shouldn't throw with null logger
            // (though using it might cause issues later)
            Assert.DoesNotThrow(() => new ClosureManager(null));
        }

        [Test]
        public void ObjectSelf_MultipleInstances_ShouldBeIndependent()
        {
            // Arrange
            var manager1 = new ClosureManager(_mockLogger);
            var manager2 = new ClosureManager(_mockLogger);
            const uint value1 = 111u;
            const uint value2 = 222u;

            // Act
            manager1.ObjectSelf = value1;
            manager2.ObjectSelf = value2;

            // Assert
            Assert.That(manager1.ObjectSelf, Is.EqualTo(value1));
            Assert.That(manager2.ObjectSelf, Is.EqualTo(value2));
            Assert.That(manager1.ObjectSelf, Is.Not.EqualTo(manager2.ObjectSelf),
                "Different instances should maintain independent ObjectSelf values");
        }

        [Test]
        public void ObjectSelf_AfterReset_ShouldReturnNewValue()
        {
            // Arrange
            const uint invalidValue = 0x7F000000;
            _manager.ObjectSelf = 12345u;

            // Act
            _manager.ObjectSelf = invalidValue;
            var result = _manager.ObjectSelf;

            // Assert
            Assert.That(result, Is.EqualTo(invalidValue));
        }

        [Test]
        public void ClosureManager_ShouldImplementIClosureManager()
        {
            // Assert
            Assert.That(_manager, Is.InstanceOf<IClosureManager>());
        }

        [Test]
        public void ClosureManager_ShouldImplementICoreFunctionHandler()
        {
            // Assert
            Assert.That(_manager, Is.InstanceOf<global::NWN.Core.ICoreFunctionHandler>());
        }
    }
}

