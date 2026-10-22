using System.Diagnostics;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class ComposeSecretProjectionTests
{
    private const string DummyToken = "dummy-discord-token";
    private const string DummyPassword = "quoted'\";$\\ Ω ";
    private string image = "";
    private string directory = "";

    [SetUp]
    public void SetUp()
    {
        image = Environment.GetEnvironmentVariable("SWLOR_BOT_TEST_POSTGRES_IMAGE") ?? "";
        if (string.IsNullOrWhiteSpace(image))
            Assert.Ignore("Set SWLOR_BOT_TEST_POSTGRES_IMAGE to an isolated image containing jq to run Compose secret projection tests.");

        directory = Path.Combine(Path.GetTempPath(), "swlor-bot-compose-secret-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "secrets"));
        DirectoryInfo? repository = new(TestContext.CurrentContext.TestDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "SWLOR.DiscordBot", "compose.sh")))
            repository = repository.Parent;
        Assert.That(repository, Is.Not.Null, "The test needs the checked-in Compose wrapper.");
        File.Copy(Path.Combine(repository!.FullName, "SWLOR.DiscordBot", "compose.sh"), Path.Combine(directory, "compose.sh"));
        File.Copy(Path.Combine(repository.FullName, "SWLOR.DiscordBot", "rotate-database-password.sh"), Path.Combine(directory, "rotate-database-password.sh"));
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    [Test]
    public async Task WrapperProjectsOnlyDatabasePasswordAndForwardsComposeArguments()
    {
        await WriteSecretsAsync(JsonSerializer.Serialize(new { discordToken = DummyToken, databasePassword = DummyPassword }));
        var result = await RunWrapperAsync();
        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Output.Trim(), Is.EqualTo("projection-ok"));
        Assert.That(result.Output, Does.Not.Contain(DummyToken).And.Not.Contain(DummyPassword));
    }

    [Test]
    public async Task RotationSendsOnlyProjectedPasswordAndRecreatesServicesInOrder()
    {
        await WriteSecretsAsync(JsonSerializer.Serialize(new { discordToken = DummyToken, databasePassword = DummyPassword }));
        var result = await DockerAsync(RotationDockerStub, "rotate-database-password.sh");
        Assert.That(result.ExitCode, Is.Zero, result.Output);
        Assert.That(result.Output, Does.Contain("Database password rotation completed"));
        Assert.That(result.Output, Does.Not.Contain(DummyToken).And.Not.Contain(DummyPassword));
    }

    [TestCase("compose.sh")]
    [TestCase("rotate-database-password.sh")]
    public async Task MissingJqFailsClearlyBeforeCallingComposeWithoutPrintingSecrets(string script)
    {
        await WriteSecretsAsync(JsonSerializer.Serialize(new { discordToken = DummyToken, databasePassword = DummyPassword }));
        var result = await DockerAsync(MissingJqDockerStub.Replace("__SCRIPT__", script), script);
        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Does.Contain("jq is required on the host"));
        Assert.That(result.Output, Does.Not.Contain("compose-was-called"));
        Assert.That(result.Output, Does.Not.Contain(DummyToken).And.Not.Contain(DummyPassword));
    }

    [TestCase("""{"discordToken":"dummy-discord-token","databasePassword":null}""")]
    [TestCase("""{"discordToken":"dummy-discord-token","databasePassword":"   \t "}""")]
    [TestCase("""{"discordToken":"dummy-discord-token","databasePassword":"bad\npassword"}""")]
    [TestCase("""{"discordToken":"dummy-discord-token","databasePassword":"bad\rpassword"}""")]
    [TestCase("""{"discordToken":"dummy-discord-token","databasePassword":"bad\u0000password"}""")]
    [TestCase("""{"discordToken":"dummy-discord-token","databasePassword":"dummy-database-password"} {}""")]
    [TestCase("not-json")]
    public async Task InvalidSecretsFailWithoutInvokingComposeOrPrintingValues(string json)
    {
        await WriteSecretsAsync(json);
        var result = await RunWrapperAsync();
        Assert.That(result.ExitCode, Is.Not.Zero);
        Assert.That(result.Output, Does.Not.Contain(DummyToken).And.Not.Contain(DummyPassword));
        Assert.That(result.Output, Does.Not.Contain("projection-ok"));
    }

    private Task WriteSecretsAsync(string json) => File.WriteAllTextAsync(
        Path.Combine(directory, "secrets", "secrets.json"), json, new UTF8Encoding(false));

    private Task<(int ExitCode, string Output)> RunWrapperAsync() => DockerAsync(ComposeDockerStub, "compose.sh");

    private const string ComposeDockerStub = """
        mkdir -p /tmp/docker-stub
        cat > /tmp/docker-stub/docker <<'STUB'
        #!/bin/sh
        [ "$(printf '%s' "$SWLOR_BOT_DATABASE_SECRETS" | jq -r -s 'if length == 1 and (.[0] | keys) == ["databasePassword"] then .[0].databasePassword else empty end')" = "$EXPECTED_PASSWORD" ] || exit 91
        case "$(env)" in *dummy-discord-token*) exit 92;; esac
        case "$*" in *dummy-discord-token*) exit 93;; esac
        [ "$*" = 'compose probe --safe' ] || exit 94
        printf '%s\n' projection-ok
        STUB
        chmod 755 /tmp/docker-stub/docker
        PATH=/tmp/docker-stub:$PATH sh /harness/compose.sh probe --safe
        """;

    private const string MissingJqDockerStub = """
        mkdir -p /tmp/no-jq
        cat > /tmp/no-jq/dirname <<'STUB'
        #!/bin/sh
        printf '%s\n' /harness
        STUB
        cat > /tmp/no-jq/docker <<'STUB'
        #!/bin/sh
        printf '%s\n' compose-was-called
        exit 99
        STUB
        chmod 755 /tmp/no-jq/dirname /tmp/no-jq/docker
        PATH=/tmp/no-jq /bin/sh /harness/__SCRIPT__ probe
        """;
    private const string RotationDockerStub = """
        mkdir -p /tmp/docker-stub
        cat > /tmp/docker-stub/docker <<'STUB'
        #!/bin/sh
        set -eu
        projected_password="$(printf '%s' "$SWLOR_BOT_DATABASE_SECRETS" | jq -r -s 'if length == 1 and (.[0] | keys) == ["databasePassword"] then .[0].databasePassword else empty end')"
        [ "$projected_password" = "$EXPECTED_PASSWORD" ] || exit 91
        case "$(env)" in *dummy-discord-token*) exit 92;; esac
        case "$*" in *dummy-discord-token*) exit 93;; esac
        state=/tmp/docker-stub/state
        case "$*" in
          'compose stop bot')
            [ ! -e "$state" ] || exit 94
            printf '%s' stopped > "$state"
            ;;
          'compose exec -T database /usr/local/bin/swlor-postgres-entrypoint.sh rotate-password-stdin')
            [ "$(cat "$state")" = stopped ] || exit 95
            cat > /tmp/docker-stub/rotation-input
            [ "$(wc -l < /tmp/docker-stub/rotation-input | tr -d ' ')" = 1 ] || exit 96
            input_password="$(jq -r -s 'if length == 1 and (.[0] | keys) == ["databasePassword"] then .[0].databasePassword else empty end' /tmp/docker-stub/rotation-input)"
            [ "$input_password" = "$EXPECTED_PASSWORD" ] || exit 97
            printf '%s' password-updated > "$state"
            ;;
          'compose up -d --no-deps --force-recreate --wait --wait-timeout 60 database')
            [ "$(cat "$state")" = password-updated ] || exit 98
            printf '%s' database-recreated > "$state"
            ;;
          'compose up -d --no-deps --force-recreate --wait --wait-timeout 90 bot')
            [ "$(cat "$state")" = database-recreated ] || exit 99
            printf '%s' bot-recreated > "$state"
            ;;
          *) exit 100 ;;
        esac
        STUB
        chmod 755 /tmp/docker-stub/docker
        PATH=/tmp/docker-stub:$PATH sh /harness/rotate-database-password.sh
        [ "$(cat /tmp/docker-stub/state)" = bot-recreated ]
        """;

    private async Task<(int ExitCode, string Output)> DockerAsync(string dockerStub, string script)
    {
        // Normalize shell text so Docker's Linux /bin/sh never receives CRLF literals.
        var command = dockerStub.Replace("\r\n", "\n");
        var start = new ProcessStartInfo("docker")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (var argument in new[]
        {
            "run", "--rm", "--env", "EXPECTED_PASSWORD=" + DummyPassword, "--mount",
            $"type=bind,source={directory},target=/harness,readonly", "--entrypoint", "sh", image, "-c", command
        }) start.ArgumentList.Add(argument);

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Docker did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            return (process.ExitCode, await stdout + await stderr);
        }
        catch
        {
            if (!process.HasExited) process.Kill(true);
            throw;
        }
    }
}