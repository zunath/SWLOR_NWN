using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.LootService;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// Astromech gear in loot. Every looted armor-slot piece, staff and rifle has an astromech form
    /// (<see cref="AstromechItemTwins"/>), and a form drops wherever its humanoid piece drops: each roll of a loot
    /// table is followed by a second roll of the same table, at the same chance, that keeps its pick only when the
    /// pick has an astromech form and drops that form. So a form is exactly as likely as the piece it copies, the
    /// humanoid drop rates are untouched, and no loot table lists astromech gear of its own.
    /// </summary>
    public static class AstromechLoot
    {
        /// <summary>
        /// The astromech forms a loot table yields for the given rolls.
        /// </summary>
        /// <param name="table">The loot table that was just rolled for its own items.</param>
        /// <param name="attempts">How many times the table is rolled.</param>
        /// <param name="passesChance">Rolls the table's drop chance once; true when the attempt yields an item.</param>
        /// <param name="pick">Picks one entry of the table, with its rare find bonus applied.</param>
        /// <returns>The entries picked and the astromech form each one drops as.</returns>
        public static List<(LootTableItem Source, string Resref)> Roll(
            LootTable table,
            int attempts,
            Func<bool> passesChance,
            Func<LootTable, LootTableItem> pick)
        {
            var forms = new List<(LootTableItem, string)>();
            if (table == null || table.Count == 0 || !HasForms(table))
                return forms;

            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                if (!passesChance())
                    continue;

                var item = pick(table);
                if (AstromechItemTwins.HasTwin(item.Resref))
                    forms.Add((item, AstromechItemTwins.Resolve(item.Resref, Enumeration.BodyFrame.Astromech)));
            }

            return forms;
        }

        /// <summary>
        /// True when the table holds at least one piece with an astromech form. Most tables hold none (materials,
        /// credits, recipes), and those are not rolled a second time.
        /// </summary>
        public static bool HasForms(LootTable table)
        {
            foreach (var item in table)
            {
                if (AstromechItemTwins.HasTwin(item.Resref))
                    return true;
            }

            return false;
        }
    }
}
