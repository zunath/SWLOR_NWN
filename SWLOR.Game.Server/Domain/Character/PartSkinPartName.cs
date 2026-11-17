using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>An armor part and the name its model carries (footr, chest, shol).</summary>
    public readonly record struct PartSkinPartName(AppearanceArmor Part, string Name);
}
