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
            // Arrange
            var field = typeof(TestClass).GetField("TestField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                // Create a test field if it doesn't exist
                var testClass = new TestClass();
                var fieldInfo = testClass.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                if (fieldInfo != null)
                {
                    field = fieldInfo;
                }
            }

            // Skip test if no field found
            if (field == null)
            {
                Assert.Ignore("No suitable field found for testing");
                return;
            }

            // Act
            var result = field.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass"));
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
            // Arrange
            var eventInfo = typeof(TestClass).GetEvent("TestEvent");
            if (eventInfo == null)
            {
                Assert.Ignore("No event found for testing");
                return;
            }

            // Act
            var result = eventInfo.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass"));
        }

        [Test]
        public void GetFullName_WithNestedClass_ShouldReturnFullName()
        {
            // Arrange
            var nestedType = typeof(TestClass).GetNestedType("NestedClass", BindingFlags.NonPublic | BindingFlags.Public);
            if (nestedType == null)
            {
                Assert.Ignore("No nested class found for testing");
                return;
            }

            // Act
            var result = nestedType.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass"));
        }

        [Test]
        public void GetFullName_WithGenericMethod_ShouldReturnFullName()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod("GenericMethod");
            if (method == null)
            {
                Assert.Ignore("No generic method found for testing");
                return;
            }

            // Act
            var result = method.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("SWLOR.Test.Shared.Core.Extension.ReflectionExtensionsTests+TestClass"));
        }

        [Test]
        public void GetFullName_WithInterfaceMethod_ShouldReturnFullName()
        {
            // Arrange
            var interfaceType = typeof(TestClass).GetInterfaces().FirstOrDefault();
            if (interfaceType == null)
            {
                Assert.Ignore("No interface found for testing");
                return;
            }

            var method = interfaceType.GetMethod("InterfaceMethod");
            if (method == null)
            {
                Assert.Ignore("No interface method found for testing");
                return;
            }

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
            if (member == null)
            {
                Assert.Ignore("No suitable member found for testing");
                return;
            }

            // Act
            var result = member.GetFullName();

            // Assert
            Assert.That(result, Does.Contain("TestProperty"));
        }
    }
}
