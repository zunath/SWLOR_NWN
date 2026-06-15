using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using System.Reflection;

namespace SWLOR.Game.Server.Tests.Service;

public class PerkTriggerTests
{
    [Test]
    public void CacheTriggers_CachesRefundTriggersWithoutPurchaseTriggers()
    {
        var refundTriggers = GetTriggerDictionary("_refundTriggers");
        var purchaseTriggers = GetTriggerDictionary("_purchaseTriggers");
        refundTriggers.Clear();
        purchaseTriggers.Clear();

        try
        {
            var detail = new PerkDetail
            {
                Type = PerkType.BlazingSpikes
            };
            detail.RefundedTriggers.Add(_ => { });

            typeof(Perk)
                .GetMethod("CacheTriggers", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { detail });

            Perk.GetAllPurchaseTriggers().Should().NotContainKey(PerkType.BlazingSpikes);
            Perk.GetAllRefundTriggers()
                .Should()
                .ContainKey(PerkType.BlazingSpikes)
                .WhoseValue
                .Should()
                .ContainSingle();
        }
        finally
        {
            refundTriggers.Clear();
            purchaseTriggers.Clear();
        }
    }

    private static Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>> GetTriggerDictionary(string fieldName)
    {
        return (Dictionary<PerkType, List<PerkTriggerPurchasedRefundedAction>>)typeof(Perk)
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
    }
}
