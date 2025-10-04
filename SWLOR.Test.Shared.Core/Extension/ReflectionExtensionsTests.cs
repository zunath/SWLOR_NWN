using System.Reflection;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Test.Shared.Core.Extension
{
    [TestFixture]
    public class ReflectionExtensionsTests
    {
        public class TestClass
        {
            public string? TestProperty { get; set; }
            public void TestMethod() { }
            public static void StaticMethod() { }
        }

        public class DerivedTestClass : TestClass
        {
            public string? DerivedProperty { get; set; }
        }

        public class TestClassWithField
        {
            private string TestField = "test";
        }

        public class TestClassWithEvent
        {
            public event EventHandler TestEvent;
        }

        public class TestClassWithNested
        {
            public class NestedClass
            {
                public string NestedProperty { get; set; }
            }
        }

        public class TestClassWithGenericMethod
        {
            public void GenericMethod<T>(T value) { }
        }

        public interface ITestInterface
        {
            void InterfaceMethod();
        }

        [Test]
        public void GetFullName_WithProperty_ShouldReturnFullName()
        {
            // Arrange
            var property = typeof(TestClass).GetProperty(nameof(TestClass.TestProperty));

            // Act
            var result = property.GetFullName();

            // Assert
            Assert.That(result, Is.EqualTo("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass.TestProperty"));
        }

        [Test]
        public void GetFullName_WithMethod_ShouldReturnFullName()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod));

            // Act
            var result = method.GetFullName();

            // Assert
            Assert.That(result, Is.EqualTo("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass.TestMethod"));
        }

        [Test]
        public void GetFullName_WithStaticMethod_ShouldReturnFullName()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod(nameof(TestClass.StaticMethod));

            // Act
            var result = method.GetFullName();

            // Assert
            Assert.That(result, Is.EqualTo("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass.StaticMethod"));
        }

        [Test]
        public void GetFullName_WithDerivedClassProperty_ShouldReturnFullName()
        {
            // Arrange
            var property = typeof(DerivedTestClass).GetProperty(nameof(DerivedTestClass.DerivedProperty));

            // Act
            var result = property.GetFullName();

            // Assert
            Assert.That(result, Is.EqualTo("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+DerivedTestClass.DerivedProperty"));
        }

        [Test]
        public void GetFullName_WithInheritedProperty_ShouldReturnFullName()
        {
            // Arrange
            var property = typeof(DerivedTestClass).GetProperty(nameof(TestClass.TestProperty));

            // Act
            var result = property.GetFullName();

            // Assert
            Assert.That(result, Is.EqualTo("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass.TestProperty"));
        }

        [Test]
        public void GetFullName_WithField_ShouldReturnFullName()
        {
            // Arrange - Create a test class with a field
            var testClass = new TestClassWithField();
            var field = typeof(TestClassWithField).GetField("TestField", BindingFlags.NonPublic | BindingFlags.Instance);
            
            // Act
            var result = field.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClassWithField"));
        }

        [Test]
        public void GetFullName_WithConstructor_ShouldReturnFullName()
        {
            // Arrange
            var constructor = typeof(TestClass).GetConstructor(Type.EmptyTypes);

            // Act
            var result = constructor.GetFullName();

            // Assert
            Assert.That(result, Is.EqualTo("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass..ctor"));
        }

        [Test]
        public void GetFullName_WithEvent_ShouldReturnFullName()
        {
            // Arrange - Create a test class with an event
            var eventInfo = typeof(TestClassWithEvent).GetEvent("TestEvent");

            // Act
            var result = eventInfo.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClassWithEvent"));
        }

        [Test]
        public void GetFullName_WithNestedClass_ShouldReturnFullName()
        {
            // Arrange - Create a test class with a nested class
            var nestedType = typeof(TestClassWithNested).GetNestedType("NestedClass", BindingFlags.NonPublic | BindingFlags.Public);

            // Act
            var result = nestedType.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClassWithNested"));
        }

        [Test]
        public void GetFullName_WithGenericMethod_ShouldReturnFullName()
        {
            // Arrange - Create a test class with a generic method
            var method = typeof(TestClassWithGenericMethod).GetMethod("GenericMethod");

            // Act
            var result = method.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClassWithGenericMethod"));
        }

        [Test]
        public void GetFullName_WithInterfaceMethod_ShouldReturnFullName()
        {
            // Arrange - Create a test class that implements an interface
            var interfaceType = typeof(ITestInterface);
            var method = interfaceType.GetMethod("InterfaceMethod");

            // Act
            var result = method.GetFullName();

            // Assert
            Assert.That(result, Does.Contain(interfaceType.FullName));
        }

        [Test]
        public void GetFullName_WithNullDeclaringType_ShouldReturnMemberName()
        {
            // Arrange
            var member = typeof(TestClass).GetMember("TestProperty").FirstOrDefault();

            // Act
            var result = member.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("TestProperty"));
        }
    }
}
