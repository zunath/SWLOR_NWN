using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Tests.Service;

public class StatusEffectRefreshInPlaceTests
{
    // An in-place refresh never runs Apply, Remove, Reapply or Tick again, so an effect that opts in
    // may not do anything there. MovementSpeedStatusEffectBase.Apply only sets the effect's own slow.
    private static readonly string[] SkippedHooks = { "Apply", "Remove", "Reapply", "Tick" };

    [Test]
    public void EffectsThatRefreshInPlace_HaveNoApplyOrRemoveBehavior()
    {
        var optedIn = typeof(StatusEffectBase).Assembly.GetTypes()
            .Where(type => typeof(StatusEffectBase).IsAssignableFrom(type) && !type.IsAbstract)
            .Where(type => type.GetMethod(nameof(StatusEffectBase.CanRefreshInPlace))!.DeclaringType != typeof(StatusEffectBase))
            .ToList();

        optedIn.Should().Contain(new[]
        {
            typeof(HobbleStatusEffect),
            typeof(ForceSanctuary1StatusEffect),
            typeof(EmergencyBunker1StatusEffect),
            typeof(KoltoMistHealingStatusEffect),
        });

        foreach (var type in optedIn)
        {
            var effect = (StatusEffectBase)Activator.CreateInstance(type)!;
            effect.StackingType.Should().NotBe(StatusEffectStackType.UnlimitedStacking, $"{type.Name} refreshes one instance");
            foreach (var hook in SkippedHooks)
            {
                var declaringType = type
                    .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(method => method.Name == hook && method.GetBaseDefinition().DeclaringType == typeof(StatusEffectBase))
                    .DeclaringType;
                var allowed = declaringType == typeof(StatusEffectBase) ||
                              (hook == "Apply" && declaringType == typeof(MovementSpeedStatusEffectBase));
                allowed.Should().BeTrue($"{type.Name} refreshes in place, so {declaringType!.Name}.{hook} would be skipped");
            }
        }
    }
}
