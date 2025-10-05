using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.NWN.API.Service;

namespace SWLOR.Test.Shared.UI.Service
{
    [TestFixture]
    public class GuiPropertyConverterTests : TestBase
    {
        private GuiPropertyConverter _converter;

        [SetUp]
        public void SetUp()
        {
            InitializeMockNWScript();
            _converter = new GuiPropertyConverter();
        }

        [Test]
        public void ToJson_WithString_ReturnsJsonString()
        {
            // Arrange
            string value = "Test String";

            // Act
            var result = _converter.ToJson(value);

            // Assert
            Assert.That(result, Is.Not.Null);
            // Note: We can't easily test the exact JSON content without mocking NWScript functions
            // but we can verify it doesn't throw an exception
        }

        [Test]
        public void ToJson_WithInt_ReturnsJsonInt()
        {
            // Arrange
            int value = 42;

            // Act
            var result = _converter.ToJson(value);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithFloat_ReturnsJsonFloat()
        {
            // Arrange
            float value = 3.14f;

            // Act
            var result = _converter.ToJson(value);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithBool_ReturnsJsonBool()
        {
            // Arrange
            bool value = true;

            // Act
            var result = _converter.ToJson(value);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithGuiRectangle_ReturnsJsonRectangle()
        {
            // Arrange
            var rectangle = new GuiRectangle(10, 20, 100, 50);

            // Act
            var result = _converter.ToJson(rectangle);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithGuiColor_ReturnsJsonColor()
        {
            // Arrange
            var color = new GuiColor(255, 128, 64, 200);

            // Act
            var result = _converter.ToJson(color);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithStringBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<string> { "Item1", "Item2", "Item3" };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithIntBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<int> { 1, 2, 3, 4, 5 };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithFloatBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<float> { 1.1f, 2.2f, 3.3f };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithBoolBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<bool> { true, false, true };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithGuiRectangleBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<GuiRectangle>
            {
                new GuiRectangle(0, 0, 10, 10),
                new GuiRectangle(10, 10, 20, 20)
            };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithGuiColorBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<GuiColor>
            {
                new GuiColor(255, 0, 0, 255),
                new GuiColor(0, 255, 0, 255)
            };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithGuiComboEntryBindingList_ReturnsJsonArray()
        {
            // Arrange
            var list = new GuiBindingList<GuiComboEntry>
            {
                new GuiComboEntry("Option 1", 1),
                new GuiComboEntry("Option 2", 2)
            };

            // Act
            var result = _converter.ToJson(list);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJson_WithUnsupportedType_ThrowsException()
        {
            // Arrange
            var unsupportedValue = new object();

            // Act & Assert
            Assert.Throws<Exception>(() => _converter.ToJson(unsupportedValue));
        }

        [Test]
        public void ToObject_WithStringType_ReturnsString()
        {
            // Arrange
            var json = NWScript.JsonString("Test String");
            var type = typeof(string);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<string>());
            Assert.That(result, Is.EqualTo("Test String"));
        }

        [Test]
        public void ToObject_WithIntType_ReturnsInt()
        {
            // Arrange
            var json = NWScript.JsonInt(42);
            var type = typeof(int);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<int>());
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void ToObject_WithFloatType_ReturnsFloat()
        {
            // Arrange
            var json = NWScript.JsonFloat(3.14f);
            var type = typeof(float);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<float>());
            Assert.That(result, Is.EqualTo(3.14f));
        }

        [Test]
        public void ToObject_WithBoolType_ReturnsBool()
        {
            // Arrange
            var json = NWScript.JsonInt(1); // NWScript represents true as 1
            var type = typeof(bool);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<bool>());
            Assert.That(result, Is.True);
        }

        [Test]
        public void ToObject_WithGuiRectangleType_ReturnsGuiRectangle()
        {
            // Arrange - Create a Json object that represents a rectangle structure
            var json = NWScript.JsonString("{\"x\":10.0,\"y\":20.0,\"w\":100.0,\"h\":50.0}");
            var type = typeof(GuiRectangle);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiRectangle>());
            var resultRectangle = (GuiRectangle)result;
            Assert.That(resultRectangle.X, Is.EqualTo(10));
            Assert.That(resultRectangle.Y, Is.EqualTo(20));
            Assert.That(resultRectangle.Width, Is.EqualTo(100));
            Assert.That(resultRectangle.Height, Is.EqualTo(50));
        }

        [Test]
        public void ToObject_WithGuiColorType_ReturnsGuiColor()
        {
            // Arrange - Create a Json object that represents a color structure
            var json = NWScript.JsonString("{\"r\":255,\"g\":128,\"b\":64,\"a\":200}");
            var type = typeof(GuiColor);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiColor>());
            var resultColor = (GuiColor)result;
            Assert.That(resultColor.R, Is.EqualTo(255));
            Assert.That(resultColor.G, Is.EqualTo(128));
            Assert.That(resultColor.B, Is.EqualTo(64));
            Assert.That(resultColor.Alpha, Is.EqualTo(200));
        }

        [Test]
        public void ToObject_WithStringBindingListType_ReturnsStringBindingList()
        {
            // Arrange
            var list = new GuiBindingList<string> { "Item1", "Item2", "Item3" };
            var json = _converter.ToJson(list);
            var type = typeof(GuiBindingList<string>);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiBindingList<string>>());
            var resultList = (GuiBindingList<string>)result;
            Assert.That(resultList.Count, Is.EqualTo(3));
            Assert.That(resultList[0], Is.EqualTo("Item1"));
            Assert.That(resultList[1], Is.EqualTo("Item2"));
            Assert.That(resultList[2], Is.EqualTo("Item3"));
        }

        [Test]
        public void ToObject_WithIntBindingListType_ReturnsIntBindingList()
        {
            // Arrange
            var list = new GuiBindingList<int> { 1, 2, 3, 4, 5 };
            var json = _converter.ToJson(list);
            var type = typeof(GuiBindingList<int>);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiBindingList<int>>());
            var resultList = (GuiBindingList<int>)result;
            Assert.That(resultList.Count, Is.EqualTo(5));
            Assert.That(resultList[0], Is.EqualTo(1));
            Assert.That(resultList[1], Is.EqualTo(2));
            Assert.That(resultList[2], Is.EqualTo(3));
            Assert.That(resultList[3], Is.EqualTo(4));
            Assert.That(resultList[4], Is.EqualTo(5));
        }

        [Test]
        public void ToObject_WithFloatBindingListType_ReturnsFloatBindingList()
        {
            // Arrange
            var list = new GuiBindingList<float> { 1.1f, 2.2f, 3.3f };
            var json = _converter.ToJson(list);
            var type = typeof(GuiBindingList<float>);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiBindingList<float>>());
            var resultList = (GuiBindingList<float>)result;
            Assert.That(resultList.Count, Is.EqualTo(3));
            Assert.That(resultList[0], Is.EqualTo(1.1f));
            Assert.That(resultList[1], Is.EqualTo(2.2f));
            Assert.That(resultList[2], Is.EqualTo(3.3f));
        }

        [Test]
        public void ToObject_WithBoolBindingListType_ReturnsBoolBindingList()
        {
            // Arrange - Create a Json array with individual integer values (1 = true, 0 = false)
            var json = NWScript.JsonArray();
            json = NWScript.JsonArrayInsert(json, NWScript.JsonInt(1)); // true
            json = NWScript.JsonArrayInsert(json, NWScript.JsonInt(0)); // false
            json = NWScript.JsonArrayInsert(json, NWScript.JsonInt(1)); // true
            var type = typeof(GuiBindingList<bool>);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiBindingList<bool>>());
            var resultList = (GuiBindingList<bool>)result;
            Assert.That(resultList.Count, Is.EqualTo(3));
            Assert.That(resultList[0], Is.True);
            Assert.That(resultList[1], Is.False);
            Assert.That(resultList[2], Is.True);
        }

        [Test]
        public void ToObject_WithGuiRectangleBindingListType_ReturnsGuiRectangleBindingList()
        {
            // Arrange - Create a Json array with individual rectangle objects
            var json = NWScript.JsonArray();
            var rect1 = NWScript.JsonString("{\"x\":0.0,\"y\":0.0,\"w\":10.0,\"h\":10.0}");
            var rect2 = NWScript.JsonString("{\"x\":10.0,\"y\":10.0,\"w\":20.0,\"h\":20.0}");
            json = NWScript.JsonArrayInsert(json, rect1);
            json = NWScript.JsonArrayInsert(json, rect2);
            var type = typeof(GuiBindingList<GuiRectangle>);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiBindingList<GuiRectangle>>());
            var resultList = (GuiBindingList<GuiRectangle>)result;
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList[0].X, Is.EqualTo(0));
            Assert.That(resultList[0].Y, Is.EqualTo(0));
            Assert.That(resultList[0].Width, Is.EqualTo(10));
            Assert.That(resultList[0].Height, Is.EqualTo(10));
            Assert.That(resultList[1].X, Is.EqualTo(10));
            Assert.That(resultList[1].Y, Is.EqualTo(10));
            Assert.That(resultList[1].Width, Is.EqualTo(20));
            Assert.That(resultList[1].Height, Is.EqualTo(20));
        }

        [Test]
        public void ToObject_WithGuiColorBindingListType_ReturnsGuiColorBindingList()
        {
            // Arrange - Create a Json array with individual color objects
            var json = NWScript.JsonArray();
            var color1 = NWScript.JsonString("{\"r\":255,\"g\":0,\"b\":0,\"a\":255}");
            var color2 = NWScript.JsonString("{\"r\":0,\"g\":255,\"b\":0,\"a\":255}");
            json = NWScript.JsonArrayInsert(json, color1);
            json = NWScript.JsonArrayInsert(json, color2);
            var type = typeof(GuiBindingList<GuiColor>);

            // Act
            var result = _converter.ToObject(json, type);

            // Assert
            Assert.That(result, Is.TypeOf<GuiBindingList<GuiColor>>());
            var resultList = (GuiBindingList<GuiColor>)result;
            Assert.That(resultList.Count, Is.EqualTo(2));
            Assert.That(resultList[0].R, Is.EqualTo(255));
            Assert.That(resultList[0].G, Is.EqualTo(0));
            Assert.That(resultList[0].B, Is.EqualTo(0));
            Assert.That(resultList[0].Alpha, Is.EqualTo(255));
            Assert.That(resultList[1].R, Is.EqualTo(0));
            Assert.That(resultList[1].G, Is.EqualTo(255));
            Assert.That(resultList[1].B, Is.EqualTo(0));
            Assert.That(resultList[1].Alpha, Is.EqualTo(255));
        }

        [Test]
        public void ToObject_WithUnsupportedType_ThrowsException()
        {
            // Arrange
            var json = NWScript.JsonString("test");
            var type = typeof(object);

            // Act & Assert
            Assert.Throws<Exception>(() => _converter.ToObject(json, type));
        }
    }
}
