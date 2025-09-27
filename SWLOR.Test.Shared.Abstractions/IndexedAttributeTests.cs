using NUnit.Framework;
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

        // Test class for attribute testing
        private class TestClassWithIndexedProperty
        {
            [Indexed]
            public string IndexedProperty { get; set; }

            public string NonIndexedProperty { get; set; }
        }
    }
}
