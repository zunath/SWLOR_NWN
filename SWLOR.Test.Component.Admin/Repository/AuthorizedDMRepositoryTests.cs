using NSubstitute;
using SWLOR.Component.Admin.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Admin.Enums;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.Admin.Repository
{
    [TestFixture]
    public class AuthorizedDMRepositoryTests
    {
        private IDatabaseService _db;
        private AuthorizedDMRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _db = Substitute.For<IDatabaseService>();
            _repository = new AuthorizedDMRepository(_db);
        }

        [Test]
        public void GetById_ShouldReturnAuthorizedDM_WhenFound()
        {
            // Arrange
            var id = "test-id";
            var expectedDM = new AuthorizedDM { Id = id, Name = "Test DM" };
            _db.Get<AuthorizedDM>(id).Returns(expectedDM);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.EqualTo(expectedDM));
            _db.Received(1).Get<AuthorizedDM>(id);
        }

        [Test]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Get<AuthorizedDM>(id).Returns((AuthorizedDM)null);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.Null);
            _db.Received(1).Get<AuthorizedDM>(id);
        }

        [Test]
        public void GetByName_ShouldReturnAuthorizedDM_WhenFound()
        {
            // Arrange
            var name = "Test DM";
            var expectedDM = new AuthorizedDM { Name = name };
            var query = Arg.Any<DBQuery<AuthorizedDM>>();
            _db.Search(query).Returns(new[] { expectedDM });

            // Act
            var result = _repository.GetByName(name);

            // Assert
            Assert.That(result, Is.EqualTo(new[] { expectedDM }));
            _db.Received(1).Search(Arg.Any<DBQuery<AuthorizedDM>>());
        }

        [Test]
        public void GetByCDKey_ShouldReturnAuthorizedDM_WhenFound()
        {
            // Arrange
            var cdKey = "test-cdkey";
            var expectedDM = new AuthorizedDM { CDKey = cdKey };
            var query = Arg.Any<DBQuery<AuthorizedDM>>();
            _db.Search(query).Returns(new[] { expectedDM });

            // Act
            var result = _repository.GetByCDKey(cdKey);

            // Assert
            Assert.That(result, Is.EqualTo(expectedDM));
            _db.Received(1).Search(Arg.Any<DBQuery<AuthorizedDM>>());
        }

        [Test]
        public void GetByAuthorizationLevel_ShouldReturnAuthorizedDMs_WhenFound()
        {
            // Arrange
            var level = AuthorizationLevel.Admin;
            var expectedDMs = new[] { new AuthorizedDM { Authorization = level } };
            var query = Arg.Any<DBQuery<AuthorizedDM>>();
            _db.Search(query).Returns(expectedDMs);

            // Act
            var result = _repository.GetByAuthorizationLevel(level);

            // Assert
            Assert.That(result, Is.EqualTo(expectedDMs));
            _db.Received(1).Search(Arg.Any<DBQuery<AuthorizedDM>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllAuthorizedDMs()
        {
            // Arrange
            var expectedDMs = new[] { new AuthorizedDM(), new AuthorizedDM() };
            var query = Arg.Any<DBQuery<AuthorizedDM>>();
            _db.Search(query).Returns(expectedDMs);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedDMs));
            _db.Received(1).Search(Arg.Any<DBQuery<AuthorizedDM>>());
        }


        [Test]
        public void Save_ShouldCallSet()
        {
            // Arrange
            var dm = new AuthorizedDM { Id = "test-id" };

            // Act
            _repository.Save(dm);

            // Assert
            _db.Received(1).Set(dm);
        }

        [Test]
        public void Delete_ShouldCallDelete()
        {
            // Arrange
            var id = "test-id";

            // Act
            _repository.Delete(id);

            // Assert
            _db.Received(1).Delete<AuthorizedDM>(id);
        }

        [Test]
        public void Exists_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var id = "test-id";
            _db.Exists<AuthorizedDM>(id).Returns(true);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.True);
            _db.Received(1).Exists<AuthorizedDM>(id);
        }

        [Test]
        public void Exists_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Exists<AuthorizedDM>(id).Returns(false);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.False);
            _db.Received(1).Exists<AuthorizedDM>(id);
        }
    }
}
