using NUnit.Framework;
using SWLOR.Shared.Caching.Service;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class InterfaceCacheTests : TestBase
    {
        private Dictionary<string, string> _allItems;
        private Dictionary<string, Dictionary<string, string>> _filteredCaches;
        private Dictionary<string, object> _groupedCaches;
        private Dictionary<string, object> _filteredGroupedCaches;
        private InterfaceCache<string, string> _cache;

        [SetUp]
        public void SetUp()
        {
            _allItems = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" },
                { "key3", "value3" }
            };

            _filteredCaches = new Dictionary<string, Dictionary<string, string>>
            {
                { "Filtered", new Dictionary<string, string>
                    {
                        { "key1", "value1" },
                        { "key2", "value2" }
                    }
                }
            };

            _groupedCaches = new Dictionary<string, object>
            {
                { "Grouped", new Dictionary<int, Dictionary<string, string>>
                    {
                        { 1, new Dictionary<string, string>
                            {
                                { "key1", "value1" }
                            }
                        },
                        { 2, new Dictionary<string, string>
                            {
                                { "key2", "value2" },
                                { "key3", "value3" }
                            }
                        }
                    }
                }
            };

            _filteredGroupedCaches = new Dictionary<string, object>
            {
                { "FilteredGrouped", new Dictionary<int, Dictionary<string, string>>
                    {
                        { 1, new Dictionary<string, string>
                            {
                                { "key1", "value1" }
                            }
                        }
                    }
                }
            };

            _cache = new InterfaceCache<string, string>(
                _allItems, 
                _filteredCaches, 
                _groupedCaches, 
                _filteredGroupedCaches);
        }

        [Test]
        public void AllItems_ShouldReturnAllItems()
        {
            // Act
            var result = _cache.AllItems;

            // Assert
            Assert.That(result, Is.EqualTo(_allItems));
        }

        [Test]
        public void GetFilteredCache_WithExistingName_ShouldReturnFilteredCache()
        {
            // Act
            var result = _cache.GetFilteredCache("Filtered");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ContainsKey("key1"), Is.True);
            Assert.That(result.ContainsKey("key2"), Is.True);
        }

        [Test]
        public void GetFilteredCache_WithNonExistingName_ShouldReturnNull()
        {
            // Act
            var result = _cache.GetFilteredCache("NonExistent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetGroupedCache_WithExistingName_ShouldReturnGroupedCache()
        {
            // Act
            var result = _cache.GetGroupedCache<int>("Grouped");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ContainsKey(1), Is.True);
            Assert.That(result.ContainsKey(2), Is.True);
            Assert.That(result[1].Count, Is.EqualTo(1));
            Assert.That(result[2].Count, Is.EqualTo(2));
        }

        [Test]
        public void GetGroupedCache_WithNonExistingName_ShouldReturnNull()
        {
            // Act
            var result = _cache.GetGroupedCache<int>("NonExistent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetFilteredGroupedCache_WithExistingName_ShouldReturnFilteredGroupedCache()
        {
            // Act
            var result = _cache.GetFilteredGroupedCache<int>("FilteredGrouped");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey(1), Is.True);
            Assert.That(result[1].Count, Is.EqualTo(1));
        }

        [Test]
        public void GetFilteredGroupedCache_WithNonExistingName_ShouldReturnNull()
        {
            // Act
            var result = _cache.GetFilteredGroupedCache<int>("NonExistent");

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
