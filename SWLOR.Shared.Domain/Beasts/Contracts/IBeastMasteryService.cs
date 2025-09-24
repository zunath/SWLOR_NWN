using SWLOR.Shared.Domain.Beasts.Enums;
using SWLOR.Shared.Domain.Beasts.ValueObjects;
using SWLOR.Shared.Domain.Crafting.ValueObjects;

namespace SWLOR.Shared.Domain.Beasts.Contracts
{
    public interface IBeastMasteryService
    {
        public int MaxLevel { get; }
        public string HydrolaseResrefPrefix { get; }
        public string LyaseResrefPrefix { get; }
        public string IsomeraseResrefPrefix { get; }
        public string DNAResref { get; }
        public string BeastEggResref { get; }
        string ExtractCorpseObjectResref { get; }
        string BeastTypeVariable { get; }
        string BeastLevelVariable { get; }
        void CacheData();
        BeastDetail GetBeastDetail(BeastType type);
        BeastRoleAttribute GetBeastRoleDetail(BeastRoleType type);
        string GetBeastId(uint beast);
        void SetBeastId(uint beast, string beastId);
        BeastType GetBeastType(uint beast);
        bool IsPlayerBeast(uint beast);
        void SetBeastType(uint beast, BeastType type);
        void GiveBeastXP(uint beast, int xp, bool ignoreBonuses);
        int GetRequiredXP(int level, int xpPenalty);
        void SpawnBeast(uint player, string beastId, int percentHeal);
        (BeastFoodType, BeastFoodType) GetLikedAndHatedFood();
        void CombatPointXPDistributed();

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        void RemoveAssociate();

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        void OnAcquireItem();

        void BeastOnBlocked();
        void BeastOnEndCombatRound();
        void BeastOnConversation();
        void BeastOnDamaged();
        void BeastOnDeath();
        void BeastOnDisturbed();
        void BeastOnHeartbeat();
        void BeastOnPerception();
        void BeastOnPhysicalAttacked();
        void BeastOnRested();
        void BeastOnSpawn();
        void BeastOnSpellCastAt();
        void BeastOnUserDefined();
        void OpenStablesMenu();

        /// <summary>
        /// Retrieves the percentage associated with a specific item property Id for the incubation stats.
        /// </summary>
        /// <param name="itemPropertyId">The incubation stat Id</param>
        /// <returns>The percentage associated or 0.0 if not found.</returns>
        float GetIncubationPercentageById(int itemPropertyId);

        void UseIncubator();
        void CreateBeastEgg(IncubationJob job, uint player);

        /// <summary>
        /// Determines if the specified item is an incubation crafting item.
        /// This includes enzymes and DNA but excludes beast eggs.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if used in incubation, false otherwise</returns>
        bool IsIncubationCraftingItem(uint item);

        /// <summary>
        /// Determines if the specified item is a beast egg.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if beast egg, false otherwise</returns>
        bool IsBeastEgg(uint item);

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        void OnRemoveProperty();

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        void UseExtractDNAObject();
    }
}