using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of beast companions: the beast catalog, taming and leveling, spawning a beast for its master, the
    /// creature event handlers of a spawned beast, and the incubation of DNA into beast eggs. The logic lives in
    /// <see cref="IBeastMasteryService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class BeastMastery
    {
        private static IBeastMasteryService Instance => FacadeAccessor.Resolve<IBeastMasteryService>();

        public const string BeastClawResref = "beast_claw";
        public const int MaxLevel = Domain.Companions.BeastMasteryService.MaxLevel;

        public const string HydrolaseResrefPrefix = "hydrolase_";
        public const string LyaseResrefPrefix = "lyase_";
        public const string IsomeraseResrefPrefix = "isomerase_";
        public const string DNAResref = Domain.Companions.BeastMasteryService.DNAResref;
        public const string BeastEggResref = Domain.Companions.BeastMasteryService.BeastEggResref;
        public const string EnzymeTag = Domain.Companions.BeastMasteryService.EnzymeTag;

        public const string ExtractCorpseObjectResref = "extract_corpse";
        public const string BeastTypeVariable = "BEAST_TYPE";
        public const string BeastLevelVariable = "BEAST_LEVEL";

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            Instance.CacheData();
        }

        public static BeastDetail GetBeastDetail(BeastType type)
        {
            return Instance.GetBeastDetail(type);
        }

        public static IEnumerable<BeastType> GetAllBeastTypes()
        {
            return Instance.GetAllBeastTypes();
        }

        public static BeastRoleAttribute GetBeastRoleDetail(BeastRoleType type)
        {
            return Instance.GetBeastRoleDetail(type);
        }

        public static string GetBeastId(uint beast)
        {
            return Instance.GetBeastId(beast);
        }

        public static void SetBeastId(uint beast, string beastId)
        {
            Instance.SetBeastId(beast, beastId);
        }

        public static BeastType GetBeastType(uint beast)
        {
            return Instance.GetBeastType(beast);
        }

        public static bool IsPlayerBeast(uint beast)
        {
            return Instance.IsPlayerBeast(beast);
        }

        public static void SetBeastType(uint beast, BeastType type)
        {
            Instance.SetBeastType(beast, type);
        }

        public static void GiveBeastXP(uint beast, int xp, bool ignoreBonuses)
        {
            Instance.GiveBeastXP(beast, xp, ignoreBonuses);
        }

        /// <summary>
        /// Gives RP XP to one of the player's beasts, whether it is out or in the stables.
        /// The award is capped so no XP is lost, and XP bonuses do not apply.
        /// </summary>
        /// <param name="player">The beast's owner</param>
        /// <param name="beastId">The beast to receive the XP</param>
        /// <param name="xp">The RP XP to give</param>
        /// <returns>The XP the beast accepted</returns>
        public static int GiveBeastRPXP(uint player, string beastId, int xp)
        {
            return Instance.GiveBeastRPXP(player, beastId, xp);
        }

        /// <summary>
        /// Retrieves the highest level a beast can reach with the given Tame perk level.
        /// </summary>
        public static int GetBeastLevelCap(int tamePerkLevel)
        {
            return Instance.GetBeastLevelCap(tamePerkLevel);
        }

        /// <summary>
        /// Calculates how much XP a beast can take before it reaches its level cap, so none is lost.
        /// </summary>
        /// <param name="dbBeast">The beast entity</param>
        /// <param name="levelCap">The highest level the beast can reach</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public static int GetMaxDistributableXP(Beast dbBeast, int levelCap)
        {
            return Instance.GetMaxDistributableXP(dbBeast, levelCap);
        }

        public static int GetRequiredXP(int level, int xpPenalty)
        {
            return Instance.GetRequiredXP(level, xpPenalty);
        }

        /// <summary>
        /// Determines whether a player is free to spawn a new companion (beast or droid).
        /// The game engine only permits one henchman-type associate at a time, so beast and
        /// droid activation must share this check to prevent an orphaned, master-less companion.
        /// </summary>
        /// <param name="player">The player attempting to spawn a companion.</param>
        /// <returns>An error message if a companion is already active, otherwise an empty string.</returns>
        public static string GetCompanionSlotValidationError(uint player)
        {
            return Instance.GetCompanionSlotValidationError(player);
        }

        public static void SpawnBeast(uint player, string beastId, int percentHeal)
        {
            Instance.SpawnBeast(player, beastId, percentHeal);
        }

        public static (BeastFoodType, BeastFoodType) GetLikedAndHatedFood()
        {
            return Instance.GetLikedAndHatedFood();
        }

        [NWNEventHandler(ScriptName.OnCombatPointXPDistribute)]
        public static void CombatPointXPDistributed()
        {
            Instance.CombatPointXPDistributed();
        }

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
        public static void RemoveAssociate()
        {
            Instance.RemoveAssociate();
        }

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void OnAcquireItem()
        {
            Instance.OnAcquireItem();
        }

        [NWNEventHandler(ScriptName.OnBeastBlocked)]
        public static void BeastOnBlocked()
        {
            Instance.BeastOnBlocked();
        }

        [NWNEventHandler(ScriptName.OnBeastRoundEnd)]
        public static void BeastOnEndCombatRound()
        {
            Instance.BeastOnEndCombatRound();
        }

        [NWNEventHandler(ScriptName.OnBeastConversation)]
        public static void BeastOnConversation()
        {
            Instance.BeastOnConversation();
        }

        [NWNEventHandler(ScriptName.OnBeastDamaged)]
        public static void BeastOnDamaged()
        {
            Instance.BeastOnDamaged();
        }

        [NWNEventHandler(ScriptName.OnBeastDeath)]
        public static void BeastOnDeath()
        {
            Instance.BeastOnDeath();
        }

        [NWNEventHandler(ScriptName.OnBeastDisturbed)]
        public static void BeastOnDisturbed()
        {
            Instance.BeastOnDisturbed();
        }

        [NWNEventHandler(ScriptName.OnBeastHeartbeat)]
        public static void BeastOnHeartbeat()
        {
            Instance.BeastOnHeartbeat();
        }

        [NWNEventHandler(ScriptName.OnBeastAttacked)]
        public static void BeastOnPhysicalAttacked()
        {
            Instance.BeastOnPhysicalAttacked();
        }

        [NWNEventHandler(ScriptName.OnBeastRest)]
        public static void BeastOnRested()
        {
            Instance.BeastOnRested();
        }

        [NWNEventHandler(ScriptName.OnBeastSpawn)]
        public static void BeastOnSpawn()
        {
            Instance.BeastOnSpawn();
        }

        [NWNEventHandler(ScriptName.OnBeastSpellCast)]
        public static void BeastOnSpellCastAt()
        {
            Instance.BeastOnSpellCastAt();
        }

        [NWNEventHandler(ScriptName.OnBeastUserDefined)]
        public static void BeastOnUserDefined()
        {
            Instance.BeastOnUserDefined();
        }

        [NWNEventHandler(ScriptName.OnBeastTerminate)]
        public static void OpenStablesMenu()
        {
            Instance.OpenStablesMenu();
        }

        /// <summary>
        /// Retrieves the percentage associated with a specific item property Id for the incubation stats.
        /// </summary>
        /// <param name="itemPropertyId">The incubation stat Id</param>
        /// <returns>The percentage associated or 0.0 if not found.</returns>
        public static float GetIncubationPercentageById(int itemPropertyId)
        {
            return Instance.GetIncubationPercentageById(itemPropertyId);
        }

        [NWNEventHandler(ScriptName.OnIncubatorTerminal)]
        public static void UseIncubator()
        {
            Instance.UseIncubator();
        }

        public static void CreateBeastEgg(IncubationJob job, uint player)
        {
            Instance.CreateBeastEgg(job, player);
        }

        public static uint CreateBeastEgg(BeastType beastType, uint player)
        {
            return Instance.CreateBeastEgg(beastType, player);
        }

        /// <summary>
        /// Determines if the specified item is an incubation crafting item.
        /// This includes enzymes and DNA but excludes beast eggs.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if used in incubation, false otherwise</returns>
        public static bool IsIncubationCraftingItem(uint item)
        {
            return Instance.IsIncubationCraftingItem(item);
        }

        /// <summary>
        /// Determines if the specified item is a beast egg.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if beast egg, false otherwise</returns>
        public static bool IsBeastEgg(uint item)
        {
            return Instance.IsBeastEgg(item);
        }

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnRemoveProperty()
        {
            Instance.OnRemoveProperty();
        }

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDNAExtractUsed)]
        public static void UseExtractDNAObject()
        {
            Instance.UseExtractDNAObject();
        }
    }
}
