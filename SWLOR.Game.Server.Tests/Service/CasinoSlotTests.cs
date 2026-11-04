using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class CasinoSlotTests
{
    [Test]
    public void PayoutsRetainTheHouseEdge()
    {
        var payouts = Enumerable.Range(0, 10000).Select(CasinoSlot.GetPayout).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(payouts.Count(p => p == 0), Is.EqualTo(6919));
            Assert.That(payouts.Count(p => p == 100), Is.EqualTo(2000));
            Assert.That(payouts.Count(p => p == 500), Is.EqualTo(800));
            Assert.That(payouts.Count(p => p == 1000), Is.EqualTo(280));
            Assert.That(payouts.Count(p => p == 10000), Is.EqualTo(1));
            Assert.That(payouts.Max(), Is.EqualTo(CasinoSlot.MaximumPayout));
            Assert.That(payouts.Average(), Is.EqualTo(89));
            Assert.That(CasinoSlot.SpinCost, Is.EqualTo(100));
        }
    }

    [TestCase(-1)]
    [TestCase(10000)]
    public void InvalidRollsAreRejected(int roll) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CasinoSlot.GetPayout(roll));

    [TestCase("pw_ar_nscasino", "swd_smach01", "SWTOR_SLOT1", true)]
    [TestCase("pw_ar_nscasino", "swd_smach02", "SWTOR_SLOT2", true)]
    [TestCase("pw_ar_nscasino", "swd_smach03", "SWTOR_SLOT3", true)]
    [TestCase("pw_ar_nscasino", "swd_smach04", "SWTOR_SLOT4", true)]
    [TestCase("pw_ar_nscasino", "swd_smach03", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_narshahub", "swd_smach02", "SWTOR_SLOT2", false)]
    [TestCase("tat_anc_junix", "swd_smach02", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_nscasino", "swd_smach06", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_nscasino", "swd_smach02", "OTHER", false)]
    public void OnlyCasinoSlotMachinesAreEligible(string area, string resref, string tag, bool expected) =>
        Assert.That(CasinoSlot.IsCasinoMachine(area, resref, tag), Is.EqualTo(expected));

    [Test]
    public void EveryCasinoSlotMachineIsEnabledAndBlueprintsStayDecorative()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module", "git")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
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
                Assert.That(CasinoSlot.IsCasinoMachine("pw_ar_nscasino", resref, tag), Is.True);
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
