
using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.PerkService;

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
        public int Amount { get; }
        public bool IsSelectable { get; }
        public string MenuName => Amount + " Credits";
        public bool IsGuildQuest { get; }

        public GoldReward(int amount, bool isSelectable, bool isGuildQuest)
        {
            Amount = amount;
            IsSelectable = isSelectable;
            IsGuildQuest = isGuildQuest;
        }

        public void GiveReward(uint player)
        {
            var amount = Quest.CalculateQuestGoldReward(player, IsGuildQuest, Amount);
            GiveGoldToCreature(player, amount);
        }
    }

    public class XPReward : IQuestReward
    {
        public int Amount { get; }
        public bool IsSelectable { get; }
        public string MenuName => Amount + " XP";

        public XPReward(int amount, bool isSelectable)
        {
            Amount = amount;
            IsSelectable = isSelectable;
        }

        public void GiveReward(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            dbPlayer.UnallocatedXP += Amount;

            DB.Set(dbPlayer);
            SendMessageToPC(player, $"You earned {Amount} XP!");
        }
    }


    public class ItemReward : IQuestReward
    {
        public bool IsSelectable { get; }
        public string MenuName { get; }
        private readonly string _resref;
        private readonly int _quantity;

        public ItemReward(string resref, int quantity, bool isSelectable)
        {
            _resref = resref;
            _quantity = quantity;
            IsSelectable = isSelectable;

            var itemName = Cache.GetItemNameByResref(resref);

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
        public bool IsSelectable { get; }

        public string MenuName
        {
            get
            {
                var detail = KeyItem.GetKeyItem(KeyItemType);
                return detail.Name;
            }
        }

        public KeyItemType KeyItemType { get; }

        public KeyItemReward(KeyItemType keyItemType, bool isSelectable)
        {
            KeyItemType = keyItemType;
            IsSelectable = isSelectable;
        }

        public void GiveReward(uint player)
        {
            KeyItem.GiveKeyItem(player, KeyItemType);
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
