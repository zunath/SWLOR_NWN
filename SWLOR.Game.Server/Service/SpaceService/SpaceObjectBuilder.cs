using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public class SpaceObjectBuilder
    {
        private readonly Dictionary<string, SpaceObjectDetail> _spaceObjects = new Dictionary<string, SpaceObjectDetail>();
        private SpaceObjectDetail _activeSpaceObject;

        /// <summary>
        /// Creates a new space object.
        /// </summary>
        /// <param name="objectTag">The tag of the object to associate with this space object detail.</param>
        /// <returns>A ship enemy builder with the configured options.</returns>
        public SpaceObjectBuilder Create(string objectTag)
        {
            _activeSpaceObject = new SpaceObjectDetail();
            _spaceObjects[objectTag] = _activeSpaceObject;

            return this;
        }

        /// <summary>
        /// Sets the item tag of the ship being used by the enemy.
        /// </summary>
        /// <param name="itemTag"></param>
        /// <returns></returns>
        public SpaceObjectBuilder ItemTag(string itemTag)
        {
            _activeSpaceObject.ShipItemTag = itemTag;

            return this;
        }

        public Dictionary<string, SpaceObjectDetail> Build()
        {
            return _spaceObjects;
        }
    }
}
