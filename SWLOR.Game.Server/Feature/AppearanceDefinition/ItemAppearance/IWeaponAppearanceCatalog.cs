using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>The weapon appearance definition of each base item the appearance editor can restyle.</summary>
    public interface IWeaponAppearanceCatalog
    {
        IReadOnlyDictionary<BaseItem, WeaponAppearanceBaseDefinition> Definitions { get; }
    }
}
