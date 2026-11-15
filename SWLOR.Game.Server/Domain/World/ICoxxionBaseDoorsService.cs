using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.World
{
    /// <summary>
    /// The color doors of the Coxxion Base dungeon, which its terminals open and close. Static callers reach the container's
    /// instance through <c>GameServices.CoxxionBaseDoors</c>; the event handler is on <see cref="CoxxionBaseDoorsService"/>.
    /// </summary>
    public interface ICoxxionBaseDoorsService
    {
        /// <summary>
        /// When the module loads, store the doors for the Coxxion Base dungeon into cache.
        /// </summary>
        void LoadDoors();

        /// <summary>
        /// The doors of the Coxxion Base dungeon that have a color, in the order the area lists them.
        /// </summary>
        IReadOnlyList<uint> Doors { get; }
    }
}
