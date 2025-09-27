using SWLOR.Shared.Core.Extension;

namespace SWLOR.Test.Shared.Core.Extension
{
    [TestFixture]
    public class CollectionExtensionsTests
    {
        [Test]
        public void InsertOrdered_WithEmptyList_ShouldInsertItem()
        {
            // Arrange
            var list = new List<int>();
            const int item = 5;

            // Act
            list.InsertOrdered(item);

            // Assert
            Assert.That(list.Count, Is.EqualTo(1));
            Assert.That(list[0], Is.EqualTo(item));
        }

        [Test]
        public void InsertOrdered_WithSingleItem_ShouldInsertInCorrectOrder()
        {
            // Arrange
            var list = new List<int> { 3 };
            const int item = 5;

            // Act
            list.InsertOrdered(item);

            // Assert
            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0], Is.EqualTo(3));
            Assert.That(list[1], Is.EqualTo(5));
        }

        [Test]
        public void InsertOrdered_WithMultipleItems_ShouldInsertInCorrectOrder()
        {
            // Arrange
            var list = new List<int> { 1, 3, 7, 9 };
            const int item = 5;

            // Act
            list.InsertOrdered(item);

            // Assert
            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list[2], Is.EqualTo(5));
        }

        [Test]
        public void InsertOrdered_WithDuplicateItem_ShouldInsertAtCorrectPosition()
        {
            // Arrange
            var list = new List<int> { 1, 3, 5, 7 };
            const int item = 5;

            // Act
            list.InsertOrdered(item);

            // Assert
            Assert.That(list.Count, Is.EqualTo(5));
            Assert.That(list[2], Is.EqualTo(5));
            Assert.That(list[3], Is.EqualTo(5));
        }

        [Test]
        public void InsertOrdered_WithCustomComparer_ShouldUseComparer()
        {
            // Arrange
            var list = new List<string> { "apple", "banana" }; // Sorted list
            const string item = "cherry";
            var comparer = StringComparer.Ordinal;

            // Act
            list.InsertOrdered(item, comparer);

            // Assert
            Assert.That(list.Count, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo("apple"));
            Assert.That(list[1], Is.EqualTo("banana"));
            Assert.That(list[2], Is.EqualTo("cherry"));
        }

        [Test]
        public void AddElement_WithNewKey_ShouldCreateNewList()
        {
            // Arrange
            var dictionary = new Dictionary<string, List<int>>();
            const string key = "test";
            const int value = 42;

            // Act
            dictionary.AddElement(key, value);

            // Assert
            Assert.That(dictionary.ContainsKey(key), Is.True);
            Assert.That(dictionary[key].Count, Is.EqualTo(1));
            Assert.That(dictionary[key][0], Is.EqualTo(value));
        }

        [Test]
        public void AddElement_WithExistingKey_ShouldAddToExistingList()
        {
            // Arrange
            var dictionary = new Dictionary<string, List<int>>
            {
                { "test", new List<int> { 1, 2, 3 } }
            };
            const string key = "test";
            const int value = 42;

            // Act
            dictionary.AddElement(key, value);

            // Assert
            Assert.That(dictionary[key].Count, Is.EqualTo(4));
            Assert.That(dictionary[key][3], Is.EqualTo(value));
        }

        [Test]
        public void AddElement_WithMultipleValues_ShouldAddAllValues()
        {
            // Arrange
            var dictionary = new Dictionary<string, List<string>>();
            const string key = "test";

            // Act
            dictionary.AddElement(key, "first");
            dictionary.AddElement(key, "second");
            dictionary.AddElement(key, "third");

            // Assert
            Assert.That(dictionary[key].Count, Is.EqualTo(3));
            Assert.That(dictionary[key], Contains.Item("first"));
            Assert.That(dictionary[key], Contains.Item("second"));
            Assert.That(dictionary[key], Contains.Item("third"));
        }

        [Test]
        public void SafeGet_WithExistingKey_ShouldReturnValue()
        {
            // Arrange
            var dictionary = new Dictionary<string, int>
            {
                { "key1", 42 },
                { "key2", 100 }
            };

            // Act
            var result = dictionary.SafeGet("key1");

            // Assert
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void SafeGet_WithNonExistingKey_ShouldReturnDefault()
        {
            // Arrange
            var dictionary = new Dictionary<string, int>
            {
                { "key1", 42 }
            };

            // Act
            var result = dictionary.SafeGet("nonexistent");

            // Assert
            Assert.That(result, Is.EqualTo(0)); // default for int
        }

        [Test]
        public void SafeGet_WithStringDictionary_ShouldReturnDefaultForNonExistingKey()
        {
            // Arrange
            var dictionary = new Dictionary<string, string>
            {
                { "key1", "value1" }
            };

            // Act
            var result = dictionary.SafeGet("nonexistent");

            // Assert
            Assert.That(result, Is.Null); // default for string
        }

        [Test]
        public void SafeGet_WithReferenceType_ShouldReturnDefaultForNonExistingKey()
        {
            // Arrange
            var dictionary = new Dictionary<string, List<int>>
            {
                { "key1", new List<int> { 1, 2, 3 } }
            };

            // Act
            var result = dictionary.SafeGet("nonexistent");

            // Assert
            Assert.That(result, Is.Null); // default for reference type
        }
    }
}
