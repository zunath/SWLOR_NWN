using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>The item definition builder: every call configures all the tags created together, and the defaults hold otherwise.</summary>
[TestFixture]
public class ItemBuilderTests
{
    private static readonly Location NoLocation = null!;

    [Test]
    public void AnItemStartsWithTheDocumentedDefaults()
    {
        var items = new ItemBuilder().Create("tag").Build();

        var detail = items["tag"];
        detail.InitializationMessageAction(1, 2, 3, NoLocation, 0).Should().BeEmpty();
        detail.DelayAction(1, 2, 3, NoLocation, 0).Should().Be(0f);
        detail.CalculateDistanceAction(1, 2, 3, NoLocation, 0).Should().Be(3.5f);
        detail.ReducesItemChargeAction(1, 2, 3, NoLocation, 0).Should().BeFalse();
        detail.UserFacesTarget.Should().BeFalse();
        detail.CanTargetLocation.Should().BeFalse();
        detail.ActivationAnimation.Should().Be(Animation.Invalid);
        detail.ActivationSpell.Should().BeNull();
        detail.RecastGroup.Should().BeNull();
        detail.ValidateAction.Should().BeNull();
        detail.ApplyAction.Should().BeNull();
    }

    [Test]
    public void EveryTagCreatedTogetherGetsItsOwnDetailWithTheSameSettings()
    {
        var items = new ItemBuilder()
            .Create("first", "second", "third")
            .UserFacesTarget().TargetsLocation().PlaysAnimation(Animation.FireForgetSalute)
            .ActivationSpell(CastSpell.AID_3)
            .Build();

        items.Keys.Should().BeEquivalentTo("first", "second", "third");
        items["first"].Should().NotBeSameAs(items["second"]);
        foreach (var detail in items.Values)
        {
            detail.UserFacesTarget.Should().BeTrue();
            detail.CanTargetLocation.Should().BeTrue();
            detail.ActivationAnimation.Should().Be(Animation.FireForgetSalute);
            detail.ActivationSpell.Should().Be(CastSpell.AID_3);
        }
    }

    [Test]
    public void ASettingOnlyAppliesToTheItemsOfTheLatestCreate()
    {
        var items = new ItemBuilder()
            .Create("old").UserFacesTarget()
            .Create("new").TargetsLocation()
            .Build();

        items["old"].UserFacesTarget.Should().BeTrue();
        items["old"].CanTargetLocation.Should().BeFalse();
        items["new"].UserFacesTarget.Should().BeFalse();
        items["new"].CanTargetLocation.Should().BeTrue();
    }

    [Test]
    public void FixedValuesAreReturnedByTheirDelegates()
    {
        var detail = new ItemBuilder().Create("tag")
            .InitializationMessage("Working...").Delay(2.5f).MaxDistance(9f).ReducesItemCharge()
            .Build()["tag"];

        detail.InitializationMessageAction(1, 2, 3, NoLocation, 0).Should().Be("Working...");
        detail.DelayAction(1, 2, 3, NoLocation, 0).Should().Be(2.5f);
        detail.CalculateDistanceAction(1, 2, 3, NoLocation, 0).Should().Be(9f);
        detail.ReducesItemChargeAction(1, 2, 3, NoLocation, 0).Should().BeTrue();
    }

    [Test]
    public void ComputedValuesSeeTheUseContext()
    {
        var detail = new ItemBuilder().Create("tag")
            .InitializationMessage((user, item, target, location, index) => $"{user}/{item}/{target}/{index}")
            .Delay((user, item, target, location, index) => index * 2f)
            .MaxDistance((user, item, target, location, index) => item)
            .ReducesItemCharge((user, item, target, location, index) => user == target)
            .Build()["tag"];

        detail.InitializationMessageAction(1, 2, 3, NoLocation, 4).Should().Be("1/2/3/4");
        detail.DelayAction(1, 2, 3, NoLocation, 4).Should().Be(8f);
        detail.CalculateDistanceAction(1, 7, 3, NoLocation, 4).Should().Be(7f);
        detail.ReducesItemChargeAction(5, 2, 5, NoLocation, 4).Should().BeTrue();
        detail.ReducesItemChargeAction(5, 2, 6, NoLocation, 4).Should().BeFalse();
    }

    [Test]
    public void ValidationApplicationAndRecastAreStored()
    {
        ValidateItemDelegate validate = (user, item, target, location, index) => "no";
        var applied = 0;
        ApplyItemEffectsDelegate apply = (user, item, target, location, index) => applied++;

        var detail = new ItemBuilder().Create("tag")
            .ValidationAction(validate).ApplyAction(apply).HasRecastDelay(RecastGroup.Flash, 30f)
            .Build()["tag"];

        detail.ValidateAction(1, 2, 3, NoLocation, 0).Should().Be("no");
        detail.ApplyAction(1, 2, 3, NoLocation, 0);
        applied.Should().Be(1);
        detail.RecastGroup.Should().Be(RecastGroup.Flash);
        detail.RecastCooldown.Should().Be(30f);
    }

    [Test]
    public void CreatingATagAgainReplacesItsDetail()
    {
        var builder = new ItemBuilder();
        builder.Create("tag").UserFacesTarget();
        builder.Create("tag");

        builder.Build()["tag"].UserFacesTarget.Should().BeFalse();
    }
}
