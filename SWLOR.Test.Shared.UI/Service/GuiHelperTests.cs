using System.Linq.Expressions;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.Shared.UI.Service
{
    [TestFixture]
    public class GuiHelperTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void GetPropertyName_WithValidPropertyExpression_ReturnsPropertyName()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestProperty;

            // Act
            var result = GuiHelper<TestViewModel>.GetPropertyName(expression);

            // Assert
            Assert.That(result, Is.EqualTo("TestProperty"));
        }

        [Test]
        public void GetPropertyName_WithNestedPropertyExpression_ReturnsPropertyName()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, int>> expression = vm => vm.NestedProperty.Value;

            // Act
            var result = GuiHelper<TestViewModel>.GetPropertyName(expression);

            // Assert
            Assert.That(result, Is.EqualTo("Value"));
        }

        [Test]
        public void GetPropertyName_WithMethodExpression_ThrowsArgumentException()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => GuiHelper<TestViewModel>.GetPropertyName(expression));
        }

        [Test]
        public void GetPropertyName_WithFieldExpression_ThrowsArgumentException()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestField;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => GuiHelper<TestViewModel>.GetPropertyName(expression));
        }

        [Test]
        public void GetMethodInfo_WithValidMethodExpression_ReturnsMethodDetail()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = GuiHelper<TestViewModel>.GetMethodInfo(expression);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Method, Is.Not.Null);
            Assert.That(result.Method.Name, Is.EqualTo("TestMethodReturningString"));
            Assert.That(result.Arguments, Is.Not.Null);
        }

        [Test]
        public void GetMethodInfo_WithMethodWithParameters_ReturnsMethodDetailWithArguments()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, Action>> expression = vm => vm.TestMethodWithParameters("test", 42);

            // Act
            var result = GuiHelper<TestViewModel>.GetMethodInfo(expression);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Method, Is.Not.Null);
            Assert.That(result.Method.Name, Is.EqualTo("TestMethodWithParameters"));
            Assert.That(result.Arguments, Is.Not.Null);
            Assert.That(result.Arguments.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetMethodInfo_WithPropertyExpression_ThrowsInvalidCastException()
        {
            // Arrange
            var testViewModel = new TestViewModel();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestProperty;

            // Act & Assert
            Assert.Throws<InvalidCastException>(() => GuiHelper<TestViewModel>.GetMethodInfo(expression));
        }

        // Test helper classes
        public class TestViewModel : IGuiViewModel
        {
            public string TestProperty { get; set; } = "Test Value";
            public string TestField = "Test Field";
            public NestedProperty NestedProperty { get; set; } = new NestedProperty();

            public Action TestMethod => () => { };
            public Action TestMethodWithParameters(string text, int number) => () => { };
            public string TestMethodReturningString() => "test";

            public uint TetherObject { get; set; }
            public GuiRectangle Geometry { get; set; }
            public string ModalPromptText { get; set; }
            public string ModalConfirmButtonText { get; set; }
            public string ModalCancelButtonText { get; set; }

            public void Bind(uint player, int windowToken, GuiRectangle initialGeometry, GuiWindowType type, IGuiPayload payload, uint tetherObject)
            {
                TetherObject = tetherObject;
                Geometry = initialGeometry;
            }

            public void UpdatePropertyFromClient(string propertyName)
            {
                // Test implementation
            }

            public void ChangePartialView(string elementId, string partialName)
            {
                // Test implementation
            }

            public Action OnModalClose()
            {
                return () => { };
            }

            public Action OnModalConfirmClick()
            {
                return () => { };
            }

            public Action OnModalCancelClick()
            {
                return () => { };
            }

            public Action OnWindowClosed()
            {
                return () => { };
            }
        }

        public class NestedProperty
        {
            public int Value { get; set; } = 42;
        }
    }
}
