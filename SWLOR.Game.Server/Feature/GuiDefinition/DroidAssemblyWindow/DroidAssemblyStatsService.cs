using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Domain.Items;

namespace SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow
{
    /// <summary>
    /// The droid assembly's stat bookkeeping: adding and removing a part adjusts the session's tallies and rewrites the stat texts.
    /// </summary>
    public class DroidAssemblyStatsService
    {
        private IItemService Item { get; }

        public DroidAssemblyStatsService(IItemService item)
        {
            Item = item;
        }

        public static void ShowError(IDroidAssemblyHost host, string text)
        {
            host.Error = text;
        }

        public static void ClearError(IDroidAssemblyHost host)
        {
            host.Error = string.Empty;
        }

        public static void RefreshStats(IDroidAssemblyHost host)
        {
            ClearError(host);

            host.Tier = $"Tier: {host.Session.Tier}";
            host.Level = $"Level: {host.Session.Level}";
            host.AISlots = $"AI Slots: {host.Session.AiSlots}";
            host.HP = $"HP: {host.Session.Hp}";
            host.Stamina = $"STM: {host.Session.Stamina}";
            host.Might = $"MGT: {host.Session.Might}";
            host.Perception = $"PER: {host.Session.Perception}";
            host.Vitality = $"VIT: {host.Session.Vitality}";
            host.Willpower = $"WIL: {host.Session.Willpower}";
            host.Agility = $"AGI: {host.Session.Agility}";
            host.Social = $"SOC: {host.Session.Social}";
            host.Vibroblade = $"Vibroblade: {host.Session.Vibroblade}";
            host.Vibroknife = $"Vibroknife: {host.Session.Vibroknife}";
            host.HeavyVibroblade = $"Heavy Vibroblade: {host.Session.HeavyVibroblade}";
            host.Spear = $"Spear: {host.Session.Spear}";
            host.TwinBlade = $"Twin Blade: {host.Session.TwinBlade}";
            host.Katar = $"Katar: {host.Session.Katar}";
            host.Staff = $"Staff: {host.Session.Staff}";
            host.Pistol = $"Pistol: {host.Session.Pistol}";
            host.Rifle = $"Rifle: {host.Session.Rifle}";
            host.Throwing = $"Throwing: {host.Session.Throwing}";
            host.Unarmed = $"Unarmed: {host.Session.Unarmed}";
            host.TwinPistol = $"Twin Pistol: {host.Session.TwinPistol}";
            host.Armor = $"Armor: {host.Session.Armor}";
            host.FireResistance = $"Fire RES: {host.Session.FireResistance}";
            host.PoisonResistance = $"Poison RES: {host.Session.PoisonResistance}";
            host.ElectricalResistance = $"Elec. RES: {host.Session.ElectricalResistance}";
            host.IceResistance = $"Ice RES: {host.Session.IceResistance}";
            host.MindResistance = $"Mind RES: {host.Session.MindResistance}";
            host.MobilityResistance = $"Mob. RES: {host.Session.MobilityResistance}";
            host.TraumaResistance = $"Trauma RES: {host.Session.TraumaResistance}";
            host.DisruptionResistance = $"Disr. RES: {host.Session.DisruptionResistance}";
        }

        private static void AdjustResistances(IDroidAssemblyHost host, IReadOnlyDictionary<ResistanceType, int> resistances, int multiplier)
        {
            foreach (var (type, value) in resistances)
            {
                var adjusted = value * multiplier;
                switch (type)
                {
                    case ResistanceType.Fire:
                        host.Session.FireResistance += adjusted;
                        break;
                    case ResistanceType.Poison:
                        host.Session.PoisonResistance += adjusted;
                        break;
                    case ResistanceType.Electrical:
                        host.Session.ElectricalResistance += adjusted;
                        break;
                    case ResistanceType.Ice:
                        host.Session.IceResistance += adjusted;
                        break;
                    case ResistanceType.Mind:
                        host.Session.MindResistance += adjusted;
                        break;
                    case ResistanceType.Mobility:
                        host.Session.MobilityResistance += adjusted;
                        break;
                    case ResistanceType.Trauma:
                        host.Session.TraumaResistance += adjusted;
                        break;
                    case ResistanceType.Disruption:
                        host.Session.DisruptionResistance += adjusted;
                        break;
                }
            }
        }

        public void AddPart(IDroidAssemblyHost host, DroidPartItemPropertyDetails part, uint item)
        {
            var assemblyTier = CraftRankMilestones.GetDroidAssemblyTier(host.Player);

            var serialized = ObjectPlugin.Serialize(item);
            var icon = Item.GetIconResref(item);
            switch (part.PartType)
            {
                case DroidPartItemPropertySubType.CPU:

                    if (assemblyTier < part.Tier)
                    {
                        ShowError(host, $"Engineering rank too low for a tier {part.Tier} CPU. (Required: {CraftRankMilestones.RequiredRankForTier(part.Tier)})");
                        return;
                    }

                    host.Session.CpuItem = serialized;
                    host.Session.Level = part.Level;
                    host.Session.Tier = part.Tier;
                    host.IsCPUSelected = true;
                    host.CPUResref = icon;
                    break;
                case DroidPartItemPropertySubType.Head:
                    if (part.Tier > host.Session.Tier)
                    {
                        ShowError(host, $"Head part tier must be less than or equal to CPU tier ({host.Session.Tier}).");
                        return;
                    }
                    host.Session.HeadItem = serialized;
                    host.HeadResref = icon;
                    break;
                case DroidPartItemPropertySubType.Body:
                    if (part.Tier > host.Session.Tier)
                    {
                        ShowError(host, $"Body part tier must be less than or equal to CPU tier ({host.Session.Tier}).");
                        return;
                    }
                    host.Session.BodyItem = serialized;
                    host.BodyResref = icon;
                    break;
                case DroidPartItemPropertySubType.Arms:
                    if (part.Tier > host.Session.Tier)
                    {
                        ShowError(host, $"Arms part tier must be less than or equal to CPU tier ({host.Session.Tier}).");
                        return;
                    }
                    host.Session.ArmsItem = serialized;
                    host.ArmsResref = icon;
                    break;
                case DroidPartItemPropertySubType.Legs:
                    if (part.Tier > host.Session.Tier)
                    {
                        ShowError(host, $"Legs part tier must be less than or equal to CPU tier ({host.Session.Tier}).");
                        return;
                    }
                    host.Session.LegsItem = serialized;
                    host.LegsResref = icon;
                    break;
            }
            DestroyObject(item);

            host.Session.AiSlots += part.AISlots;
            host.Session.Hp += part.HP;
            host.Session.Stamina += part.STM;
            host.Session.Might += part.MGT;
            host.Session.Perception += part.PER;
            host.Session.Vitality += part.VIT;
            host.Session.Willpower += part.WIL;
            host.Session.Agility += part.AGI;
            host.Session.Social += part.SOC;
            host.Session.Vibroblade += part.Vibroblade;
            host.Session.Vibroknife += part.Vibroknife;
            host.Session.HeavyVibroblade += part.HeavyVibroblade;
            host.Session.Spear += part.Spear;
            host.Session.TwinBlade += part.TwinBlade;
            host.Session.Katar += part.Katar;
            host.Session.Staff += part.Staff;
            host.Session.Pistol += part.Pistol;
            host.Session.Rifle += part.Rifle;
            host.Session.Throwing += part.Throwing;
            host.Session.Unarmed += part.Unarmed;
            host.Session.TwinPistol += part.TwinPistol;
            host.Session.Armor += part.Armor;
            AdjustResistances(host, part.Resistances, 1);

            RefreshStats(host);
        }

        public static void RemovePart(IDroidAssemblyHost host, DroidPartItemPropertyDetails part)
        {
            switch (part.PartType)
            {
                case DroidPartItemPropertySubType.CPU:
                    host.Session.CpuItem = string.Empty;
                    host.Session.Level = 0;
                    host.Session.Tier = 0;
                    host.IsCPUSelected = false;
                    host.CPUResref = DroidAssemblySession.BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Head:
                    host.Session.HeadItem = string.Empty;
                    host.HeadResref = DroidAssemblySession.BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Body:
                    host.Session.BodyItem = string.Empty;
                    host.BodyResref = DroidAssemblySession.BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Arms:
                    host.Session.ArmsItem = string.Empty;
                    host.ArmsResref = DroidAssemblySession.BlankTexture;
                    break;
                case DroidPartItemPropertySubType.Legs:
                    host.Session.LegsItem = string.Empty;
                    host.LegsResref = DroidAssemblySession.BlankTexture;
                    break;
            }

            host.Session.AiSlots -= part.AISlots;
            host.Session.Hp -= part.HP;
            host.Session.Stamina -= part.STM;
            host.Session.Might -= part.MGT;
            host.Session.Perception -= part.PER;
            host.Session.Vitality -= part.VIT;
            host.Session.Willpower -= part.WIL;
            host.Session.Agility -= part.AGI;
            host.Session.Social -= part.SOC;
            host.Session.Vibroblade -= part.Vibroblade;
            host.Session.Vibroknife -= part.Vibroknife;
            host.Session.HeavyVibroblade -= part.HeavyVibroblade;
            host.Session.Spear -= part.Spear;
            host.Session.TwinBlade -= part.TwinBlade;
            host.Session.Katar -= part.Katar;
            host.Session.Staff -= part.Staff;
            host.Session.Pistol -= part.Pistol;
            host.Session.Rifle -= part.Rifle;
            host.Session.Throwing -= part.Throwing;
            host.Session.Unarmed -= part.Unarmed;
            host.Session.TwinPistol -= part.TwinPistol;
            host.Session.Armor -= part.Armor;
            AdjustResistances(host, part.Resistances, -1);

            RefreshStats(host);
        }
    }
}
