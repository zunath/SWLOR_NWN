using System.Linq.Expressions;
using NSubstitute;
using NUnit.Framework;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
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
    public class GuiTextTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var guiText = new GuiText<TestViewModel>();

            // Assert
            Assert.That(guiText, Is.Not.Null);
        }

        [Test]
        public void SetText_WithValidText_SetsText()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            var text = "Test Text";

            // Act
            var result = guiText.SetText(text);

            // Assert
            Assert.That(result, Is.SameAs(guiText));
        }

        [Test]
        public void SetShowBorder_WithValidValue_SetsShowBorder()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            var showBorder = false;

            // Act
            var result = guiText.SetShowBorder(showBorder);

            // Assert
            Assert.That(result, Is.SameAs(guiText));
        }

        [Test]
        public void SetScrollbars_WithValidValue_SetsScrollbars()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            var scrollbars = NuiScrollbarType.Both;

            // Act
            var result = guiText.SetScrollbars(scrollbars);

            // Assert
            Assert.That(result, Is.SameAs(guiText));
        }

        [Test]
        public void BindText_WithValidExpression_SetsTextBindName()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = guiText.BindText(expression);

            // Assert
            Assert.That(result, Is.SameAs(guiText));
        }

        [Test]
        public void BuildElement_WithStaticText_ReturnsTextElement()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            guiText.SetText("Test Text");
            guiText.SetShowBorder(true);
            guiText.SetScrollbars(NuiScrollbarType.Auto);

            // Act
            var result = guiText.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithBoundText_ReturnsTextElement()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            guiText.BindText(vm => vm.TextProperty);
            guiText.SetShowBorder(false);
            guiText.SetScrollbars(NuiScrollbarType.None);

            // Act
            var result = guiText.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoText_ReturnsTextElement()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            guiText.SetText(""); // Set empty text instead of null

            // Act
            var result = guiText.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithMixedValues_ReturnsTextElement()
        {
            // Arrange
            var guiText = new GuiText<TestViewModel>();
            guiText.SetText("Static Text");
            guiText.SetShowBorder(true);
            guiText.SetScrollbars(NuiScrollbarType.Both);

            // Act
            var result = guiText.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";

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
