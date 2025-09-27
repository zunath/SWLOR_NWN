using SWLOR.Shared.Abstractions;

namespace SWLOR.Test.Shared.Abstractions
{
    [TestFixture]
    public class IndexedAttributeTests
    {
        [Test]
        public void IndexedAttribute_ShouldBeAttribute()
        {
            // Arrange & Act
            var attribute = new IndexedAttribute();

            // Assert
            Assert.That(attribute, Is.InstanceOf<Attribute>());
        }

        [Test]
        public void IndexedAttribute_ShouldBeUsableAsAttribute()
        {
            // Arrange
            var testClass = new TestClassWithIndexedProperty();

            // Act
            var properties = typeof(TestClassWithIndexedProperty).GetProperties();
            var indexedProperty = properties.FirstOrDefault(p => p.Name == nameof(TestClassWithIndexedProperty.IndexedProperty));

            // Assert
            Assert.That(indexedProperty, Is.Not.Null);
            Assert.That(indexedProperty.GetCustomAttributes(typeof(IndexedAttribute), false), Is.Not.Empty);
        }

        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Arrange & Act
            var attribute = new IndexedAttribute();

            // Assert
            Assert.That(attribute, Is.Not.Null);
        }

        [Test]
        public void Attribute_ShouldInheritFromAttribute()
        {
            // Arrange & Act
            var attribute = new IndexedAttribute();

            // Assert
            Assert.That(attribute, Is.InstanceOf<Attribute>());
        }


        [Test]
        public void Attribute_ShouldHavePublicConstructor()
        {
            // Arrange & Act
            var attributeType = typeof(IndexedAttribute);
            var constructors = attributeType.GetConstructors();

            // Assert
            Assert.That(constructors, Has.Length.EqualTo(1));
            Assert.That(constructors[0].IsPublic, Is.True);
        }

        // Test class for attribute testing
        private class TestClassWithIndexedProperty
        {
            [Indexed]
            public string? IndexedProperty { get; set; }

            public string? NonIndexedProperty { get; set; }
        }
    }
}
