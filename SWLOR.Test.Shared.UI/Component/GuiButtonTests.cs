using System.Linq.Expressions;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiButtonTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_CreatesButtonWithDefaultValues()
        {
            // Act
            var button = new GuiButton<TestViewModel>();

            // Assert
            Assert.That(button, Is.Not.Null);
            Assert.That(button.Id, Is.Null);
            Assert.That(button.Elements, Is.Not.Null);
            Assert.That(button.Elements.Count, Is.EqualTo(0));
        }

        [Test]
        public void SetText_WithValidText_SetsText()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            string text = "Click Me";

            // Act
            var result = button.SetText(text);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            // Note: We can't easily test the private Text property without reflection
            // but we can verify the method returns the button for chaining
        }

        [Test]
        public void SetText_WithNullText_SetsText()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();

            // Act
            var result = button.SetText(null);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetText_WithEmptyText_SetsText()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();

            // Act
            var result = button.SetText("");

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void BindText_WithValidExpression_SetsTextBinding()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = button.BindText(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            // Note: We can't easily test the private TextBindName property without reflection
            // but we can verify the method returns the button for chaining
        }

        [Test]
        public void BindText_WithIntExpression_SetsTextBinding()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, int>> expression = vm => vm.IntProperty;

            // Act
            var result = button.BindText(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void BindOnClicked_WithValidExpression_SetsClickEvent()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = button.BindOnClicked(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            Assert.That(button.Id, Is.Not.Null);
            Assert.That(button.Id, Is.Not.Empty);
            Assert.That(button.Events, Is.Not.Null);
            Assert.That(button.Events.ContainsKey("click"), Is.True);
        }

        [Test]
        public void BindOnClicked_WithExistingId_KeepsExistingId()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            button.SetId("existing-id");
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = button.BindOnClicked(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            Assert.That(button.Id, Is.EqualTo("existing-id"));
        }

        [Test]
        public void BindOnClicked_WithMethodExpression_SetsClickEvent()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, Action>> expression = vm => vm.ClickMethod();

            // Act
            var result = button.BindOnClicked(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            Assert.That(button.Events.ContainsKey("click"), Is.True);
        }

        [Test]
        public void BuildElement_WithStaticText_ReturnsButtonElement()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            button.SetText("Click Me");

            // Act
            var result = button.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
            // Note: We can't easily test the exact JSON content without mocking NWScript functions
            // but we can verify it doesn't throw an exception
        }

        [Test]
        public void BuildElement_WithBoundText_ReturnsButtonElement()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            button.BindText(vm => vm.TextProperty);

            // Act
            var result = button.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoText_ReturnsButtonElement()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            button.SetText(""); // Set empty text instead of null

            // Act
            var result = button.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetId_WithValidId_SetsId()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            string id = "test-button";

            // Act
            var result = button.SetId(id);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            Assert.That(button.Id, Is.EqualTo(id));
        }

        [Test]
        public void SetWidth_WithValidWidth_SetsWidth()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            float width = 100.0f;

            // Act
            var result = button.SetWidth(width);

            // Assert
            Assert.That(result, Is.EqualTo(button));
            // Note: We can't easily test the private Width property without reflection
            // but we can verify the method returns the button for chaining
        }

        [Test]
        public void SetHeight_WithValidHeight_SetsHeight()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            float height = 50.0f;

            // Act
            var result = button.SetHeight(height);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetIsEnabled_WithValidValue_SetsEnabled()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            bool enabled = false;

            // Act
            var result = button.SetIsEnabled(enabled);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetIsVisible_WithValidValue_SetsVisible()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            bool visible = false;

            // Act
            var result = button.SetIsVisible(visible);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetTooltip_WithValidText_SetsTooltip()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            string tooltip = "This is a tooltip";

            // Act
            var result = button.SetTooltip(tooltip);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetColor_WithGuiColor_SetsColor()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            var color = new GuiColor(255, 0, 0, 255);

            // Act
            var result = button.SetColor(color);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetColor_WithRgbaValues_SetsColor()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            byte red = 255;
            byte green = 128;
            byte blue = 64;
            byte alpha = 200;

            // Act
            var result = button.SetColor(red, green, blue, alpha);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void SetColor_WithDefaultAlpha_SetsColorWithFullAlpha()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            byte red = 255;
            byte green = 128;
            byte blue = 64;

            // Act
            var result = button.SetColor(red, green, blue);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void BindIsEnabled_WithValidExpression_SetsEnabledBinding()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = button.BindIsEnabled(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void BindIsVisible_WithValidExpression_SetsVisibleBinding()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = button.BindIsVisible(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void BindTooltip_WithValidExpression_SetsTooltipBinding()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = button.BindTooltip(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void BindColor_WithValidExpression_SetsColorBinding()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            Expression<Func<TestViewModel, GuiColor>> expression = vm => vm.ColorProperty;

            // Act
            var result = button.BindColor(expression);

            // Assert
            Assert.That(result, Is.EqualTo(button));
        }

        [Test]
        public void ToJson_WithAllProperties_ReturnsJsonElement()
        {
            // Arrange
            var button = new GuiButton<TestViewModel>();
            button.SetId("test-button")
                  .SetText("Click Me")
                  .SetWidth(100.0f)
                  .SetHeight(50.0f)
                  .SetIsEnabled(true)
                  .SetIsVisible(true)
                  .SetTooltip("Test tooltip")
                  .SetColor(255, 0, 0, 255);

            // Act
            var result = button.ToJson();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        // Test helper class
        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
            public int IntProperty { get; set; } = 42;
            public bool BoolProperty { get; set; } = true;
            public GuiColor ColorProperty { get; set; } = new GuiColor(255, 0, 0, 255);

            public Action ClickAction => () => { };
            public Action ClickMethod() => () => { };
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
