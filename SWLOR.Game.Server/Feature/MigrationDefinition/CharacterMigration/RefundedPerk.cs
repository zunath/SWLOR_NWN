using System.Linq;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>A retired crafting perk: its SP prices per level, the change that replaced it and the feats it granted.</summary>
internal sealed record RefundedPerk(PerkType Type, string Name, int[] Prices, string Change, params FeatType[] GrantedFeats)
{
    public int GetRefund(int level) => Prices.Take(level).Sum();
}
