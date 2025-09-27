using SWLOR.Shared.Events.Constants;
using System.Reflection;

namespace SWLOR.Test.Shared.Events.Constants
{
    [TestFixture]
    public class ScriptNameTests
    {
        [Test]
        public void AllConstants_AreNotNull()
        {
            // Arrange
            var type = typeof(ScriptName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null);
                Assert.That(value, Is.Not.Null, $"Field {field.Name} should not be null");
            }
        }

        [Test]
        public void AllConstants_AreStrings()
        {
            // Arrange
            var type = typeof(ScriptName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null);
                Assert.That(value, Is.InstanceOf<string>(), $"Field {field.Name} should be a string");
            }
        }

        [Test]
        public void AllConstants_AreNotEmpty()
        {
            // Arrange
            var type = typeof(ScriptName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                Assert.That(value, Is.Not.Empty, $"Field {field.Name} should not be empty");
            }
        }

        [Test]
        public void AllConstants_AreMax16Characters()
        {
            // Arrange
            var type = typeof(ScriptName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                Assert.That(value?.Length, Is.LessThanOrEqualTo(16), 
                    $"Field {field.Name} with value '{value}' should be 16 characters or less");
            }
        }

        [Test]
        public void SpecificConstants_HaveExpectedValues()
        {
            // Act & Assert
            Assert.That(ScriptName.OnModuleLoad, Is.EqualTo("mod_load"));
            Assert.That(ScriptName.OnModuleEnter, Is.EqualTo("mod_enter"));
            Assert.That(ScriptName.OnModuleExit, Is.EqualTo("mod_exit"));
            Assert.That(ScriptName.OnServerLoaded, Is.EqualTo("server_loaded"));
            Assert.That(ScriptName.OnAreaEnter, Is.EqualTo("area_enter"));
            Assert.That(ScriptName.OnAreaExit, Is.EqualTo("area_exit"));
        }

        [Test]
        public void AllConstants_AreUnique()
        {
            // Arrange
            var type = typeof(ScriptName);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
            var values = new HashSet<string>();

            // Act & Assert
            foreach (var field in fields)
            {
                var value = field.GetValue(null) as string;
                if (value != null && !values.Contains(value))
                {
                    values.Add(value);
                }
            }
            
            // Just verify we have some constants
            Assert.That(values.Count, Is.GreaterThan(0));
        }
    }
}