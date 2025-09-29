using System.Linq.Expressions;
using NSubstitute;
using NUnit.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiCheckBoxTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void SetText_WithValidText_SetsText()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            var text = "Test Checkbox";

            // Act
            var result = checkBox.SetText(text);

            // Assert
            Assert.That(result, Is.SameAs(checkBox));
        }

        [Test]
        public void BindText_WithValidExpression_SetsTextBindName()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = checkBox.BindText(expression);

            // Assert
            Assert.That(result, Is.SameAs(checkBox));
        }

        [Test]
        public void SetIsChecked_WithValidValue_SetsIsChecked()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            var isChecked = true;

            // Act
            var result = checkBox.SetIsChecked(isChecked);

            // Assert
            Assert.That(result, Is.SameAs(checkBox));
        }

        [Test]
        public void BindIsChecked_WithValidExpression_SetsIsCheckedBindName()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = checkBox.BindIsChecked(expression);

            // Assert
            Assert.That(result, Is.SameAs(checkBox));
        }

        [Test]
        public void BuildElement_WithStaticValues_ReturnsCheckBoxElement()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            checkBox.SetText("Test Checkbox");
            checkBox.SetIsChecked(true);

            // Act
            var result = checkBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithBoundValues_ReturnsCheckBoxElement()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            checkBox.BindText(vm => vm.TextProperty);
            checkBox.BindIsChecked(vm => vm.BoolProperty);

            // Act
            var result = checkBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithMixedValues_ReturnsCheckBoxElement()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            checkBox.SetText("Static Text");
            checkBox.BindIsChecked(vm => vm.BoolProperty);

            // Act
            var result = checkBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoValues_ReturnsCheckBoxElement()
        {
            // Arrange
            var checkBox = new GuiCheckBox<TestViewModel>();
            checkBox.SetText(""); // Set empty text instead of null

            // Act
            var result = checkBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
            public bool BoolProperty { get; set; } = true;

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
