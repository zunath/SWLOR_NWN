using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using Player = SWLOR.Game.Server.Entity.Player;
using Skill = SWLOR.Game.Server.Core.NWScript.Enum.Skill;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class PlayerInitialization
    {
        /// <summary>
        /// Handles 
        /// </summary>
        [NWNEventHandler("mod_enter")]
        public static void InitializePlayer()
        {
            var player = GetEnteringObject();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player();

            // Already been initialized. Don't do it again.
            if (dbPlayer.Version >= 1) return;

            ClearInventory(player);
            AutoLevelPlayer(player);
            InitializeSkills(player);
            InitializeSavingThrows(player);
            RemoveNWNSpells(player);
            ClearFeats(player);
            GrantBasicFeats(player);
            InitializeHotBar(player);
            AdjustStats(player, dbPlayer);

            DB.Set(playerId, dbPlayer);
        }

        private static void AutoLevelPlayer(uint player)
        {
            // Capture original stats before we level up the player.
            var str = Creature.GetRawAbilityScore(player, AbilityType.Strength);
            var con = Creature.GetRawAbilityScore(player, AbilityType.Constitution);
            var dex = Creature.GetRawAbilityScore(player, AbilityType.Dexterity);
            var @int = Creature.GetRawAbilityScore(player, AbilityType.Intelligence);
            var wis = Creature.GetRawAbilityScore(player, AbilityType.Wisdom);
            var cha = Creature.GetRawAbilityScore(player, AbilityType.Charisma);

            GiveXPToCreature(player, 10000);

            for(var level = 1; level <= 5; level++)
            {
                var @class = GetClassByPosition(1, player);
                LevelUpHenchman(player, @class);
            }

            // Set stats back to how they were on entry.
            Creature.SetRawAbilityScore(player, AbilityType.Strength, str);
            Creature.SetRawAbilityScore(player, AbilityType.Constitution, con);
            Creature.SetRawAbilityScore(player, AbilityType.Dexterity, dex);
            Creature.SetRawAbilityScore(player, AbilityType.Intelligence, @int);
            Creature.SetRawAbilityScore(player, AbilityType.Wisdom, wis);
            Creature.SetRawAbilityScore(player, AbilityType.Charisma, cha);
        }

        /// <summary>
        /// Wipes a player's equipped items and inventory.
        /// </summary>
        /// <param name="player">The player to wipe an inventory for.</param>
        private static void ClearInventory(uint player)
        {
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            {
                var item = GetItemInSlot((InventorySlot)slot, player);
                if (!GetIsObjectValid(item)) continue;

                DestroyObject(item);
            }

            var inventory = GetFirstItemInInventory(player);
            while (GetIsObjectValid(inventory))
            {
                DestroyObject(inventory);
                inventory = GetNextItemInInventory(player);
            }
        }

        /// <summary>
        /// Initializes all player NWN skills to zero.
        /// </summary>
        /// <param name="player">The player to modify</param>
        private static void InitializeSkills(uint player)
        {
            for (var iCurSkill = 1; iCurSkill <= 27; iCurSkill++)
            {
                var skill = (Skill) (iCurSkill - 1);
                Creature.SetSkillRank(player, skill, 0);
            }
        }

        /// <summary>
        /// Initializes all player saving throws to zero.
        /// </summary>
        /// <param name="player">The player to modify</param>
        private static void InitializeSavingThrows(uint player)
        {
            SetFortitudeSavingThrow(player, 0);
            SetReflexSavingThrow(player, 0);
            SetWillSavingThrow(player, 0);
        }

        /// <summary>
        /// Removes all NWN spells from a player.
        /// </summary>
        /// <param name="player">The player to modify.</param>
        private static void RemoveNWNSpells(uint player)
        {
            var @class = GetClassByPosition(1, player);
            for (var index = 0; index <= 255; index++)
            {
                Creature.RemoveKnownSpell(player, @class, 0, index);
            }
        }

        private static void ClearFeats(uint player)
        {
            var numberOfFeats = Creature.GetFeatCount(player);
            for (var currentFeat = numberOfFeats; currentFeat >= 0; currentFeat--)
            {
                Creature.RemoveFeat(player, Creature.GetFeatByIndex(player, currentFeat - 1));
            }
        }

        private static void GrantBasicFeats(uint player)
        {
            Creature.AddFeatByLevel(player, Feat.ArmorProficiencyLight, 1);
            Creature.AddFeatByLevel(player, Feat.ArmorProficiencyMedium, 1);
            Creature.AddFeatByLevel(player, Feat.ArmorProficiencyHeavy, 1);
            Creature.AddFeatByLevel(player, Feat.ShieldProficiency, 1);
            Creature.AddFeatByLevel(player, Feat.WeaponProficiencyExotic, 1);
            Creature.AddFeatByLevel(player, Feat.WeaponProficiencyMartial, 1);
            Creature.AddFeatByLevel(player, Feat.WeaponProficiencySimple, 1);
            Creature.AddFeatByLevel(player, Feat.UncannyDodge1, 1);
            Creature.AddFeatByLevel(player, Feat.OpenRestMenu, 1);
            Creature.AddFeatByLevel(player, Feat.ChatCommandTargeter, 1);
            Creature.AddFeatByLevel(player, Feat.StructureTool, 1);
        }


        private static void InitializeHotBar(uint player)
        {
            var openRestMenu = PlayerQuickBarSlot.UseFeat(Feat.OpenRestMenu);
            var chatCommandTargeter = PlayerQuickBarSlot.UseFeat(Feat.ChatCommandTargeter);

            Core.NWNX.Player.SetQuickBarSlot(player, 0, openRestMenu);
            Core.NWNX.Player.SetQuickBarSlot(player, 1, chatCommandTargeter);
        }

        /// <summary>
        /// Modifies the player's unallocated SP, version, max HP, and other assorted stats.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="dbPlayer">The player entity.</param>
        private static void AdjustStats(uint player, Player dbPlayer)
        {
            dbPlayer.UnallocatedSP = 10;
            dbPlayer.Version = 1;
            dbPlayer.Name = GetName(player);
            Stat.AdjustMaxHP(dbPlayer, player, 10);
            Stat.AdjustMaxSTM(dbPlayer, 10);
            Stat.AdjustBAB(dbPlayer, player, 1);
            dbPlayer.HP = GetCurrentHitPoints(player);
            dbPlayer.FP = Stat.GetMaxFP(player, dbPlayer);
            dbPlayer.Stamina = Stat.GetMaxStamina(player, dbPlayer);

            dbPlayer.BaseStats[AbilityType.Strength] = Creature.GetRawAbilityScore(player, AbilityType.Strength);
            dbPlayer.BaseStats[AbilityType.Dexterity] = Creature.GetRawAbilityScore(player, AbilityType.Dexterity);
            dbPlayer.BaseStats[AbilityType.Constitution] = Creature.GetRawAbilityScore(player, AbilityType.Constitution);
            dbPlayer.BaseStats[AbilityType.Wisdom] = Creature.GetRawAbilityScore(player, AbilityType.Wisdom);
            dbPlayer.BaseStats[AbilityType.Intelligence] = Creature.GetRawAbilityScore(player, AbilityType.Intelligence);
            dbPlayer.BaseStats[AbilityType.Charisma] = Creature.GetRawAbilityScore(player, AbilityType.Charisma);
        }
    }
}
