using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// Part 2 of the stat split as the status effects' listener (P5-4e): it registers itself at boot and answers the two notices
/// the status effects send.
/// </summary>
public class CreatureStatListenerTests : StatServiceTestBase
{
    [Test]
    public void TheBootHandlerRegistersTheServiceWithTheStatusEffects()
    {
        CreatureStat.RegisterStatEffectListener();

        Bench.RegisteredListeners.Should().ContainSingle().Which.Should().BeSameAs(CreatureStat);
    }

    [Test]
    public void TheServiceIsTheListenerTheStatusEffectsNotify()
    {
        ((IStatEffectListener)CreatureStat).Should().BeSameAs(CreatureStat);
    }

    [Test]
    public void RestoredStatusEffectsReconcileTheFoodHPOfThePlayer()
    {
        var record = Bench.AddPlayer();
        record.TemporaryFoodHP = 30;
        record.MaxHP = 100;

        CreatureStat.OnStatusEffectsRestored(PlayerCreature);

        record.TemporaryFoodHP.Should().Be(0);
    }

    [Test]
    public void AChangeOfStatAdjustingEffectsAppliesTheMovementRate()
    {
        Bench.AddNpc();
        Bench.Creature.SetReturn("GetMovementRateFactor", 1.0f);

        CreatureStat.OnStatAdjustingEffectsChanged(Npc);

        Bench.Creature.CountOf("SetMovementRateFactor").Should().Be(1);
    }
}
