using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Moves a held horn so its mouth end reaches each body's mouth. One playing clip serves very different heads and
/// shoulders, and each species' fit is measured in the client while it plays.
/// </summary>
public static class HornFit
{
    private const ObjectVisualTransformDataScopeType FitScope =
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2;
    // Saved on items in play, so the slitherhorn-era names stay.
    private const string AppliedZVariable = "SLITHERHORN_FIT_Z";
    private const string FittedItemVariable = "SLITHERHORN_FIT_ITEM";
    private const string AppliedXVariable = "HORN_FIT_X";
    private const string AppliedYVariable = "HORN_FIT_Y";
    private const string AppliedRotateYVariable = "HORN_FIT_ROTATE_Y";

    private static readonly Dictionary<(string Resref, AppearanceType Appearance, Gender Gender), HornFitOffset> _playingFits =
        BuildPlayingFits();

    private static Dictionary<(string, AppearanceType, Gender), HornFitOffset> BuildPlayingFits()
    {
        const string kloo = "kloo_horn";
        const string slitherhorn = "slitherhorn";
        var fits = new Dictionary<(string, AppearanceType, Gender), HornFitOffset>
        {
            [(kloo, AppearanceType.MonCalamari, Gender.Male)] = new(0f, 0f, 0f, -8f),
            [(kloo, AppearanceType.MonCalamari, Gender.Female)] = new(0f, 0f, 0f, -10f),
            [(kloo, AppearanceType.Wookiee, Gender.Male)] = new(0f, 0f, 0f, -8f),
            [(kloo, AppearanceType.Wookiee, Gender.Female)] = new(.03f, 0f, .03f, -12f),
            [(kloo, AppearanceType.Cathar, Gender.Male)] = new(0f, .02f, -.26f, 12f),
            [(kloo, AppearanceType.Cathar, Gender.Female)] = new(0f, 0f, 0f, -12f),
            [(kloo, AppearanceType.Trandoshan, Gender.Male)] = new(0f, 0f, 0f, -8f),
            [(kloo, AppearanceType.Rodian, Gender.Male)] = new(0f, 0f, 0f, -8f),
            // The slitherhorn keeps its carry layer, so the same offsets move it the other way.
            [(slitherhorn, AppearanceType.Cathar, Gender.Male)] = new(0f, 0f, .12f, 0f),
            [(slitherhorn, AppearanceType.Cathar, Gender.Female)] = new(0f, 0f, 0f, 8f),
        };

        var femaleKloo = new HornFitOffset(.02f, 0f, 0f, -13f);
        foreach (var appearance in new[]
                 {
                     AppearanceType.Human, AppearanceType.Chiss, AppearanceType.Zabrak, AppearanceType.Twilek,
                     AppearanceType.Mirialan, AppearanceType.Echani, AppearanceType.KelDor, AppearanceType.Cyborg,
                     AppearanceType.Trandoshan, AppearanceType.Togruta, AppearanceType.Nautolan,
                     AppearanceType.Bothan, AppearanceType.Rodian
                 })
            fits[(kloo, appearance, Gender.Female)] = femaleKloo;

        return fits;
    }

    [NWNEventHandler(ScriptName.OnModuleEnter)]
    public static void OnModuleEnter() => RefreshInventory(GetEnteringObject());

    [NWNEventHandler(ScriptName.OnPlayerSpawn)]
    [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
    [NWNEventHandler(ScriptName.OnDroidSpawn)]
    public static void OnSpawn() => RefreshInventory(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var item = GetModuleItemAcquired();
        NormalizeCurrentItem(item);
        Apply(GetItemPossessor(item));
    }

    [NWNEventHandler(ScriptName.OnModuleUnacquire)]
    public static void OnUnacquire() => NormalizeCurrentItem(GetModuleItemLost());

    [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
    public static void OnEquipBefore()
    {
        Release(GetItemInSlot(InventorySlot.RightHand, OBJECT_SELF));
        Release(GetItemInSlot(InventorySlot.LeftHand, OBJECT_SELF));
    }

    [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
    public static void OnUnequipBefore() => Release(StringToObject(EventsPlugin.GetEventData("ITEM")));

    [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
    [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
    public static void OnEquipmentChanged() => RefreshInventory(OBJECT_SELF);

    [NWNEventHandler(ScriptName.OnModuleEquip)]
    public static void OnModuleEquip() => Apply(GetPCItemLastEquippedBy());

    [NWNEventHandler(ScriptName.OnModuleUnequip)]
    public static void OnModuleUnequip()
    {
        Release(GetPCItemLastUnequipped());
        Apply(GetPCItemLastUnequippedBy());
    }

    public static void RefreshInventory(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;

        var right = GetItemInSlot(InventorySlot.RightHand, creature);
        for (var item = GetFirstItemInInventory(creature); GetIsObjectValid(item); item = GetNextItemInInventory(creature))
            NormalizeItemAndContents(item, right);
        Apply(creature);
    }

    private static void NormalizeCurrentItem(uint item)
    {
        if (!GetIsObjectValid(item) || GetObjectType(item) != ObjectType.Item)
            return;
        var owner = GetItemPossessor(item);
        NormalizeItemAndContents(item, GetObjectType(owner) == ObjectType.Creature
            ? GetItemInSlot(InventorySlot.RightHand, owner) : OBJECT_INVALID);
    }

    private static void NormalizeItemAndContents(uint item, uint right)
    {
        if (!GetIsObjectValid(item))
            return;
        if (item != right)
            Release(item);
        if (!GetHasInventory(item))
            return;
        for (var child = GetFirstItemInInventory(item); GetIsObjectValid(child); child = GetNextItemInInventory(item))
            NormalizeItemAndContents(child, right);
    }

    public static void Apply(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
            return;

        Release(GetItemInSlot(InventorySlot.LeftHand, creature));
        var item = GetItemInSlot(InventorySlot.RightHand, creature);
        if (!GetIsObjectValid(item))
            return;
        var performing = Performance.TryGetSession(creature, out var session) && session.Kind == PerformanceKind.Music;
        ApplyFit(item, ResolveFit(GetResRef(item), GetAppearanceType(creature),
            RobeModelRenderer.GetBasePhenotype(creature), GetGender(creature), performing));
    }

    /// <summary>
    /// The playing fits were measured against the playing pose, and would tilt a horn that is only being carried. The
    /// Human slitherhorn offsets are small enough to stay on whenever the horn is held.
    /// </summary>
    private static HornFitOffset ResolveFit(
        string resref, AppearanceType appearance, int phenotype, Gender gender, bool performing)
    {
        if (string.IsNullOrEmpty(resref) || phenotype != 0)
            return default;

        var key = resref.ToLowerInvariant();
        if (key == "slitherhorn" && appearance == AppearanceType.Human)
        {
            return gender switch
            {
                Gender.Male => new HornFitOffset(0f, 0f, .035f, 0f),
                Gender.Female => new HornFitOffset(0f, 0f, -.028f, 0f),
                _ => default
            };
        }

        return performing ? _playingFits.GetValueOrDefault((key, appearance, gender)) : default;
    }

    private static void Release(uint item)
    {
        if (GetIsObjectValid(item) && GetObjectType(item) == ObjectType.Item)
            ApplyFit(item, default);
    }

    private static float RebaseOffset(float current, float previousOffset, float nextOffset) =>
        current - previousOffset + nextOffset;

    private static HornFitOffset ResolveOwnedFit(string fittedItem, string currentItem, HornFitOffset fit) =>
        !string.IsNullOrEmpty(fittedItem) && fittedItem == currentItem ? fit : default;

    private static void ApplyFit(uint item, HornFitOffset fit)
    {
        var saved = new HornFitOffset(GetLocalFloat(item, AppliedXVariable), GetLocalFloat(item, AppliedYVariable),
            GetLocalFloat(item, AppliedZVariable), GetLocalFloat(item, AppliedRotateYVariable));
        if (saved == default && fit == default)
            return;
        // CopyItem copies locals but resets visual transforms; UUIDs survive ordinary reloads.
        var identity = GetObjectUUID(item);
        var previous = ResolveOwnedFit(GetLocalString(item, FittedItemVariable), identity, saved);
        ApplyChannel(item, ObjectVisualTransform.TranslateX, previous.TranslateX, fit.TranslateX);
        ApplyChannel(item, ObjectVisualTransform.TranslateY, previous.TranslateY, fit.TranslateY);
        ApplyChannel(item, ObjectVisualTransform.TranslateZ, previous.TranslateZ, fit.TranslateZ);
        ApplyChannel(item, ObjectVisualTransform.RotateY, previous.RotateY, fit.RotateY);
        // The fit and its owner persist on the item, so reloading cannot compound the fit.
        if (fit == default)
        {
            DeleteLocalFloat(item, AppliedXVariable);
            DeleteLocalFloat(item, AppliedYVariable);
            DeleteLocalFloat(item, AppliedZVariable);
            DeleteLocalFloat(item, AppliedRotateYVariable);
            DeleteLocalString(item, FittedItemVariable);
        }
        else
        {
            SetLocalFloat(item, AppliedXVariable, fit.TranslateX);
            SetLocalFloat(item, AppliedYVariable, fit.TranslateY);
            SetLocalFloat(item, AppliedZVariable, fit.TranslateZ);
            SetLocalFloat(item, AppliedRotateYVariable, fit.RotateY);
            SetLocalString(item, FittedItemVariable, identity);
        }
    }

    private static void ApplyChannel(uint item, ObjectVisualTransform channel, float previous, float offset)
    {
        var current = GetObjectVisualTransform(item, channel, nScope: FitScope);
        var next = RebaseOffset(current, previous, offset);
        if (Math.Abs(current - next) > .000001f)
            SetObjectVisualTransform(item, channel, next, nScope: FitScope);
    }
}
