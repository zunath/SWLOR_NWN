using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class MessagingTests
{
    [Test]
    public void SendMessageNearbyToPlayers_RejectsNullMessageBuilder()
    {
        var action = () => Messaging.SendMessageNearbyToPlayers(
            0,
            (Messaging.BuildMessageDelegate)null);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("buildMessage");
    }
}
