using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>A creature body part and the name its model carries (footr, chest, head).</summary>
    public readonly record struct TintMapBodyPartName(CreaturePart Part, string Name);
}
