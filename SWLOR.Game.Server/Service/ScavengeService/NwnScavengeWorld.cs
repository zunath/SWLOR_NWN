using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>The game-engine side of a visit to a scavenge site. The rules it carries out are in <see cref="ScavengeLifecycle"/>.</summary>
    public sealed class NwnScavengeWorld : IScavengeWorld
    {
        public static readonly NwnScavengeWorld Instance = new();

        /// <summary>
        /// The scavenger a site's inventory is being opened for, set until they open it. Opening it says nothing for them, and nobody else
        /// may open the site meanwhile.
        /// </summary>
        public const string OpeningVariable = "SCAVENGE_OPENING_FOR_SEARCHER";

        /// <summary>
        /// Set on a site once the scavenger it is held for has opened its inventory. While they have it open the hold does not lapse: they are
        /// still taking what the site gave, however long that takes. Closing the site releases the hold.
        /// </summary>
        public const string HolderOpenedVariable = "SCAVENGE_HOLDER_OPENED";

        /// <summary>
        /// How long after the search animation a site stays held for the scavenger who searched it if they never open it, and how often a hold
        /// on an open site is checked for a scavenger who has gone.
        /// </summary>
        public const float OpeningHoldSeconds = 60f;

        /// <summary>Set on a site while a player has closed its inventory to open the board, so closing it does not remove the site.</summary>
        public const string DoNotDestroyVariable = "DO_NOT_DESTROY";

        public const string SearchedVariable = "FULLY_HARVESTED";

        public void Message(uint player, string text)
        {
            SendMessageToPC(player, text);
        }

        public void SkillCheckMessage(uint player, string text)
        {
            FloatingTextStringOnCreature(ColorToken.SkillCheck(text), player, false);
        }

        public void GiveGatheringXp(uint player, int xp)
        {
            Skill.GiveSkillXP(player, SkillType.Gathering, xp, false, false);
        }

        public string CreateItem(uint site, string resref, int quantity, out int placed)
        {
            // The count decides: a full site creates nothing, and an item that merges into a stack can come back as an invalid handle.
            var before = NwnQuickCraftWorld.CountComponent(site, resref);
            var item = CreateItemOnObject(resref, site, quantity);
            placed = Math.Clamp(NwnQuickCraftWorld.CountComponent(site, resref) - before, 0, quantity);
            return GetIsObjectValid(item) ? GetName(item) : Cache.GetItemNameByResref(resref);
        }

        public void SavePendingFinds(uint player, IReadOnlyList<ScavengeFind> finds)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.PendingScavengeFinds = finds
                .GroupBy(find => find.Resref)
                .ToDictionary(group => group.Key, group => group.Sum(find => find.Quantity));
            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Gives a player the finds of a board whose visit never put them in a site (a restart, a death, a logout or a missing site), plus
        /// any finds an earlier return could not fit, and keeps whatever still has no room.
        /// </summary>
        /// <remarks>
        /// Three pieces of the player's record take part. <c>PendingScavengeFinds</c> holds the finds of a board in progress, or, between the
        /// save and the clear of a return, the portion that return delivered. <c>PendingScavengeReturnSequence</c> stamps that delivered
        /// portion with the craft file sequence of its export. <c>ScavengeFindsAwaitingRoom</c> holds what had no room.
        /// Invariants:
        /// <list type="bullet">
        /// <item>An item is delivered only if the inventory really gained it. An item the engine put anywhere else (on the ground) is
        /// destroyed and its quantity stays waiting, so nothing is looted twice or lost.</item>
        /// <item>Only the delivered portion is ever stamped and cleared. The remainder is written in the same save as the stamp, as plain
        /// unstamped waiting finds, so no login can read the stamp as covering it and clear it.</item>
        /// <item>Before the stamp is saved the record still holds everything to be tried, and after it the record holds the delivered
        /// portion (stamped) and the remainder, together exactly what was wanted. A crash before the export leaves the stamp above the
        /// file's sequence, so the next login tries both again; a crash after it leaves the stamp covered, so that login only clears
        /// the delivered portion and then tries the remainder. At login the check reads the sequence the file loaded with; later it
        /// reads the last export.</item>
        /// <item>The waiting finds are not <c>PendingScavengeFinds</c>, which a new board overwrites, so a board started while some are
        /// waiting cannot merge with, replace or duplicate them. Every visit start first tries to deliver them.</item>
        /// </list>
        /// </remarks>
        public void ReturnPendingFinds(uint player, bool atLogin = false)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer == null)
                return;

            dbPlayer.PendingScavengeFinds ??= new Dictionary<string, int>();
            dbPlayer.ScavengeFindsAwaitingRoom ??= new Dictionary<string, int>();

            var returned = dbPlayer.PendingScavengeReturnSequence;
            if (returned > 0 && dbPlayer.PendingScavengeFinds.Count > 0)
            {
                var inFile = GetLocalInt(player, atLogin ? CraftSessionFileSequence.LoadedVariable : CraftSessionFileSequence.ExportedVariable);
                if (inFile >= returned)
                {
                    // A file already holds the delivered finds: only the stamp and the delivered portion go. Waiting finds are not covered.
                    ClearDeliveredFinds(dbPlayer);
                }
                else if (!atLogin)
                {
                    // They are in this session's inventory waiting for its export.
                    return;
                }
            }
            else if (returned > 0)
            {
                dbPlayer.PendingScavengeReturnSequence = 0;
                DB.Set(dbPlayer);
            }

            var wanted = ScavengeReturn.Merge(dbPlayer.PendingScavengeFinds, dbPlayer.ScavengeFindsAwaitingRoom);
            if (wanted.Count == 0)
                return;

            var names = new Dictionary<string, string>();
            var split = ScavengeReturn.Divide(wanted, (resref, quantity) => Deliver(player, resref, quantity, names));

            // The remainder is saved with the stamp (or alone when nothing was delivered) as plain waiting finds.
            dbPlayer.ScavengeFindsAwaitingRoom = split.Remainder.ToDictionary(find => find.Key, find => find.Value);

            var exportable = GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);
            if (split.Delivered.Count > 0 && exportable)
            {
                var sequence = GetLocalInt(player, CraftSessionFileSequence.Variable) + 1;
                SetLocalInt(player, CraftSessionFileSequence.Variable, sequence);
                dbPlayer.PendingScavengeFinds = split.Delivered.ToDictionary(find => find.Key, find => find.Value);
                dbPlayer.PendingScavengeReturnSequence = sequence;
                DB.Set(dbPlayer);
                ExportSingleCharacter(player);
                SetLocalInt(player, CraftSessionFileSequence.ExportedVariable, sequence);
            }

            ClearDeliveredFinds(dbPlayer);
            SendMessageToPC(player, ReturnMessage(split, names));
        }

        private static void ClearDeliveredFinds(Player dbPlayer)
        {
            dbPlayer.PendingScavengeFinds = new Dictionary<string, int>();
            dbPlayer.PendingScavengeReturnSequence = 0;
            DB.Set(dbPlayer);
        }

        /// <summary>
        /// Creates the item in the player's inventory and returns how many the inventory gained. A created item that is not in the
        /// player's inventory (a full inventory leaves it on the ground) is destroyed, so it cannot be looted while its quantity stays
        /// waiting. Credits are added to the player's gold, never as an inventory item, so their delivery is what the gold gained.
        /// </summary>
        private static int Deliver(uint player, string resref, int quantity, Dictionary<string, string> names)
        {
            if (resref == ScavengeLoot.GoldResref)
            {
                var goldBefore = GetGold(player);
                GiveGoldToCreature(player, quantity);
                names[resref] = "Credits";
                return Math.Clamp(GetGold(player) - goldBefore, 0, quantity);
            }

            var before = CountInInventory(player, resref);
            var item = CreateItemOnObject(resref, player, quantity);
            if (GetIsObjectValid(item))
            {
                names[resref] = GetName(item);
                if (GetItemPossessor(item) != player)
                    DestroyObject(item);
            }

            return CountInInventory(player, resref) - before;
        }

        private static int CountInInventory(uint player, string resref)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    count += GetItemStackSize(item);
            }

            return count;
        }

        private static string ReturnMessage(ScavengeReturnSplit split, IReadOnlyDictionary<string, string> names)
        {
            var waiting = split.Remainder.Count == 0
                ? string.Empty
                : $"There is no room in your inventory for {ScavengeReturn.Describe(split.Remainder, names)}. " +
                  "It stays waiting for you and comes to you when you free up space and open a search site, respawn, or log in.";

            if (split.Delivered.Count == 0)
                return $"Your last search ended before you could collect it. {waiting}";

            var delivered = $"Your last search ended before you could collect it. What it found is in your inventory: {ScavengeReturn.Describe(split.Delivered, names)}.";
            return waiting.Length == 0 ? delivered : $"{delivered} {waiting}";
        }

        public void SpawnBonusLoot(uint site, uint player)
        {
            Loot.SpawnLoot(site, player, "LOOT_TABLE_");
        }

        public void MarkSearched(uint site)
        {
            SetLocalBool(site, SearchedVariable, true);
        }

        public void HoldSite(uint player, uint site, double animationSeconds)
        {
            // Hold the site for the scavenger until they open it, so nobody else can take what it gave while the search animation runs.
            SetLocalObject(site, OpeningVariable, player);
            DeleteLocalBool(site, HolderOpenedVariable);
            ScheduleHoldLapse(site, player, (float)animationSeconds + OpeningHoldSeconds);
        }

        public void OpenSite(uint player, uint site, double animationSeconds)
        {
            HoldSite(player, site, animationSeconds);
            AssignCommand(player, () =>
            {
                if (animationSeconds > 0)
                    ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, (float)animationSeconds);

                ActionInteractObject(site);
            });
        }

        /// <summary>
        /// Ends the hold on a site a scavenger never opened (they logged out) so it is not held for good. A scavenger who has it open keeps
        /// the hold for as long as they stay by the site; it is checked again every <see cref="OpeningHoldSeconds"/> in case they left without closing it.
        /// </summary>
        private static void ScheduleHoldLapse(uint site, uint player, float seconds)
        {
            AssignCommand(GetModule(), () => DelayCommand(seconds, () =>
            {
                if (!GetIsObjectValid(site) || GetLocalObject(site, OpeningVariable) != player)
                    return;

                if (GetLocalBool(site, HolderOpenedVariable) && GetIsObjectValid(player) &&
                    GetArea(player) == GetArea(site) && GetDistanceBetween(player, site) <= ScavengeLifecycle.MaxDistance)
                {
                    ScheduleHoldLapse(site, player, OpeningHoldSeconds);
                    return;
                }

                DeleteLocalObject(site, OpeningVariable);
                DeleteLocalBool(site, HolderOpenedVariable);
            }));
        }

        public void DestroySite(uint player, uint site, double animationSeconds)
        {
            if (animationSeconds > 0 && GetIsObjectValid(player))
                AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetLow, 1.0f, (float)animationSeconds));

            // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
            // Use EffectDeath to ensure that we trigger death processing.
            AssignCommand(GetModule(), () => DelayCommand((float)animationSeconds, () =>
            {
                if (!GetIsObjectValid(site))
                    return;

                SetPlotFlag(site, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), site);
            }));
        }

        public bool OpenWindow(uint player, ScavengeSession session)
        {
            // The result screen of the last visit may still be open.
            if (Gui.IsWindowOpen(player, GuiWindowType.Search))
                Gui.ClosePlayerWindow(player, GuiWindowType.Search);

            Gui.TogglePlayerWindow(player, GuiWindowType.Search, new SearchPayload(session));
            return Gui.IsWindowOpen(player, GuiWindowType.Search);
        }

        public void CloseWindow(uint player)
        {
            if (Gui.IsWindowOpen(player, GuiWindowType.Search))
                Gui.ClosePlayerWindow(player, GuiWindowType.Search);
        }

        public ScavengePlayerState GetPlayerState(uint player, uint site)
        {
            if (!GetIsObjectValid(player))
                return default;

            return new ScavengePlayerState(
                true,
                GetIsDead(player),
                GetIsInCombat(player),
                Gui.IsWindowOpen(player, GuiWindowType.Search),
                GetIsObjectValid(site) ? GetDistanceBetween(player, site) : 0f,
                GetIsObjectValid(site));
        }

        public DateTime UtcNow => DateTime.UtcNow;

        public void Schedule(float seconds, Action action)
        {
            AssignCommand(GetModule(), () => DelayCommand(seconds, action));
        }
    }
}
