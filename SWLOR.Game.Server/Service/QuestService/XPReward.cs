using System;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Data.Entity;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Service.QuestService
{
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
}
