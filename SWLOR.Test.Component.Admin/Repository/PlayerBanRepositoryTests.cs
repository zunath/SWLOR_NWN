using NSubstitute;
using SWLOR.Component.Admin.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.Admin.Repository
{
    [TestFixture]
    public class PlayerBanRepositoryTests
    {
        private IDatabaseService _db;
        private PlayerBanRepository _repository;

        [SetUp]
        public void SetUp()
        {
            _db = Substitute.For<IDatabaseService>();
            _repository = new PlayerBanRepository(_db);
        }

        [Test]
        public void GetById_ShouldReturnPlayerBan_WhenFound()
        {
            // Arrange
            var id = "test-id";
            var expectedBan = new PlayerBan { Id = id, CDKey = "test-cdkey" };
            _db.Get<PlayerBan>(id).Returns(expectedBan);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.EqualTo(expectedBan));
            _db.Received(1).Get<PlayerBan>(id);
        }

        [Test]
        public void GetById_ShouldReturnNull_WhenNotFound()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Get<PlayerBan>(id).Returns((PlayerBan)null);

            // Act
            var result = _repository.GetById(id);

            // Assert
            Assert.That(result, Is.Null);
            _db.Received(1).Get<PlayerBan>(id);
        }

        [Test]
        public void GetByCDKey_ShouldReturnPlayerBan_WhenFound()
        {
            // Arrange
            var cdKey = "test-cdkey";
            var expectedBan = new PlayerBan { CDKey = cdKey };
            var query = Arg.Any<DBQuery<PlayerBan>>();
            _db.Search(query).Returns(new[] { expectedBan });

            // Act
            var result = _repository.GetByCDKey(cdKey);

            // Assert
            Assert.That(result, Is.EqualTo(expectedBan));
            _db.Received(1).Search(Arg.Any<DBQuery<PlayerBan>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllPlayerBans()
        {
            // Arrange
            var expectedBans = new[] { new PlayerBan(), new PlayerBan() };
            var query = Arg.Any<DBQuery<PlayerBan>>();
            _db.Search(query).Returns(expectedBans);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedBans));
            _db.Received(1).Search(Arg.Any<DBQuery<PlayerBan>>());
        }

        [Test]
        public void GetCount_ShouldReturnCount()
        {
            // Arrange
            var expectedCount = 3L;
            var query = Arg.Any<DBQuery<PlayerBan>>();
            _db.SearchCount(query).Returns(expectedCount);

            // Act
            var result = _repository.GetCount();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _db.Received(1).SearchCount(Arg.Any<DBQuery<PlayerBan>>());
        }

        [Test]
        public void Save_ShouldCallSet()
        {
            // Arrange
            var ban = new PlayerBan { Id = "test-id" };

            // Act
            _repository.Save(ban);

            // Assert
            _db.Received(1).Set(ban);
        }

        [Test]
        public void Delete_ShouldCallDelete()
        {
            // Arrange
            var id = "test-id";

            // Act
            _repository.Delete(id);

            // Assert
            _db.Received(1).Delete<PlayerBan>(id);
        }

        [Test]
        public void Exists_ShouldReturnTrue_WhenExists()
        {
            // Arrange
            var id = "test-id";
            _db.Exists<PlayerBan>(id).Returns(true);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.True);
            _db.Received(1).Exists<PlayerBan>(id);
        }

        [Test]
        public void Exists_ShouldReturnFalse_WhenNotExists()
        {
            // Arrange
            var id = "non-existent-id";
            _db.Exists<PlayerBan>(id).Returns(false);

            // Act
            var result = _repository.Exists(id);

            // Assert
            Assert.That(result, Is.False);
            _db.Received(1).Exists<PlayerBan>(id);
        }
    }
}
