
using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Service.QuestService
{
    public interface IQuestReward
    {
        /// <summary>
        /// If true, this reward will become available for the player to select.
        /// If false, this reward will be given regardless if other rewards are selectable.
        /// Note that if the quest doesn't allow reward selection, this is given to them every time no matter what.
        /// </summary>
        bool IsSelectable { get; }

        /// <summary>
        /// The name of the reward as it shows in the 'Select a Reward' menu.
        /// If the quest doesn't allow reward selection, this does nothing.
        /// </summary>
        string MenuName { get; }

        /// <summary>
        /// The actions to take when this reward is given to a player.
        /// </summary>
        /// <param name="player">The player receiving the reward.</param>
        void GiveReward(uint player);
    }

    public class GoldReward : IQuestReward
    {
        private readonly IQuestService _questService;
        public int Amount { get; }
        public bool IsSelectable { get; }
        public string MenuName => Amount + " Credits";
        public bool IsGuildQuest { get; }

        public GoldReward(int amount, bool isSelectable, bool isGuildQuest, IQuestService questService)
        {
            Amount = amount;
            IsSelectable = isSelectable;
            IsGuildQuest = isGuildQuest;
            _questService = questService;
        }

        public void GiveReward(uint player)
        {
            var amount = _questService.CalculateQuestGoldReward(player, IsGuildQuest, Amount);
            GiveGoldToCreature(player, amount);
        }
    }

    public class XPReward : IQuestReward
    {
        private readonly IDatabaseService _db;
        private readonly IItemCacheService _itemCache;
        public int Amount { get; }
        public bool IsSelectable { get; }
        public string MenuName => Amount + " XP";

        public XPReward(IDatabaseService db, IItemCacheService itemCache, int amount, bool isSelectable)
        {
            _db = db;
            _itemCache = itemCache;
            Amount = amount;
            IsSelectable = isSelectable;
        }

        public void GiveReward(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            dbPlayer.UnallocatedXP += Amount;

            _db.Set(dbPlayer);
            SendMessageToPC(player, $"You earned {Amount} XP!");
        }
    }


    public class ItemReward : IQuestReward
    {
        private readonly IItemCacheService _itemCache;
        public bool IsSelectable { get; }
        public string MenuName { get; }
        private readonly string _resref;
        private readonly int _quantity;

        public ItemReward(IItemCacheService itemCache, string resref, int quantity, bool isSelectable)
        {
            _itemCache = itemCache;
            _resref = resref;
            _quantity = quantity;
            IsSelectable = isSelectable;

            var itemName = _itemCache.GetItemNameByResref(resref);

            if (_quantity > 1)
                MenuName = _quantity + "x " + itemName;
            else
                MenuName = itemName;
        }

        public void GiveReward(uint player)
        {
            CreateItemOnObject(_resref, player, _quantity);
        }
    }

    public class KeyItemReward : IQuestReward
    {
        private readonly IKeyItemService _keyItemService;
        public bool IsSelectable { get; }

        public string MenuName
        {
            get
            {
                var detail = _keyItemService.GetKeyItem(KeyItemType);
                return detail.Name;
            }
        }

        public KeyItemType KeyItemType { get; }

        public KeyItemReward(KeyItemType keyItemType, bool isSelectable, IKeyItemService keyItemService)
        {
            KeyItemType = keyItemType;
            IsSelectable = isSelectable;
            _keyItemService = keyItemService;
        }

        public void GiveReward(uint player)
        {
            _keyItemService.GiveKeyItem(player, KeyItemType);
        }
    }

    public class GPReward: IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public GuildType Guild { get; }
        public int Amount { get; }

        public GPReward(GuildType guild, int amount, bool isSelectable)
        {
            IsSelectable = isSelectable;
            Guild = guild;
            Amount = amount;

            var guildDetail = Service.Guild.GetGuild(guild);
            MenuName = amount + " " + guildDetail.Name + " GP";
        }

        public void GiveReward(uint player)
        {
            var reward = Service.Guild.CalculateGPReward(player, Guild, Amount);
            Service.Guild.GiveGuildPoints(player, Guild, reward);
        }
    }

    public class FactionStandingReward : IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionStandingReward(FactionType faction, int amount, bool isSelectable)
        {
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = amount;

            var factionDetail = Service.Faction.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} standing";
        }

        public void GiveReward(uint player)
        {
            Service.Faction.AdjustPlayerFactionStanding(player, Faction, Amount);
        }
    }
    public class FactionPointsReward : IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        public FactionType Faction { get; }
        public int Amount { get; }

        public FactionPointsReward(FactionType faction, int amount, bool isSelectable)
        {
            IsSelectable = isSelectable;
            Faction = faction;
            Amount = Math.Abs(amount);

            var factionDetail = Service.Faction.GetFactionDetail(Faction);
            MenuName = $"{factionDetail.Name} points";
        }

        public void GiveReward(uint player)
        {
            Service.Faction.AdjustPlayerFactionPoints(player, Faction, Amount);
        }
    }
}
