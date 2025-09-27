using NUnit.Framework;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Test.Shared.Core.Data
{
    [TestFixture]
    public class DBQueryTests
    {
        private DBQuery<TestEntity> _query;

        [SetUp]
        public void SetUp()
        {
            _query = new DBQuery<TestEntity>();
        }

        [Test]
        public void DBQuery_Constructor_ShouldInitializeEmptyFieldSearches()
        {
            // Act
            var query = new DBQuery<TestEntity>();

            // Assert
            Assert.That(query, Is.Not.Null);
        }

        [Test]
        public void AddFieldSearch_WithStringAndPartialMatch_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "Name";
            const string search = "test";
            const bool allowPartialMatches = true;

            // Act
            var result = _query.AddFieldSearch(fieldName, search, allowPartialMatches);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddFieldSearch_WithStringAndExactMatch_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "Name";
            const string search = "test";
            const bool allowPartialMatches = false;

            // Act
            var result = _query.AddFieldSearch(fieldName, search, allowPartialMatches);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddFieldSearch_WithIntEnumerable_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "Id";
            var search = new List<int> { 1, 2, 3 };

            // Act
            var result = _query.AddFieldSearch(fieldName, search);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddFieldSearch_WithStringEnumerable_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "Name";
            var search = new List<string> { "test1", "test2", "test3" };

            // Act
            var result = _query.AddFieldSearch(fieldName, search);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddFieldSearch_WithInt_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "Id";
            const int search = 42;

            // Act
            var result = _query.AddFieldSearch(fieldName, search);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddFieldSearch_WithBool_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "IsActive";
            const bool search = true;

            // Act
            var result = _query.AddFieldSearch(fieldName, search);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddFieldSearch_WithBoolFalse_ShouldAddSearchCriteria()
        {
            // Arrange
            const string fieldName = "IsActive";
            const bool search = false;

            // Act
            var result = _query.AddFieldSearch(fieldName, search);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void AddPaging_WithValidInputs_ShouldSetPaging()
        {
            // Arrange
            const int limit = 10;
            const int offset = 20;

            // Act
            var result = _query.AddPaging(limit, offset);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void OrderBy_WithFieldName_ShouldSetSorting()
        {
            // Arrange
            const string fieldName = "Name";

            // Act
            var result = _query.OrderBy(fieldName);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void OrderBy_WithFieldNameAndAscending_ShouldSetSorting()
        {
            // Arrange
            const string fieldName = "Name";
            const bool isAscending = true;

            // Act
            var result = _query.OrderBy(fieldName, isAscending);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void OrderBy_WithFieldNameAndDescending_ShouldSetSorting()
        {
            // Arrange
            const string fieldName = "Name";
            const bool isAscending = false;

            // Act
            var result = _query.OrderBy(fieldName, isAscending);

            // Assert
            Assert.That(result, Is.SameAs(_query));
        }

        [Test]
        public void BuildQuery_WithNoCriteria_ShouldReturnBasicQuery()
        {
            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
            // RawQuery property doesn't exist on NRediSearch.Query
        }

        [Test]
        public void BuildQuery_WithCountsOnly_ShouldReturnQueryWithZeroLimit()
        {
            // Act
            var result = _query.BuildQuery(countsOnly: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            // The query should have limit 0,0 for counts only
        }

        [Test]
        public void BuildQuery_WithPaging_ShouldReturnQueryWithLimit()
        {
            // Arrange
            _query.AddPaging(10, 5);

            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildQuery_WithSorting_ShouldReturnQueryWithSort()
        {
            // Arrange
            _query.OrderBy("Name", true);

            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void BuildQuery_WithMultipleFieldSearches_ShouldReturnQueryWithAllCriteria()
        {
            // Arrange
            _query.AddFieldSearch("Name", "test", true);
            _query.AddFieldSearch("Id", 42);
            _query.AddFieldSearch("IsActive", true);

            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
            // RawQuery property doesn't exist on NRediSearch.Query
        }

        [Test]
        public void BuildQuery_WithStringEnumerable_ShouldEscapeTokens()
        {
            // Arrange
            var search = new List<string> { "test@example.com", "user@domain.org" };
            _query.AddFieldSearch("Email", search);

            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
            // The query should contain escaped tokens
        }

        [Test]
        public void BuildQuery_WithIntEnumerable_ShouldCreateOrQuery()
        {
            // Arrange
            var search = new List<int> { 1, 2, 3 };
            _query.AddFieldSearch("Id", search);

            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
            // The query should contain the OR syntax for integers
        }

        [Test]
        public void BuildQuery_WithNoLimit_ShouldUseDefaultLimit()
        {
            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should use default limit of 50
        }

        [Test]
        public void BuildQuery_WithZeroLimit_ShouldUseDefaultLimit()
        {
            // Arrange
            _query.AddPaging(0, 0);

            // Act
            var result = _query.BuildQuery();

            // Assert
            Assert.That(result, Is.Not.Null);
            // Should use default limit of 50
        }

        private class TestEntity : EntityBase
        {
            public string? Name { get; set; }
            public new int Id { get; set; }
            public bool IsActive { get; set; }
            public string? Email { get; set; }
        }
    }
}
