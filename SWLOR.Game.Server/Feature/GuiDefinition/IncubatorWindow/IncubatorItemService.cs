using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow
{
    /// <summary>
    /// The incubator's items: placing and returning the DNA and the three enzymes, keeping the stage totals in step with them,
    /// restoring what stayed in the incubator, and the click bodies that pick them.
    /// </summary>
    public class IncubatorItemService
    {
        private IncubatorJobCalculator IncubatorJobCalculator { get; }
        private IItemService Item { get; }
        private ITargetingService Targeting { get; }

        public IncubatorItemService(IncubatorJobCalculator incubatorJobCalculator, IItemService item, ITargetingService targeting)
        {
            IncubatorJobCalculator = incubatorJobCalculator;
            Item = item;
            Targeting = targeting;
        }

        /// <summary>
        /// Returns the enzymes, then the DNA. An item without room stays in the incubator, and the DNA stays while any
        /// enzyme does.
        /// </summary>
        /// <returns>False when an item stayed.</returns>
        public bool RemoveDNA(IIncubatorHost host)
        {
            uint item;
            if (!string.IsNullOrWhiteSpace(host.Session.HydrolaseItem) &&
                Item.TryAcquireItem(host.Player, item = ObjectPlugin.Deserialize(host.Session.HydrolaseItem)))
            {
                SubtractItemStats(host, item);
                host.Session.HydrolaseItem = string.Empty;
                host.Session.HydrolaseColor = EnzymeColorType.Invalid;
                host.HydrolaseItemResref = IncubatorSession.Blank;
                host.HydrolaseTitle = "Hydrolase";
            host.HydrolaseDetail = "Empty";
            }
            if (!string.IsNullOrWhiteSpace(host.Session.LyaseItem) &&
                Item.TryAcquireItem(host.Player, item = ObjectPlugin.Deserialize(host.Session.LyaseItem)))
            {
                SubtractItemStats(host, item);
                host.Session.LyaseItem = string.Empty;
                host.Session.LyaseColor = EnzymeColorType.Invalid;
                host.LyaseItemResref = IncubatorSession.Blank;
                host.LyaseTitle = "Lyase";
            host.LyaseDetail = "Empty";
            }
            if (!string.IsNullOrWhiteSpace(host.Session.IsomeraseItem) &&
                Item.TryAcquireItem(host.Player, item = ObjectPlugin.Deserialize(host.Session.IsomeraseItem)))
            {
                SubtractItemStats(host, item);
                host.Session.IsomeraseItem = string.Empty;
                host.Session.IsomeraseColor = EnzymeColorType.Invalid;
                host.IsomeraseItemResref = IncubatorSession.Blank;
                host.IsomeraseTitle = "Isomerase";
            host.IsomeraseDetail = "Empty";
            }

            var enzymesReturned = string.IsNullOrWhiteSpace(host.Session.HydrolaseItem) &&
                                  string.IsNullOrWhiteSpace(host.Session.LyaseItem) &&
                                  string.IsNullOrWhiteSpace(host.Session.IsomeraseItem);
            if (enzymesReturned && !string.IsNullOrWhiteSpace(host.Session.DnaItem) &&
                Item.TryAcquireItem(host.Player, ObjectPlugin.Deserialize(host.Session.DnaItem)))
            {
                host.Session.DnaItem = string.Empty;
                host.DNAItemResref = IncubatorSession.Blank;
            }

            var allReturned = enzymesReturned && string.IsNullOrWhiteSpace(host.Session.DnaItem);
            if (allReturned)
            {
                host.IsErraticGeniusChecked = false;
                host.Session.ClearStats();
            }

            return allReturned;
        }

        /// <summary>Returns items another incubator kept. Returns false when some still do not fit.</summary>
        public bool ReturnItemsHeldElsewhere(IIncubatorHost host)
        {
            if (!string.IsNullOrWhiteSpace(host.Session.HydrolaseItem) && Item.TryAcquireItem(host.Player, ObjectPlugin.Deserialize(host.Session.HydrolaseItem)))
            {
                host.Session.HydrolaseItem = string.Empty;
                host.Session.HydrolaseColor = EnzymeColorType.Invalid;
            }
            if (!string.IsNullOrWhiteSpace(host.Session.LyaseItem) && Item.TryAcquireItem(host.Player, ObjectPlugin.Deserialize(host.Session.LyaseItem)))
            {
                host.Session.LyaseItem = string.Empty;
                host.Session.LyaseColor = EnzymeColorType.Invalid;
            }
            if (!string.IsNullOrWhiteSpace(host.Session.IsomeraseItem) && Item.TryAcquireItem(host.Player, ObjectPlugin.Deserialize(host.Session.IsomeraseItem)))
            {
                host.Session.IsomeraseItem = string.Empty;
                host.Session.IsomeraseColor = EnzymeColorType.Invalid;
            }
            if (!string.IsNullOrWhiteSpace(host.Session.DnaItem) && Item.TryAcquireItem(host.Player, ObjectPlugin.Deserialize(host.Session.DnaItem)))
                host.Session.DnaItem = string.Empty;

            return string.IsNullOrWhiteSpace(host.Session.DnaItem) && string.IsNullOrWhiteSpace(host.Session.HydrolaseItem) &&
                   string.IsNullOrWhiteSpace(host.Session.LyaseItem) && string.IsNullOrWhiteSpace(host.Session.IsomeraseItem);
        }

        /// <summary>Shows the items that stayed in the incubator when its window last closed.</summary>
        public void RestoreHeldItems(IIncubatorHost host)
        {
            host.HydrolaseDetail = "Empty";
            host.LyaseDetail = "Empty";
            host.IsomeraseDetail = "Empty";
            host.HydrolaseTitle = "Hydrolase";
            host.LyaseTitle = "Lyase";
            host.IsomeraseTitle = "Isomerase";

            if (!string.IsNullOrWhiteSpace(host.Session.DnaItem))
            {
                var item = ObjectPlugin.Deserialize(host.Session.DnaItem);
                LoadDNAStats(host, item);
                host.DNAItemResref = Item.GetIconResref(item);
                DestroyObject(item);
            }
            if (!string.IsNullOrWhiteSpace(host.Session.HydrolaseItem))
            {
                var item = ObjectPlugin.Deserialize(host.Session.HydrolaseItem);
                host.Session.HydrolaseColor = AddItemStats(host, item);
                host.HydrolaseItemResref = Item.GetIconResref(item);
                host.HydrolaseTitle = EnzymeTitle("Hydrolase", host.Session.HydrolaseColor);
                host.HydrolaseDetail = DescribeEnzyme(item);
                DestroyObject(item);
            }
            if (!string.IsNullOrWhiteSpace(host.Session.LyaseItem))
            {
                var item = ObjectPlugin.Deserialize(host.Session.LyaseItem);
                host.Session.LyaseColor = AddItemStats(host, item);
                host.LyaseItemResref = Item.GetIconResref(item);
                host.LyaseTitle = EnzymeTitle("Lyase", host.Session.LyaseColor);
                host.LyaseDetail = DescribeEnzyme(item);
                DestroyObject(item);
            }
            if (!string.IsNullOrWhiteSpace(host.Session.IsomeraseItem))
            {
                var item = ObjectPlugin.Deserialize(host.Session.IsomeraseItem);
                host.Session.IsomeraseColor = AddItemStats(host, item);
                host.IsomeraseItemResref = Item.GetIconResref(item);
                host.IsomeraseTitle = EnzymeTitle("Isomerase", host.Session.IsomeraseColor);
                host.IsomeraseDetail = DescribeEnzyme(item);
                DestroyObject(item);
            }
        }

        public static void LoadJobStats(IIncubatorHost host, IncubationJob job)
        {
            host.Session.BaseStats[IncubationStatType.MutationChance] = job.MutationChance;
            host.Session.BaseStats[IncubationStatType.AttackPurity] = job.AttackPurity;
            host.Session.BaseStats[IncubationStatType.AccuracyPurity] = job.AccuracyPurity;
            host.Session.BaseStats[IncubationStatType.EvasionPurity] = job.EvasionPurity;
            host.Session.BaseStats[IncubationStatType.LearningPurity] = job.LearningPurity;
            host.Session.BaseStats[IncubationStatType.XPPenalty] = job.XPPenalty;
            host.Session.BaseStats[IncubationStatType.PhysicalDefensePurity] = job.DefensePurities[CombatDamageType.Physical];
            host.Session.BaseStats[IncubationStatType.ForceDefensePurity] = job.DefensePurities[CombatDamageType.Force];
            foreach (var stat in IncubationProjection.ResistancePurityStats)
            {
                var resistance = IncubationProjection.GetResistanceType(stat);
                host.Session.BaseStats[stat] = job.ResistancePurities.TryGetValue(resistance, out var value) ? value : 0;
            }
        }

        public Action OnClickDNA(IIncubatorHost host) => () =>
        {
            if (IncubatorJobCalculator.GetJob(host) != null)
                return;

            if (!string.IsNullOrWhiteSpace(host.Session.DnaItem))
            {
                host.ShowModal("Will you remove the DNA from the incubator? All enzymes will also be removed.", () =>
                {
                    if (!RemoveDNA(host))
                        FloatingTextStringOnCreature(ItemService.InventoryFullMessage, host.Player, false);
                    host.SwitchViews();
                }, host.SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, "Select a DNA item from your inventory.", item =>
                {
                    if (GetResRef(item) != BeastMasteryService.DNAResref)
                    {
                        FloatingTextStringOnCreature("Only DNA items may be selected.", host.Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, host.Player, false);
                        return;
                    }

                    LoadDNAStats(host, item);

                    host.DNAItemResref = Item.GetIconResref(item);
                    host.Session.DnaItem = ObjectPlugin.Serialize(item);
                    DestroyObject(item);

                    host.RefreshAllStats();
                });
            }
        };

        private static void LoadDNAStats(IIncubatorHost host, uint item)
        {
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);
                if (type == ItemPropertyType.DNAType)
                {
                    host.Session.DnaType = (BeastType)GetItemPropertySubType(ip);
                }
                else if (type == ItemPropertyType.Incubation)
                {
                    var subType = (IncubationStatType)GetItemPropertySubType(ip);
                    if (!IncubationProjection.IsKnownStat(subType))
                        throw new ArgumentOutOfRangeException(nameof(subType), subType, "Unknown incubation stat on DNA.");

                    host.Session.BaseStats[subType] = GetItemPropertyCostTableValue(ip);
                }
            }
        }

        private static EnzymeColorType AddItemStats(IIncubatorHost host, uint item)
        {
            return ApplyItemStats(host, item, 1);
        }

        private static void SubtractItemStats(IIncubatorHost host, uint item)
        {
            ApplyItemStats(host, item, -1);
        }

        /// <summary>
        /// Adds (sign 1) or removes (sign -1) an enzyme's incubation stats from this stage's totals.
        /// Only Incubation properties count; the enzyme color property is read, never applied as a stat.
        /// </summary>
        private static EnzymeColorType ApplyItemStats(IIncubatorHost host, uint item, int sign)
        {
            var colorType = EnzymeColorType.Invalid;
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var ipType = GetItemPropertyType(ip);
                if (ipType == ItemPropertyType.Incubation)
                {
                    var type = (IncubationStatType)GetItemPropertySubType(ip);
                    if (!IncubationProjection.IsKnownStat(type))
                        throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown incubation stat on enzyme.");

                    host.Session.StageStats.TryGetValue(type, out var current);
                    host.Session.StageStats[type] = current + sign * GetItemPropertyCostTableValue(ip);
                }
                else if (ipType == ItemPropertyType.EnzymeColor)
                {
                    colorType = (EnzymeColorType)GetItemPropertySubType(ip);
                }
            }

            return colorType;
        }

        private static string DescribeEnzyme(uint item)
        {
            var color = EnzymeColorType.Invalid;
            var stats = new List<string>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var ipType = GetItemPropertyType(ip);
                if (ipType == ItemPropertyType.EnzymeColor)
                {
                    color = (EnzymeColorType)GetItemPropertySubType(ip);
                }
                else if (ipType == ItemPropertyType.Incubation)
                {
                    var stat = (IncubationStatType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip) / 10f;
                    stats.Add($"{IncubationProjection.GetShortStatName(stat)}+{value:0.0}");
                }
            }

            return stats.Count == 0 ? "No stats" : string.Join(" ", stats);
        }

        /// <summary>Names a filled enzyme slot by its color, for example "Blue hydrolase".</summary>
        public static string EnzymeTitle(string name, EnzymeColorType color)
        {
            return color == EnzymeColorType.Invalid ? name : $"{color} {name.ToLower()}";
        }

        public delegate void EnzymeSlotSetter(string serialized, EnzymeColorType color, string icon, string detail);

        public Action OnClickEnzyme(
            IIncubatorHost host,
            string name,
            string resrefPrefix,
            Func<string> getSerialized,
            EnzymeSlotSetter setSlot) => () =>
        {
            var job = IncubatorJobCalculator.GetJob(host);
            if (job != null && Clock.UtcNow < job.DateCompleted)
                return;
            if (job != null && job.CurrentStage >= IncubatorJobCalculator.NumberOfStages)
                return;

            var serialized = getSerialized();
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                host.ShowModal($"Will you remove the {name} enzyme?", () =>
                {
                    var item = ObjectPlugin.Deserialize(serialized);
                    if (!Item.TryAcquireItem(host.Player, item))
                    {
                        FloatingTextStringOnCreature(ItemService.InventoryFullMessage, host.Player, false);
                        host.SwitchViews();
                        return;
                    }

                    SubtractItemStats(host, item);
                    setSlot(string.Empty, EnzymeColorType.Invalid, IncubatorSession.Blank, "Empty");
                    host.SwitchViews();
                }, host.SwitchViews);
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, $"Select a {name} item from your inventory.",
                item =>
                {
                    if (!GetResRef(item).StartsWith(resrefPrefix))
                    {
                        FloatingTextStringOnCreature($"Only {name} items may be selected.", host.Player, false);
                        return;
                    }

                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        FloatingTextStringOnCreature(error, host.Player, false);
                        return;
                    }

                    var icon = Item.GetIconResref(item);
                    var detail = DescribeEnzyme(item);
                    var color = AddItemStats(host, item);
                    setSlot(ObjectPlugin.Serialize(item), color, icon, detail);
                    DestroyObject(item);
                    host.RefreshAllStats();
                });
            }
        };
    }
}
