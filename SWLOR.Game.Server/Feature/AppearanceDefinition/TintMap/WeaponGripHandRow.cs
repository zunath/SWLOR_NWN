using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;

public sealed record WeaponGripHandRow(string Source, string Render, BaseItem Weapon = (BaseItem)(-1), int Model = 0);
