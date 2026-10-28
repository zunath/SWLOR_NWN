using SWLOR.Game.Server.Service.PlayerMarketService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The item a retired space item becomes, as <c>CreateItemOnObject</c> makes it (UTI, base64), with the name, icon and
/// market category a storage or market listing of it shows.
/// </summary>
public sealed record LegacySpaceItemBlueprint(string Data, string Name, string IconResref, MarketCategoryType Category);
