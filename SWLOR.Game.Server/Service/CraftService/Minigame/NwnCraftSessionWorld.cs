using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>The game-engine side of a craft session. The rules it carries out are in <see cref="CraftSessionLifecycle"/>.</summary>
public sealed class NwnCraftSessionWorld : ICraftSessionWorld
{
    public static readonly NwnCraftSessionWorld Instance = new();

    private static readonly BlueprintBonuses BlueprintBonusPicker = new();

    private const string StationAnimationVariable = "CRAFT_STATION_ANIMATION";
    private const string PersistPendingVariable = "CRAFT_PERSIST_PENDING";
    private const float PersistDelaySeconds = 0.1f;

    public DateTime UtcNow => DateTime.UtcNow;

    public RecipeDetail GetRecipe(RecipeType recipe) => Craft.GetRecipe(recipe);

    public bool CanCraft(uint player, RecipeType recipe) => Craft.CanPlayerCraftRecipe(player, recipe);

    public string ValidateBlueprint(uint player, uint blueprintItem, RecipeType recipe)
    {
        if (!GetIsObjectValid(blueprintItem) || GetItemPossessor(blueprintItem) != player)
            return "The blueprint must be in your inventory.";

        var details = Craft.GetBlueprintDetails(blueprintItem);
        if (details.Recipe != recipe)
            return "The blueprint is for a different recipe.";

        if (details.LicensedRuns <= 0)
            return "No licensed runs remaining!";

        if (GetGold(player) < Craft.CalculateBlueprintCraftCreditCost(blueprintItem))
            return "Insufficient credits!";

        return string.Empty;
    }

    public BlueprintDetail ChargeBlueprint(uint player, uint blueprintItem)
    {
        var cost = Craft.CalculateBlueprintCraftCreditCost(blueprintItem);
        AssignCommand(player, () => TakeGoldFromCreature(cost, player, true));

        var details = Craft.GetBlueprintDetails(blueprintItem);
        details.LicensedRuns--;
        Craft.SetBlueprintDetails(blueprintItem, details);
        SendMessageToPC(player, $"Remaining licensed runs: {details.LicensedRuns}");

        return details;
    }

    public bool TryTakeComponents(uint player, RecipeDetail recipe, List<string> taken)
    {
        var plan = PlanComponents(player, recipe);
        if (plan == null)
            return false;

        foreach (var (stack, quantity) in plan)
        {
            var stackSize = GetItemStackSize(stack);
            if (quantity < stackSize)
            {
                SetItemStackSize(stack, quantity);
                taken.Add(ObjectPlugin.Serialize(stack));
                SetItemStackSize(stack, stackSize - quantity);
            }
            else
            {
                taken.Add(ObjectPlugin.Serialize(stack));
                DestroyObject(stack);
            }
        }

        return true;
    }

    /// <summary>True when <see cref="TryTakeComponents"/> would find every component of one craft. Nothing is taken.</summary>
    public bool HasComponents(uint player, RecipeDetail recipe) => PlanComponents(player, recipe) != null;

    /// <summary>
    /// Plans the units one craft takes, stack by stack, or returns null when a component is missing. Nothing is touched. Ship recipes
    /// (space revamp) also collect the traited components that fill an Advanced deed's rare-trait slot or stand in for its rare
    /// component, plan those first, and spend traited components where the crafted item keeps their traits
    /// (<see cref="ShipComponents"/>); every other recipe plans exactly as before.
    /// </summary>
    private static List<(uint Stack, int Quantity)> PlanComponents(uint player, RecipeDetail recipe)
    {
        var stacks = new List<uint>();
        for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
        {
            if (recipe.Components.ContainsKey(GetResRef(item)) || ShipComponents.IsStandInCandidate(recipe, item))
                stacks.Add(item);
        }

        stacks = ShipComponents.OrderForConsumption(recipe, stacks);

        var remaining = recipe.Components.ToDictionary(x => x.Key, y => y.Value);
        var plan = new List<(uint Stack, int Quantity)>();
        var planned = new Dictionary<uint, int>();
        foreach (var standIn in ShipComponents.ReserveRareSlot(recipe, stacks, remaining))
        {
            plan.Add((standIn, 1));
            planned[standIn] = planned.GetValueOrDefault(standIn) + 1;
        }

        // Plan from the last stack to the first; nothing is touched until every component is found.
        for (var index = stacks.Count - 1; index >= 0; index--)
        {
            var resref = GetResRef(stacks[index]);
            if (!remaining.TryGetValue(resref, out var needed))
                continue;

            var available = GetItemStackSize(stacks[index]) - planned.GetValueOrDefault(stacks[index]);
            if (available <= 0)
                continue;

            var quantity = Math.Min(available, needed);
            plan.Add((stacks[index], quantity));
            planned[stacks[index]] = planned.GetValueOrDefault(stacks[index]) + quantity;

            if (needed - quantity <= 0)
                remaining.Remove(resref);
            else
                remaining[resref] = needed - quantity;
        }

        return remaining.Count > 0 ? null : plan;
    }

    public string ValidateStationFee(uint player, RecipeDetail recipe, uint blueprintItem)
    {
        var fee = Craft.GetStationFee(player, recipe);
        if (fee <= 0)
            return string.Empty;

        var blueprintCost = GetIsObjectValid(blueprintItem) ? Craft.CalculateBlueprintCraftCreditCost(blueprintItem) : 0;
        return GetGold(player) < fee + blueprintCost
            ? $"The Shipyard fee is {fee} credits!"
            : string.Empty;
    }

    public void ChargeStationFee(uint player, RecipeDetail recipe) => Craft.PayStationFee(player, recipe);

    public void ApplyComponentTraits(uint item, RecipeType recipeType, RecipeDetail recipe, IReadOnlyList<string> components) =>
        ShipComponents.ApplyCraftedTraits(recipeType, recipe, item, components);

    public bool HasEnhancement(uint player, uint item) => GetIsObjectValid(item) && GetItemPossessor(item) == player;

    public string TakeEnhancement(uint player, uint item)
    {
        var serialized = ObjectPlugin.Serialize(item);
        DestroyObject(item);
        return serialized;
    }

    public bool TryReturnItem(uint player, string serialized)
    {
        var item = ObjectPlugin.Deserialize(serialized);
        if (GetIsObjectValid(item))
            return Item.TryAcquireItem(player, item);

        // Data that no longer loads can never be returned; dropping it keeps the player from being told about it forever.
        Log.Write(LogGroup.Error, $"A saved craft item for {GetObjectUUID(player)} could not be restored and was dropped.", true);
        return true;
    }

    public void BeginStationWork(uint player)
    {
        var effect = TagEffect(EffectCutsceneImmobilize(), PlayerActivityEffectTag.CraftingImmobilize);
        ApplyEffectToObject(DurationType.Permanent, effect, player);

        // Separate from the immobilize: the engine rejects a ghost on a ghosted player, and a linked immobilize with it.
        PlayerTemporaryEffects.ApplyCutsceneGhostToPlayer(player);
    }

    public void PlayStationWork(uint player, SkillType skill)
    {
        // A second craft at the station replaces the first one's loop instead of stacking on it.
        StopStationAnimation(player);

        var clip = WorkAnimation.ForStation(skill);
        if (clip == null || !NamedAnimation.CanLoop(player))
            return;

        var token = NamedAnimation.Play(player, clip, WorkAnimation.StationLoopSeconds);
        if (!string.IsNullOrEmpty(token))
            SetLocalString(player, StationAnimationVariable, token);
    }

    /// <summary>True while a station work clip started by <see cref="PlayStationWork"/> is the player's.</summary>
    public bool IsStationWorkPlaying(uint player) => !string.IsNullOrEmpty(GetLocalString(player, StationAnimationVariable));

    public void EndStationWork(uint player)
    {
        RemoveEffectByTag(player, PlayerActivityEffectTag.CraftingImmobilize);
        StopStationAnimation(player);
    }

    private static void StopStationAnimation(uint player)
    {
        var token = GetLocalString(player, StationAnimationVariable);
        if (string.IsNullOrEmpty(token))
            return;

        DeleteLocalString(player, StationAnimationVariable);
        NamedAnimation.StopIfCurrent(player, token);
    }

    public uint CreateCraftedItem(uint player, RecipeDetail recipe)
    {
        var item = CreateItemOnObject(recipe.Resref, player, recipe.Quantity);
        SetLocalBool(item, Item.PlayerProducedItemVariable, true);
        return item;
    }

    public uint CreateFinishedItem(RecipeDetail recipe)
    {
        var item = CreateItemOnObject(recipe.Resref, GetObjectByTag("TEMP_ITEM_STORAGE"), Math.Max(1, recipe.Quantity));
        SetLocalBool(item, Item.PlayerProducedItemVariable, true);
        return item;
    }

    public List<CraftedUnit> PrepareUnits(uint player, uint item, RecipeDetail recipe)
    {
        var units = new List<CraftedUnit>();
        if (!GetIsObjectValid(item))
        {
            Log.Write(LogGroup.Error, $"The finished item '{recipe.Resref}' for {GetObjectUUID(player)} could not be created.", true);
            return units;
        }

        var made = new List<uint> { item };
        var stacks = StringToInt(Get2DAString("baseitems", "Stacking", (int)GetBaseItemType(item))) > 1;
        if (!stacks)
        {
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            for (var unit = 1; unit < Math.Max(1, recipe.Quantity); unit++)
                made.Add(CopyItem(item, storage, true));
        }

        // A copy that could not be made stands as the finished item's serialized form, to be held.
        var serialized = ObjectPlugin.Serialize(item);
        foreach (var unit in made)
            units.Add(new CraftedUnit(unit, GetIsObjectValid(unit) ? ObjectPlugin.Serialize(unit) : serialized));

        return units;
    }

    public bool DeliverUnit(uint player, CraftedUnit unit)
    {
        if (!GetIsObjectValid(unit.Item))
            return false;

        if (Item.FitsInInventory(player, unit.Item) && ObjectPlugin.AcquireItem(player, unit.Item))
            return true;

        DestroyObject(unit.Item);
        return false;
    }

    public void ApplyVendorValue(uint item, int addGoldPiece) => ItemPlugin.SetAddGoldPieceValue(item, addGoldPiece);

    public void ApplyEnhancement(uint item, string serializedEnhancement, RecipeDetail recipe)
    {
        var enhancement = ObjectPlugin.Deserialize(serializedEnhancement);
        if (!GetIsObjectValid(enhancement))
            return;

        var reading = CraftEnhancementReader.Read(enhancement, recipe);
        DestroyObject(enhancement);

        foreach (var property in reading.Properties)
            Craft.ApplyCraftedItemProperty(item, property);
    }

    public void ApplyFoodBonus(uint item, int durationSeconds, int bonusCharges)
    {
        if (durationSeconds > 0)
        {
            SetLocalInt(item, CraftOutcomeRules.FoodDurationBonusSecondsVariable, durationSeconds);
            SetDescription(item, FoodDurationText.AppendTo(GetDescription(item), durationSeconds));
        }

        if (bonusCharges > 0)
            SetItemCharges(item, GetItemCharges(item) + bonusCharges);
    }

    public void ApplyBlueprintBonuses(uint player, uint item, RecipeDetail recipe, BlueprintDetail blueprint)
    {
        // A ship module or deed blueprint's bonuses are blueprint traits (space revamp), stamped after the component traits.
        if (ShipBlueprintRules.IsResearchable(recipe))
        {
            ApplyShipBlueprintTraits(player, item, recipe, blueprint);
            return;
        }

        for (var currentBonus = 1; currentBonus <= blueprint.ItemBonuses; currentBonus++)
        {
            var tier = currentBonus;

            // Stat pool tier is based on the recipe level.
            // This ensures top-end stats don't get applied to a low-tier weapon, for balancing purposes.
            if (recipe.Level >= 0 && recipe.Level <= 20 && tier > 1)
                tier = 1;
            else if (recipe.Level >= 21 && recipe.Level <= 40 && tier > 2)
                tier = 2;

            var bonus = BlueprintBonusPicker.PickBonus(recipe.EnhancementType, tier, recipe.IsItemIntendedForCrafting);
            if (bonus == null)
                continue;

            foreach (var ip in Craft.BuildItemPropertiesForEnhancement(bonus.Type, bonus.Amount, bonus.DamageType))
                Craft.ApplyCraftedItemProperty(item, ip);

            var subTypeDetail = Craft.GetEnhancementSubType(bonus.Type);
            var bonusName = bonus.DamageType != CombatDamageType.Invalid &&
                            !bonus.DamageType.IsPhysicalDamageType()
                ? $"{subTypeDetail.Name} - {bonus.DamageType}"
                : subTypeDetail.Name;
            SendMessageToPC(player, ColorToken.Green($"Blueprint Bonus applied: {bonusName} +{bonus.Amount}"));
        }

        foreach (var ip in blueprint.GuaranteedBonuses)
            Craft.ApplyCraftedItemProperty(item, ip);

        // Identify the blueprint level on the finished item.
        var blueprintLevelIP = ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.Level, blueprint.Level);
        BiowareXP2.IPSafeAddItemProperty(item, blueprintLevelIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
    }

    /// <summary>
    /// A ship blueprint's item bonuses and guaranteed bonuses as blueprint traits (<see cref="ShipBlueprintRules.PickItemBonuses"/>: no
    /// repeats, capped by the recipe level's tier), added to the traits the components passed on.
    /// </summary>
    private static void ApplyShipBlueprintTraits(uint player, uint item, RecipeDetail recipe, BlueprintDetail blueprint)
    {
        var traits = new List<ShipComponentTraitType>();
        foreach (var trait in ShipBlueprintRules.PickItemBonuses(recipe, blueprint.ItemBonuses, blueprint.GuaranteedTraits, Random.Next))
        {
            traits.Add(trait);
            SendMessageToPC(player, ColorToken.Green($"Blueprint Bonus applied: {ShipTraitRules.Get(trait).Name}"));
        }

        traits.AddRange(blueprint.GuaranteedTraits);
        ShipComponents.AddTraits(item, traits);

        // Identify the blueprint level on the finished item.
        var blueprintLevelIP = ItemPropertyCustom(ItemPropertyType.Blueprint, (int)BlueprintSubType.Level, blueprint.Level);
        BiowareXP2.IPSafeAddItemProperty(item, blueprintLevelIP, 0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
    }

    public bool AwardPendingXp(uint player, Player dbPlayer, SkillType skill, int xp)
    {
        // The same guards Skill.GiveSkillXP returns on before it loads the record: the entitlement stays until an award can run.
        if (!GetIsPC(player) || GetIsDM(player) || GetIsDead(player) || GetCurrentHitPoints(player) <= 0)
            return false;

        // GiveSkillXP loads the record through DB.Get, which returns this frame's cached instance, and saves it. The removal is made on
        // that instance, so the save that holds the award also holds the removal.
        var record = DB.Get<Player>(dbPlayer.Id) ?? dbPlayer;
        record.PendingCraftXp?.Remove(skill);
        if (!ReferenceEquals(record, dbPlayer))
            dbPlayer.PendingCraftXp?.Remove(skill);

        if (xp > 0)
            Skill.GiveSkillXP(player, skill, xp, false, false);

        // GiveSkillXP does not save when the XP changed nothing (a capped skill with no debt); this save then records the removal.
        DB.Set(record);
        return true;
    }

    public bool CanReceiveXp(uint player) => !GetIsDead(player) && GetCurrentHitPoints(player) > 0;

    public void SaveRecord(Player dbPlayer) => DB.Set(dbPlayer);

    public string SerializeItem(uint item) => ObjectPlugin.Serialize(item);

    public int FileSequence(uint player) => GetLocalInt(player, CraftSessionFileSequence.Variable);

    public int AdvanceFileSequence(uint player)
    {
        var next = GetLocalInt(player, CraftSessionFileSequence.Variable) + 1;
        SetLocalInt(player, CraftSessionFileSequence.Variable, next);
        return next;
    }

    public int LoadedSequence(uint player) => GetLocalInt(player, CraftSessionFileSequence.LoadedVariable);

    public int ExportedSequence(uint player) => GetLocalInt(player, CraftSessionFileSequence.ExportedVariable);

    public void FlushCharacter(uint player)
    {
        if (IsExportable(player))
            ExportAndRecord(player);
    }

    private static void ExportAndRecord(uint player)
    {
        // The sequence is read at the moment of the export: the file written carries the value the local has now.
        var sequence = GetLocalInt(player, CraftSessionFileSequence.Variable);
        ExportSingleCharacter(player);
        SetLocalInt(player, CraftSessionFileSequence.ExportedVariable, sequence);
    }

    public void PersistCharacter(uint player)
    {
        if (!IsExportable(player) || GetLocalBool(player, PersistPendingVariable))
            return;

        // A destroyed item is only gone once the current script ends, so the export waits for it; one export covers every change
        // made in the meantime.
        SetLocalBool(player, PersistPendingVariable, true);
        AssignCommand(player, () => DelayCommand(PersistDelaySeconds, () =>
        {
            if (!GetIsObjectValid(player))
                return;

            DeleteLocalBool(player, PersistPendingVariable);
            if (IsExportable(player))
                ExportAndRecord(player);
        }));
    }

    private static bool IsExportable(uint player) =>
        GetIsObjectValid(player) && GetIsPC(player) && !GetIsDM(player) && !GetIsDMPossessed(player);

    public void Message(uint player, string text) => SendMessageToPC(player, text);

    public void LogCraft(uint player, string text) =>
        Log.Write(LogGroup.Crafting, $"{GetName(player)} ({GetObjectUUID(player)}) {text}");
}
