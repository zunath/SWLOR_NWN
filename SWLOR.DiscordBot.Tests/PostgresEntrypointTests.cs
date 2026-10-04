using System.Diagnostics;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace SWLOR.DiscordBot.Tests;

[TestFixture]
public sealed class PostgresEntrypointTests
{
    private string image = "";
    private string directory = "";
    private string container = "";
    private string volume = "";
    private bool volumeCreated;
    private const string OriginalPassword = "isolated-original-password";
    private const string RotatedPassword = " quoted'\";\\$ spaces Ω ";

    [SetUp]
    public async Task SetUp()
    {
        image = Environment.GetEnvironmentVariable("SWLOR_BOT_TEST_POSTGRES_IMAGE") ?? "";
        if (string.IsNullOrWhiteSpace(image))
            Assert.Ignore("Set SWLOR_BOT_TEST_POSTGRES_IMAGE to an isolated PostgreSQL image containing jq to run entrypoint integration tests.");
        var suffix = Guid.NewGuid().ToString("N");
        directory = Path.Combine(Path.GetTempPath(), "swlor-bot-postgres-tests-" + suffix);
        container = "swlor-bot-postgres-test-" + suffix;
        volume = container + "-data";
        try
        {
            Directory.CreateDirectory(directory);
            DirectoryInfo? repository = new(TestContext.CurrentContext.TestDirectory);
            while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "SWLOR.DiscordBot", "postgres-entrypoint.sh")))
                repository = repository.Parent;
            Assert.That(repository, Is.Not.Null, "The integration test needs the checked-in entrypoint.");
            File.Copy(Path.Combine(repository!.FullName, "SWLOR.DiscordBot", "postgres-entrypoint.sh"),
                Path.Combine(directory, "postgres-entrypoint.sh"));
            await WriteSecretAsync(OriginalPassword);
            await RequireAsync(await DockerAsync(null, "volume", "create", volume));
            volumeCreated = true;
            await StartAsync();
        }
        catch { await TearDown(); throw; }
    }
    [TearDown]
    public async Task TearDown()
    {
        if (!string.IsNullOrEmpty(container)) await DockerAsync(null, "rm", "--force", container);
        if (volumeCreated) await RequireAsync(await DockerAsync(null, "volume", "rm", volume));
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory)) Directory.Delete(directory, true);
        container = volume = directory = "";
        volumeCreated = false;
    }

    [Test]
    public async Task RotationChangesStoredPasswordAndPreservesDataAcrossContainerReplacement()
    {
        await RequireAsync(await DockerAsync(null, "exec", container, "psql", "-X", "-U", "swlor_bot_test",
            "-d", "swlor_bot_test", "-c", "CREATE TABLE preserved(value integer); INSERT INTO preserved VALUES (42);"));
        await WriteSecretAsync(RotatedPassword);
        var mismatch = await CheckAsync();
        Assert.That(mismatch.ExitCode, Is.Not.Zero, "Health must authenticate, not merely check server availability.");
        Assert.That(mismatch.Output, Does.Not.Contain(RotatedPassword));
        var rotated = await DockerAsync(JsonSerializer.Serialize(new { databasePassword = RotatedPassword }),
            "exec", "-i", container, "sh", "/harness/postgres-entrypoint.sh", "rotate-password-stdin");
        await RequireAsync(rotated);
        Assert.That(rotated.Output, Does.Not.Contain(RotatedPassword));
        await RequireAsync(await CheckAsync());
        await WriteSecretAsync(OriginalPassword);
        Assert.That((await CheckAsync()).ExitCode, Is.Not.Zero, "The old password must no longer authenticate.");
        await WriteSecretAsync(RotatedPassword);
        await RequireAsync(await DockerAsync(null, "rm", "--force", container));
        await StartAsync();
        var preserved = await DockerAsync(null, "exec", container, "psql", "-X", "-tA", "-U", "swlor_bot_test",
            "-d", "swlor_bot_test", "-c", "SELECT value FROM preserved;");
        await RequireAsync(preserved);
        Assert.That(preserved.Output.Trim(), Is.EqualTo("42"));
    }

    [TestCase("{}")]
    [TestCase("{\"databasePassword\":\"bad\\npassword\"}")]
    [TestCase("{\"databasePassword\":\"new-dummy\"} {}")]
    public async Task InvalidRotationInputFailsWithoutChangingStoredPassword(string json)
    {
        var rejected = await DockerAsync(json, "exec", "-i", container, "sh",
            "/harness/postgres-entrypoint.sh", "rotate-password-stdin");
        Assert.That(rejected.ExitCode, Is.Not.Zero);
        Assert.That(rejected.Output, Does.Not.Contain("bad").And.Not.Contain("new-dummy"));
        await RequireAsync(await CheckAsync());
    }

    private Task WriteSecretAsync(string password) => File.WriteAllTextAsync(
        Path.Combine(directory, "secrets.json"),
        JsonSerializer.Serialize(new { discordToken = "dummy-token", databasePassword = password }), new UTF8Encoding(false));

    private async Task StartAsync()
    {
        await RequireAsync(await DockerAsync(null, "run", "--detach", "--name", container,
            "--mount", $"type=volume,source={volume},target=/var/lib/postgresql/data",
            "--mount", $"type=bind,source={directory},target=/harness,readonly",
            "-e", "POSTGRES_USER=swlor_bot_test", "-e", "POSTGRES_DB=swlor_bot_test",
            "-e", "SWLOR_BOT_SECRETS_FILE=/harness/secrets.json", "--entrypoint", "sh", image,
            "/harness/postgres-entrypoint.sh", "postgres"));
        var deadline = Stopwatch.StartNew();
        do
        {
            if ((await CheckAsync()).ExitCode == 0) return;
            await Task.Delay(250);
        } while (deadline.Elapsed < TimeSpan.FromSeconds(25));
        Assert.Fail("The isolated PostgreSQL entrypoint did not pass authenticated health.");
    }

    private Task<(int ExitCode, string Output)> CheckAsync() => DockerAsync(null, "exec", container,
        "sh", "/harness/postgres-entrypoint.sh", "check-password");

    private static Task RequireAsync((int ExitCode, string Output) result)
    {
        Assert.That(result.ExitCode, Is.Zero, result.Output);
        return Task.CompletedTask;
    }

    private static async Task<(int ExitCode, string Output)> DockerAsync(string? input, params string[] arguments)
    {
        var start = new ProcessStartInfo("docker")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            RedirectStandardInput = input is not null,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        if (input is not null) start.StandardInputEncoding = new UTF8Encoding(false);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Docker did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            if (input is not null)
            {
                await process.StandardInput.WriteAsync(input.AsMemory(), timeout.Token);
                process.StandardInput.Close();
            }
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