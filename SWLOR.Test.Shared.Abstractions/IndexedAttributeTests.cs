using SWLOR.Shared.Abstractions;

namespace SWLOR.Test.Shared.Abstractions
{
    [TestFixture]
    public class IndexedAttributeTests
    {
        [Test]
        public void Constructor_ShouldCreateInstance()
        {
            // Act
            var attribute = new IndexedAttribute();

            // Assert
            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute, Is.InstanceOf<Attribute>());
        }

        [Test]
        public void Attribute_ShouldBeUsableOnProperties()
        {
            // Arrange
            var testClass = new TestClassWithIndexedProperty();

            // Act
            var property = typeof(TestClassWithIndexedProperty).GetProperty(nameof(TestClassWithIndexedProperty.IndexedProperty));
            var attributes = property?.GetCustomAttributes(typeof(IndexedAttribute), false);

            // Assert
            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.Length, Is.EqualTo(1));
            Assert.That(attributes[0], Is.InstanceOf<IndexedAttribute>());
        }

        [Test]
        public void Attribute_ShouldBeUsableOnMultipleProperties()
        {
            // Arrange
            var testClass = new TestClassWithMultipleIndexedProperties();

            // Act
            var properties = typeof(TestClassWithMultipleIndexedProperties).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(2));
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithMultipleIndexedProperties.Property1)), Is.True);
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithMultipleIndexedProperties.Property2)), Is.True);
        }

        [Test]
        public void Attribute_ShouldNotBeOnNonIndexedProperties()
        {
            // Arrange
            var testClass = new TestClassWithMixedProperties();

            // Act
            var properties = typeof(TestClassWithMixedProperties).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();
            var nonIndexedProperties = properties.Where(p => !p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(1));
            Assert.That(nonIndexedProperties.Length, Is.EqualTo(1));
            Assert.That(indexedProperties[0].Name, Is.EqualTo(nameof(TestClassWithMixedProperties.IndexedProperty)));
            Assert.That(nonIndexedProperties[0].Name, Is.EqualTo(nameof(TestClassWithMixedProperties.NonIndexedProperty)));
        }

        [Test]
        public void Attribute_ShouldBeInheritable()
        {
            // Arrange
            var testClass = new TestClassWithInheritedAttribute();

            // Act
            var property = typeof(TestClassWithInheritedAttribute).GetProperty(nameof(TestClassWithInheritedAttribute.IndexedProperty));
            var attributes = property?.GetCustomAttributes(typeof(IndexedAttribute), true);

            // Assert
            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.Length, Is.EqualTo(1));
            Assert.That(attributes[0], Is.InstanceOf<IndexedAttribute>());
        }

        [Test]
        public void Attribute_ShouldAllowMultipleInstances()
        {
            // Arrange
            var testClass = new TestClassWithMultipleAttributes();

            // Act
            var property = typeof(TestClassWithMultipleAttributes).GetProperty(nameof(TestClassWithMultipleAttributes.MultipleAttributesProperty));
            var attributes = property?.GetCustomAttributes(typeof(IndexedAttribute), false);

            // Assert
            Assert.That(attributes, Is.Not.Null);
            Assert.That(attributes.Length, Is.EqualTo(1));
            Assert.That(attributes.All(a => a is IndexedAttribute), Is.True);
        }

        [Test]
        public void Attribute_ShouldWorkWithDifferentPropertyTypes()
        {
            // Arrange
            var testClass = new TestClassWithDifferentPropertyTypes();

            // Act
            var properties = typeof(TestClassWithDifferentPropertyTypes).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(4));
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithDifferentPropertyTypes.StringProperty)), Is.True);
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithDifferentPropertyTypes.IntProperty)), Is.True);
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithDifferentPropertyTypes.BoolProperty)), Is.True);
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithDifferentPropertyTypes.DateTimeProperty)), Is.True);
        }

        [Test]
        public void Attribute_ShouldWorkWithNullableTypes()
        {
            // Arrange
            var testClass = new TestClassWithNullableTypes();

            // Act
            var properties = typeof(TestClassWithNullableTypes).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(2));
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithNullableTypes.NullableIntProperty)), Is.True);
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithNullableTypes.NullableStringProperty)), Is.True);
        }

        [Test]
        public void Attribute_ShouldWorkWithGenericTypes()
        {
            // Arrange
            var testClass = new TestClassWithGenericTypes();

            // Act
            var properties = typeof(TestClassWithGenericTypes).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(2));
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithGenericTypes.ListProperty)), Is.True);
            Assert.That(indexedProperties.Any(p => p.Name == nameof(TestClassWithGenericTypes.DictionaryProperty)), Is.True);
        }

        [Test]
        public void Attribute_ShouldWorkWithStaticProperties()
        {
            // Arrange
            var testClass = new TestClassWithStaticProperties();

            // Act
            var properties = typeof(TestClassWithStaticProperties).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(1));
            Assert.That(indexedProperties[0].Name, Is.EqualTo(nameof(TestClassWithStaticProperties.StaticIndexedProperty)));
        }

        [Test]
        public void Attribute_ShouldWorkWithVirtualProperties()
        {
            // Arrange
            var testClass = new TestClassWithVirtualProperties();

            // Act
            var properties = typeof(TestClassWithVirtualProperties).GetProperties();
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(1));
            Assert.That(indexedProperties[0].Name, Is.EqualTo(nameof(TestClassWithVirtualProperties.VirtualIndexedProperty)));
        }

        [Test]
        public void Attribute_ShouldWorkWithAbstractProperties()
        {
            // Arrange
            var concreteClass = new TestClassWithConcreteAbstractProperties();

            // Act
            var properties = typeof(TestClassWithAbstractProperties).GetProperties(); // Look at the abstract class
            var indexedProperties = properties.Where(p => p.GetCustomAttributes(typeof(IndexedAttribute), false).Any()).ToArray();

            // Assert
            Assert.That(indexedProperties.Length, Is.EqualTo(1));
            Assert.That(indexedProperties[0].Name, Is.EqualTo(nameof(TestClassWithAbstractProperties.AbstractIndexedProperty)));
        }

        // Test helper classes
        private class TestClassWithIndexedProperty
        {
            [Indexed]
            public string IndexedProperty { get; set; }
        }

        private class TestClassWithMultipleIndexedProperties
        {
            [Indexed]
            public string Property1 { get; set; }

            [Indexed]
            public int Property2 { get; set; }
        }

        private class TestClassWithMixedProperties
        {
            [Indexed]
            public string IndexedProperty { get; set; }

            public string NonIndexedProperty { get; set; }
        }

        private class TestClassWithInheritedAttribute
        {
            [Indexed]
            public virtual string IndexedProperty { get; set; }
        }

        private class TestClassWithMultipleAttributes
        {
            [Indexed]
            public string MultipleAttributesProperty { get; set; }
        }

        private class TestClassWithDifferentPropertyTypes
        {
            [Indexed]
            public string StringProperty { get; set; }

            [Indexed]
            public int IntProperty { get; set; }

            [Indexed]
            public bool BoolProperty { get; set; }

            [Indexed]
            public DateTime DateTimeProperty { get; set; }
        }

        private class TestClassWithNullableTypes
        {
            [Indexed]
            public int? NullableIntProperty { get; set; }

            [Indexed]
            public string? NullableStringProperty { get; set; }
        }

        private class TestClassWithGenericTypes
        {
            [Indexed]
            public List<string> ListProperty { get; set; }

            [Indexed]
            public Dictionary<string, object> DictionaryProperty { get; set; }
        }

        private class TestClassWithStaticProperties
        {
            [Indexed]
            public static string StaticIndexedProperty { get; set; }
        }

        private class TestClassWithVirtualProperties
        {
            [Indexed]
            public virtual string VirtualIndexedProperty { get; set; }
        }

        private abstract class TestClassWithAbstractProperties
        {
            [Indexed]
            public abstract string AbstractIndexedProperty { get; set; }
        }

        private class TestClassWithConcreteAbstractProperties : TestClassWithAbstractProperties
        {
            public override string AbstractIndexedProperty { get; set; }
        }
    }
}