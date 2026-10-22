using System.Text.Json;
using Npgsql;
using NUnit.Framework;
using SWLOR.DiscordBot.Hosting;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class BotSecretsTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), "swlor-bot-secrets-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_directory, recursive: true);

    [Test]
    public void OneJsonFileSuppliesBothCredentialsAndPreservesPasswordPunctuation()
    {
        const string password = " spaces;=\"quoted\"\\backslash$ ";
        var secrets = Load(new { discordToken = "dummy-token", databasePassword = password });
        var connection = new NpgsqlConnectionStringBuilder(secrets.Database);
        Assert.That(secrets.Token, Is.EqualTo("dummy-token"));
        Assert.That(connection.Password, Is.EqualTo(password));
        Assert.That(connection.IncludeErrorDetail, Is.False);
        Assert.That(connection.Timeout, Is.EqualTo(15));
        Assert.That(connection.CommandTimeout, Is.EqualTo(30));
    }

    [Test]
    public void ExternalDatabaseTlsSettingsRemainInSameFileAndErrorDetailIsDisabled()
    {
        var secrets = Load(new
        {
            discordToken = "dummy-token",
            databasePassword = "dummy-password",
            databaseConnectionString = "Host=db.example.test;Database=bot;Username=bot;SSL Mode=VerifyFull;Root Certificate=/certs/root.crt;Include Error Detail=true"
        });
        var connection = new NpgsqlConnectionStringBuilder(secrets.Database);
        Assert.That(connection.Host, Is.EqualTo("db.example.test"));
        Assert.That(connection.SslMode, Is.EqualTo(SslMode.VerifyFull));
        Assert.That(connection.RootCertificate, Is.EqualTo("/certs/root.crt"));
        Assert.That(connection.Password, Is.EqualTo("dummy-password"));
        Assert.That(connection.IncludeErrorDetail, Is.False);
    }

    [TestCase("{}")]
    [TestCase("[]")]
    [TestCase("null")]
    [TestCase("""{"discordToken":"token"}""")]
    [TestCase("""{"discordToken":"token","databasePassword":true}""")]
    [TestCase("""{"discordToken":"token","databasePassword":"   "}""")]
    [TestCase("""{"discordToken":" ","databasePassword":"password"}""")]
    [TestCase("""{"discordToken":"token","databasePassword":"password","extra":"unrecognized"}""")]
    [TestCase("""{"DiscordToken":"token","databasePassword":"password"}""")]
    [TestCase("""{"discordToken":"token","discordToken":"other","databasePassword":"password"}""")]
    [TestCase("""{"discordToken":"token","databasePassword":"password","databaseConnectionString":null}""")]
    [TestCase("""{"discordToken":"token","databasePassword":"password","databaseConnectionString":""}""")]
    public void InvalidSchemaFailsClosed(string json)
    {
        Assert.That(() => LoadJson(json), Throws.TypeOf<InvalidOperationException>());
    }

    [TestCase("\n")]
    [TestCase("\r")]
    [TestCase("\0")]
    public void RejectsPasswordCharactersThatPostgresWrapperCannotPreserve(string character)
    {
        Assert.That(() => Load(new { discordToken = "token", databasePassword = "before" + character + "after" }),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contains("line breaks or NUL"));
    }

    [TestCase("token value")]
    [TestCase("token\n")]
    [TestCase("token\0")]
    public void RejectsInvalidDiscordToken(string token)
    {
        Assert.That(() => Load(new { discordToken = token, databasePassword = "password" }),
            Throws.TypeOf<InvalidOperationException>());
    }

    [TestCase("Password=embedded-secret")]
    [TestCase("Pwd=embedded-secret")]
    public void RejectsSecondDatabasePasswordLocation(string passwordPart)
    {
        Assert.That(() => Load(new
        {
            discordToken = "token", databasePassword = "password",
            databaseConnectionString = "Host=database;" + passwordPart
        }), Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Use databasePassword"));
    }

    [Test]
    public void EmptyConnectionPasswordIsNormalizedAwayAndFilePasswordWins()
    {
        var secrets = Load(new
        {
            discordToken = "token", databasePassword = "file-password",
            databaseConnectionString = "Host=database;Password="
        });
        Assert.That(new NpgsqlConnectionStringBuilder(secrets.Database).Password, Is.EqualTo("file-password"));
    }

    [Test]
    public void MalformedJsonDiagnosticDoesNotExposeItsContents()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => LoadJson("{\"discordToken\": SENSITIVE_DUMMY"));
        Assert.That(exception!.ToString(), Does.Not.Contain("SENSITIVE_DUMMY"));
    }

    [Test]
    public void MalformedConnectionDiagnosticDoesNotExposeItsContents()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Load(new
        {
            discordToken = "token", databasePassword = "password",
            databaseConnectionString = "Host=database;Port=SENSITIVE_DUMMY"
        }));
        Assert.That(exception!.ToString(), Does.Not.Contain("SENSITIVE_DUMMY"));
    }

    private BotSecrets Load(object values) => LoadJson(JsonSerializer.Serialize(values));

    private BotSecrets LoadJson(string json)
    {
        var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, json);
        return BotSecrets.Load(path);
    }
}
