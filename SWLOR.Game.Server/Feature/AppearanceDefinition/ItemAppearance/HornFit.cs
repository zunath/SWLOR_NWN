using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;

/// <summary>
/// Moves held wind instruments so their mouth ends reach each body's mouth. One playing clip serves different heads and
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
    private const string AppliedRotateZVariable = "HORN_FIT_ROTATE_Z";
    private const string AppliedRotateXVariable = "HORN_FIT_ROTATE_X";

    /// <summary>Places the Flanged Jessoon's shaft and mouthpiece where the kloo horn's are, with no body fit.</summary>
    public static readonly HornFitOffset JessoonOnKlooHorn = new(-.035f, .065f, -.017f, 12.3f, -152.4f, 28.6f);

    /// <summary>
    /// The Jessoon clips hold the instrument where the horn clips hold the kloo horn, with the right hand 6 cm lower on its
    /// body (tools/DeriveJessoonPerformanceMotion.py), so the Jessoon slides 6 cm up through that hand.
    /// </summary>
    public static readonly HornFitOffset JessoonHold =
        HornFitTransform.Compose(JessoonOnKlooHorn, new HornFitOffset(-.0188f, .0569f, .0034f, 0f));

    private static readonly Dictionary<(string Resref, AppearanceType Appearance, Gender Gender), HornFitOffset> _playingFits =
        BuildPlayingFits();

    private static Dictionary<(string, AppearanceType, Gender), HornFitOffset> BuildPlayingFits()
    {
        const string kloo = "kloo_horn";
        const string jessoon = "jessoon";
        const string slitherhorn = "slitherhorn";
        const string panpipes = "panpipes";
        const string tambourine = "tambourine";
        var fits = new Dictionary<(string, AppearanceType, Gender), HornFitOffset>
        {
            // Native front/side checks against Human head 1: turn the row inward, then fit mouth contact.
            [(panpipes, AppearanceType.Human, Gender.Male)] = new(.02f, 0f, 0f, 0f, 180f),
            // Cathar and droid arms hold the pipes short of the mouth: at his cheek, and against her neck and its jaw.
            [(panpipes, AppearanceType.Cathar, Gender.Male)] = new(.085f, 0f, 0f, 0f, 180f),
            [(panpipes, AppearanceType.Cathar, Gender.Female)] = new(.085f, 0f, 0f, 0f, 180f),
            [(panpipes, AppearanceType.Droid, Gender.Male)] = new(.085f, 0f, 0f, 0f, 180f),
            [(panpipes, AppearanceType.Droid, Gender.Female)] = new(.085f, 0f, 0f, 0f, 180f),
            // Wookiees play the pipes with their hands apart (PerformanceAnimations.FitToBody); these fit that hold.
            [(panpipes, AppearanceType.Wookiee, Gender.Male)] = new(.05f, .03f, 0f, 0f, 180f),
            [(panpipes, AppearanceType.Wookiee, Gender.Female)] = new(.05f, 0f, 0f, 0f, 180f),
            // The wide tambourine hold carries the drum out with the holding hand; this slides it back under the palm.
            [(tambourine, AppearanceType.Wookiee, Gender.Male)] = new(.08f, .08f, 0f, 0f),
            [(tambourine, AppearanceType.Wookiee, Gender.Female)] = new(.08f, .08f, 0f, 0f),
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

        // The short bodies hold the pipes at the cheek; one fit reaches the mouth of both species and genders.
        var shortBodyPanpipes = new HornFitOffset(.04f, -.03f, -.06f, 0f, 180f);
        foreach (var appearance in new[] { AppearanceType.Ugnaught, AppearanceType.Ewok })
        foreach (var gender in new[] { Gender.Male, Gender.Female })
            fits[(panpipes, appearance, gender)] = shortBodyPanpipes;

        // The female body holds the pipes at the chin; raising them three centimetres reaches the mouth.
        var femalePanpipes = new HornFitOffset(0f, .03f, 0f, 0f, 180f);
        foreach (var appearance in new[]
                 {
                     AppearanceType.Human, AppearanceType.Chiss, AppearanceType.Zabrak, AppearanceType.Twilek,
                     AppearanceType.Mirialan, AppearanceType.Echani, AppearanceType.KelDor, AppearanceType.Cyborg,
                     AppearanceType.Trandoshan, AppearanceType.Togruta, AppearanceType.Nautolan,
                     AppearanceType.Bothan, AppearanceType.Rodian, AppearanceType.MonCalamari
                 })
            fits[(panpipes, appearance, Gender.Female)] = femalePanpipes;

        var femaleKloo = new HornFitOffset(.02f, 0f, 0f, -13f);
        var femaleKlooBodies = new[]
        {
            AppearanceType.Human, AppearanceType.Chiss, AppearanceType.Zabrak, AppearanceType.Twilek,
            AppearanceType.Mirialan, AppearanceType.Echani, AppearanceType.KelDor, AppearanceType.Cyborg,
            AppearanceType.Trandoshan, AppearanceType.Togruta, AppearanceType.Nautolan,
            AppearanceType.Bothan, AppearanceType.Rodian
        };
        foreach (var appearance in femaleKlooBodies)
            fits[(kloo, appearance, Gender.Female)] = femaleKloo;

        // The Flanged Jessoon plays its own clips (PerformanceAnimations.FitToInstrument): the horn motion with the hands spread
        // along its long body. Each body's fit hangs it from the right hand so the tube runs through both grips and the
        // mouthpiece reaches that body's mouth over the whole loop. tools/SolveJessoonFits.py solves them from the compiled
        // clips; most bodies of one skeleton share a fit, and the rest differ by their heads and hands.
        foreach (var appearance in RacialAppearanceRegistry.GetAppearanceTypes())
        foreach (var gender in new[] { Gender.Male, Gender.Female })
            fits[(jessoon, appearance, gender)] = JessoonHold;

        var femaleJessoon = new HornFitOffset(-.0146f, .1162f, -.0124f, .84f, -146.14f, 27.88f);
        foreach (var appearance in new[]
                 {
                     AppearanceType.Human, AppearanceType.Chiss, AppearanceType.Zabrak, AppearanceType.Twilek,
                     AppearanceType.Mirialan, AppearanceType.Echani, AppearanceType.KelDor, AppearanceType.Cyborg,
                     AppearanceType.Trandoshan, AppearanceType.Togruta, AppearanceType.Nautolan
                 })
            fits[(jessoon, appearance, Gender.Female)] = femaleJessoon;

        var heavyHeadJessoon = new HornFitOffset(-.0472f, .1211f, -.0251f, 2.95f, -173.72f, 32.33f);
        fits[(jessoon, AppearanceType.Trandoshan, Gender.Male)] = heavyHeadJessoon;
        fits[(jessoon, AppearanceType.MonCalamari, Gender.Male)] = heavyHeadJessoon;
        fits[(jessoon, AppearanceType.MonCalamari, Gender.Female)] = new(-.0153f, .1176f, -.0099f, 3.37f, -139.11f, 26.14f);
        fits[(jessoon, AppearanceType.Bothan, Gender.Male)] = new(-.042f, .1162f, -.0142f, 11.35f, -159.97f, 29.58f);
        var snoutJessoon = new HornFitOffset(-.0402f, .1141f, -.0227f, 2.95f, -173.44f, 31.87f);
        fits[(jessoon, AppearanceType.Rodian, Gender.Male)] = snoutJessoon;
        fits[(jessoon, AppearanceType.Wookiee, Gender.Male)] = snoutJessoon;
        var smallFemaleJessoon = new HornFitOffset(-.0126f, .1077f, -.0119f, 1.23f, -140.49f, 26.61f);
        fits[(jessoon, AppearanceType.Bothan, Gender.Female)] = smallFemaleJessoon;
        fits[(jessoon, AppearanceType.Rodian, Gender.Female)] = smallFemaleJessoon;
        fits[(jessoon, AppearanceType.Wookiee, Gender.Female)] = new(-.0138f, .1096f, -.0115f, 2.05f, -138.74f, 26.71f);
        fits[(jessoon, AppearanceType.Cathar, Gender.Male)] = new(-.0502f, .1248f, -.0163f, 12.35f, -152.21f, 28.66f);
        fits[(jessoon, AppearanceType.Cathar, Gender.Female)] = new(-.05f, .1182f, -.0416f, 1.93f, -146.11f, 27.8f);
        fits[(jessoon, AppearanceType.Ugnaught, Gender.Male)] = new(-.0201f, .0388f, -.0136f, 10.47f, -170.76f, 31.22f);
        fits[(jessoon, AppearanceType.Ugnaught, Gender.Female)] = new(-.0224f, .0384f, -.0134f, 10.24f, -168.57f, 31.46f);
        fits[(jessoon, AppearanceType.Ewok, Gender.Male)] = new(-.0367f, .1063f, -.0096f, 9.84f, -170.22f, 31.46f);
        fits[(jessoon, AppearanceType.Ewok, Gender.Female)] = new(-.0395f, .1055f, -.0103f, 9.89f, -167.44f, 31.37f);

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

        // The row must face inward for every normal playing body; head fits add small translations.
        if (key == "panpipes" && performing && (gender == Gender.Male || gender == Gender.Female))
            return _playingFits.GetValueOrDefault((key, appearance, gender), new HornFitOffset(0f, 0f, 0f, 0f, 180f));

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
            GetLocalFloat(item, AppliedZVariable), GetLocalFloat(item, AppliedRotateYVariable),
            GetLocalFloat(item, AppliedRotateZVariable), GetLocalFloat(item, AppliedRotateXVariable));
        if (saved == default && fit == default)
            return;
        // CopyItem copies locals but resets visual transforms; UUIDs survive ordinary reloads.
        var identity = GetObjectUUID(item);
        var previous = ResolveOwnedFit(GetLocalString(item, FittedItemVariable), identity, saved);
        ApplyChannel(item, ObjectVisualTransform.TranslateX, previous.TranslateX, fit.TranslateX);
        ApplyChannel(item, ObjectVisualTransform.TranslateY, previous.TranslateY, fit.TranslateY);
        ApplyChannel(item, ObjectVisualTransform.TranslateZ, previous.TranslateZ, fit.TranslateZ);
        ApplyChannel(item, ObjectVisualTransform.RotateY, previous.RotateY, fit.RotateY);
        ApplyChannel(item, ObjectVisualTransform.RotateZ, previous.RotateZ, fit.RotateZ);
        ApplyChannel(item, ObjectVisualTransform.RotateX, previous.RotateX, fit.RotateX);
        // The fit and its owner persist on the item, so reloading cannot compound the fit.
        if (fit == default)
        {
            DeleteLocalFloat(item, AppliedXVariable);
            DeleteLocalFloat(item, AppliedYVariable);
            DeleteLocalFloat(item, AppliedZVariable);
            DeleteLocalFloat(item, AppliedRotateYVariable);
            DeleteLocalFloat(item, AppliedRotateZVariable);
            DeleteLocalFloat(item, AppliedRotateXVariable);
            DeleteLocalString(item, FittedItemVariable);
        }
        else
        {
            SetLocalFloat(item, AppliedXVariable, fit.TranslateX);
            SetLocalFloat(item, AppliedYVariable, fit.TranslateY);
            SetLocalFloat(item, AppliedZVariable, fit.TranslateZ);
            SetLocalFloat(item, AppliedRotateYVariable, fit.RotateY);
            SetLocalFloat(item, AppliedRotateZVariable, fit.RotateZ);
            SetLocalFloat(item, AppliedRotateXVariable, fit.RotateX);
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
