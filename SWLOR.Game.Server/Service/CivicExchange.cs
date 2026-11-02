using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.CivicExchangeService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The Civic Exchange (Player City Revamp, "Civic Exchange"): a store in the Cantina where citizens in good standing spend
    /// Available XP, the RP XP currency the Training Store uses, on crates of random crafting materials and a long buff. The
    /// stock, prices and rules are <see cref="CivicExchangeCatalog"/> and <see cref="CivicExchangeRules"/>; this looks up the
    /// facts (the player, the city's Cantina, the CD key's saved allowance) and carries a purchase out. It sells no research time
    /// and no faster training, by the Plan. The Training Store window shows it, in its city mode.
    /// </summary>
    public static class CivicExchange
    {
        /// <summary>Whether the player may use the Civic Exchange of the city, with the taxes they owe for the reason.</summary>
        public static CivicExchangeAccess GetAccess(Player dbPlayer, string cityId, DateTime? nowUtc = null)
        {
            if (dbPlayer == null)
                return CivicExchangeAccess.NotACitizen;

            return CivicExchangeRules.GetAccess(
                dbPlayer.CitizenPropertyId,
                cityId,
                dbPlayer.IsDeleted,
                dbPlayer.PropertyOwedTaxes,
                dbPlayer.CitizenSinceUtc,
                nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>Why the player may not use the city's Civic Exchange, worded for them, or an empty string when they may.</summary>
        public static string GetAccessFailure(Player dbPlayer, string cityId, DateTime? nowUtc = null)
        {
            return CivicExchangeRules.DescribeAccess(GetAccess(dbPlayer, cityId, nowUtc), dbPlayer?.PropertyOwedTaxes ?? 0);
        }

        /// <summary>The city's effective Cantina level, which decides what the Exchange stocks. 0 when it has no Cantina.</summary>
        public static int GetCantinaLevel(string cityId)
        {
            return Property.GetEffectiveUpgradeLevel(cityId, PropertyUpgradeType.CantinaLevel);
        }

        /// <summary>How many crates the player's CD key may still buy this week.</summary>
        public static int GetCratesRemaining(uint player, DateTime? nowUtc = null)
        {
            var record = DB.Get<CivicExchangeAllowance>(GetAllowanceKey(player));
            return CivicExchangeRules.GetCratesRemaining(record, nowUtc ?? DateTime.UtcNow);
        }

        /// <summary>
        /// Buys an item for the player at the city's Civic Exchange. Checks everything again (the window may have been open a while),
        /// then takes the Available XP and the week's crate and delivers the purchase. Returns why nothing was bought, or an empty
        /// string when the purchase went through.
        /// </summary>
        public static string TryBuy(uint player, string cityId, string itemKey)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return "Only players may use the Civic Exchange.";

            var now = DateTime.UtcNow;
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            var accessFailure = GetAccessFailure(dbPlayer, cityId, now);
            if (!string.IsNullOrEmpty(accessFailure))
                return accessFailure;

            var item = CivicExchangeCatalog.Get(itemKey);
            var allowanceKey = GetAllowanceKey(player);
            var allowance = DB.Get<CivicExchangeAllowance>(allowanceKey) ?? new CivicExchangeAllowance(allowanceKey);
            var refusal = CivicExchangeRules.GetRefusal(
                item,
                GetCantinaLevel(cityId),
                dbPlayer.UnallocatedXP,
                CivicExchangeRules.GetCratesRemaining(allowance, now));
            if (refusal != CivicExchangeRefusal.None)
                return CivicExchangeRules.DescribeRefusal(refusal, item);

            return item.Kind == CivicExchangeItemKind.Buff
                ? BuyBuff(player, dbPlayer, item)
                : BuyCrate(player, dbPlayer, item, allowance, now);
        }

        /// <summary>
        /// Applies the buff first, so a buff that fails to apply costs nothing, then takes the XP. The buff replaces any food buff the
        /// player is under (see <see cref="CivicProvisionsStatusEffect"/>).
        /// </summary>
        private static string BuyBuff(uint player, Player dbPlayer, CivicExchangeItem item)
        {
            if (!StatusEffect.ApplyStatusEffect<CivicProvisionsStatusEffect>(player, player, CivicExchangeCatalog.BuffDurationSeconds))
                return $"The {item.Name} could not take hold. You were not charged.";

            dbPlayer.UnallocatedXP -= item.PriceXp;
            DB.Set(dbPlayer);

            SendMessageToPC(player, $"You buy the {item.Name} for {item.PriceXp:N0} Available XP. {CivicExchangeCatalog.BuffSummary}");
            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} bought {item.Key} at the Civic Exchange for {item.PriceXp} Available XP.");
            return string.Empty;
        }

        /// <summary>
        /// Rolls the crate, then writes the records and gives the items: the XP and the week's allowance are saved first and the
        /// materials second, so a restart can lose a crate but never hand one out twice. What does not fit in the inventory goes to
        /// overflow storage (/overflow).
        /// </summary>
        private static string BuyCrate(uint player, Player dbPlayer, CivicExchangeItem item, CivicExchangeAllowance allowance, DateTime now)
        {
            var contents = CivicExchangeRules.PlanCrate(
                item.Tier,
                Random.Next,
                table => Loot.GetLootTableByName(table).GetRandomItem().Resref);
            if (contents.Count == 0)
                return $"The {item.Name} is out of stock. You were not charged.";

            dbPlayer.UnallocatedXP -= item.PriceXp;
            DB.Set(dbPlayer);
            CivicExchangeRules.RecordCrate(allowance, now);
            DB.Set(allowance);

            var playerId = GetObjectUUID(player);
            var location = GetLocation(player);
            var overflow = 0;
            foreach (var (resref, count) in contents)
            {
                overflow += FarmItems.Give(player, playerId, resref, count, location);
            }

            ExportSingleCharacter(player);

            SendMessageToPC(player, $"You open the {item.Name} and find: {DescribeContents(contents)}.");
            if (overflow > 0)
                SendMessageToPC(player, $"{overflow} of the items did not fit in your inventory. Claim them with /overflow.");

            Log.Write(LogGroup.Property, $"{PlayerName.GetAuditName(player)} bought {item.Key} at the Civic Exchange for {item.PriceXp} Available XP.");
            return string.Empty;
        }

        private static string DescribeContents(IReadOnlyDictionary<string, int> contents)
        {
            return string.Join(", ", contents
                .OrderByDescending(x => x.Value)
                .Select(x => $"{x.Value} x {Cache.GetItemNameByResref(x.Key)}"));
        }

        private static string GetAllowanceKey(uint player)
        {
            return CivicExchangeRules.GetAllowanceKey(GetPCPublicCDKey(player), GetObjectUUID(player));
        }
    }
}
