using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ShipModuleDefinition
{
    /// <summary>
    /// The Gas Harvester and Salvager cycles (plan section 12). Both work like the Mining Laser: a toggle runs one
    /// cycle on its target, holds the target while it runs, and delivers the yield to the ship's hold when it ends.
    /// </summary>
    public static class ShipSalvageActions
    {
        /// <summary>The gas cloud's loot table (placeable local).</summary>
        public const string GasCloudLootTableVariable = "GAS_CLOUD_LOOT_TABLE_ID";

        /// <summary>The gas cloud's tier (placeable local).</summary>
        public const string GasCloudTierVariable = "GAS_CLOUD_TIER";

        private const string GasCloudUnitsVariable = "GAS_CLOUD_REMAINING_UNITS";

        /// <summary>
        /// A gas cloud is an invisible, clickable placeable (spc_gascloud_t3/t4); this looping effect is what players see.
        /// </summary>
        public const VisualEffect GasCloudVisual = VisualEffect.Vfx_Dur_Aura_Cold;

        /// <summary>The cloud effect's scale, sized to read beside a ship.</summary>
        public const float GasCloudVisualScale = 3.0f;

        /// <summary>Gives a newly spawned gas cloud its looping cloud effect.</summary>
        public static void ShowGasCloud(uint cloud)
        {
            var visual = SupernaturalEffect(EffectVisualEffect(GasCloudVisual, false, GasCloudVisualScale));
            ApplyEffectToObject(DurationType.Permanent, visual, cloud);
        }

        // Gas harvesting

        /// <summary>An error message when the target is not a gas cloud this gas harvester can harvest.</summary>
        public static string GasHarvesterError(uint activator, uint target, int tier)
        {
            if (string.IsNullOrWhiteSpace(GetLocalString(target, GasCloudLootTableVariable)))
                return "Only gas clouds may be targeted with this module.";

            if (tier < ShipSalvageRules.RequiredModuleTier(GetLocalInt(target, GasCloudTierVariable)))
                return "This gas harvester is not powerful enough to harvest that gas cloud.";

            if (ShipModuleActions.IsExtractedByAnotherShip(target, activator))
                return "This gas cloud is already being harvested.";

            return string.Empty;
        }

        /// <summary>Harvests one cycle of a gas cloud and delivers the gas when the cycle ends.</summary>
        public static void HarvestGasCloud(uint activator, uint target, float cycleSeconds, int moduleBonus)
        {
            cycleSeconds = ShipModuleActions.MiningCycleSeconds(activator, cycleSeconds);
            ShipModuleActions.HoldExtraction(target, activator);
            if (GetLocalInt(target, GasCloudUnitsVariable) <= 0)
                SetLocalInt(target, GasCloudUnitsVariable, Random.D4(1) + 2);

            AssignCommand(activator, () =>
            {
                var beam = EffectBeam(VisualEffect.Vfx_Beam_Cold, activator, BodyNode.Chest);
                ApplyEffectToObject(DurationType.Temporary, beam, target, cycleSeconds + 0.1f);
            });

            DelayCommand(cycleSeconds + 0.1f, () =>
            {
                ShipModuleActions.ReleaseExtraction(target, activator);

                var shipStatus = Space.GetShipStatus(activator);
                if (shipStatus == null)
                    return;

                var remainingUnits = GetLocalInt(target, GasCloudUnitsVariable);
                if (!GetIsObjectValid(target) || remainingUnits <= 0)
                {
                    SendMessageToPC(activator, "Your target has been fully harvested.");
                    return;
                }

                // Gas Siphoning (Gathering) adds gas to every cycle.
                var amount = ShipSalvageRules.GasPerCycle(
                    shipStatus.Industrial, moduleBonus, Stat.GetStatAdjustment(activator, StatType.SpaceGasYieldBonus));

                remainingUnits -= SpaceCombatMath.MiningUnitsPerCycle(amount);
                if (remainingUnits <= 0)
                {
                    SetPlotFlag(target, false);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), target);
                    SendMessageToPC(activator, $"{GetName(target)} has been fully harvested.");
                }
                else
                {
                    SetLocalInt(target, GasCloudUnitsVariable, remainingUnits);
                }

                // A droid in the ship's droid bay adds to the gas delivered, not to what the cloud loses.
                var delivered = DroidIndustryRules.ApplyPercent(amount, Space.GetDroidBayYieldPercent(activator));
                var lootTable = Loot.GetLootTableByName(GetLocalString(target, GasCloudLootTableVariable));
                var gas = new List<string>();
                for (var count = 1; count <= delivered; count++)
                {
                    gas.Add(lootTable.GetRandomItem().Resref);
                }

                ShipHold.DeliverYield(activator, gas);
                GiveGatheringExperience(activator, GetLocalInt(target, GasCloudTierVariable));
            });
        }

        // Salvage

        /// <summary>An error message when the target is not a wreck this salvager can salvage right now.</summary>
        public static string SalvagerError(uint activator, ShipStatus status, uint target, int tier)
        {
            var wreck = Space.GetWreck(target);
            if (wreck == null)
                return "Only wrecks may be targeted with this module.";

            if (!ShipSalvageRules.HullCanSalvage(wreck.Size, status.Size))
                return "Salvaging a capital wreck needs a Large or Capital hull.";

            if (tier < ShipSalvageRules.RequiredModuleTier(wreck.Tier))
                return "This salvager is not powerful enough to salvage that wreck.";

            var now = DateTime.UtcNow;
            if (!ShipSalvageRules.CanSalvage(wreck.Claimants, GetObjectUUID(activator), wreck.DestroyedAt, now))
                return $"The ship's killers have first claim on this wreck for {ShipSalvageRules.ClaimSecondsLeft(wreck.DestroyedAt, now)} more seconds.";

            if (ShipModuleActions.IsExtractedByAnotherShip(target, activator))
                return "This wreck is already being salvaged.";

            return string.Empty;
        }

        /// <summary>Salvages one cycle of a wreck and delivers what it yields when the cycle ends.</summary>
        public static void SalvageWreck(uint activator, uint target, float cycleSeconds, int moduleBonus)
        {
            cycleSeconds = ShipModuleActions.MiningCycleSeconds(activator, cycleSeconds);
            ShipModuleActions.HoldExtraction(target, activator);

            // The salvager's RareFind traits (Precision) raise an elite or boss wreck's rare-find chance.
            var traitRareFind = ShipTraitRules.Sum(ShipModuleTraitContext.Current, ShipTraitStat.RareFind);

            AssignCommand(activator, () =>
            {
                var beam = EffectBeam(VisualEffect.Vfx_Beam_Fire, activator, BodyNode.Chest);
                ApplyEffectToObject(DurationType.Temporary, beam, target, cycleSeconds + 0.1f);
            });

            DelayCommand(cycleSeconds + 0.1f, () =>
            {
                ShipModuleActions.ReleaseExtraction(target, activator);

                var shipStatus = Space.GetShipStatus(activator);
                if (shipStatus == null)
                    return;

                var wreck = Space.GetWreck(target);
                if (wreck == null || wreck.RemainingCycles <= 0)
                {
                    SendMessageToPC(activator, "Your target has been fully salvaged.");
                    return;
                }

                // Salvage Expert (Gathering) adds items to every cycle.
                // A droid in the ship's droid bay adds items and, with Prospecting Protocol, rare-find chance.
                var items = DroidIndustryRules.ApplyPercent(ShipSalvageRules.SalvageItemsPerCycle(
                        shipStatus.Industrial, moduleBonus, Stat.GetStatAdjustment(activator, StatType.SpaceSalvageYieldBonus)),
                    Space.GetDroidBayYieldPercent(activator));
                var yield = ShipSalvageRules.ScrapYield(wreck.Tier, items);
                var rareFind = traitRareFind + Space.GetDroidBayRareFindBonus(activator);

                if (ShipSalvageRules.RareFindPercent(wreck.Difficulty) > 0 &&
                    ShipSalvageRules.RollsRareFind(wreck.Difficulty, Random.Next(100) - rareFind))
                {
                    // Some rare finds are a schematic or rare torpedoes; the rest come from the tier's table.
                    var reward = ShipRareFindRules.Roll(wreck.Tier, wreck.Difficulty, Random.Next, wreck.WildSpace);
                    if (reward.Count > 0)
                        yield.AddRange(reward);
                    else
                        yield.Add(Loot.GetLootTableByName(ShipSalvageRules.RareFindTable(wreck.Tier, wreck.Difficulty, wreck.WildSpace)).GetRandomItem().Resref);
                }

                var areaAurodium = GetLocalInt(GetArea(target), ShipSalvageRules.AurodiumPercentVariable);
                if (ShipSalvageRules.RollsAurodium(wreck.Tier, areaAurodium, Random.Next(100)))
                    yield.Add(ShipSalvageItems.Aurodium);

                var components = RollSalvagedComponents(activator, wreck);

                var tier = wreck.Tier;
                wreck.RemainingCycles--;
                if (wreck.RemainingCycles <= 0)
                {
                    SendMessageToPC(activator, $"{GetName(target)} has been fully salvaged.");
                    Space.RemoveWreck(target);
                }

                ShipHold.DeliverYield(activator, yield, components);
                GiveGatheringExperience(activator, tier);
            });
        }

        /// <summary>
        /// The salvaged components a cycle yields (plan sections 12 and 14): maybe a generic component of the wreck's tier
        /// carrying a random trait, rare more often from elite and boss wrecks and with Trait Hunter (Gathering), and maybe
        /// the manufacturer component of the maker the ship belonged to (Appendix D).
        /// </summary>
        private static List<(string Resref, ShipComponentTraitType Trait)> RollSalvagedComponents(uint activator, ShipWreck wreck)
        {
            var components = new List<(string Resref, ShipComponentTraitType Trait)>();
            // Wild Space wrecks roll rare traits more often (plan section 23).
            var rareTraitChance = Stat.GetStatAdjustment(activator, StatType.SpaceRareTraitChance) +
                                  (wreck.WildSpace ? WildSpaceTuning.RareTraitBonusPercent : 0);
            var rolled = ShipTraitRules.RollSalvagedComponent(wreck.Tier, wreck.Difficulty, rareTraitChance, Random.Next);
            if (rolled != null)
                components.Add(rolled.Value);

            var maker = ShipManufacturerRules.RollSalvage(wreck.Manufacturer, wreck.Difficulty, Random.Next);
            if (maker != null)
                components.Add(maker.Value);

            return components;
        }

        /// <summary>Gathering XP for a gas or salvage cycle: the target's level (tier x 10) against the player's Gathering rank.</summary>
        private static void GiveGatheringExperience(uint activator, int tier)
        {
            if (!GetIsPC(activator) || GetIsDM(activator) || GetIsDMPossessed(activator))
                return;

            var dbPlayer = DB.Get<Player>(GetObjectUUID(activator));
            var rank = dbPlayer.Skills[SkillType.Gathering].Rank;
            var delta = Math.Clamp(tier, 1, 6) * 10 - rank;

            Skill.GiveSkillXP(activator, SkillType.Gathering, Skill.GetDeltaXP(delta));
        }
    }
}
