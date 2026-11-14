using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>Design Schematics (plan section 7): using one unlocks its Hull Design for the character's refits.</summary>
    public static partial class Space
    {
        /// <summary>The character-file local naming the schematic resrefs of the designs being learned, comma-separated.</summary>
        private const string LearnedDesignVariable = "HULL_DESIGN_LEARNED";

        /// <summary>
        /// Spends the schematic and learns its design. The schematic leaves the character file only after this script,
        /// so the design is saved as learned after a save without it, with a mark naming the design: a restart before
        /// that save leaves the schematic unspent, and one after learns the design at the next login. The mark keeps every
        /// design still waiting, so a failed save followed by another schematic loses neither design.
        /// </summary>
        public static void LearnHullDesign(uint user, uint schematic)
        {
            var resref = GetResRef(schematic);
            var design = HullDesignRules.FindBySchematic(resref);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(user));
            if (design == null || dbPlayer == null)
                return;

            if (dbPlayer.UnlockedHullDesigns.ContainsKey(HullDesignRules.Key(design)))
            {
                SendMessageToPC(user, $"You already know the {design.ShipName} - {design.Name} Hull Design.");
                return;
            }

            var pending = GetPendingDesigns(user);
            if (pending.Contains(resref))
            {
                SendMessageToPC(user, $"You are already learning the {design.ShipName} - {design.Name} Hull Design.");
                return;
            }

            Item.ReduceItemStack(schematic, 1);
            pending.Add(resref);
            SetPendingDesigns(user, pending);
            AssignCommand(GetModule(), () => DelayCommand(0.1f, () =>
            {
                // A player who logged out first saved the mark without the schematic; their next login learns the design.
                if (!GetIsObjectValid(user))
                    return;

                // A save that fails leaves the mark: the next login learns the design once a later save holds it.
                if (!CharacterCheckpoint.Save(user))
                    return;

                // The save holds every schematic spent so far, including one whose own save failed earlier.
                foreach (var spent in GetPendingDesigns(user))
                    SaveLearnedDesign(user, spent);
            }));
        }

        /// <summary>The schematic resrefs of the designs whose schematics are spent but not yet saved as learned.</summary>
        private static List<string> GetPendingDesigns(uint player)
        {
            return ObjectPlugin.GetString(player, LearnedDesignVariable)
                .Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();
        }

        private static void SetPendingDesigns(uint player, List<string> pending)
        {
            if (pending.Count == 0)
                ObjectPlugin.DeleteString(player, LearnedDesignVariable);
            else
                ObjectPlugin.SetString(player, LearnedDesignVariable, string.Join(",", pending), true);
        }

        /// <summary>Saves the design as learned (again after a restart, changing nothing) and clears its mark.</summary>
        private static void SaveLearnedDesign(uint player, string resref)
        {
            var design = HullDesignRules.FindBySchematic(resref);
            var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
            if (design != null && dbPlayer != null && HullDesignRules.Unlock(dbPlayer.UnlockedHullDesigns, design, DateTime.UtcNow))
            {
                DB.Set(dbPlayer);
                SendMessageToPC(player, ColorToken.Green(
                    $"You learn the {design.ShipName} - {design.Name} Hull Design. Refit a {design.ShipName} to it in Ship Management at a starport."));
            }

            // A learned design stays learned, so a mark saved again later changes nothing.
            var pending = GetPendingDesigns(player);
            if (pending.Remove(resref))
                SetPendingDesigns(player, pending);
        }

        /// <summary>Learns the designs whose schematics a restart left spent in the character file but unsaved as learned.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void SettleLearnedHullDesign()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            foreach (var resref in GetPendingDesigns(player))
            {
                SaveLearnedDesign(player, resref);
                Log.Write(LogGroup.Space, $"HULL DESIGN - {PlayerName.GetAuditName(player)} - settled the design of {resref} after a restart");
            }
        }
    }
}
