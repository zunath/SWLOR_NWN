using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The tint layer of one armor part (or of every part, with <see cref="AppearanceArmor.Invalid"/>) whose edits are
    /// counted while an equipment-swap color carry is pending.
    /// </summary>
    public readonly record struct TintMapStateRevisionScope(
        TintMapLayerType Layer,
        AppearanceArmor ArmorPart);
}
