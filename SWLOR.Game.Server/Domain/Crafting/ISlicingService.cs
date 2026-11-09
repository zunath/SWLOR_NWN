using SWLOR.Game.Server.Service.SlicingService;

namespace SWLOR.Game.Server.Domain.Crafting
{
    /// <summary>
    /// Slicing: the register of the attempts in progress, by player. The attempts themselves are run by
    /// <see cref="ISlicingSessionService"/>. Static callers reach the container's instance through <c>GameServices.Slicing</c>.
    /// </summary>
    public interface ISlicingService
    {
        /// <summary>The attempt in progress for the player id, or null.</summary>
        ActiveSlicingSession GetSession(string playerId);

        /// <summary>True when the player id has an attempt in progress.</summary>
        bool HasSession(string playerId);

        /// <summary>Registers the attempt in progress for the player id, replacing any earlier one.</summary>
        void SetSession(string playerId, ActiveSlicingSession session);

        /// <summary>Forgets the attempt in progress for the player id. Does nothing when there is none.</summary>
        void RemoveSession(string playerId);
    }
}
