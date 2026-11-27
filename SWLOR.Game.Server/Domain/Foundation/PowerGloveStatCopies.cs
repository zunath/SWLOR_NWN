using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Foundation;

/// <summary>
/// How many times an equipped item's stats count. A Power Glove fills both hands, so its equipment stats count once per
/// hand: one glove grants what two identical Katars do. Lives in Foundation so the saved character code, which runs on
/// worker threads before any service exists, reads it without naming the Power Glove service.
/// </summary>
public static class PowerGloveStatCopies
{
    public static int For(BaseItem type) => type == BaseItem.PowerGlove ? 2 : 1;
}
