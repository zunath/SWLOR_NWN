using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Adds or removes what one item property of an equipped item grants its wearer.
    /// </summary>
    /// <param name="creature">The wearer</param>
    /// <param name="item">The equipped item</param>
    /// <param name="ip">The item property</param>
    /// <param name="isAdding">If true, the property's stat is added, if false it's removed</param>
    public delegate void StatEquipmentAction(uint creature, uint item, ItemProperty ip, bool isAdding);
}
