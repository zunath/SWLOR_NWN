using SWLOR.Shared.UI.Model;

namespace SWLOR.Test.Shared.UI.Model
{
    [TestFixture]
    public class SimpleGuiMethodDetailTests
    {
        [Test]
        public void Constructor_WithMethodAndArguments_SetsProperties()
        {
            // Arrange
            var methodInfo = typeof(string).GetMethod("ToString", new Type[0]);
            var arguments = new List<KeyValuePair<Type, object>>();

            // Act
            var methodDetail = new GuiMethodDetail(methodInfo, arguments);

            // Assert
            Assert.That(methodDetail.Method, Is.EqualTo(methodInfo));
            Assert.That(methodDetail.Arguments, Is.EqualTo(arguments));
        }

        [Test]
        public void SetMethod_WithValidMethod_SetsMethod()
        {
            // Arrange
            var methodDetail = new GuiMethodDetail(null, new List<KeyValuePair<Type, object>>());
            var methodInfo = typeof(string).GetMethod("ToString", new Type[0]);

            // Act
            methodDetail.Method = methodInfo;

            // Assert
            Assert.That(methodDetail.Method, Is.EqualTo(methodInfo));
        }

        [Test]
        public void SetArguments_WithValidArguments_SetsArguments()
        {
            // Arrange
            var methodDetail = new GuiMethodDetail(null, new List<KeyValuePair<Type, object>>());
            var arguments = new List<KeyValuePair<Type, object>>();

            // Act
            methodDetail.Arguments = arguments;

            // Assert
            Assert.That(methodDetail.Arguments, Is.EqualTo(arguments));
        }
    }
}
