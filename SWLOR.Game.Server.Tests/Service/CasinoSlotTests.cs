using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The module data of the casino slot machines. The service's behaviour (payouts, eligibility, spins) is tested on the
/// instance in <c>Domain/Economy/CasinoSlotServiceTests</c>.
/// </summary>
public class CasinoSlotTests
{
    [Test]
    public void EveryCasinoSlotMachineIsEnabledAndBlueprintsStayDecorative()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module", "git")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
        var service = new CasinoSlotService(new InMemoryDBService(), FacadeForwarder.For<IGuiService>());
        string[] resrefs = ["swd_smach01", "swd_smach02", "swd_smach03", "swd_smach04"];
        using var area = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root!.FullName,
            "Module", "git", "pw_ar_nscasino.git.json")));
        var machines = area.RootElement.GetProperty("Placeable List").GetProperty("value")
            .EnumerateArray().Where(p => resrefs.Contains(p.GetProperty("TemplateResRef").GetProperty("value")
                .GetString())).ToArray();
        Assert.That(machines, Has.Length.EqualTo(32));
        foreach (var machine in machines)
        {
            var resref = machine.GetProperty("TemplateResRef").GetProperty("value").GetString()!;
            var tag = machine.GetProperty("Tag").GetProperty("value").GetString()!;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(service.IsCasinoMachine("pw_ar_nscasino", resref, tag), Is.True);
                Assert.That(machine.GetProperty("Useable").GetProperty("value").GetInt32(), Is.EqualTo(1));
                Assert.That(machine.GetProperty("Static").GetProperty("value").GetInt32(), Is.Zero);
                Assert.That(machine.GetProperty("Plot").GetProperty("value").GetInt32(), Is.EqualTo(1));
                Assert.That(machine.GetProperty("OnUsed").GetProperty("value").GetString(), Is.EqualTo("casino_slot_used"));
            }
        }

        foreach (var resref in resrefs)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root.FullName,
                "Module", "utp", $"{resref}.utp.json")));
            Assert.That(blueprint.RootElement.GetProperty("Useable").GetProperty("value").GetInt32(), Is.Zero);
            Assert.That(blueprint.RootElement.GetProperty("OnUsed").GetProperty("value").GetString(), Is.Empty);
        }
    }
}
