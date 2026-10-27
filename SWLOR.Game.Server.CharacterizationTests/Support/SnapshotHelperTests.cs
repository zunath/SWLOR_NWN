using NUnit.Framework;

namespace SWLOR.Game.Server.CharacterizationTests.Support;

/// <summary>Guards the snapshot helper itself, so a later slice can trust a green or red result.</summary>
[NonParallelizable]
public class SnapshotHelperTests
{
    private string? _originalUpdate;
    private string? _originalDirectory;
    private string _directory = string.Empty;

    private enum Sample
    {
        First,
        Second
    }

    [SetUp]
    public void SetUp()
    {
        _originalUpdate = Environment.GetEnvironmentVariable(Snapshot.UpdateVariable);
        _originalDirectory = Environment.GetEnvironmentVariable(Snapshot.DirectoryVariable);
        _directory = Path.Combine(Path.GetTempPath(), "swlor-snapshot-helper-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable(Snapshot.DirectoryVariable, _directory);
        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, null);
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, _originalUpdate);
        Environment.SetEnvironmentVariable(Snapshot.DirectoryVariable, _originalDirectory);
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    [Test]
    public void Render_UsesAFixedLayoutWithOneRowPerLine()
    {
        var table = new SnapshotTable("Sample.Function", "A sample.", "Two rows.",
            new[] { "a", "b" }, new[] { "result" }, 7);
        table.Add(1, "x\"y", new object?[] { 1, 2.5f, null });
        table.Add(-2, "z", new object?[] { true, Sample.Second, float.NaN, new[] { 1, 2 } });

        var expected =
            "{\n" +
            "  \"function\": \"Sample.Function\",\n" +
            "  \"description\": \"A sample.\",\n" +
            "  \"seed\": 7,\n" +
            "  \"inputs\": \"Two rows.\",\n" +
            "  \"rowCount\": 2,\n" +
            "  \"columns\": [\"a\", \"b\", \"=> result\"],\n" +
            "  \"rows\": [\n" +
            "    [1, \"x\\\"y\", [1, 2.5, null]],\n" +
            "    [-2, \"z\", [true, \"Second\", \"NaN\", [1, 2]]]\n" +
            "  ]\n" +
            "}\n";
        Assert.That(SnapshotJson.Render(table), Is.EqualTo(expected));
    }

    [Test]
    public void Cross_VariesTheLastAxisFastestAndSkipsEmptyAxes()
    {
        var rows = Grid.Cross(new[] { 1, 2 }, new[] { 10, 20, 30 }).Select(row => string.Join(",", row)).ToArray();
        Assert.That(rows, Is.EqualTo(new[] { "1,10", "1,20", "1,30", "2,10", "2,20", "2,30" }));
        Assert.That(Grid.Cross(new[] { 1 }, Array.Empty<int>()), Is.Empty);
    }

    [Test]
    public void Verify_FailsWhenTheSnapshotIsMissing_AndWritesItWhenUpdating()
    {
        var ex = Assert.Throws<AssertionException>(() => Snapshot.Verify("Area", "Missing", NewTable(3)));
        Assert.That(ex!.Message, Does.Contain("does not exist").And.Contain(Snapshot.UpdateVariable));

        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, "1");
        Snapshot.Verify("Area", "Missing", NewTable(3));
        Assert.That(File.Exists(Path.Combine(_directory, "Area", "Missing.json")), Is.True);

        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, null);
        Snapshot.Verify("Area", "Missing", NewTable(3));
    }

    [Test]
    public void Verify_ReportsTheChangedRowsAndAcceptsWindowsLineEndings()
    {
        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, "1");
        Snapshot.Verify("Area", "Changed", NewTable(3));
        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, null);

        var path = Path.Combine(_directory, "Area", "Changed.json");
        var original = File.ReadAllText(path);
        File.WriteAllText(path, original.Replace("\n", "\r\n"));
        Snapshot.Verify("Area", "Changed", NewTable(3));

        File.WriteAllText(path, original.Replace("[2, 4]", "[2, 5]"));
        var ex = Assert.Throws<AssertionException>(() => Snapshot.Verify("Area", "Changed", NewTable(3)));
        Assert.That(ex!.Message, Does.Contain("[2, 5]").And.Contain("[2, 4]").And.Contain("only in the snapshot"));
    }

    [Test]
    public void Verify_RejectsAnOutputThatNeverChanges_UnlessAllowed()
    {
        var constant = NewTable(0);
        for (var i = 0; i < 3; i++)
            constant.Add(i, 9);

        var ex = Assert.Throws<AssertionException>(() => Snapshot.Verify("Area", "Constant", constant));
        Assert.That(ex!.Message, Does.Contain("probably not exercised"));

        var allowed = new SnapshotTable("Sample.Function", "A sample.", "Rows.",
            new[] { "input" }, new[] { "output" }) { AllowConstantOutputs = true };
        allowed.Add(1, 9).Add(2, 9);
        Environment.SetEnvironmentVariable(Snapshot.UpdateVariable, "1");
        Snapshot.Verify("Area", "Constant", allowed);
    }

    private static SnapshotTable NewTable(int rows)
    {
        var table = new SnapshotTable("Sample.Double", "Doubles the input.", "Small integers.",
            new[] { "input" }, new[] { "doubled" });
        for (var i = 1; i <= rows; i++)
            table.Add(i, i * 2);
        return table;
    }
}
