using NUnit.Framework;
using SWLOR.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterCliTests
{
    private static (int Exit, string Output, string Error) Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = RecipeParameterCli.Run(args, output, error);
        return (exit, output.ToString(), error.ToString());
    }

    [Test]
    public void WithoutACommand_ItPrintsTheUsage()
    {
        var (exit, _, error) = Run();

        Assert.That(exit, Is.EqualTo(2));
        Assert.That(error, Does.Contain("Usage:").And.Contain("write <Key>").And.Contain("export <out.json>"));
    }

    [Test]
    public void List_Succeeds()
    {
        Assert.That(Run("list").Exit, Is.Zero);
    }

    [Test]
    public void Write_RefusesAnUnknownProfile()
    {
        var (exit, _, error) = Run("write", "NoSuchMinigame");

        Assert.That(exit, Is.EqualTo(2));
        Assert.That(error, Does.Contain("No profile named 'NoSuchMinigame'"));
    }

    [Test]
    public void Write_RefusesAMissingProfileAndUnknownOptions()
    {
        Assert.That(Run("write").Exit, Is.EqualTo(2));
        Assert.That(Run("write", "Anything", "--bogus").Exit, Is.EqualTo(2));
    }

    [Test]
    public void Export_NeedsAnOutputPathAndARegisteredProfile()
    {
        Assert.That(Run("export").Exit, Is.EqualTo(2));
        Assert.That(Run("export", Path.Combine(Path.GetTempPath(), "never-written.json"), "NoSuchMinigame").Exit, Is.EqualTo(2));
    }

    [Test]
    public void AnUnknownCommand_PrintsTheUsage()
    {
        Assert.That(Run("frobnicate").Exit, Is.EqualTo(2));
    }
}
