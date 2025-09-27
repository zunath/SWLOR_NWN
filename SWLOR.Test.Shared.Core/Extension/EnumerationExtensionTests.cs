using System.ComponentModel;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Test.Shared.Core.Extension
{
    [TestFixture]
    public class EnumerationExtensionTests
    {
        public enum TestEnum
        {
            [System.ComponentModel.Description("First Value")]
            First,
            [System.ComponentModel.Description("Second Value")]
            Second,
            Third
        }

        public enum TestEnumWithMultipleAttributes
        {
            [System.ComponentModel.Description("First Description")]
            First,
            Second
        }

        [Test]
        public void GetDescriptionAttribute_WithDescriptionAttribute_ShouldReturnDescription()
        {
            // Arrange
            var enumValue = TestEnum.First;

            // Act
            var result = enumValue.GetDescriptionAttribute();

            // Assert
            Assert.That(result, Is.EqualTo("First Value"));
        }

        [Test]
        public void GetDescriptionAttribute_WithDescriptionAttribute_ShouldReturnSecondDescription()
        {
            // Arrange
            var enumValue = TestEnum.Second;

            // Act
            var result = enumValue.GetDescriptionAttribute();

            // Assert
            Assert.That(result, Is.EqualTo("Second Value"));
        }

        [Test]
        public void GetDescriptionAttribute_WithoutDescriptionAttribute_ShouldReturnEnumName()
        {
            // Arrange
            var enumValue = TestEnum.Third;

            // Act
            var result = enumValue.GetDescriptionAttribute();

            // Assert
            Assert.That(result, Is.EqualTo("Third"));
        }

        [Test]
        public void GetAttribute_WithValidAttribute_ShouldReturnAttribute()
        {
            // Arrange
            var enumValue = TestEnum.First;

            // Act
            var result = enumValue.GetAttribute<TestEnum, System.ComponentModel.DescriptionAttribute>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Description, Is.EqualTo("First Value"));
        }

        [Test]
        public void GetAttribute_WithNonExistentAttribute_ShouldThrowException()
        {
            // Arrange
            var enumValue = TestEnum.Third;

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => 
                enumValue.GetAttribute<TestEnum, System.ComponentModel.DescriptionAttribute>());
            
            Assert.That(exception.Message, Does.Contain("Could not find attribute"));
        }

        [Test]
        public void GetAttributes_WithMultipleAttributes_ShouldReturnAllAttributes()
        {
            // Arrange
            var enumValue = TestEnumWithMultipleAttributes.First;

            // Act
            var result = enumValue.GetAttributes<TestEnumWithMultipleAttributes, System.ComponentModel.DescriptionAttribute>();

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(1));
            Assert.That(resultList[0].Description, Is.EqualTo("First Description"));
        }

        [Test]
        public void GetAttributes_WithNoAttributes_ShouldReturnEmptyArray()
        {
            // Arrange
            var enumValue = TestEnumWithMultipleAttributes.Second;

            // Act
            var result = enumValue.GetAttributes<TestEnumWithMultipleAttributes, System.ComponentModel.DescriptionAttribute>();

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetAttributes_WithMultipleAttributes_ShouldReturnMultipleAttributes()
        {
            // Arrange
            var enumValue = TestEnumWithMultipleAttributes.First;

            // Act
            var descriptionAttributes = enumValue.GetAttributes<TestEnumWithMultipleAttributes, System.ComponentModel.DescriptionAttribute>();

            // Assert
            var descriptionList = descriptionAttributes.ToList();
            Assert.That(descriptionList.Count, Is.EqualTo(1));
            Assert.That(descriptionList[0].Description, Is.EqualTo("First Description"));
        }
    }
}
