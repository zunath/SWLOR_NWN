using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using AuthorizationLevel = SWLOR.Game.Server.Enumeration.AuthorizationLevel;

namespace SWLOR.Game.Server.Tests.Domain.Core;

/// <summary>
/// Unit tests of the authorization instance: the engine is a recording fake that answers the player's public CD key, and
/// the database an in-memory one (which answers a search with every stored entity, so the tests store only the entries
/// the query would match and assert on the recorded query).
/// </summary>
[TestFixture]
public class AuthorizationServiceTests
{
    private const uint Player = 1001;
    private const string CdKey = "ABCD1234";
    private const string SuperAdminVariable = "SWLOR_SUPER_ADMIN_CD_KEY";

    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;
    private InMemoryDBService _db = null!;
    private AuthorizationService _service = null!;
    private string? _originalSuperAdmin;

    [SetUp]
    public void SetUp()
    {
        _originalSuperAdmin = Environment.GetEnvironmentVariable(SuperAdminVariable);
        Environment.SetEnvironmentVariable(SuperAdminVariable, null);
        _engine = new FakeNWScript();
        _engine.SetReturn("GetPCPublicCDKey", CdKey);
        _engineScope = NWScriptRuntime.Override(_engine);
        _db = new InMemoryDBService();
        _service = new AuthorizationService(_db);
    }

    [TearDown]
    public void TearDown()
    {
        _engineScope.Dispose();
        Environment.SetEnvironmentVariable(SuperAdminVariable, _originalSuperAdmin);
    }

    private static AuthorizedDM Entry(AuthorizationLevel level) =>
        new() { Id = Guid.NewGuid().ToString(), Name = "A DM", CDKey = CdKey, Authorization = level };

    [Test]
    public void A_player_with_no_entry_is_a_player_and_the_database_is_asked_for_their_cd_key()
    {
        _service.GetAuthorizationLevel(Player).Should().Be(AuthorizationLevel.Player);

        _db.Queries.Should().ContainSingle();
        var query = ((DBQuery<AuthorizedDM>)_db.Queries[0]).BuildQuery();
        query.QueryString.Should().Be("@EntityType:\"AuthorizedDM\" @CDKey:" + CdKey);
        _engine.CallsTo("GetPCPublicCDKey").Should().ContainSingle().Which.Arguments[0].Should().Be(Player);
    }

    [TestCase(AuthorizationLevel.DM)]
    [TestCase(AuthorizationLevel.Admin)]
    public void A_stored_entry_gives_its_level(AuthorizationLevel level)
    {
        _db.With(Entry(level));

        _service.GetAuthorizationLevel(Player).Should().Be(level);
    }

    [Test]
    public void The_super_admin_key_from_the_environment_is_an_admin_without_asking_the_database()
    {
        Environment.SetEnvironmentVariable(SuperAdminVariable, CdKey);

        _service.GetAuthorizationLevel(Player).Should().Be(AuthorizationLevel.Admin);

        _db.Queries.Should().BeEmpty("the environment variable settles it");
    }

    [Test]
    public void The_super_admin_key_of_somebody_else_does_not_make_a_player_an_admin()
    {
        Environment.SetEnvironmentVariable(SuperAdminVariable, "SOMEONE1");
        _db.With(Entry(AuthorizationLevel.DM));

        _service.GetAuthorizationLevel(Player).Should().Be(AuthorizationLevel.DM, "the stored entry still applies");
        _db.Queries.Should().ContainSingle();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void A_blank_super_admin_variable_is_ignored(string value)
    {
        Environment.SetEnvironmentVariable(SuperAdminVariable, value);

        _service.GetAuthorizationLevel(Player).Should().Be(AuthorizationLevel.Player);
        _db.Queries.Should().ContainSingle();
    }
}
