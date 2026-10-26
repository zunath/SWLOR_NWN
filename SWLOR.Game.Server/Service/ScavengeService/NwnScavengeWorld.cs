using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
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

        public string CreateItem(uint site, string resref, int quantity)
        {
            var item = CreateItemOnObject(resref, site, quantity);
            return GetIsObjectValid(item) ? GetName(item) : resref;
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
        /// Gives a player the finds of a board whose visit never put them in a site (a restart, a death, a logout or a missing site), and
        /// clears them.
        /// </summary>
        public void ReturnPendingFinds(uint player)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (dbPlayer?.PendingScavengeFinds == null || dbPlayer.PendingScavengeFinds.Count == 0)
                return;

            var names = new List<string>();
            foreach (var (resref, quantity) in dbPlayer.PendingScavengeFinds)
            {
                var item = CreateItemOnObject(resref, player, quantity);
                names.Add($"{quantity}x {(GetIsObjectValid(item) ? GetName(item) : resref)}");
            }

            dbPlayer.PendingScavengeFinds.Clear();
            DB.Set(dbPlayer);
            // Save the character file with the record, so a crash cannot leave the finds cleared from the record but missing from the file.
            if (GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player))
                ExportSingleCharacter(player);
            SendMessageToPC(player, $"Your last search ended before you could collect it. What it found is in your inventory: {string.Join(", ", names)}.");
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
