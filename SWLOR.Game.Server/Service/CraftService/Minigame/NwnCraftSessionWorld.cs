using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SkillService;
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
        var stacks = new List<uint>();
        for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
        {
            if (recipe.Components.ContainsKey(GetResRef(item)))
                stacks.Add(item);
        }

        // Plan from the last stack to the first; nothing is touched until every component is found.
        var remaining = recipe.Components.ToDictionary(x => x.Key, y => y.Value);
        var plan = new List<(uint Stack, int Quantity)>();
        for (var index = stacks.Count - 1; index >= 0; index--)
        {
            var resref = GetResRef(stacks[index]);
            if (!remaining.TryGetValue(resref, out var needed))
                continue;

            var quantity = Math.Min(GetItemStackSize(stacks[index]), needed);
            plan.Add((stacks[index], quantity));

            if (needed - quantity <= 0)
                remaining.Remove(resref);
            else
                remaining[resref] = needed - quantity;
        }

        if (remaining.Count > 0)
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

    public bool TryTakeEnhancement(uint player, uint item, out string serialized)
    {
        serialized = string.Empty;
        if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
            return false;

        serialized = ObjectPlugin.Serialize(item);
        DestroyObject(item);
        return true;
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

    public void GiveXp(uint player, SkillType skill, int xp) => Skill.GiveSkillXP(player, skill, xp, false, false);

    public void SaveRecord(Player dbPlayer) => DB.Set(dbPlayer);

    public void Message(uint player, string text) => SendMessageToPC(player, text);

    public void LogCraft(uint player, string text) =>
        Log.Write(LogGroup.Crafting, $"{GetName(player)} ({GetObjectUUID(player)}) {text}");
}
