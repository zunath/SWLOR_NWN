using SWLOR.Shared.UI.Model;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiMethodDetailTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_WithValidParameters_SetsProperties()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod("TestMethod");
            var arguments = new List<KeyValuePair<Type, object>>
            {
                new KeyValuePair<Type, object>(typeof(string), "test")
            };

            // Act
            var methodDetail = new GuiMethodDetail(method, arguments);

            // Assert
            Assert.That(methodDetail.Method, Is.EqualTo(method));
            Assert.That(methodDetail.Arguments, Is.EqualTo(arguments));
        }

        [Test]
        public void Constructor_WithNullMethod_SetsMethodToNull()
        {
            // Arrange
            var arguments = new List<KeyValuePair<Type, object>>();

            // Act
            var methodDetail = new GuiMethodDetail(null, arguments);

            // Assert
            Assert.That(methodDetail.Method, Is.Null);
            Assert.That(methodDetail.Arguments, Is.EqualTo(arguments));
        }

        [Test]
        public void Constructor_WithNullArguments_SetsArgumentsToNull()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod("TestMethod");

            // Act
            var methodDetail = new GuiMethodDetail(method, null);

            // Assert
            Assert.That(methodDetail.Method, Is.EqualTo(method));
            Assert.That(methodDetail.Arguments, Is.Null);
        }

        [Test]
        public void Constructor_WithEmptyArguments_SetsArguments()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod("TestMethod");
            var arguments = new List<KeyValuePair<Type, object>>();

            // Act
            var methodDetail = new GuiMethodDetail(method, arguments);

            // Assert
            Assert.That(methodDetail.Method, Is.EqualTo(method));
            Assert.That(methodDetail.Arguments, Is.EqualTo(arguments));
            Assert.That(methodDetail.Arguments.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithMultipleArguments_SetsAllArguments()
        {
            // Arrange
            var method = typeof(TestClass).GetMethod("TestMethod");
            var arguments = new List<KeyValuePair<Type, object>>
            {
                new KeyValuePair<Type, object>(typeof(string), "test1"),
                new KeyValuePair<Type, object>(typeof(int), 42),
                new KeyValuePair<Type, object>(typeof(bool), true)
            };

            // Act
            var methodDetail = new GuiMethodDetail(method, arguments);

            // Assert
            Assert.That(methodDetail.Method, Is.EqualTo(method));
            Assert.That(methodDetail.Arguments, Is.EqualTo(arguments));
            Assert.That(methodDetail.Arguments.Count, Is.EqualTo(3));
        }

        public class TestClass
        {
            public void TestMethod() { }
        }
    }
}