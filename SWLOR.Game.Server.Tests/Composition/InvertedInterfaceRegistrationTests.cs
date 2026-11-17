using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// The interfaces a lower domain owns and a higher service implements (P5-15a): the container hands the lower service the
/// same instance the higher service's own interface gives, so a state the service keeps is not split across two objects.
/// </summary>
[TestFixture]
public class InvertedInterfaceRegistrationTests
{
    private static readonly (Type Lower, Type Service)[] Pairs =
    {
        (typeof(IAreaLookup), typeof(IAreaService)),
        (typeof(ICityUpgradeLookup), typeof(IPropertyCityGovernanceService)),
        (typeof(IQuestAdvancer), typeof(IQuestService)),
        (typeof(IConcealmentQuery), typeof(IStealthService)),
        (typeof(ICurrencyBalance), typeof(ICurrencyService)),
        (typeof(IPlayerListRefresh), typeof(IPlayerListService)),
        (typeof(IPropertyIdLookup), typeof(IPropertyService)),
        (typeof(IDMActorResolver), typeof(IDMPresenceService)),
        (typeof(IDMWindowPresence), typeof(IDMPresenceService)),
    };

    private static ContainerHost Build()
    {
        var host = new ContainerHost();
        CompositionRoot.Build(host);
        return host;
    }

    [TestCaseSource(nameof(Pairs))]
    public void The_lower_interface_is_the_instance_of_the_service((Type Lower, Type Service) pair)
    {
        var host = Build();

        pair.Service.Should().BeAssignableTo(pair.Lower, "the service's interface extends the one the lower domain owns");
        host.Provider.GetRequiredService(pair.Lower).Should().BeSameAs(host.Provider.GetRequiredService(pair.Service));
    }

    [Test]
    public void The_space_mode_query_is_answered_by_one_instance_for_every_reader()
    {
        var host = Build();

        host.Provider.GetRequiredService<ISpaceModeQuery>().Should().BeSameAs(host.Provider.GetRequiredService<ISpaceModeQuery>());
    }

    [Test]
    public void The_adapters_in_Creatures_answer_the_Administration_interfaces()
    {
        var host = Build();

        host.Provider.GetRequiredService<IAIProfileDescription>().Should().BeOfType<AIProfileDescription>();
        host.Provider.GetRequiredService<IDMSpawnPreparation>().Should().BeOfType<DMSpawnPreparation>();
        host.Provider.GetRequiredService<IItemCacheInfo>().Should().BeOfType<ItemCacheInfo>();
    }
}
