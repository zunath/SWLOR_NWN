using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Market.Enums;

namespace SWLOR.Test.Shared.Domain.Entities
{
    [TestFixture]
    public class MarketItemTests
    {
        [Test]
        public void MarketItem_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var marketItem = new MarketItem();

            // Assert
            Assert.That(marketItem.Id, Is.Not.Null);
            Assert.That(marketItem.MarketId, Is.Null);
            Assert.That(marketItem.MarketName, Is.Null);
            Assert.That(marketItem.PlayerId, Is.Null);
            Assert.That(marketItem.SellerName, Is.Null);
            Assert.That(marketItem.Price, Is.EqualTo(0));
            Assert.That(marketItem.IsListed, Is.False);
            Assert.That(marketItem.Name, Is.Null);
            Assert.That(marketItem.Tag, Is.Null);
            Assert.That(marketItem.Resref, Is.Null);
            Assert.That(marketItem.Data, Is.Null);
            Assert.That(marketItem.Quantity, Is.EqualTo(0));
            Assert.That(marketItem.IconResref, Is.Null);
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Invalid));
            Assert.That(marketItem.DateListed, Is.Null);
        }

        [Test]
        public void MarketItem_WithMarketId_ShouldStoreMarketIdCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.MarketId = "market-123";

            // Assert
            Assert.That(marketItem.MarketId, Is.EqualTo("market-123"));
        }

        [Test]
        public void MarketItem_WithMarketName_ShouldStoreMarketNameCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.MarketName = "Test Market";

            // Assert
            Assert.That(marketItem.MarketName, Is.EqualTo("Test Market"));
        }

        [Test]
        public void MarketItem_WithPlayerId_ShouldStorePlayerIdCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.PlayerId = "player-456";

            // Assert
            Assert.That(marketItem.PlayerId, Is.EqualTo("player-456"));
        }

        [Test]
        public void MarketItem_WithSellerName_ShouldStoreSellerNameCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.SellerName = "Test Seller";

            // Assert
            Assert.That(marketItem.SellerName, Is.EqualTo("Test Seller"));
        }

        [Test]
        public void MarketItem_WithPrice_ShouldStorePriceCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 1000;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(1000));
        }

        [Test]
        public void MarketItem_WithIsListed_ShouldStoreIsListedCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.IsListed = true;

            // Assert
            Assert.That(marketItem.IsListed, Is.True);
        }

        [Test]
        public void MarketItem_WithName_ShouldStoreNameCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Name = "Test Item";

            // Assert
            Assert.That(marketItem.Name, Is.EqualTo("Test Item"));
        }

        [Test]
        public void MarketItem_WithTag_ShouldStoreTagCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Tag = "test_tag";

            // Assert
            Assert.That(marketItem.Tag, Is.EqualTo("test_tag"));
        }

        [Test]
        public void MarketItem_WithResref_ShouldStoreResrefCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Resref = "item_resref";

            // Assert
            Assert.That(marketItem.Resref, Is.EqualTo("item_resref"));
        }

        [Test]
        public void MarketItem_WithData_ShouldStoreDataCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Data = "item_data_json";

            // Assert
            Assert.That(marketItem.Data, Is.EqualTo("item_data_json"));
        }

        [Test]
        public void MarketItem_WithQuantity_ShouldStoreQuantityCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Quantity = 5;

            // Assert
            Assert.That(marketItem.Quantity, Is.EqualTo(5));
        }

        [Test]
        public void MarketItem_WithIconResref_ShouldStoreIconResrefCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.IconResref = "icon_resref";

            // Assert
            Assert.That(marketItem.IconResref, Is.EqualTo("icon_resref"));
        }

        [Test]
        public void MarketItem_WithCategory_ShouldStoreCategoryCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Category = MarketCategoryType.Vibroblade;

            // Assert
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Vibroblade));
        }

        [Test]
        public void MarketItem_WithDateListed_ShouldStoreDateListedCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();
            var listedDate = DateTime.Now.AddDays(-1);

            // Act
            marketItem.DateListed = listedDate;

            // Assert
            Assert.That(marketItem.DateListed, Is.EqualTo(listedDate));
        }

        [Test]
        public void MarketItem_WithAllProperties_ShouldStoreAllPropertiesCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();
            var listedDate = DateTime.Now.AddDays(-2);

            // Act
            marketItem.MarketId = "market-123";
            marketItem.MarketName = "Test Market";
            marketItem.PlayerId = "player-456";
            marketItem.SellerName = "Test Seller";
            marketItem.Price = 1000;
            marketItem.IsListed = true;
            marketItem.Name = "Test Item";
            marketItem.Tag = "test_tag";
            marketItem.Resref = "item_resref";
            marketItem.Data = "item_data_json";
            marketItem.Quantity = 5;
            marketItem.IconResref = "icon_resref";
            marketItem.Category = MarketCategoryType.Vibroblade;
            marketItem.DateListed = listedDate;

            // Assert
            Assert.That(marketItem.MarketId, Is.EqualTo("market-123"));
            Assert.That(marketItem.MarketName, Is.EqualTo("Test Market"));
            Assert.That(marketItem.PlayerId, Is.EqualTo("player-456"));
            Assert.That(marketItem.SellerName, Is.EqualTo("Test Seller"));
            Assert.That(marketItem.Price, Is.EqualTo(1000));
            Assert.That(marketItem.IsListed, Is.True);
            Assert.That(marketItem.Name, Is.EqualTo("Test Item"));
            Assert.That(marketItem.Tag, Is.EqualTo("test_tag"));
            Assert.That(marketItem.Resref, Is.EqualTo("item_resref"));
            Assert.That(marketItem.Data, Is.EqualTo("item_data_json"));
            Assert.That(marketItem.Quantity, Is.EqualTo(5));
            Assert.That(marketItem.IconResref, Is.EqualTo("icon_resref"));
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Vibroblade));
            Assert.That(marketItem.DateListed, Is.EqualTo(listedDate));
        }

        [Test]
        public void MarketItem_WithZeroValues_ShouldStoreZeroValues()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 0;
            marketItem.Quantity = 0;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(0));
            Assert.That(marketItem.Quantity, Is.EqualTo(0));
        }

        [Test]
        public void MarketItem_WithNegativeValues_ShouldStoreNegativeValues()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = -100;
            marketItem.Quantity = -5;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(-100));
            Assert.That(marketItem.Quantity, Is.EqualTo(-5));
        }

        [Test]
        public void MarketItem_WithLargeValues_ShouldStoreLargeValues()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 1000000;
            marketItem.Quantity = 1000000;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(1000000));
            Assert.That(marketItem.Quantity, Is.EqualTo(1000000));
        }

        [Test]
        public void MarketItem_WithMaxValues_ShouldStoreMaxValues()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = int.MaxValue;
            marketItem.Quantity = int.MaxValue;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(int.MaxValue));
            Assert.That(marketItem.Quantity, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void MarketItem_WithMinValues_ShouldStoreMinValues()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = int.MinValue;
            marketItem.Quantity = int.MinValue;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(int.MinValue));
            Assert.That(marketItem.Quantity, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void MarketItem_WithEmptyStrings_ShouldStoreEmptyStrings()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.MarketId = "";
            marketItem.MarketName = "";
            marketItem.PlayerId = "";
            marketItem.SellerName = "";
            marketItem.Name = "";
            marketItem.Tag = "";
            marketItem.Resref = "";
            marketItem.Data = "";
            marketItem.IconResref = "";

            // Assert
            Assert.That(marketItem.MarketId, Is.EqualTo(""));
            Assert.That(marketItem.MarketName, Is.EqualTo(""));
            Assert.That(marketItem.PlayerId, Is.EqualTo(""));
            Assert.That(marketItem.SellerName, Is.EqualTo(""));
            Assert.That(marketItem.Name, Is.EqualTo(""));
            Assert.That(marketItem.Tag, Is.EqualTo(""));
            Assert.That(marketItem.Resref, Is.EqualTo(""));
            Assert.That(marketItem.Data, Is.EqualTo(""));
            Assert.That(marketItem.IconResref, Is.EqualTo(""));
        }

        [Test]
        public void MarketItem_WithNullStrings_ShouldStoreNullStrings()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.MarketId = null;
            marketItem.MarketName = null;
            marketItem.PlayerId = null;
            marketItem.SellerName = null;
            marketItem.Name = null;
            marketItem.Tag = null;
            marketItem.Resref = null;
            marketItem.Data = null;
            marketItem.IconResref = null;

            // Assert
            Assert.That(marketItem.MarketId, Is.Null);
            Assert.That(marketItem.MarketName, Is.Null);
            Assert.That(marketItem.PlayerId, Is.Null);
            Assert.That(marketItem.SellerName, Is.Null);
            Assert.That(marketItem.Name, Is.Null);
            Assert.That(marketItem.Tag, Is.Null);
            Assert.That(marketItem.Resref, Is.Null);
            Assert.That(marketItem.Data, Is.Null);
            Assert.That(marketItem.IconResref, Is.Null);
        }

        [Test]
        public void MarketItem_WithSpecialCharacters_ShouldStoreSpecialCharacters()
        {
            // Arrange
            var marketItem = new MarketItem();
            const string specialString = "!@#$%^&*()_+-=[]{}|;':\",./<>?";

            // Act
            marketItem.MarketName = specialString;
            marketItem.SellerName = specialString;
            marketItem.Name = specialString;
            marketItem.Tag = specialString;
            marketItem.Resref = specialString;
            marketItem.Data = specialString;
            marketItem.IconResref = specialString;

            // Assert
            Assert.That(marketItem.MarketName, Is.EqualTo(specialString));
            Assert.That(marketItem.SellerName, Is.EqualTo(specialString));
            Assert.That(marketItem.Name, Is.EqualTo(specialString));
            Assert.That(marketItem.Tag, Is.EqualTo(specialString));
            Assert.That(marketItem.Resref, Is.EqualTo(specialString));
            Assert.That(marketItem.Data, Is.EqualTo(specialString));
            Assert.That(marketItem.IconResref, Is.EqualTo(specialString));
        }

        [Test]
        public void MarketItem_WithLongStrings_ShouldStoreLongStrings()
        {
            // Arrange
            var marketItem = new MarketItem();
            var longString = new string('a', 1000);

            // Act
            marketItem.MarketName = longString;
            marketItem.SellerName = longString;
            marketItem.Name = longString;
            marketItem.Tag = longString;
            marketItem.Resref = longString;
            marketItem.Data = longString;
            marketItem.IconResref = longString;

            // Assert
            Assert.That(marketItem.MarketName, Is.EqualTo(longString));
            Assert.That(marketItem.SellerName, Is.EqualTo(longString));
            Assert.That(marketItem.Name, Is.EqualTo(longString));
            Assert.That(marketItem.Tag, Is.EqualTo(longString));
            Assert.That(marketItem.Resref, Is.EqualTo(longString));
            Assert.That(marketItem.Data, Is.EqualTo(longString));
            Assert.That(marketItem.IconResref, Is.EqualTo(longString));
        }

        [Test]
        public void MarketItem_WithPriceIncrement_ShouldIncrementPrice()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 100;
            marketItem.Price += 50;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(150));
        }

        [Test]
        public void MarketItem_WithPriceDecrement_ShouldDecrementPrice()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 100;
            marketItem.Price -= 25;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(75));
        }

        [Test]
        public void MarketItem_WithQuantityIncrement_ShouldIncrementQuantity()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Quantity = 5;
            marketItem.Quantity++;

            // Assert
            Assert.That(marketItem.Quantity, Is.EqualTo(6));
        }

        [Test]
        public void MarketItem_WithQuantityDecrement_ShouldDecrementQuantity()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Quantity = 5;
            marketItem.Quantity--;

            // Assert
            Assert.That(marketItem.Quantity, Is.EqualTo(4));
        }

        [Test]
        public void MarketItem_WithPriceMultiplication_ShouldMultiplyPrice()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 100;
            marketItem.Price *= 2;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(200));
        }

        [Test]
        public void MarketItem_WithPriceDivision_ShouldDividePrice()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.Price = 200;
            marketItem.Price /= 2;

            // Assert
            Assert.That(marketItem.Price, Is.EqualTo(100));
        }

        [Test]
        public void MarketItem_WithAllCategories_ShouldStoreAllCategories()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act & Assert
            marketItem.Category = MarketCategoryType.Invalid;
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Invalid));

            marketItem.Category = MarketCategoryType.Vibroblade;
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Vibroblade));

            marketItem.Category = MarketCategoryType.Breastplate;
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Breastplate));

            marketItem.Category = MarketCategoryType.Miscellaneous;
            Assert.That(marketItem.Category, Is.EqualTo(MarketCategoryType.Miscellaneous));
        }

        [Test]
        public void MarketItem_WithDateListedInFuture_ShouldStoreFutureDate()
        {
            // Arrange
            var marketItem = new MarketItem();
            var futureDate = DateTime.Now.AddDays(1);

            // Act
            marketItem.DateListed = futureDate;

            // Assert
            Assert.That(marketItem.DateListed, Is.EqualTo(futureDate));
        }

        [Test]
        public void MarketItem_WithDateListedInPast_ShouldStorePastDate()
        {
            // Arrange
            var marketItem = new MarketItem();
            var pastDate = DateTime.Now.AddDays(-30);

            // Act
            marketItem.DateListed = pastDate;

            // Assert
            Assert.That(marketItem.DateListed, Is.EqualTo(pastDate));
        }

        [Test]
        public void MarketItem_WithDateListedNull_ShouldStoreNullDate()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            marketItem.DateListed = null;

            // Assert
            Assert.That(marketItem.DateListed, Is.Null);
        }

        [Test]
        public void MarketItem_WithSerialization_ShouldSerializeCorrectly()
        {
            // Arrange
            var marketItem = new MarketItem();
            marketItem.MarketId = "market-123";
            marketItem.MarketName = "Test Market";
            marketItem.PlayerId = "player-456";
            marketItem.SellerName = "Test Seller";
            marketItem.Price = 1000;
            marketItem.IsListed = true;
            marketItem.Name = "Test Item";
            marketItem.Tag = "test_tag";
            marketItem.Resref = "item_resref";
            marketItem.Data = "item_data_json";
            marketItem.Quantity = 5;
            marketItem.IconResref = "icon_resref";
            marketItem.Category = MarketCategoryType.Vibroblade;
            marketItem.DateListed = DateTime.Now;

            // Act
            var json = System.Text.Json.JsonSerializer.Serialize(marketItem);
            var deserializedItem = System.Text.Json.JsonSerializer.Deserialize<MarketItem>(json);

            // Assert
            Assert.That(deserializedItem, Is.Not.Null);
            Assert.That(deserializedItem.MarketId, Is.EqualTo(marketItem.MarketId));
            Assert.That(deserializedItem.MarketName, Is.EqualTo(marketItem.MarketName));
            Assert.That(deserializedItem.PlayerId, Is.EqualTo(marketItem.PlayerId));
            Assert.That(deserializedItem.SellerName, Is.EqualTo(marketItem.SellerName));
            Assert.That(deserializedItem.Price, Is.EqualTo(marketItem.Price));
            Assert.That(deserializedItem.IsListed, Is.EqualTo(marketItem.IsListed));
            Assert.That(deserializedItem.Name, Is.EqualTo(marketItem.Name));
            Assert.That(deserializedItem.Tag, Is.EqualTo(marketItem.Tag));
            Assert.That(deserializedItem.Resref, Is.EqualTo(marketItem.Resref));
            Assert.That(deserializedItem.Data, Is.EqualTo(marketItem.Data));
            Assert.That(deserializedItem.Quantity, Is.EqualTo(marketItem.Quantity));
            Assert.That(deserializedItem.IconResref, Is.EqualTo(marketItem.IconResref));
            Assert.That(deserializedItem.Category, Is.EqualTo(marketItem.Category));
            Assert.That(deserializedItem.DateListed, Is.EqualTo(marketItem.DateListed).Within(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void MarketItem_WithEquality_ShouldCompareEqualityCorrectly()
        {
            // Arrange
            var marketItem1 = new MarketItem();
            var marketItem2 = new MarketItem();
            marketItem1.Price = 1000;
            marketItem2.Price = 1000;

            // Act & Assert
            Assert.That(marketItem1.Price, Is.EqualTo(marketItem2.Price));
        }

        [Test]
        public void MarketItem_WithInequality_ShouldCompareInequalityCorrectly()
        {
            // Arrange
            var marketItem1 = new MarketItem();
            var marketItem2 = new MarketItem();
            marketItem1.Price = 1000;
            marketItem2.Price = 2000;

            // Act & Assert
            Assert.That(marketItem1.Price, Is.Not.EqualTo(marketItem2.Price));
        }

        [Test]
        public void MarketItem_WithToString_ShouldReturnStringRepresentation()
        {
            // Arrange
            var marketItem = new MarketItem();
            marketItem.Name = "Test Item";

            // Act
            var result = marketItem.ToString();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Empty);
        }

        [Test]
        public void MarketItem_WithGetType_ShouldReturnCorrectType()
        {
            // Arrange
            var marketItem = new MarketItem();

            // Act
            var type = marketItem.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(MarketItem)));
        }

        [Test]
        public void MarketItem_WithHashCode_ShouldReturnHashCode()
        {
            // Arrange
            var marketItem = new MarketItem();
            marketItem.Name = "Test Item";

            // Act
            var hashCode = marketItem.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.Not.EqualTo(0));
        }

        [Test]
        public void MarketItem_WithJsonData_ShouldStoreJsonData()
        {
            // Arrange
            var marketItem = new MarketItem();
            var jsonData = "{\"property1\":\"value1\",\"property2\":123,\"property3\":true}";

            // Act
            marketItem.Data = jsonData;

            // Assert
            Assert.That(marketItem.Data, Is.EqualTo(jsonData));
        }

        [Test]
        public void MarketItem_WithXmlData_ShouldStoreXmlData()
        {
            // Arrange
            var marketItem = new MarketItem();
            var xmlData = "<item><name>Test Item</name><quantity>5</quantity></item>";

            // Act
            marketItem.Data = xmlData;

            // Assert
            Assert.That(marketItem.Data, Is.EqualTo(xmlData));
        }

        [Test]
        public void MarketItem_WithBase64Data_ShouldStoreBase64Data()
        {
            // Arrange
            var marketItem = new MarketItem();
            var base64Data = "SGVsbG8gV29ybGQ="; // "Hello World" in base64

            // Act
            marketItem.Data = base64Data;

            // Assert
            Assert.That(marketItem.Data, Is.EqualTo(base64Data));
        }
    }
}
