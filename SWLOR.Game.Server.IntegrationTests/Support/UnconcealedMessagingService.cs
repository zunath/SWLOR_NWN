using SWLOR.Game.Server.Domain.Core;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// The messaging service of the integration container: no creature is concealed, because reading a creature's effects
/// goes through the native library, which the tier does not load.
/// </summary>
internal sealed class UnconcealedMessagingService : MessagingService
{
    protected override bool IsConcealed(uint creature) => false;
}
