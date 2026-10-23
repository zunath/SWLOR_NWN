using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class CasinoSlotTests
{
    [Test]
    public void PayoutsHaveTheAdvertisedOddsAndReturn()
    {
        var payouts = Enumerable.Range(0, 10000).Select(CasinoSlot.GetPayout).ToArray();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(payouts.Count(p => p == 0), Is.EqualTo(7000));
            Assert.That(payouts.Count(p => p == 100), Is.EqualTo(2000));
            Assert.That(payouts.Count(p => p == 500), Is.EqualTo(800));
            Assert.That(payouts.Count(p => p == 1000), Is.EqualTo(190));
            Assert.That(payouts.Count(p => p == 10000), Is.EqualTo(10));
            Assert.That(payouts.Max(), Is.EqualTo(CasinoSlot.MaximumPayout));
            Assert.That(payouts.Average(), Is.EqualTo(89));
            Assert.That(CasinoSlot.SpinCost, Is.EqualTo(100));
        }
    }

    [TestCase(-1)]
    [TestCase(10000)]
    public void InvalidRollsAreRejected(int roll) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CasinoSlot.GetPayout(roll));

    [TestCase("pw_ar_nscasino", "swd_smach02", "SWTOR_SLOT2", true)]
    [TestCase("pw_ar_narshahub", "swd_smach02", "SWTOR_SLOT2", false)]
    [TestCase("tat_anc_junix", "swd_smach02", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_nscasino", "swd_smach06", "SWTOR_SLOT2", false)]
    [TestCase("pw_ar_nscasino", "swd_smach02", "OTHER", false)]
    public void OnlyTheCasinoMachineIsEligible(string area, string resref, string tag, bool expected) =>
        Assert.That(CasinoSlot.IsCasinoMachine(area, resref, tag), Is.EqualTo(expected));

    [Test]
    public void OnlyOneCasinoInstanceIsEnabledAndBlueprintStaysDecorative()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Module", "git")))
            root = root.Parent;
        Assert.That(root, Is.Not.Null);
        using var area = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root!.FullName,
            "Module", "git", "pw_ar_nscasino.git.json")));
        var machines = area.RootElement.GetProperty("Placeable List").GetProperty("value")
            .EnumerateArray().Where(p => p.GetProperty("TemplateResRef").GetProperty("value")
                .GetString() == "swd_smach02").ToArray();
        var enabled = machines.Where(p => p.GetProperty("Useable").GetProperty("value").GetInt32() == 1).ToArray();
        Assert.That(enabled, Has.Length.EqualTo(1));
        var machine = enabled.Single();
        Assert.That(machine.GetProperty("Static").GetProperty("value").GetInt32(), Is.Zero);
        Assert.That(machine.GetProperty("Plot").GetProperty("value").GetInt32(), Is.EqualTo(1));
        Assert.That(machine.GetProperty("OnUsed").GetProperty("value").GetString(), Is.EqualTo("casino_slot_used"));
        using var blueprint = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root.FullName,
            "Module", "utp", "swd_smach02.utp.json")));
        Assert.That(blueprint.RootElement.GetProperty("Useable").GetProperty("value").GetInt32(), Is.Zero);
        Assert.That(blueprint.RootElement.GetProperty("OnUsed").GetProperty("value").GetString(), Is.Empty);
    }
}
