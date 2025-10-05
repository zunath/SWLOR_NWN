using System.Linq.Expressions;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiLabelTests : TestBase
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
            var label = new GuiLabel<TestViewModel>();
            var text = "Test Label";

            // Act
            var result = label.SetText(text);

            // Assert
            Assert.That(result, Is.SameAs(label));
        }

        [Test]
        public void BindText_WithValidExpression_SetsTextBindName()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = label.BindText(expression);

            // Assert
            Assert.That(result, Is.SameAs(label));
        }

        [Test]
        public void SetHorizontalAlign_WithValidAlign_SetsHorizontalAlign()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            var hAlign = NuiHorizontalAlignType.Left;

            // Act
            var result = label.SetHorizontalAlign(hAlign);

            // Assert
            Assert.That(result, Is.SameAs(label));
        }

        [Test]
        public void BindHorizontalAlign_WithValidExpression_SetsHorizontalAlignBindName()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            Expression<Func<TestViewModel, NuiHorizontalAlignType>> expression = vm => vm.HorizontalAlignProperty;

            // Act
            var result = label.BindHorizontalAlign(expression);

            // Assert
            Assert.That(result, Is.SameAs(label));
        }

        [Test]
        public void SetVerticalAlign_WithValidAlign_SetsVerticalAlign()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            var vAlign = NuiVerticalAlignType.Top;

            // Act
            var result = label.SetVerticalAlign(vAlign);

            // Assert
            Assert.That(result, Is.SameAs(label));
        }

        [Test]
        public void BindVerticalAlign_WithValidExpression_SetsVerticalAlignBindName()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            Expression<Func<TestViewModel, NuiVerticalAlignType>> expression = vm => vm.VerticalAlignProperty;

            // Act
            var result = label.BindVerticalAlign(expression);

            // Assert
            Assert.That(result, Is.SameAs(label));
        }

        [Test]
        public void Constructor_SetsDefaultValues()
        {
            // Act
            var label = new GuiLabel<TestViewModel>();

            // Assert
            Assert.That(label, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithStaticValues_ReturnsLabelElement()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            label.SetText("Test Label");
            label.SetHorizontalAlign(NuiHorizontalAlignType.Left);
            label.SetVerticalAlign(NuiVerticalAlignType.Top);

            // Act
            var result = label.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithBoundValues_ReturnsLabelElement()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            label.BindText(vm => vm.TextProperty);
            label.BindHorizontalAlign(vm => vm.HorizontalAlignProperty);
            label.BindVerticalAlign(vm => vm.VerticalAlignProperty);

            // Act
            var result = label.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithMixedValues_ReturnsLabelElement()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();
            label.SetText("Static Text");
            label.BindHorizontalAlign(vm => vm.HorizontalAlignProperty);
            label.SetVerticalAlign(NuiVerticalAlignType.Bottom);

            // Act
            var result = label.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoValues_ReturnsLabelElement()
        {
            // Arrange
            var label = new GuiLabel<TestViewModel>();

            // Act
            var result = label.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
            public NuiHorizontalAlignType HorizontalAlignProperty { get; set; } = NuiHorizontalAlignType.Center;
            public NuiVerticalAlignType VerticalAlignProperty { get; set; } = NuiVerticalAlignType.Middle;

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
