using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class PerkTriggerTests
{
    private FacadeContainer _container = null!;

    [OneTimeSetUp]
    public void BuildContainer() => _container = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeContainer() => _container.Dispose();

    [Test]
    public void CacheTriggers_CachesRefundTriggersWithoutPurchaseTriggers()
    {
        using var installed = _container.Install();
        var refundTriggers = PerkInternals.RefundTriggers;
        var purchaseTriggers = PerkInternals.PurchaseTriggers;
        refundTriggers.Clear();
        purchaseTriggers.Clear();

        try
        {
            var detail = new PerkDetail
            {
                Type = PerkType.BlazingSpikes
            };
            detail.RefundedTriggers.Add(_ => { });

            PerkInternals.Invoke("CacheTriggers", detail);

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

}
