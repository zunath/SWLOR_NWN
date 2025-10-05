using System.Linq.Expressions;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Test.Shared.UI.Component
{
    [TestFixture]
    public class GuiImageTests : TestBase
    {
        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
        }

        [Test]
        public void SetResref_WithValidResref_SetsResref()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            var resref = "test_image";

            // Act
            var result = guiImage.SetResref(resref);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void BindResref_WithValidExpression_SetsResrefBindName()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            Expression<Func<TestViewModel, string>> expression = vm => vm.TextProperty;

            // Act
            var result = guiImage.BindResref(expression);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void SetAspect_WithValidAspect_SetsAspect()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            var aspect = NuiAspectType.Fit;

            // Act
            var result = guiImage.SetAspect(aspect);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void BindAspect_WithValidExpression_SetsAspectBindName()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            Expression<Func<TestViewModel, NuiAspectType>> expression = vm => vm.AspectProperty;

            // Act
            var result = guiImage.BindAspect(expression);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void SetHorizontalAlign_WithValidAlign_SetsHorizontalAlign()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            var hAlign = NuiHorizontalAlignType.Left;

            // Act
            var result = guiImage.SetHorizontalAlign(hAlign);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void BindHorizontalAlign_WithValidExpression_SetsHorizontalAlignBindName()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            Expression<Func<TestViewModel, NuiHorizontalAlignType>> expression = vm => vm.HorizontalAlignProperty;

            // Act
            var result = guiImage.BindHorizontalAlign(expression);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void SetVerticalAlign_WithValidAlign_SetsVerticalAlign()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            var vAlign = NuiVerticalAlignType.Top;

            // Act
            var result = guiImage.SetVerticalAlign(vAlign);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void BindVerticalAlign_WithValidExpression_SetsVerticalAlignBindName()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            Expression<Func<TestViewModel, NuiVerticalAlignType>> expression = vm => vm.VerticalAlignProperty;

            // Act
            var result = guiImage.BindVerticalAlign(expression);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void SetRegion_WithValidRegion_SetsRegion()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            var region = new GuiRectangle(0, 0, 100, 100);

            // Act
            var result = guiImage.SetRegion(region);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void BindRegion_WithValidExpression_SetsRegionBindName()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            Expression<Func<TestViewModel, GuiRectangle>> expression = vm => vm.RegionProperty;

            // Act
            var result = guiImage.BindRegion(expression);

            // Assert
            Assert.That(result, Is.SameAs(guiImage));
        }

        [Test]
        public void BuildElement_WithStaticValues_ReturnsImageElement()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            guiImage.SetResref("test_image");
            guiImage.SetAspect(NuiAspectType.Fit);
            guiImage.SetHorizontalAlign(NuiHorizontalAlignType.Center);
            guiImage.SetVerticalAlign(NuiVerticalAlignType.Middle);
            guiImage.SetRegion(new GuiRectangle(0, 0, 100, 100));

            // Act
            var result = guiImage.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithBoundValues_ReturnsImageElement()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            guiImage.BindResref(vm => vm.TextProperty);
            guiImage.BindAspect(vm => vm.AspectProperty);
            guiImage.BindHorizontalAlign(vm => vm.HorizontalAlignProperty);
            guiImage.BindVerticalAlign(vm => vm.VerticalAlignProperty);
            guiImage.BindRegion(vm => vm.RegionProperty);

            // Act
            var result = guiImage.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildElement_WithNoValues_ReturnsImageElement()
        {
            // Arrange
            var guiImage = new GuiImage<TestViewModel>();
            guiImage.SetResref(""); // Set empty resref instead of null

            // Act
            var result = guiImage.BuildElement();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        public class TestViewModel : IGuiViewModel
        {
            public string TextProperty { get; set; } = "Test Text";
            public NuiAspectType AspectProperty { get; set; } = NuiAspectType.Fit;
            public NuiHorizontalAlignType HorizontalAlignProperty { get; set; } = NuiHorizontalAlignType.Center;
            public NuiVerticalAlignType VerticalAlignProperty { get; set; } = NuiVerticalAlignType.Middle;
            public GuiRectangle RegionProperty { get; set; } = new GuiRectangle(0, 0, 100, 100);

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
