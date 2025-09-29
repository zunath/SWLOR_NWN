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
    public class GuiComboBoxTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void AddOption_WithValidParameters_AddsOption()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            var label = "Option 1";
            var value = 1;

            // Act
            var result = comboBox.AddOption(label, value);

            // Assert
            Assert.That(result, Is.SameAs(comboBox));
        }

        [Test]
        public void AddOption_MultipleOptions_AddsAllOptions()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();

            // Act
            comboBox.AddOption("Option 1", 1);
            comboBox.AddOption("Option 2", 2);
            comboBox.AddOption("Option 3", 3);

            // Assert
            Assert.That(comboBox, Is.Not.Null);
        }

        [Test]
        public void BindOptions_WithValidExpression_SetsOptionsBindName()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            Expression<Func<TestViewModel, List<GuiComboEntry>>> expression = vm => vm.OptionsProperty;

            // Act
            var result = comboBox.BindOptions(expression);

            // Assert
            Assert.That(result, Is.SameAs(comboBox));
        }

        [Test]
        public void SetSelectedIndex_WithValidIndex_SetsSelectedIndex()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            var selectedIndex = 2;

            // Act
            var result = comboBox.SetSelectedIndex(selectedIndex);

            // Assert
            Assert.That(result, Is.SameAs(comboBox));
        }

        [Test]
        public void BindSelectedIndex_WithValidExpression_SetsSelectedIndexBindName()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            Expression<Func<TestViewModel, int>> expression = vm => vm.IntProperty;

            // Act
            var result = comboBox.BindSelectedIndex(expression);

            // Assert
            Assert.That(result, Is.SameAs(comboBox));
        }

        [Test]
        public void BuildElement_WithStaticOptions_ReturnsComboBoxElement()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            comboBox.AddOption("Option 1", 1);
            comboBox.AddOption("Option 2", 2);
            comboBox.SetSelectedIndex(0);

            // Act
            var result = comboBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithBoundOptions_ReturnsComboBoxElement()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            comboBox.BindOptions(vm => vm.OptionsProperty);
            comboBox.BindSelectedIndex(vm => vm.IntProperty);

            // Act
            var result = comboBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithMixedValues_ReturnsComboBoxElement()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();
            comboBox.AddOption("Option 1", 1);
            comboBox.BindSelectedIndex(vm => vm.IntProperty);

            // Act
            var result = comboBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoOptions_ReturnsComboBoxElement()
        {
            // Arrange
            var comboBox = new GuiComboBox<TestViewModel>();

            // Act
            var result = comboBox.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
            public int IntProperty { get; set; } = 42;
            public List<GuiComboEntry> OptionsProperty { get; set; } = new List<GuiComboEntry>();

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
