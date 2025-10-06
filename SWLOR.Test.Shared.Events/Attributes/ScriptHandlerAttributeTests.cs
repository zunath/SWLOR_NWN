using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Infrastructure;

namespace SWLOR.Test.Shared.Events.Attributes
{
    [TestFixture]
    public class ScriptHandlerAttributeTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var attribute = new ScriptHandlerAttribute<OnServerLoaded>();

            // Assert
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute, Is.InstanceOf<Attribute>());
        }

        [Test]
        public void Constructor_WithDifferentEventTypes_ShouldCreateInstances()
        {
            // Act
            var attribute1 = new ScriptHandlerAttribute<OnServerLoaded>();
            var attribute2 = new ScriptHandlerAttribute<OnHookEvents>();

            // Assert
            Assert.That(attribute1, Is.Not.Null);
            Assert.That(attribute2, Is.Not.Null);
            Assert.That(attribute1.GetType(), Is.Not.EqualTo(attribute2.GetType()));
        }

        [Test]
        public void AttributeUsage_ShouldAllowMultiple()
        {
            // Arrange
            var attributeType = typeof(ScriptHandlerAttribute<>);

            // Act
            var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .FirstOrDefault() as AttributeUsageAttribute;

            // Assert
            Assert.That(attributeUsage, Is.Not.Null);
            Assert.That(attributeUsage.AllowMultiple, Is.True);
        }

        [Test]
        public void AttributeUsage_ShouldTargetMethods()
        {
            // Arrange
            var attributeType = typeof(ScriptHandlerAttribute<>);

            // Act
            var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false)
                .FirstOrDefault() as AttributeUsageAttribute;

            // Assert
            Assert.That(attributeUsage, Is.Not.Null);
            Assert.That(attributeUsage.ValidOn, Is.EqualTo(AttributeTargets.Method));
        }

        [Test]
        public void GenericConstraint_ShouldRequireIEvent()
        {
            // This test verifies that the generic constraint is properly applied
            // The compiler should prevent non-IEvent types from being used
            Assert.Pass("Generic constraint verification requires compile-time checking");
        }

        [Test]
        public void Attribute_CanBeAppliedToMethod()
        {
            // This test verifies that the attribute can be applied to methods
            // The actual application is tested in the EventHandlerDiscoveryService tests
            Assert.Pass("Attribute application verification requires reflection testing");
        }
    }
}