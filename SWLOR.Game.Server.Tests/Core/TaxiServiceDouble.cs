using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// A test double that derives from a service whose handler is on its instance class, the way an engine test double
/// would. It inherits <see cref="TaxiService.LoadTaxiDestinations"/> and its handler attribute; the script registry must
/// not register the handler a second time for it (<see cref="ScriptRegistryInstanceHandlerTests"/>).
/// </summary>
public sealed class TaxiServiceDouble : TaxiService
{
    public TaxiServiceDouble(IDBService db)
        : base(db)
    {
    }
}
