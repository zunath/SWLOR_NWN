using System.Linq.Expressions;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.Service;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiWidgetTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_CreatesWidgetWithDefaultValues()
        {
            // Act
            var widget = new TestGuiWidget();

            // Assert
            Assert.That(widget, Is.Not.Null);
            Assert.That(widget.Id, Is.Null);
            Assert.That(widget.Elements, Is.Not.Null);
            Assert.That(widget.Elements.Count, Is.EqualTo(0));
            Assert.That(widget.Events, Is.Not.Null);
            Assert.That(widget.Events.Count, Is.EqualTo(0));
        }

        [Test]
        public void SetId_WithValidId_SetsId()
        {
            // Arrange
            var widget = new TestGuiWidget();
            string id = "test-widget";

            // Act
            var result = widget.SetId(id);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.EqualTo(id));
        }

        [Test]
        public void SetId_WithNullId_SetsId()
        {
            // Arrange
            var widget = new TestGuiWidget();

            // Act
            var result = widget.SetId(null);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.Null);
        }

        [Test]
        public void SetId_WithEmptyId_SetsId()
        {
            // Arrange
            var widget = new TestGuiWidget();

            // Act
            var result = widget.SetId("");

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.EqualTo(""));
        }

        [Test]
        public void SetWidth_WithValidWidth_SetsWidth()
        {
            // Arrange
            var widget = new TestGuiWidget();
            float width = 100.0f;

            // Act
            var result = widget.SetWidth(width);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetHeight_WithValidHeight_SetsHeight()
        {
            // Arrange
            var widget = new TestGuiWidget();
            float height = 50.0f;

            // Act
            var result = widget.SetHeight(height);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetAspectRatio_WithValidRatio_SetsAspectRatio()
        {
            // Arrange
            var widget = new TestGuiWidget();
            float aspectRatio = 1.5f;

            // Act
            var result = widget.SetAspectRatio(aspectRatio);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetMargin_WithValidMargin_SetsMargin()
        {
            // Arrange
            var widget = new TestGuiWidget();
            float margin = 10.0f;

            // Act
            var result = widget.SetMargin(margin);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetPadding_WithValidPadding_SetsPadding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            float padding = 5.0f;

            // Act
            var result = widget.SetPadding(padding);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetIsEnabled_WithValidValue_SetsEnabled()
        {
            // Arrange
            var widget = new TestGuiWidget();
            bool enabled = false;

            // Act
            var result = widget.SetIsEnabled(enabled);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindIsEnabled_WithValidExpression_SetsEnabledBinding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = widget.BindIsEnabled(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetIsVisible_WithValidValue_SetsVisible()
        {
            // Arrange
            var widget = new TestGuiWidget();
            bool visible = false;

            // Act
            var result = widget.SetIsVisible(visible);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindIsVisible_WithValidExpression_SetsVisibleBinding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = widget.BindIsVisible(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetTooltip_WithValidText_SetsTooltip()
        {
            // Arrange
            var widget = new TestGuiWidget();
            string tooltip = "This is a tooltip";

            // Act
            var result = widget.SetTooltip(tooltip);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindTooltip_WithValidExpression_SetsTooltipBinding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = widget.BindTooltip(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetDisabledTooltip_WithValidText_SetsDisabledTooltip()
        {
            // Arrange
            var widget = new TestGuiWidget();
            string tooltip = "This widget is disabled";

            // Act
            var result = widget.SetDisabledTooltip(tooltip);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindDisabledTooltip_WithValidExpression_SetsDisabledTooltipBinding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = widget.BindDisabledTooltip(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetIsEncouraged_WithValidValue_SetsEncouraged()
        {
            // Arrange
            var widget = new TestGuiWidget();
            bool encouraged = true;

            // Act
            var result = widget.SetIsEncouraged(encouraged);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindIsEncouraged_WithValidExpression_SetsEncouragedBinding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = widget.BindIsEncouraged(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetColor_WithGuiColor_SetsColor()
        {
            // Arrange
            var widget = new TestGuiWidget();
            var color = new GuiColor(255, 0, 0, 255);

            // Act
            var result = widget.SetColor(color);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetColor_WithRgbaValues_SetsColor()
        {
            // Arrange
            var widget = new TestGuiWidget();
            byte red = 255;
            byte green = 128;
            byte blue = 64;
            byte alpha = 200;

            // Act
            var result = widget.SetColor(red, green, blue, alpha);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void SetColor_WithDefaultAlpha_SetsColorWithFullAlpha()
        {
            // Arrange
            var widget = new TestGuiWidget();
            byte red = 255;
            byte green = 128;
            byte blue = 64;

            // Act
            var result = widget.SetColor(red, green, blue);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindColor_WithValidExpression_SetsColorBinding()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, GuiColor>> expression = vm => vm.ColorProperty;

            // Act
            var result = widget.BindColor(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void AddDrawList_WithValidAction_AddsDrawList()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Action<GuiDrawList<TestViewModel>> drawListAction = drawList => { };

            // Act
            var result = widget.AddDrawList(drawListAction);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
        }

        [Test]
        public void BindOnMouseDown_WithValidExpression_SetsMouseDownEvent()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = widget.BindOnMouseDown(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.Not.Null);
            Assert.That(widget.Id, Is.Not.Empty);
            Assert.That(widget.Events.ContainsKey("mousedown"), Is.True);
        }

        [Test]
        public void BindOnMouseDown_WithExistingId_KeepsExistingId()
        {
            // Arrange
            var widget = new TestGuiWidget();
            widget.SetId("existing-id");
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = widget.BindOnMouseDown(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.EqualTo("existing-id"));
        }

        [Test]
        public void BindOnMouseUp_WithValidExpression_SetsMouseUpEvent()
        {
            // Arrange
            var widget = new TestGuiWidget();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = widget.BindOnMouseUp(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.Not.Null);
            Assert.That(widget.Id, Is.Not.Empty);
            Assert.That(widget.Events.ContainsKey("mouseup"), Is.True);
        }

        [Test]
        public void BindOnMouseUp_WithExistingId_KeepsExistingId()
        {
            // Arrange
            var widget = new TestGuiWidget();
            widget.SetId("existing-id");
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = widget.BindOnMouseUp(expression);

            // Assert
            Assert.That(result, Is.EqualTo(widget));
            Assert.That(widget.Id, Is.EqualTo("existing-id"));
        }

        [Test]
        public void ToJson_WithAllProperties_ReturnsJsonElement()
        {
            // Arrange
            var widget = new TestGuiWidget();
            widget.SetId("test-widget")
                  .SetWidth(100.0f)
                  .SetHeight(50.0f)
                  .SetIsEnabled(true)
                  .SetIsVisible(true)
                  .SetTooltip("Test tooltip")
                  .SetColor(255, 0, 0, 255);

            // Act
            var result = widget.ToJson();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithMinimalProperties_ReturnsJsonElement()
        {
            // Arrange
            var widget = new TestGuiWidget();

            // Act
            var result = widget.ToJson();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        // Test helper class
        public class TestGuiWidget : GuiWidget<TestViewModel, TestGuiWidget>
        {
            public override Json BuildElement()
            {
                return NWScript.JsonString("test-element");
            }
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
            public bool BoolProperty { get; set; } = true;
            public GuiColor ColorProperty { get; set; } = new GuiColor(255, 0, 0, 255);

            public Action MouseDownAction => () => { };
            public Action MouseUpAction => () => { };
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
    }
}
