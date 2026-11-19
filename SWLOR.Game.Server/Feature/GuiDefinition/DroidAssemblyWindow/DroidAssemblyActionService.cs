using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow
{
    /// <summary>
    /// The droid assembly's actions: the five part clicks, reset, returning the installed parts, a new droid and the construct click.
    /// </summary>
    public class DroidAssemblyActionService
    {
        private IDroidService Droid { get; }
        private DroidAssemblyStatsService DroidAssemblyStatsService { get; }
        private IItemService Item { get; }
        private ITargetingService Targeting { get; }

        public DroidAssemblyActionService(IDroidService droid, DroidAssemblyStatsService droidAssemblyStatsService, IItemService item, ITargetingService targeting)
        {
            Droid = droid;
            DroidAssemblyStatsService = droidAssemblyStatsService;
            Item = item;
            Targeting = targeting;
        }

        /// <summary>
        /// Returns the installed parts and clears the assembly. A part without room stays installed with its stats.
        /// </summary>
        /// <returns>False when a part stayed installed.</returns>
        public bool Reset(IDroidAssemblyHost host)
        {
            host.Error = string.Empty;
            host.Name = string.Empty;
            host.PersonalityIndex = 0;

            if (!ReturnParts(host))
            {
                // The build stays open so the parts that stayed can be removed once there is room.
                host.ProcessNotStarted = false;
                host.IsBuildInProgress = true;
                return false;
            }

            host.ProcessNotStarted = true;
            host.IsBuildInProgress = false;
            host.IsCPUSelected = false;

            host.Session.Tier = 0;
            host.Session.Level = 0;
            host.Session.AiSlots = 0;
            host.Session.Hp = 0;
            host.Session.Stamina = 0;
            host.Session.Might = 0;
            host.Session.Vitality = 0;
            host.Session.Perception = 0;
            host.Session.Agility = 0;
            host.Session.Willpower = 0;
            host.Session.Social = 0;
            host.Session.Vibroblade = 0;
            host.Session.Vibroknife = 0;
            host.Session.HeavyVibroblade = 0;
            host.Session.Spear = 0;
            host.Session.TwinBlade = 0;
            host.Session.Katar = 0;
            host.Session.Staff = 0;
            host.Session.Pistol = 0;
            host.Session.Rifle = 0;
            host.Session.Throwing = 0;
            host.Session.Unarmed = 0;
            host.Session.TwinPistol = 0;
            host.Session.Armor = 0;
            host.Session.FireResistance = 0;
            host.Session.PoisonResistance = 0;
            host.Session.ElectricalResistance = 0;
            host.Session.IceResistance = 0;
            host.Session.MindResistance = 0;
            host.Session.MobilityResistance = 0;
            host.Session.TraumaResistance = 0;
            host.Session.DisruptionResistance = 0;

            DroidAssemblyStatsService.RefreshStats(host);

            host.CPUResref = DroidAssemblySession.BlankTexture;
            host.HeadResref = DroidAssemblySession.BlankTexture;
            host.BodyResref = DroidAssemblySession.BlankTexture;
            host.ArmsResref = DroidAssemblySession.BlankTexture;
            host.LegsResref = DroidAssemblySession.BlankTexture;
            return true;
        }

        /// <summary>
        /// Returns each installed part that fits and removes its stats. The CPU is returned last, and only once the other
        /// parts are out, because it holds them.
        /// </summary>
        /// <returns>True when no part is left installed.</returns>
        private bool ReturnParts(IDroidAssemblyHost host)
        {
            foreach (var serialized in new[] { host.Session.HeadItem, host.Session.BodyItem, host.Session.ArmsItem, host.Session.LegsItem })
                ReturnPart(host, serialized);

            if (string.IsNullOrWhiteSpace(host.Session.HeadItem) && string.IsNullOrWhiteSpace(host.Session.BodyItem) &&
                string.IsNullOrWhiteSpace(host.Session.ArmsItem) && string.IsNullOrWhiteSpace(host.Session.LegsItem))
                ReturnPart(host, host.Session.CpuItem);

            return string.IsNullOrWhiteSpace(host.Session.CpuItem) && string.IsNullOrWhiteSpace(host.Session.HeadItem) &&
                   string.IsNullOrWhiteSpace(host.Session.BodyItem) && string.IsNullOrWhiteSpace(host.Session.ArmsItem) &&
                   string.IsNullOrWhiteSpace(host.Session.LegsItem);
        }

        private void ReturnPart(IDroidAssemblyHost host, string serialized)
        {
            if (string.IsNullOrWhiteSpace(serialized))
                return;

            var item = ObjectPlugin.Deserialize(serialized);
            var part = Droid.LoadDroidPartItemPropertyDetails(item);
            if (Item.TryAcquireItem(host.Player, item))
                DroidAssemblyStatsService.RemovePart(host, part);
        }

        public void OnClickCPU(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            if (!string.IsNullOrWhiteSpace(host.Session.CpuItem))
            {
                host.ShowModal("Will you remove the CPU part?", () =>
                {
                    if (!string.IsNullOrWhiteSpace(host.Session.HeadItem) ||
                        !string.IsNullOrWhiteSpace(host.Session.BodyItem) ||
                        !string.IsNullOrWhiteSpace(host.Session.ArmsItem) ||
                        !string.IsNullOrWhiteSpace(host.Session.LegsItem))
                    {
                        DroidAssemblyStatsService.ShowError(host, "Remove all parts first.");
                    }
                    else
                    {
                        var item = ObjectPlugin.Deserialize(host.Session.CpuItem);
                        var part = Droid.LoadDroidPartItemPropertyDetails(item);
                        if (!Item.TryAcquireItem(host.Player, item))
                        {
                            DroidAssemblyStatsService.ShowError(host, ItemService.InventoryFullMessage);
                            return;
                        }

                        host.CPUResref = DroidAssemblySession.BlankTexture;
                        DroidAssemblyStatsService.RemovePart(host, part);
                    }
                });
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, "Select a CPU part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        DroidAssemblyStatsService.ShowError(host, error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.CPU)
                    {
                        DroidAssemblyStatsService.ShowError(host, "Select a CPU part.");
                        return;
                    }

                    DroidAssemblyStatsService.AddPart(host, part, item);
                });
            }
        }

        public void OnClickHead(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            if (!string.IsNullOrWhiteSpace(host.Session.HeadItem))
            {
                host.ShowModal("Will you remove the Head part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(host.Session.HeadItem);
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);
                    if (!Item.TryAcquireItem(host.Player, item))
                    {
                        DroidAssemblyStatsService.ShowError(host, ItemService.InventoryFullMessage);
                        return;
                    }

                    host.HeadResref = DroidAssemblySession.BlankTexture;
                    DroidAssemblyStatsService.RemovePart(host, part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, "Select a Head part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        DroidAssemblyStatsService.ShowError(host, error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Head)
                    {
                        DroidAssemblyStatsService.ShowError(host, "Select a Head part.");
                        return;
                    }

                    DroidAssemblyStatsService.AddPart(host, part, item);
                });
            }
        }

        public void OnClickBody(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            if (!string.IsNullOrWhiteSpace(host.Session.BodyItem))
            {
                host.ShowModal("Will you remove the Body part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(host.Session.BodyItem);
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);
                    if (!Item.TryAcquireItem(host.Player, item))
                    {
                        DroidAssemblyStatsService.ShowError(host, ItemService.InventoryFullMessage);
                        return;
                    }

                    host.BodyResref = DroidAssemblySession.BlankTexture;
                    DroidAssemblyStatsService.RemovePart(host, part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, "Select a Body part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        DroidAssemblyStatsService.ShowError(host, error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Body)
                    {
                        DroidAssemblyStatsService.ShowError(host, "Select a Body part.");
                        return;
                    }

                    DroidAssemblyStatsService.AddPart(host, part, item);
                });
            }
        }

        public void OnClickArms(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            if (!string.IsNullOrWhiteSpace(host.Session.ArmsItem))
            {
                host.ShowModal("Will you remove the Arms part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(host.Session.ArmsItem);
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);
                    if (!Item.TryAcquireItem(host.Player, item))
                    {
                        DroidAssemblyStatsService.ShowError(host, ItemService.InventoryFullMessage);
                        return;
                    }

                    host.ArmsResref = DroidAssemblySession.BlankTexture;
                    DroidAssemblyStatsService.RemovePart(host, part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, "Select an Arms part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        DroidAssemblyStatsService.ShowError(host, error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Arms)
                    {
                        DroidAssemblyStatsService.ShowError(host, "Select an Arms part.");
                        return;
                    }

                    DroidAssemblyStatsService.AddPart(host, part, item);
                });
            }
        }

        public void OnClickLegs(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            if (!string.IsNullOrWhiteSpace(host.Session.LegsItem))
            {
                host.ShowModal("Will you remove the Legs part?", () =>
                {
                    var item = ObjectPlugin.Deserialize(host.Session.LegsItem);
                    var part = Droid.LoadDroidPartItemPropertyDetails(item);
                    if (!Item.TryAcquireItem(host.Player, item))
                    {
                        DroidAssemblyStatsService.ShowError(host, ItemService.InventoryFullMessage);
                        return;
                    }

                    host.LegsResref = DroidAssemblySession.BlankTexture;
                    DroidAssemblyStatsService.RemovePart(host, part);
                });
            }
            else
            {
                Targeting.EnterTargetingMode(host.Player, ObjectType.Item, "Select a Legs part from your inventory.", item =>
                {
                    var error = Item.CanBePersistentlyStored(host.Player, item);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        DroidAssemblyStatsService.ShowError(host, error);
                        return;
                    }

                    var part = Droid.LoadDroidPartItemPropertyDetails(item);

                    if (part.PartType != DroidPartItemPropertySubType.Legs)
                    {
                        DroidAssemblyStatsService.ShowError(host, "Select a Legs part.");
                        return;
                    }

                    DroidAssemblyStatsService.AddPart(host, part, item);
                });
            }
        }

        public void OnClickNewDroid(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            host.IsBuildInProgress = true;
            host.ProcessNotStarted = false;
        }

        public void OnClickReset(IDroidAssemblyHost host)
        {
            DroidAssemblyStatsService.ClearError(host);

            host.ShowModal("Are you sure you want to reset everything?", () =>
            {
                if (!Reset(host))
                    DroidAssemblyStatsService.ShowError(host, ItemService.InventoryFullMessage);
            });
        }

        public void OnClickConstruct(IDroidAssemblyHost host)
        {
            if (host.ProcessNotStarted)
                return;

            DroidAssemblyStatsService.ClearError(host);

            if (string.IsNullOrWhiteSpace(host.Session.CpuItem) ||
                string.IsNullOrWhiteSpace(host.Session.HeadItem) ||
                string.IsNullOrWhiteSpace(host.Session.BodyItem) ||
                string.IsNullOrWhiteSpace(host.Session.ArmsItem) ||
                string.IsNullOrWhiteSpace(host.Session.LegsItem))
            {
                DroidAssemblyStatsService.ShowError(host, "Missing required part!");
                return;
            }

            if (string.IsNullOrWhiteSpace(host.Name))
            {
                DroidAssemblyStatsService.ShowError(host, "Enter droid name.");
                return;
            }

            if (host.PersonalityIndex == 0)
            {
                DroidAssemblyStatsService.ShowError(host, "Select droid personality.");
                return;
            }

            host.ShowModal("You are about to construct the droid. Are you sure you want to continue?", () =>
            {
                var controller = CreateItemOnObject(DroidService.DroidControlItemResref, host.Player);
                SetName(controller, $"Droid Controller: {host.Name}");

                var constructedDroid = Droid.LoadConstructedDroid(controller);
                constructedDroid.Name = host.Name;

                var ipPersonality = ItemPropertyCustom(ItemPropertyType.DroidPersonality, host.PersonalityIndex);
                var ipTier = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Tier, host.Session.Tier);
                var ipAISlots = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AISlots, host.Session.AiSlots);

                var ipHP = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HP, host.Session.Hp); ;
                var ipSTM = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.STM, host.Session.Stamina); ;

                var ipAgility = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.AGI, host.Session.Agility);
                var ipMight = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.MGT, host.Session.Might);
                var ipPerception = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.PER, host.Session.Perception);
                var ipVitality = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.VIT, host.Session.Vitality);
                var ipWillpower = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.WIL, host.Session.Willpower);
                var ipSocial = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.SOC, host.Session.Social);

                var ipVibroblade = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroblade, host.Session.Vibroblade);
                var ipVibroknife = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Vibroknife, host.Session.Vibroknife);
                var ipHeavyVibroblade = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.HeavyVibroblade, host.Session.HeavyVibroblade);
                var ipSpear = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Spear, host.Session.Spear);
                var ipTwinBlade = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinBlade, host.Session.TwinBlade);
                var ipKatar = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Katar, host.Session.Katar);
                var ipStaff = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Staff, host.Session.Staff);
                var ipPistol = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Pistol, host.Session.Pistol);
                var ipRifle = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Rifle, host.Session.Rifle);
                var ipThrowing = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Throwing, host.Session.Throwing);
                var ipUnarmed = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Unarmed, host.Session.Unarmed);
                var ipTwinPistol = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.TwinPistol, host.Session.TwinPistol);
                var ipArmor = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor, host.Session.Armor);
                var ipFireResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceFire, host.Session.FireResistance);
                var ipPoisonResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistancePoison, host.Session.PoisonResistance);
                var ipElectricalResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceElectrical, host.Session.ElectricalResistance);
                var ipIceResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceIce, host.Session.IceResistance);
                var ipMindResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceMind, host.Session.MindResistance);
                var ipMobilityResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceMobility, host.Session.MobilityResistance);
                var ipTraumaResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceTrauma, host.Session.TraumaResistance);
                var ipDisruptionResistance = ItemPropertyCustom(ItemPropertyType.DroidStat, (int)DroidStatSubType.ResistanceDisruption, host.Session.DisruptionResistance);

                BiowareXP2.IPSafeAddItemProperty(controller, ipPersonality, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTier, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipAISlots, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipHP, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSTM, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipAgility, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMight, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipPerception, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipVitality, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipWillpower, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSocial, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipVibroblade, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipVibroknife, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipHeavyVibroblade, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipSpear, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTwinBlade, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipKatar, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipStaff, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipPistol, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipRifle, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipThrowing, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipUnarmed, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTwinPistol, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipArmor, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipFireResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipPoisonResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipElectricalResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipIceResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMindResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipMobilityResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipTraumaResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
                BiowareXP2.IPSafeAddItemProperty(controller, ipDisruptionResistance, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

                constructedDroid.SerializedCPU = host.Session.CpuItem;
                constructedDroid.SerializedHead = host.Session.HeadItem;
                constructedDroid.SerializedBody = host.Session.BodyItem;
                constructedDroid.SerializedArms = host.Session.ArmsItem;
                constructedDroid.SerializedLegs = host.Session.LegsItem;

                Droid.SaveConstructedDroid(controller, constructedDroid);

                host.Session.CpuItem = string.Empty;
                host.Session.HeadItem = string.Empty;
                host.Session.BodyItem = string.Empty;
                host.Session.ArmsItem = string.Empty;
                host.Session.LegsItem = string.Empty;

                Reset(host);
            });
        }

    }
}
