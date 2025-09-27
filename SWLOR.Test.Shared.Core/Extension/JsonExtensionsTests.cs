using Newtonsoft.Json.Linq;
using SWLOR.Shared.Core.Extension;

namespace SWLOR.Test.Shared.Core.Extension
{
    [TestFixture]
    public class JsonExtensionsTests
    {
        [Test]
        public void FindTokens_WithSimpleObject_ShouldFindMatchingTokens()
        {
            // Arrange
            var json = JObject.Parse("{\"name\": \"John\", \"age\": 30, \"city\": \"New York\"}");
            const string tokenName = "name";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Value<string>(), Is.EqualTo("John"));
        }

        [Test]
        public void FindTokens_WithNestedObject_ShouldFindMatchingTokens()
        {
            // Arrange
            var json = JObject.Parse("{\"person\": {\"name\": \"John\", \"age\": 30}, \"company\": {\"name\": \"Acme Corp\"}}");
            const string tokenName = "name";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Value<string>(), Is.EqualTo("John"));
            Assert.That(result[1].Value<string>(), Is.EqualTo("Acme Corp"));
        }

        [Test]
        public void FindTokens_WithArray_ShouldFindMatchingTokens()
        {
            // Arrange
            var json = JArray.Parse("[{\"name\": \"John\"}, {\"name\": \"Jane\"}, {\"age\": 25}]");
            const string tokenName = "name";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Value<string>(), Is.EqualTo("John"));
            Assert.That(result[1].Value<string>(), Is.EqualTo("Jane"));
        }

        [Test]
        public void FindTokens_WithComplexNestedStructure_ShouldFindMatchingTokens()
        {
            // Arrange
            var json = JObject.Parse("{\"users\": [{\"name\": \"John\", \"address\": {\"city\": \"New York\"}}], \"metadata\": {\"name\": \"UserData\"}}");
            const string tokenName = "name";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Value<string>(), Is.EqualTo("John"));
            Assert.That(result[1].Value<string>(), Is.EqualTo("UserData"));
        }

        [Test]
        public void FindTokens_WithNonExistentToken_ShouldReturnEmptyList()
        {
            // Arrange
            var json = JObject.Parse("{\"name\": \"John\", \"age\": 30}");
            const string tokenName = "nonexistent";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void FindTokens_WithEmptyObject_ShouldReturnEmptyList()
        {
            // Arrange
            var json = JObject.Parse("{}");
            const string tokenName = "name";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void FindTokens_WithEmptyArray_ShouldReturnEmptyList()
        {
            // Arrange
            var json = JArray.Parse("[]");
            const string tokenName = "name";

            // Act
            var result = json.FindTokens(tokenName);

            // Assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void FindTokens_WithNullToken_ShouldThrowException()
        {
            // Arrange
            JToken json = null;
            const string tokenName = "name";

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => json.FindTokens(tokenName));
        }

        [Test]
        public void Rename_WithValidProperty_ShouldRenameProperty()
        {
            // Arrange
            var json = JObject.Parse("{\"oldName\": \"value\"}");
            var property = json["oldName"];

            // Act
            property.Rename("newName");

            // Assert
            Assert.That(json["newName"], Is.Not.Null);
            Assert.That(json["newName"].Value<string>(), Is.EqualTo("value"));
            Assert.That(json["oldName"], Is.Null);
        }

        [Test]
        public void Rename_WithNestedProperty_ShouldRenameProperty()
        {
            // Arrange
            var json = JObject.Parse("{\"parent\": {\"oldName\": \"value\"}}");
            var property = json["parent"]["oldName"];

            // Act
            property.Rename("newName");

            // Assert
            Assert.That(json["parent"]["newName"], Is.Not.Null);
            Assert.That(json["parent"]["newName"].Value<string>(), Is.EqualTo("value"));
            Assert.That(json["parent"]["oldName"], Is.Null);
        }

        [Test]
        public void Rename_WithArrayProperty_ShouldRenameProperty()
        {
            // Arrange
            var json = JArray.Parse("[{\"oldName\": \"value\"}]");
            var property = json[0]["oldName"];

            // Act
            property.Rename("newName");

            // Assert
            Assert.That(json[0]["newName"], Is.Not.Null);
            Assert.That(json[0]["newName"].Value<string>(), Is.EqualTo("value"));
            Assert.That(json[0]["oldName"], Is.Null);
        }

        [Test]
        public void Rename_WithNullToken_ShouldThrowArgumentNullException()
        {
            // Arrange
            JToken token = null;
            const string newName = "newName";

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => token.Rename(newName));
        }

        [Test]
        public void Rename_WithTokenWithoutParent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var json = JObject.Parse("{\"name\": \"value\"}");
            var property = json["name"];
            json.Remove("name"); // Remove from parent

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => property.Rename("newName"));
        }

        [Test]
        public void Rename_WithTokenWithNonPropertyParent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var json = JArray.Parse("[\"value\"]");
            var token = json[0];

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => token.Rename("newName"));
        }

        [Test]
        public void Rename_WithEmptyStringName_ShouldRenameProperty()
        {
            // Arrange
            var json = JObject.Parse("{\"oldName\": \"value\"}");
            var property = json["oldName"];

            // Act
            property.Rename("");

            // Assert
            Assert.That(json[""], Is.Not.Null);
            Assert.That(json[""].Value<string>(), Is.EqualTo("value"));
            Assert.That(json["oldName"], Is.Null);
        }

        [Test]
        public void Rename_WithSameName_ShouldNotChangeProperty()
        {
            // Arrange
            var json = JObject.Parse("{\"name\": \"value\"}");
            var property = json["name"];

            // Act
            property.Rename("name");

            // Assert
            Assert.That(json["name"], Is.Not.Null);
            Assert.That(json["name"].Value<string>(), Is.EqualTo("value"));
        }

        [Test]
        public void Rename_WithSpecialCharacters_ShouldRenameProperty()
        {
            // Arrange
            var json = JObject.Parse("{\"oldName\": \"value\"}");
            var property = json["oldName"];

            // Act
            property.Rename("new-name_with.special@chars");

            // Assert
            Assert.That(json["new-name_with.special@chars"], Is.Not.Null);
            Assert.That(json["new-name_with.special@chars"].Value<string>(), Is.EqualTo("value"));
            Assert.That(json["oldName"], Is.Null);
        }

        [Test]
        public void Rename_WithUnicodeCharacters_ShouldRenameProperty()
        {
            // Arrange
            var json = JObject.Parse("{\"oldName\": \"value\"}");
            var property = json["oldName"];

            // Act
            property.Rename("新名称");

            // Assert
            Assert.That(json["新名称"], Is.Not.Null);
            Assert.That(json["新名称"].Value<string>(), Is.EqualTo("value"));
            Assert.That(json["oldName"], Is.Null);
        }
    }
}
