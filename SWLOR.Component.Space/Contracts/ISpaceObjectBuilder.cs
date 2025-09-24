using SWLOR.Component.Space.Model;

namespace SWLOR.Component.Space.Service;

public interface ISpaceObjectBuilder
{
    /// <summary>
    /// Creates a new space object.
    /// </summary>
    /// <param name="objectTag">The tag of the object to associate with this space object detail.</param>
    /// <returns>A ship enemy builder with the configured options.</returns>
    SpaceObjectBuilder Create(string objectTag);

    /// <summary>
    /// Sets the item tag of the ship being used by the enemy.
    /// </summary>
    /// <param name="itemTag"></param>
    /// <returns></returns>
    SpaceObjectBuilder ItemTag(string itemTag);

    /// <summary>
    /// Adds the specified ship module to the enemy's loadout.
    /// </summary>
    /// <param name="shipModuleItemTag">Item tag of the ship module to attach.</param>
    /// <returns>A ship enemy builder with the configured options.</returns>
    SpaceObjectBuilder ShipModule(string shipModuleItemTag);

    Dictionary<string, SpaceObjectDetail> Build();
}