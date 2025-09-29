using NUnit.Framework;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class EnumCacheTests : TestBase
    {
        private Dictionary<PlanetType, PlanetAttribute> _allItems;
        private Dictionary<string, Dictionary<PlanetType, PlanetAttribute>> _filteredCaches;
        private Dictionary<string, object> _groupedCaches;
        private Dictionary<string, object> _filteredGroupedCaches;
        private EnumCache<PlanetType, PlanetAttribute> _cache;

        [SetUp]
        public void SetUp()
        {
            _allItems = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) },
                { PlanetType.Tatooine, new PlanetAttribute("Tatooine", "Tatooine - ", "Tatooine_Orbit", "TATOOINE_LANDING", 400, true) },
                { PlanetType.Invalid, new PlanetAttribute("Invalid", "-- Invalid --", "", "", 0, false) }
            };

            _filteredCaches = new Dictionary<string, Dictionary<PlanetType, PlanetAttribute>>
            {
                { "Active", new Dictionary<PlanetType, PlanetAttribute>
                    {
                        { PlanetType.Viscara, _allItems[PlanetType.Viscara] },
                        { PlanetType.Tatooine, _allItems[PlanetType.Tatooine] }
                    }
                }
            };

            _groupedCaches = new Dictionary<string, object>
            {
                { "ByActive", new Dictionary<bool, Dictionary<PlanetType, PlanetAttribute>>
                    {
                        { true, new Dictionary<PlanetType, PlanetAttribute>
                            {
                                { PlanetType.Viscara, _allItems[PlanetType.Viscara] },
                                { PlanetType.Tatooine, _allItems[PlanetType.Tatooine] }
                            }
                        },
                        { false, new Dictionary<PlanetType, PlanetAttribute>
                            {
                                { PlanetType.Invalid, _allItems[PlanetType.Invalid] }
                            }
                        }
                    }
                }
            };

            _filteredGroupedCaches = new Dictionary<string, object>
            {
                { "ActiveByActive", new Dictionary<bool, Dictionary<PlanetType, PlanetAttribute>>
                    {
                        { true, new Dictionary<PlanetType, PlanetAttribute>
                            {
                                { PlanetType.Viscara, _allItems[PlanetType.Viscara] },
                                { PlanetType.Tatooine, _allItems[PlanetType.Tatooine] }
                            }
                        }
                    }
                }
            };

            _cache = new EnumCache<PlanetType, PlanetAttribute>(
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
            var result = _cache.GetFilteredCache("Active");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ContainsKey(PlanetType.Viscara), Is.True);
            Assert.That(result.ContainsKey(PlanetType.Tatooine), Is.True);
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
            var result = _cache.GetGroupedCache<bool>("ByActive");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.ContainsKey(true), Is.True);
            Assert.That(result.ContainsKey(false), Is.True);
            Assert.That(result[true].Count, Is.EqualTo(2));
            Assert.That(result[false].Count, Is.EqualTo(1));
        }

        [Test]
        public void GetGroupedCache_WithNonExistingName_ShouldReturnNull()
        {
            // Act
            var result = _cache.GetGroupedCache<bool>("NonExistent");

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetFilteredGroupedCache_WithExistingName_ShouldReturnFilteredGroupedCache()
        {
            // Act
            var result = _cache.GetFilteredGroupedCache<bool>("ActiveByActive");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.ContainsKey(true), Is.True);
            Assert.That(result[true].Count, Is.EqualTo(2));
        }

        [Test]
        public void GetFilteredGroupedCache_WithNonExistingName_ShouldReturnNull()
        {
            // Act
            var result = _cache.GetFilteredGroupedCache<bool>("NonExistent");

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
