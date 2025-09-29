using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Shared.Domain.Entities
{
    [TestFixture]
    public class InventoryItemTests
    {
        [Test]
        public void InventoryItem_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var inventoryItem = new InventoryItem();

            // Assert
            Assert.That(inventoryItem.Id, Is.Not.Null);
            Assert.That(inventoryItem.StorageId, Is.Null);
            Assert.That(inventoryItem.PlayerId, Is.Null);
            Assert.That(inventoryItem.Name, Is.Null);
            Assert.That(inventoryItem.Tag, Is.Null);
            Assert.That(inventoryItem.Resref, Is.Null);
            Assert.That(inventoryItem.Quantity, Is.EqualTo(0));
            Assert.That(inventoryItem.Data, Is.Null);
            Assert.That(inventoryItem.IconResref, Is.Null);
        }

        [Test]
        public void InventoryItem_WithStorageId_ShouldStoreStorageIdCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.StorageId = "storage-123";

            // Assert
            Assert.That(inventoryItem.StorageId, Is.EqualTo("storage-123"));
        }

        [Test]
        public void InventoryItem_WithPlayerId_ShouldStorePlayerIdCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.PlayerId = "player-456";

            // Assert
            Assert.That(inventoryItem.PlayerId, Is.EqualTo("player-456"));
        }

        [Test]
        public void InventoryItem_WithName_ShouldStoreNameCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Name = "Test Item";

            // Assert
            Assert.That(inventoryItem.Name, Is.EqualTo("Test Item"));
        }

        [Test]
        public void InventoryItem_WithTag_ShouldStoreTagCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Tag = "test_tag";

            // Assert
            Assert.That(inventoryItem.Tag, Is.EqualTo("test_tag"));
        }

        [Test]
        public void InventoryItem_WithResref_ShouldStoreResrefCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Resref = "item_resref";

            // Assert
            Assert.That(inventoryItem.Resref, Is.EqualTo("item_resref"));
        }

        [Test]
        public void InventoryItem_WithQuantity_ShouldStoreQuantityCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 5;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void InventoryItem_WithData_ShouldStoreDataCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Data = "item_data_json";

            // Assert
            Assert.That(inventoryItem.Data, Is.EqualTo("item_data_json"));
        }

        [Test]
        public void InventoryItem_WithIconResref_ShouldStoreIconResrefCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.IconResref = "icon_resref";

            // Assert
            Assert.That(inventoryItem.IconResref, Is.EqualTo("icon_resref"));
        }

        [Test]
        public void InventoryItem_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.StorageId = "storage-123";
            inventoryItem.PlayerId = "player-456";
            inventoryItem.Name = "Test Item";
            inventoryItem.Tag = "test_tag";
            inventoryItem.Resref = "item_resref";
            inventoryItem.Quantity = 5;
            inventoryItem.Data = "item_data_json";
            inventoryItem.IconResref = "icon_resref";

            // Assert
            Assert.That(inventoryItem.StorageId, Is.EqualTo("storage-123"));
            Assert.That(inventoryItem.PlayerId, Is.EqualTo("player-456"));
            Assert.That(inventoryItem.Name, Is.EqualTo("Test Item"));
            Assert.That(inventoryItem.Tag, Is.EqualTo("test_tag"));
            Assert.That(inventoryItem.Resref, Is.EqualTo("item_resref"));
            Assert.That(inventoryItem.Quantity, Is.EqualTo(5));
            Assert.That(inventoryItem.Data, Is.EqualTo("item_data_json"));
            Assert.That(inventoryItem.IconResref, Is.EqualTo("icon_resref"));
        }

        [Test]
        public void InventoryItem_WithZeroQuantity_ShouldStoreZeroQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 0;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void InventoryItem_WithNegativeQuantity_ShouldStoreNegativeQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = -5;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(-5));
        }

        [Test]
        public void InventoryItem_WithLargeQuantity_ShouldStoreLargeQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 1000000;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(1000000));
        }

        [Test]
        public void InventoryItem_WithMaxQuantity_ShouldStoreMaxQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = int.MaxValue;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void InventoryItem_WithMinQuantity_ShouldStoreMinQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = int.MinValue;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void InventoryItem_WithEmptyStrings_ShouldStoreEmptyStrings()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.StorageId = "";
            inventoryItem.PlayerId = "";
            inventoryItem.Name = "";
            inventoryItem.Tag = "";
            inventoryItem.Resref = "";
            inventoryItem.Data = "";
            inventoryItem.IconResref = "";

            // Assert
            Assert.That(inventoryItem.StorageId, Is.EqualTo(""));
            Assert.That(inventoryItem.PlayerId, Is.EqualTo(""));
            Assert.That(inventoryItem.Name, Is.EqualTo(""));
            Assert.That(inventoryItem.Tag, Is.EqualTo(""));
            Assert.That(inventoryItem.Resref, Is.EqualTo(""));
            Assert.That(inventoryItem.Data, Is.EqualTo(""));
            Assert.That(inventoryItem.IconResref, Is.EqualTo(""));
        }

        [Test]
        public void InventoryItem_WithNullStrings_ShouldStoreNullStrings()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.StorageId = null;
            inventoryItem.PlayerId = null;
            inventoryItem.Name = null;
            inventoryItem.Tag = null;
            inventoryItem.Resref = null;
            inventoryItem.Data = null;
            inventoryItem.IconResref = null;

            // Assert
            Assert.That(inventoryItem.StorageId, Is.Null);
            Assert.That(inventoryItem.PlayerId, Is.Null);
            Assert.That(inventoryItem.Name, Is.Null);
            Assert.That(inventoryItem.Tag, Is.Null);
            Assert.That(inventoryItem.Resref, Is.Null);
            Assert.That(inventoryItem.Data, Is.Null);
            Assert.That(inventoryItem.IconResref, Is.Null);
        }

        [Test]
        public void InventoryItem_WithSpecialCharacters_ShouldStoreSpecialCharacters()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            const string specialString = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            inventoryItem.Name = specialString;
            inventoryItem.Tag = specialString;
            inventoryItem.Resref = specialString;
            inventoryItem.Data = specialString;
            inventoryItem.IconResref = specialString;

            // Assert
            Assert.That(inventoryItem.Name, Is.EqualTo(specialString));
            Assert.That(inventoryItem.Tag, Is.EqualTo(specialString));
            Assert.That(inventoryItem.Resref, Is.EqualTo(specialString));
            Assert.That(inventoryItem.Data, Is.EqualTo(specialString));
            Assert.That(inventoryItem.IconResref, Is.EqualTo(specialString));
        }

        [Test]
        public void InventoryItem_WithLongStrings_ShouldStoreLongStrings()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            var longString = new string('a', 1000);

            // Act
            inventoryItem.Name = longString;
            inventoryItem.Tag = longString;
            inventoryItem.Resref = longString;
            inventoryItem.Data = longString;
            inventoryItem.IconResref = longString;

            // Assert
            Assert.That(inventoryItem.Name, Is.EqualTo(longString));
            Assert.That(inventoryItem.Tag, Is.EqualTo(longString));
            Assert.That(inventoryItem.Resref, Is.EqualTo(longString));
            Assert.That(inventoryItem.Data, Is.EqualTo(longString));
            Assert.That(inventoryItem.IconResref, Is.EqualTo(longString));
        }

        [Test]
        public void InventoryItem_WithQuantityIncrement_ShouldIncrementQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 5;
            inventoryItem.Quantity++;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(6));
        }

        [Test]
        public void InventoryItem_WithQuantityDecrement_ShouldDecrementQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 5;
            inventoryItem.Quantity--;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(4));
        }

        [Test]
        public void InventoryItem_WithQuantityAddition_ShouldAddToQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 5;
            inventoryItem.Quantity += 3;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(8));
        }

        [Test]
        public void InventoryItem_WithQuantitySubtraction_ShouldSubtractFromQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 10;
            inventoryItem.Quantity -= 3;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(7));
        }

        [Test]
        public void InventoryItem_WithQuantityMultiplication_ShouldMultiplyQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 5;
            inventoryItem.Quantity *= 3;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(15));
        }

        [Test]
        public void InventoryItem_WithQuantityDivision_ShouldDivideQuantity()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            inventoryItem.Quantity = 15;
            inventoryItem.Quantity /= 3;

            // Assert
            Assert.That(inventoryItem.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void InventoryItem_WithSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            inventoryItem.StorageId = "storage-123";
            inventoryItem.PlayerId = "player-456";
            inventoryItem.Name = "Test Item";
            inventoryItem.Tag = "test_tag";
            inventoryItem.Resref = "item_resref";
            inventoryItem.Quantity = 5;
            inventoryItem.Data = "item_data_json";
            inventoryItem.IconResref = "icon_resref";

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(inventoryItem);
            var deserializedItem = System.Text.Json.JsonSerializer.Deserialize<InventoryItem>(json);

            // Assert
            Assert.That(deserializedItem, Is.Not.Null);
            Assert.That(deserializedItem.StorageId, Is.EqualTo(inventoryItem.StorageId));
            Assert.That(deserializedItem.PlayerId, Is.EqualTo(inventoryItem.PlayerId));
            Assert.That(deserializedItem.Name, Is.EqualTo(inventoryItem.Name));
            Assert.That(deserializedItem.Tag, Is.EqualTo(inventoryItem.Tag));
            Assert.That(deserializedItem.Resref, Is.EqualTo(inventoryItem.Resref));
            Assert.That(deserializedItem.Quantity, Is.EqualTo(inventoryItem.Quantity));
            Assert.That(deserializedItem.Data, Is.EqualTo(inventoryItem.Data));
            Assert.That(deserializedItem.IconResref, Is.EqualTo(inventoryItem.IconResref));
        }

        [Test]
        public void InventoryItem_WithEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var inventoryItem1 = new InventoryItem();
            var inventoryItem2 = new InventoryItem();
            inventoryItem1.Quantity = 5;
            inventoryItem2.Quantity = 5;

            // Act & Assert
            Assert.That(inventoryItem1.Quantity, Is.EqualTo(inventoryItem2.Quantity));
        }

        [Test]
        public void InventoryItem_WithInequality_ShouldCompareInequalityCorrectly()
        {
            // Arrange
            var inventoryItem1 = new InventoryItem();
            var inventoryItem2 = new InventoryItem();
            inventoryItem1.Quantity = 5;
            inventoryItem2.Quantity = 10;

            // Act & Assert
            Assert.That(inventoryItem1.Quantity, Is.Not.EqualTo(inventoryItem2.Quantity));
        }

        [Test]
        public void InventoryItem_WithToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            inventoryItem.Name = "Test Item";

            // Act
            var result = inventoryItem.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void InventoryItem_WithGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var inventoryItem = new InventoryItem();

            // Act
            var type = inventoryItem.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(InventoryItem)));
        }

        [Test]
        public void InventoryItem_WithHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            inventoryItem.Name = "Test Item";

            // Act
            var hashCode = inventoryItem.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }

        [Test]
        public void InventoryItem_WithJsonData_ShouldStoreJsonData()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            var jsonData = "{\"property1\":\"value1\",\"property2\":123,\"property3\":true}";

            // Act
            inventoryItem.Data = jsonData;

            // Assert
            Assert.That(inventoryItem.Data, Is.EqualTo(jsonData));
        }

        [Test]
        public void InventoryItem_WithXmlData_ShouldStoreXmlData()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            var xmlData = "<item><name>Test Item</name><quantity>5</quantity></item>";

            // Act
            inventoryItem.Data = xmlData;

            // Assert
            Assert.That(inventoryItem.Data, Is.EqualTo(xmlData));
        }

        [Test]
        public void InventoryItem_WithBase64Data_ShouldStoreBase64Data()
        {
            // Arrange
            var inventoryItem = new InventoryItem();
            var base64Data = "SGVsbG8gV29ybGQ="; // "Hello World" in base64

            // Act
            inventoryItem.Data = base64Data;

            // Assert
            Assert.That(inventoryItem.Data, Is.EqualTo(base64Data));
        }
    }
}
