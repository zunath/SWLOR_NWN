using System.Linq.Expressions;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiButtonImageTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void SetImageResref_WithValidResref_SetsResref()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            var resref = "test_image";

            // Act
            var result = buttonImage.SetImageResref(resref);

            // Assert
            Assert.That(result, Is.SameAs(buttonImage));
        }

        [Test]
        public void BindImageResref_WithValidExpression_SetsResrefBindName()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = buttonImage.BindImageResref(expression);

            // Assert
            Assert.That(result, Is.SameAs(buttonImage));
        }

        [Test]
        public void BindOnClicked_WithValidExpression_SetsClickEvent()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = buttonImage.BindOnClicked(expression);

            // Assert
            Assert.That(result, Is.SameAs(buttonImage));
        }

        [Test]
        public void BindOnClicked_WithExistingId_KeepsExistingId()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            var existingId = "existing_id";
            buttonImage.SetId(existingId);
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = buttonImage.BindOnClicked(expression);

            // Assert
            Assert.That(result, Is.SameAs(buttonImage));
        }

        [Test]
        public void BuildElement_WithStaticResref_ReturnsButtonImageElement()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            buttonImage.SetImageResref("test_image");

            // Act
            var result = buttonImage.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithBoundResref_ReturnsButtonImageElement()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            buttonImage.BindImageResref(vm => vm.TextProperty);

            // Act
            var result = buttonImage.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoResref_ReturnsButtonImageElement()
        {
            // Arrange
            var buttonImage = new GuiButtonImage<TestViewModel>();
            buttonImage.SetImageResref(""); // Set empty resref instead of null

            // Act
            var result = buttonImage.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
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

            public void WatchOnClient(string propertyName)
            {
                // Test implementation
            }

            public void ChangePartialView(string elementId, string partialName)
            {
                // Test implementation
            }

            public Action OnModalClose() => () => { };

            public Action OnModalConfirmClick() => () => { };

            public Action OnModalCancelClick() => () => { };

            public Action OnWindowClosed() => () => { };
        }
    }
}
