using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Two Bible tools write the Agriculture tab: tools/UpdateCraftingProgressionBible.py (the crafting revamp's, which wrote the
/// first Farming totals) and tools/UpdateAgriculturePerkBible.py (the Farmer, Chef and Angler lines and the crop table).
/// This runs both, in every order, on copies of the repository workbook and fails when either one changes what the other
/// wrote, so rerunning the older tool cannot wipe the Farmer line.
/// </summary>
public class AgriculturePerkBibleToolTests
{
    private const string CraftingTool = "UpdateCraftingProgressionBible.py";
    private const string AgricultureTool = "UpdateAgriculturePerkBible.py";

    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(120);

    private static IEnumerable<TestCaseData> Orders()
    {
        yield return new TestCaseData((object)new[] { AgricultureTool }).SetName("AgricultureTool");
        yield return new TestCaseData((object)new[] { AgricultureTool, AgricultureTool }).SetName("AgricultureTool_Twice");
        yield return new TestCaseData((object)new[] { CraftingTool }).SetName("CraftingTool");
        yield return new TestCaseData((object)new[] { CraftingTool, CraftingTool }).SetName("CraftingTool_Twice");
        yield return new TestCaseData((object)new[] { CraftingTool, AgricultureTool }).SetName("CraftingTool_ThenAgricultureTool");
        yield return new TestCaseData((object)new[] { AgricultureTool, CraftingTool }).SetName("AgricultureTool_ThenCraftingTool");
        yield return new TestCaseData((object)new[] { AgricultureTool, CraftingTool, AgricultureTool }).SetName("AgricultureTool_CraftingTool_AgricultureTool");
    }

    [TestCaseSource(nameof(Orders))]
    public async Task TheAgricultureTabIsTheSameAfterTheToolsRun(string[] tools)
    {
        var root = FindRepositoryRoot();
        var original = ReadEntries(Path.Combine(root, "design", "bible", "SWLOR Design Bible.xlsx"));
        var sheetName = AgricultureSheetEntry(original);
        var folder = Directory.CreateTempSubdirectory("agriculture-bible-");
        try
        {
            var workbook = Path.Combine(folder.FullName, "SWLOR Design Bible.xlsx");
            File.Copy(Path.Combine(root, "design", "bible", "SWLOR Design Bible.xlsx"), workbook);

            foreach (var tool in tools)
                await RunToolAsync(root, tool, workbook);

            var after = ReadEntries(workbook);
            after[sheetName].Should().Equal(original[sheetName],
                $"{string.Join(", then ", tools)} must leave the Agriculture tab as the repository workbook has it");

            // The agriculture tool owns the tab, so on its own it leaves every part of the workbook as it is.
            if (tools.All(tool => tool == AgricultureTool))
            {
                after.Keys.Should().BeEquivalentTo(original.Keys);
                foreach (var (name, content) in original)
                    after[name].Should().Equal(content, name);
            }
        }
        finally
        {
            folder.Delete(true);
        }
    }

    private static async Task RunToolAsync(string root, string tool, string workbook)
    {
        var startInfo = new ProcessStartInfo("python")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-I");
        startInfo.ArgumentList.Add(Path.Combine(root, "tools", tool));
        startInfo.ArgumentList.Add("--workbook");
        startInfo.ArgumentList.Add(workbook);

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Assert.Inconclusive("Python is not installed, so the Bible tools cannot be run.");
            return;
        }

        using (process)
        {
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            try
            {
                await process.WaitForExitAsync().WaitAsync(ToolTimeout);
            }
            catch (TimeoutException)
            {
                process.Kill(true);
                throw;
            }

            process.ExitCode.Should().Be(0, $"{tool} should succeed. Output: {await output} Error: {await error}");
        }
    }

    private static Dictionary<string, byte[]> ReadEntries(string workbook)
    {
        using var archive = ZipFile.OpenRead(workbook);
        return archive.Entries.ToDictionary(entry => entry.FullName, entry =>
        {
            using var stream = entry.Open();
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        });
    }

    private static string AgricultureSheetEntry(Dictionary<string, byte[]> entries)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relationshipNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XDocument Read(string name)
        {
            using var stream = new MemoryStream(entries[name]);
            return XDocument.Load(stream);
        }

        var sheet = Read("xl/workbook.xml").Descendants(ns + "sheet").Single(element => (string)element.Attribute("name") == "Agriculture");
        var relationshipId = (string)sheet.Attribute(relationshipNs + "id")!;
        var target = (string)Read("xl/_rels/workbook.xml.rels").Root!.Elements()
            .Single(element => (string)element.Attribute("Id") == relationshipId).Attribute("Target")!;
        return target.StartsWith('/') ? target.TrimStart('/') : "xl/" + target;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!.FullName;
    }
}
