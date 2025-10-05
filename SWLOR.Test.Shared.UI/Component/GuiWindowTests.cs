using System.Linq.Expressions;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiWindowTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void Constructor_CreatesWindowWithDefaultValues()
        {
            // Act
            var window = new GuiWindow<TestViewModel>();

            // Assert
            Assert.That(window, Is.Not.Null);
            Assert.That(window.Geometry, Is.Not.Null);
            Assert.That(window.Geometry.X, Is.EqualTo(0f));
            Assert.That(window.Geometry.Y, Is.EqualTo(0f));
            Assert.That(window.Geometry.Width, Is.EqualTo(400f));
            Assert.That(window.Geometry.Height, Is.EqualTo(400f));
            Assert.That(window.PartialViews, Is.Not.Null);
            Assert.That(window.PartialViews.Count, Is.EqualTo(0));
            Assert.That(window.Elements, Is.Not.Null);
            Assert.That(window.Elements.Count, Is.EqualTo(0));
        }

        [Test]
        public void SetTitle_WithValidTitle_SetsTitle()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            string title = "Test Window";

            // Act
            var result = window.SetTitle(title);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            // Note: We can't easily test the private Title property without reflection
            // but we can verify the method returns the window for chaining
        }

        [Test]
        public void SetTitle_WithNullTitle_SetsTitle()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();

            // Act
            var result = window.SetTitle(null);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindTitle_WithValidExpression_SetsTitleBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TitleProperty;

            // Act
            var result = window.BindTitle(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void SetInitialGeometry_WithFloatValues_SetsGeometry()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            float x = 100f;
            float y = 200f;
            float width = 300f;
            float height = 400f;

            // Act
            var result = window.SetInitialGeometry(x, y, width, height);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.Geometry.X, Is.EqualTo(x));
            Assert.That(window.Geometry.Y, Is.EqualTo(y));
            Assert.That(window.Geometry.Width, Is.EqualTo(width));
            Assert.That(window.Geometry.Height, Is.EqualTo(height));
        }

        [Test]
        public void SetInitialGeometry_WithGuiRectangle_SetsGeometry()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            var rectangle = new GuiRectangle(50, 75, 250, 350);

            // Act
            var result = window.SetInitialGeometry(rectangle);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.Geometry, Is.EqualTo(rectangle));
        }

        [Test]
        public void SetIsResizable_WithValidValue_SetsResizable()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            bool resizable = false;

            // Act
            var result = window.SetIsResizable(resizable);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindIsResizable_WithValidExpression_SetsResizableBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = window.BindIsResizable(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void SetIsCollapsible_WithValidValue_SetsCollapsible()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            bool collapsible = true;

            // Act
            var result = window.SetIsCollapsible(collapsible);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindIsCollapsed_WithValidExpression_SetsCollapsibleBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = window.BindIsCollapsed(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void SetIsClosable_WithValidValue_SetsClosable()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            bool closable = false;

            // Act
            var result = window.SetIsClosable(closable);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindIsClosable_WithValidExpression_SetsClosableBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = window.BindIsClosable(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void SetIsTransparent_WithValidValue_SetsTransparent()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            bool transparent = true;

            // Act
            var result = window.SetIsTransparent(transparent);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindIsTransparent_WithValidExpression_SetsTransparentBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = window.BindIsTransparent(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void SetShowBorder_WithValidValue_SetsShowBorder()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            bool showBorder = false;

            // Act
            var result = window.SetShowBorder(showBorder);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindShowBorder_WithValidExpression_SetsShowBorderBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = window.BindShowBorder(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void SetAcceptsInput_WithValidValue_SetsAcceptsInput()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            bool acceptsInput = false;

            // Act
            var result = window.SetAcceptsInput(acceptsInput);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void BindAcceptsInput_WithValidExpression_SetsAcceptsInputBinding()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, bool>> expression = vm => vm.BoolProperty;

            // Act
            var result = window.BindAcceptsInput(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
        }

        [Test]
        public void DefinePartialView_WithValidNameAndView_AddsPartialView()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            string name = "test-partial";
            Action<GuiGroup<TestViewModel>> view = group => { };

            // Act
            var result = window.DefinePartialView(name, view);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.PartialViews.ContainsKey(name), Is.True);
            Assert.That(window.PartialViews[name], Is.Not.Null);
        }

        [Test]
        public void AddColumn_WithValidAction_AddsColumn()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Action<GuiColumn<TestViewModel>> columnAction = col => { };

            // Act
            var result = window.AddColumn(columnAction);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.Elements.Count, Is.EqualTo(1));
            Assert.That(window.Elements[0], Is.TypeOf<GuiColumn<TestViewModel>>());
        }

        [Test]
        public void AddRow_WithValidAction_AddsRow()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Action<GuiRow<TestViewModel>> rowAction = row => { };

            // Act
            var result = window.AddRow(rowAction);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.Elements.Count, Is.EqualTo(1));
            Assert.That(window.Elements[0], Is.TypeOf<GuiRow<TestViewModel>>());
        }

        [Test]
        public void BindOnOpened_WithValidExpression_SetsOpenedEvent()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = window.BindOnOpened(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.OpenedEventMethodInfo, Is.Not.Null);
        }

        [Test]
        public void BindOnClosed_WithValidExpression_SetsClosedEvent()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TestMethodReturningString();

            // Act
            var result = window.BindOnClosed(expression);

            // Assert
            Assert.That(result, Is.EqualTo(window));
            Assert.That(window.ClosedEventMethodInfo, Is.Not.Null);
        }

        [Test]
        public void Build_WithDefaultValues_ReturnsWindowElement()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();

            // Act
            var result = window.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
            // Note: We can't easily test the exact JSON content without mocking NWScript functions
            // but we can verify it doesn't throw an exception
        }

        [Test]
        public void Build_WithElements_ReturnsWindowElement()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            window.AddColumn(col => { })
                  .AddRow(row => { });

            // Act
            var result = window.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Build_WithPartialViews_ReturnsWindowElement()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            window.DefinePartialView("test", group => { });

            // Act
            var result = window.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Build_WithBoundProperties_ReturnsWindowElement()
        {
            // Arrange
            var window = new GuiWindow<TestViewModel>();
            window.BindTitle(vm => vm.TitleProperty)
                  .BindIsResizable(vm => vm.BoolProperty)
                  .BindIsCollapsed(vm => vm.BoolProperty)
                  .BindIsClosable(vm => vm.BoolProperty)
                  .BindIsTransparent(vm => vm.BoolProperty)
                  .BindShowBorder(vm => vm.BoolProperty)
                  .BindAcceptsInput(vm => vm.BoolProperty);

            // Act
            var result = window.Build();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        // Test helper class
        public class TestViewModel : IGuiViewModel
        {
            public string TitleProperty { get; set; } = "Test Title";
            public bool BoolProperty { get; set; } = true;

            public Action OpenedAction => () => { };
            public Action ClosedAction => () => { };
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
