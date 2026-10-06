using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpaceExperienceLedgerTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
    [Test]
    public void ConcurrentToolsAndIdleTime_DoNotAccelerateTheSharedActiveHourClock()
    {
        var ledger = new SpaceExperienceLedger();
        ledger.Claim("flight", ShipModuleAction.Extraction, 18, Now, Now.AddSeconds(12));
        ledger.Claim("flight", ShipModuleAction.Survey, 150, Now, Now.AddSeconds(12));
        ledger.ActiveClock.Should().Be(12);
        ledger.Claim("flight", ShipModuleAction.Extraction, 18, Now.AddHours(10), Now.AddHours(10).AddSeconds(12));
        ledger.ActiveClock.Should().Be(24);
    }
    [Test]
    public void FiveSkillBudget_PersistsAcrossExpeditionsAndSerialization()
    {
        var ledger = new SpaceExperienceLedger();
        for (var i = 0; i < 3; i++) ledger.Claim("flight" + i, ShipModuleAction.Extraction, 6000, Now.AddSeconds(i * 20), Now.AddSeconds(i * 20 + 12)).Should().Be(6000);
        ledger = JsonConvert.DeserializeObject<SpaceExperienceLedger>(JsonConvert.SerializeObject(ledger))!;
        ledger.Claim("new-flight", ShipModuleAction.Survey, 150, Now.AddSeconds(60), Now.AddSeconds(68)).Should().Be(0);
        ledger.Claim("new-flight", ShipModuleAction.Survey, 150, Now.AddSeconds(70), Now.AddSeconds(3670)).Should().Be(150);
    }
    [Test]
    public void ExpeditionCaps_ApplyAcrossAllMiningToolsAndAllSurveyObjects()
    {
        var ledger = new SpaceExperienceLedger();
        ledger.Claim("flight", ShipModuleAction.Extraction, 5990, Now, Now.AddSeconds(12)).Should().Be(5990);
        ledger.Claim("flight", ShipModuleAction.BulkSalvage, 100, Now.AddSeconds(12), Now.AddSeconds(30)).Should().Be(10);
        for (var i = 0; i < 8; i++) ledger.Claim("flight", ShipModuleAction.Survey, 150, Now.AddSeconds(30 + i * 8), Now.AddSeconds(38 + i * 8)).Should().Be(150);
        ledger.Claim("flight", ShipModuleAction.Survey, 150, Now.AddSeconds(100), Now.AddSeconds(108)).Should().Be(0);
    }
}
