using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.Properties.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Test.Component.Properties.Repository
{
    [TestFixture]
    public class WorldPropertyCategoryRepositoryTests
    {
        private WorldPropertyCategoryRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new WorldPropertyCategoryRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnWorldPropertyCategory()
        {
            // Arrange
            var categoryId = "test-category-id";
            var expectedCategory = new WorldPropertyCategory { Id = categoryId, ParentPropertyId = "test-property", Name = "Test Category" };
            _databaseService.Get<WorldPropertyCategory>(categoryId).Returns(expectedCategory);

            // Act
            var result = _repository.GetById(categoryId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCategory));
            _databaseService.Received(1).Get<WorldPropertyCategory>(categoryId);
        }

        [Test]
        public void GetByPropertyId_WithValidPropertyId_ShouldReturnWorldPropertyCategories()
        {
            // Arrange
            var propertyId = "test-property-id";
            var expectedCategories = new List<WorldPropertyCategory>
            {
                new WorldPropertyCategory { Id = "cat1", ParentPropertyId = propertyId, Name = "Category 1" },
                new WorldPropertyCategory { Id = "cat2", ParentPropertyId = propertyId, Name = "Category 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyCategory>>()).Returns(expectedCategories);

            // Act
            var result = _repository.GetByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCategories));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyCategory>>());
        }

        [Test]
        public void GetByParentCategoryId_WithValidParentCategoryId_ShouldReturnWorldPropertyCategories()
        {
            // Arrange
            var parentCategoryId = "parent-category-id";
            var expectedCategories = new List<WorldPropertyCategory>
            {
                new WorldPropertyCategory { Id = "cat1", ParentPropertyId = parentCategoryId, Name = "Child Category 1" },
                new WorldPropertyCategory { Id = "cat2", ParentPropertyId = parentCategoryId, Name = "Child Category 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyCategory>>()).Returns(expectedCategories);

            // Act
            var result = _repository.GetByParentCategoryId(parentCategoryId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCategories));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyCategory>>());
        }

        [Test]
        public void Save_WithValidWorldPropertyCategory_ShouldCallDatabaseSet()
        {
            // Arrange
            var category = new WorldPropertyCategory { Id = "test-category", ParentPropertyId = "test-property", Name = "Test Category" };

            // Act
            _repository.Save(category);

            // Assert
            _databaseService.Received(1).Set(category);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var categoryId = "test-category-id";

            // Act
            _repository.Delete(categoryId);

            // Assert
            _databaseService.Received(1).Delete<WorldPropertyCategory>(categoryId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var categoryId = "test-category-id";
            _databaseService.Exists<WorldPropertyCategory>(categoryId).Returns(true);

            // Act
            var result = _repository.Exists(categoryId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<WorldPropertyCategory>(categoryId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var categoryId = "non-existent-category";
            _databaseService.Exists<WorldPropertyCategory>(categoryId).Returns(false);

            // Act
            var result = _repository.Exists(categoryId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<WorldPropertyCategory>(categoryId);
        }
    }
}
