using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Space
{
    /// <summary>
    /// The space mode read the ability activation checks make (<see cref="ISpaceModeQuery"/>, owned by Abilities; see
    /// Readmes/MaintainabilityRefactorAbilitySplit.md). It is the same read as <see cref="SpaceService.IsPlayerInSpaceMode"/>, a
    /// player whose record names an active ship, and needs only the database, so the activation part takes it without the rest
    /// of the space service.
    /// </summary>
    public class SpaceModeQuery : ISpaceModeQuery
    {
        private readonly IDBService _db;

        public SpaceModeQuery(IDBService db)
        {
            _db = db;
        }

        public bool IsPlayerInSpaceMode(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);
            return dbPlayer.ActiveShipId != Guid.Empty.ToString();
        }
    }
}
