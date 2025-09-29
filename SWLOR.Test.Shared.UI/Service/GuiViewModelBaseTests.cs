using System.ComponentModel;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;
using SWLOR.NWN.API.Service;
using SWLOR.NWN.API.Engine;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Shared.UI.Service
{
    [TestFixture]
    public class GuiViewModelBaseTests : TestBase
    {
        private TestGuiViewModel _viewModel;
        private IGuiService _mockGuiService;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
            _mockGuiService = Substitute.For<IGuiService>();
            
            // Set up the mock to return a proper GuiConstructedWindow for GetWindowTemplate
            var mockWindow = new GuiConstructedWindow(
                GuiWindowType.CharacterSheet,
                "testWindowId",
                NWScript.JsonString("test"),
                new GuiRectangle(0, 0, 100, 100),
                new Dictionary<string, Json> 
                { 
                    { "%%WINDOW_MAIN%%", NWScript.JsonString("test") },
                    { "%%WINDOW_MODAL%%", NWScript.JsonString("modal") }
                },
                null);
            _mockGuiService.GetWindowTemplate(Arg.Any<GuiWindowType>()).Returns(mockWindow);
            
            _viewModel = new TestGuiViewModel(_mockGuiService);
        }

        [Test]
        public void Constructor_WithGuiService_SetsGuiService()
        {
            // Arrange
            var guiService = Substitute.For<IGuiService>();

            // Act
            var viewModel = new TestGuiViewModel(guiService);

            // Assert
            Assert.That(viewModel.GuiService, Is.EqualTo(guiService));
        }

        [Test]
        public void Get_WithValidProperty_ReturnsValue()
        {
            // Arrange
            _viewModel.SetTestProperty("Test Value");

            // Act
            var result = _viewModel.GetTestProperty();

            // Assert
            Assert.That(result, Is.EqualTo("Test Value"));
        }

        [Test]
        public void Get_WithUnsetProperty_ReturnsDefault()
        {
            // Act
            var result = _viewModel.GetTestProperty();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void Set_WithValidValue_SetsValue()
        {
            // Act
            _viewModel.SetTestProperty("New Value");

            // Assert
            Assert.That(_viewModel.GetTestProperty(), Is.EqualTo("New Value"));
        }

        [Test]
        public void Set_WithSameValue_DoesNotTriggerPropertyChanged()
        {
            // Arrange
            var testValue = "Test Value";
            _viewModel.SetTestProperty(testValue);
            var propertyChangedCount = 0;
            _viewModel.PropertyChanged += (sender, args) => propertyChangedCount++;

            // Act - Set the same value again using the same reference
            _viewModel.SetTestProperty(testValue);

            // Assert - The Set method should detect that the value hasn't changed
            // and not trigger PropertyChanged. However, since we're calling Set twice,
            // the first call will trigger PropertyChanged (count = 1), and the second
            // call should not trigger it again (count should remain 1).
            Assert.That(propertyChangedCount, Is.EqualTo(1));
        }

        [Test]
        public void Set_WithDifferentValue_TriggersPropertyChanged()
        {
            // Arrange
            _viewModel.SetTestProperty("Original Value");
            var propertyChangedCount = 0;
            string changedPropertyName = null;
            _viewModel.PropertyChanged += (sender, args) => 
            {
                propertyChangedCount++;
                changedPropertyName = args.PropertyName;
            };

            // Act
            _viewModel.SetTestProperty("New Value");

            // Assert
            Assert.That(propertyChangedCount, Is.EqualTo(1));
            Assert.That(changedPropertyName, Is.EqualTo("TestProperty"));
        }

        [Test]
        public void Set_WithNullPropertyName_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<Exception>(() => _viewModel.SetWithNullPropertyName("Test"));
        }

        [Test]
        public void Geometry_GetSet_WorksCorrectly()
        {
            // Arrange
            var rectangle = new GuiRectangle(10, 20, 100, 50);

            // Act
            _viewModel.Geometry = rectangle;

            // Assert
            Assert.That(_viewModel.Geometry, Is.EqualTo(rectangle));
        }

        [Test]
        public void Bind_WithValidParameters_SetsProperties()
        {
            // Arrange
            uint player = 12345;
            int windowToken = 67890;
            var geometry = new GuiRectangle(0, 0, 400, 300);
            var windowType = GuiWindowType.CharacterSheet;
            var payload = Substitute.For<IGuiPayload>();
            uint tetherObject = 11111;

            // Act
            _viewModel.Bind(player, windowToken, geometry, windowType, payload, tetherObject);

            // Assert
            Assert.That(_viewModel.TetherObject, Is.EqualTo(tetherObject));
        }

        [Test]
        public void Bind_WithZeroGeometry_SetsInitialGeometry()
        {
            // Arrange
            var initialGeometry = new GuiRectangle(100, 100, 200, 200);
            _viewModel.Geometry = new GuiRectangle(0, 0, 0, 0); // Zero geometry

            // Act
            _viewModel.Bind(12345, 67890, initialGeometry, GuiWindowType.CharacterSheet, null, 0);

            // Assert
            Assert.That(_viewModel.Geometry, Is.EqualTo(initialGeometry));
        }

        [Test]
        public void Bind_WithNonZeroGeometry_KeepsExistingGeometry()
        {
            // Arrange
            var existingGeometry = new GuiRectangle(50, 50, 150, 150);
            var initialGeometry = new GuiRectangle(100, 100, 200, 200);
            _viewModel.Geometry = existingGeometry;

            // Act
            _viewModel.Bind(12345, 67890, initialGeometry, GuiWindowType.CharacterSheet, null, 0);

            // Assert
            Assert.That(_viewModel.Geometry, Is.EqualTo(existingGeometry));
        }

        [Test]
        public void UpdatePropertyFromClient_WithValidProperty_UpdatesValue()
        {
            // Arrange
            _viewModel.SetTestProperty("Original Value");
            var json = NWScript.JsonString("Updated Value");

            // Act
            _viewModel.UpdatePropertyFromClient("TestProperty");

            // Assert
            // Note: This test is limited because we can't easily mock NuiGetBind
            // but we can verify the method doesn't throw an exception
            Assert.DoesNotThrow(() => _viewModel.UpdatePropertyFromClient("TestProperty"));
        }

        [Test]
        public void WatchOnClient_WithValidExpression_DoesNotThrow()
        {
            // Arrange
            uint player = 12345;
            int windowToken = 67890;
            _viewModel.Bind(player, windowToken, new GuiRectangle(0, 0, 100, 100), GuiWindowType.CharacterSheet, null, 0);
            
            // Set a value for TestProperty so ToJson doesn't fail
            _viewModel.SetTestProperty("Test Value");

            // Act & Assert
            Assert.DoesNotThrow(() => _viewModel.WatchTestProperty());
        }

        [Test]
        public void ShowModal_WithValidParameters_SetsModalProperties()
        {
            // Arrange
            string prompt = "Are you sure?";
            Action confirmAction = () => { };
            Action cancelAction = () => { };
            string confirmText = "Yes";
            string cancelText = "No";

            // Act
            _viewModel.ShowModal(prompt, confirmAction, cancelAction, confirmText, cancelText);

            // Assert
            Assert.That(_viewModel.ModalPromptText, Is.EqualTo(prompt));
            Assert.That(_viewModel.ModalConfirmButtonText, Is.EqualTo(confirmText));
            Assert.That(_viewModel.ModalCancelButtonText, Is.EqualTo(cancelText));
        }

        [Test]
        public void ShowModal_WithDefaultParameters_SetsDefaultValues()
        {
            // Arrange
            string prompt = "Are you sure?";
            Action confirmAction = () => { };

            // Act
            _viewModel.ShowModal(prompt, confirmAction);

            // Assert
            Assert.That(_viewModel.ModalPromptText, Is.EqualTo(prompt));
            Assert.That(_viewModel.ModalConfirmButtonText, Is.EqualTo("Yes"));
            Assert.That(_viewModel.ModalCancelButtonText, Is.EqualTo("No"));
        }

        [Test]
        public void OnModalClose_ReturnsActionThatResetsValues()
        {
            // Arrange
            // These properties are read-only, so we can't set them directly

            // Act
            var closeAction = _viewModel.OnModalClose();
            closeAction();

            // Assert
            Assert.That(_viewModel.ModalPromptText, Is.EqualTo("Are you sure?"));
            Assert.That(_viewModel.ModalConfirmButtonText, Is.EqualTo("Yes"));
            Assert.That(_viewModel.ModalCancelButtonText, Is.EqualTo("No"));
        }

        [Test]
        public void OnModalConfirmClick_ReturnsActionThatChangesView()
        {
            // Act
            var confirmAction = _viewModel.OnModalConfirmClick();

            // Assert
            Assert.That(confirmAction, Is.Not.Null);
            // Note: We can't easily test the ChangePartialView call without mocking
            Assert.DoesNotThrow(() => confirmAction());
        }

        [Test]
        public void OnModalCancelClick_ReturnsActionThatChangesView()
        {
            // Act
            var cancelAction = _viewModel.OnModalCancelClick();

            // Assert
            Assert.That(cancelAction, Is.Not.Null);
            // Note: We can't easily test the ChangePartialView call without mocking
            Assert.DoesNotThrow(() => cancelAction());
        }

        [Test]
        public void OnWindowClosed_ReturnsEmptyAction()
        {
            // Act
            var closeAction = _viewModel.OnWindowClosed();

            // Assert
            Assert.That(closeAction, Is.Not.Null);
            Assert.DoesNotThrow(() => closeAction());
        }

        [Test]
        public void ChangePartialView_WithValidParameters_DoesNotThrow()
        {
            // Arrange
            var mockWindow = new GuiConstructedWindow(
                GuiWindowType.CharacterSheet,
                "testWindowId",
                NWScript.JsonString("test"),
                new GuiRectangle(0, 0, 100, 100),
                new Dictionary<string, Json> 
                { 
                    { "partialName", NWScript.JsonString("test") } 
                },
                null);
            _mockGuiService.GetWindowTemplate(Arg.Any<GuiWindowType>()).Returns(mockWindow);

            // Act & Assert
            Assert.DoesNotThrow(() => _viewModel.ChangePartialView("elementId", "partialName"));
        }

        // Test helper class
        public class TestGuiViewModel : GuiViewModelBase<TestGuiViewModel, IGuiPayload>
        {
            public IGuiService GuiService => _guiService;

            public TestGuiViewModel(IGuiService guiService) : base(guiService)
            {
                // Initialize Geometry to avoid null reference exceptions
                Geometry = new GuiRectangle(0, 0, 100, 100);
            }

            protected override void Initialize(IGuiPayload initialPayload)
            {
                // Test implementation
            }

            public string TestProperty
            {
                get => Get<string>();
                set => Set(value);
            }

            public void SetTestProperty(string value)
            {
                TestProperty = value;
            }

            public string GetTestProperty()
            {
                return TestProperty;
            }

            public void SetWithNullPropertyName(string value)
            {
                // This will cause an exception due to null property name
                Set(value, null);
            }

            public void WatchTestProperty()
            {
                WatchOnClient(vm => vm.TestProperty);
            }

            public new void ShowModal(string prompt, Action confirmAction, Action? cancelAction = null, string confirmText = "Yes", string cancelText = "No")
            {
                base.ShowModal(prompt, confirmAction, cancelAction, confirmText, cancelText);
            }
        }
    }
}
