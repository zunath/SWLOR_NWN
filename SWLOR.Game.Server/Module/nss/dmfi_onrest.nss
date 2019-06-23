/*DMFI Rest System Alpha by hahnsoo

CONTENTS
--------
Placeables>>Special>>Custom 5 - DMFI Bed Roll, DMFI Campfire, DMFI Invisible
Rest Object, DMFI Tent

Items>>Special>>Custom5 - DMFI Bed Roll (100 gp), DMFI Firewood (1 gp), DMFI
Portable Tent (500 gp) (different gp values for different situations)

Scripts - dmfi_onrest
(Yup, that's it)

Description
-----------
This is a robust and versatile rest system that incorporates a LOT of options.
Perhaps too many, I don't know. I tried to find everything that folks would
possibly want in a resting system. The most important "feature" is the rest
conversation menu, which governs for both DM and Player the kind of resting that
is allowed.

The ways you can control rest in this system are:
1) Global vs. Local - Restrict or release restrictions on resting based on world
    settings or on a per-area basis
2) Unlimited vs. Limited vs. No Rest - Have the Players rest at any time they'd
    like. Or Limit them according to certain parameters and toggles. Or don't
    allow them to rest at all. You can set these both globally and locally
    (Unlimited and No Rest areas).
3) Time restriction - The staple of most simple rest restrictions. You can limit
    resting per 1, 2, 4, 8, 12, or 24 in-game hours, and the amount of real-time
    minutes are calculated for the DM. Again, you can set these both globally
    and locally.
4) Placeables - Popularized by Demetrious's Supply-Based Rest, this allows you
    to restrict resting according to proximity to objects. It allows you to use
    DMFI rest objects (tag = dmfi_restobject), campfires, bedrolls, beds, tents
    (a "Name-based" rest placeable), and toggles to include/exclude certain
    classes that typically don't care about such niceties.
5) Armor Restrictions - I'm not quite fond of this particular one, but it is a
    standard feature of many rest systems and thus included in the package.
    Allows you to set what weight of armor allows a PC to rest.
6) Set Hit Point Restrictions - Unlike the other restrictions, this does NOT
    prevent resting. What it does is determine how many hitpoints are regained
    upon resting, from a gradient of no hitpoints to all hitpoint, and some
    interesting options in between (1 HP per level, per 3rd edition, which
    skews against fighter classes and CON based HP gain, which skews in favor of
    lower level characters).
7) Toggle Spell Memorization - This converts the "rest" into a "pseudorest"
    which only heals HP. Useful for a "no spell memorization" zone locally, not
    much use globally.
8) Various other "fluff" settings (Snoring, the rest conversation menu,
    immobilized resting, floating text feedback).

There is also a "big red button" option that simply full rests all PCs in the
area. Useful to quickly work around rest restrictions that you have previously
set up.

Installation
------------
Change your OnRest event script to the dmfi_onrest script. Or you can do an
external execute script call by using ExecuteScript("dmfi_onrest", OBJECT_SELF);
in your current script.

The areas in your module should NOT have the "No Rest" box checked, in the areas
which you wish to use this system.

Configuration
-------------
All configuration of the system is done in-game as a DM. To bring up the Rest
Configuration Menu, press R or the rest button.
The conversation will detail the settings you have in the area (whether you are
using the default Global settings or using the Local area settings to override)
and the particular restrictions that you have set.

Settings are stored Persistently using the Bioware Database, per the DMFI W&W
default persistence options. If you want to use another database system, simply
edit the the dmfi_db_inc wrapper functions to your liking.

Unlimited Rest means just that: No restrictions. You may have global
restrictions set up, but as long as Unlimited rest is set globally or locally,
they are ignored.
No Rest means just that: No resting allowed, regardless of restrictions.
Limited Rest means that the restrictions you have set globally or locally are in
effect. You can restrict resting as stated above in the Description.

When you set any [LOCAL] Area variables, you automatically set the area to
"override" the global rest restrictions. This means that this area follows its
own rules, and isn't governed by the global rules. Setting the [LOCAL] Area
restrictions will copy the current global restriction variables, but after that,
the only way to go back to "global" is to select "Use default [GLOBAL] Module
settings"
Tip: The most useful way to use this is to simply set areas as Unlimited Rest or
No Rest, say an Inn Room or a combat zone, respectively.

Player Notes
------------
If you are using the DMFI Rest Menu (on by default), the rest restrictions (if
any) are displayed on your Rest Conversation Menu, telling you why you can't
rest (if you are restricted). You also have the option to access both the DMFI
Dicebag and the DMFI Emote wand directly from the Rest Menu. This allows you to
use emotes or dice checks WITHOUT having that silly "Use Unique Power"
animation.

Included in this package is a way to do "Alternate Resting Animations". These
animations simply change the way you appear when you rest. Since they use the
ForceRest() function, it isn't a "true" rest... rather it sets you for a certain
amount of time (equal to a normal rest) as un-moveable, and applies the rest at
the end of that time. This just means you don't get the little egg timer.

This is an ALPHA release, and I'm pretty sure I don't know everything about
Resting systems in the universe. I've tried to incorporate nearly all of the
elements I've seen in other available resting systems and encorporate them into
a small (single script), DMFI-integrated package.

I would greatly appreciate feedback, suggestions, additions, omissions, bug
reports, whatever.  Send them to me at hahns_shin@hotmail.com.*/

#include "dmfi_db_inc"
//This function calculates the resting duration based on PC Hit Dice
//Based off of restduration.2da
void FloatyText(string sText, object oPC, int iSettings)
{
    if (!(iSettings & 0x40000000))
        FloatingTextStringOnCreature(sText, oPC, FALSE);
}

float GetRestDuration(object oPC)
{
    return 10.0f + 0.5f * IntToFloat(GetHitDice(oPC));
}

// This function is used as a wrapper for the Rest VFX Object
void DoRestVFX(object oPC, float fDuration, int nEffect) {
    effect eEffect;
    if (nEffect == -1) {
        eEffect = EffectCutsceneImmobilize();
    } else {
        eEffect = EffectVisualEffect(nEffect);
    }
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, ExtraordinaryEffect(eEffect), oPC, fDuration);
}


//This function adds the Blindness/Snore effects
//Also adds cutscene immobilize to prevent movement
//Snoring should only occur at start, then follows on the module's hb
void ApplyRestVFX(object oPC, int iSettings)
{
    object oRestVFX = GetObjectByTag("dmfi_restvfxobject");
    effect eSnore = EffectVisualEffect(VFX_IMP_SLEEP); //Sleepy "ZZZ"s
    float fDuration = GetRestDuration(oPC);
    float fSeconds = 6.0f;
    if (!(iSettings & 0x80000000)) //Immobile Resting flag
    {
        // Pass a -1 for EffectCutsceneImmobilize.
        // For a visual effect, simply pass the VFX constant.
        AssignCommand(oRestVFX, DoRestVFX(oPC, fDuration, -1));
    }
    if (!(iSettings & 0x20000000)) //VFX flag
    {
        // AssignCommand(oRestVFX, ApplyEffectToObject(DURATION_TYPE_TEMPORARY, ExtraordinaryEffect(eBlind), oPC, fDuration));
        AssignCommand(oRestVFX, DoRestVFX(oPC, fDuration, VFX_DUR_BLACKOUT));
        ApplyEffectToObject(DURATION_TYPE_INSTANT, eSnore, oPC);
    }
}


// Removes blindness & immobilize -- Merle
void RemoveRestVFX(object oPC) {
    object oRestVFX = GetObjectByTag("dmfi_restvfxobject");
    effect eEffect = GetFirstEffect(oPC);
    while (GetIsEffectValid(eEffect)) {
        if (GetEffectCreator(eEffect) == oRestVFX) {
            RemoveEffect(oPC, eEffect);
        }
        eEffect = GetNextEffect(oPC);
    }
}


//This function gets the "Final HP" available to the PC after resting
int CalculateFinalHitPoints(object oPC, int iSettings)
{
    int iHP = (iSettings & 0x0f000000);
    switch(iHP)
    {
        case 0x01000000: return 0; break;
        case 0x02000000: return GetHitDice(oPC); break;
        case 0x03000000: return GetAbilityScore(oPC, ABILITY_CONSTITUTION); break;
        case 0x04000000: return GetMaxHitPoints(oPC)/10; break;
        case 0x05000000: return GetMaxHitPoints(oPC)/4; break;
        case 0x06000000: return GetMaxHitPoints(oPC)/2; break;
        case 0x07000000: return GetMaxHitPoints(oPC); break;
        default: return GetMaxHitPoints(oPC); break;
    }
    return GetMaxHitPoints(oPC);
}

void RemoveMagicalEffects(object oPC)
{
    effect eEffect = GetFirstEffect(oPC);
    while (GetIsEffectValid(eEffect))
    {
        if (GetEffectSubType(eEffect) == SUBTYPE_MAGICAL)
            RemoveEffect(oPC, eEffect);
        eEffect = GetNextEffect(oPC);
    }
}

//This function simulates a rest without restoring spells
void DoPseudoRest(object oPC, int iSettings, int iSpells = FALSE)
{
    effect eSnore = EffectVisualEffect(VFX_IMP_SLEEP);
    effect eBlind = EffectVisualEffect(VFX_DUR_BLACKOUT);
    effect eStop = EffectCutsceneImmobilize();
    float fDuration = GetRestDuration(oPC);
    float fSeconds = 6.0f;
    int iAnimation = GetLocalInt(oPC, "dmfi_r_alternate");
    if (!iAnimation)
        iAnimation = ANIMATION_LOOPING_SIT_CROSS;
    AssignCommand(oPC, PlayAnimation(iAnimation, 1.0f, fDuration));
    DelayCommand(0.1, SetCommandable(FALSE, oPC));
    DelayCommand(fDuration, SetCommandable(TRUE, oPC));
    ApplyEffectToObject(DURATION_TYPE_TEMPORARY, ExtraordinaryEffect(eStop), oPC, fDuration);
    if (!(iSettings & 0x20000000) && iAnimation != ANIMATION_LOOPING_MEDITATE && iAnimation != ANIMATION_LOOPING_WORSHIP) //If the No VFX flag is not set, do VFX
    {
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, ExtraordinaryEffect(eBlind), oPC, fDuration);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, eSnore, oPC);
        while (fSeconds < fDuration)
        {
            DelayCommand(fSeconds, ApplyEffectToObject(DURATION_TYPE_INSTANT, eSnore, oPC));
            fSeconds += 6.0f;
        }
    }
    if (!iSpells)
    {
        effect eHeal = EffectHeal(CalculateFinalHitPoints(oPC, iSettings)); //Heal the PC
        DelayCommand(fDuration + 0.1f, ApplyEffectToObject(DURATION_TYPE_INSTANT, eHeal, oPC));
        DelayCommand(fDuration + 0.1f, RemoveMagicalEffects(oPC)); //Remove all magical effects from PC
    }
    else
    {
        DelayCommand(fDuration + 0.1f, ForceRest(oPC));
    }
    DeleteLocalInt(oPC, "dmfi_r_alternate");
}

//This function determines if the PC is wearing heavy armor
int GetIsWearingHeavyArmor(object oPC, int iSettings)
{
    int iArmor = (iSettings & 0x00f00000);
    object oArmor = GetItemInSlot(INVENTORY_SLOT_CHEST, oPC);
    int iWeight = GetWeight(oArmor);
    switch(iArmor)
    {
        default:
        case 0x00100000: if (iWeight > 20) return TRUE; break;
        case 0x00200000: if (iWeight > 60) return TRUE; break;
        case 0x00300000: if (iWeight > 110) return TRUE; break;
        case 0x00400000: if (iWeight > 160) return TRUE; break;
        case 0x00500000: if (iWeight > 310) return TRUE; break;
        case 0x00600000: if (iWeight > 410) return TRUE; break;
        case 0x00700000: if (iWeight > 460) return TRUE; break;
    }
    return FALSE;
}

//This function determines if the PC is near a resting placeable
int GetIsNearRestingObject(object oPC, int iSettings)
{
    if (iSettings & 0x00020000) //Ignore Druid
    {
        if (GetLevelByClass(CLASS_TYPE_DRUID, oPC))
            return TRUE;
    }
    if (iSettings & 0x00040000) //Ignore Ranger
    {
        if (GetLevelByClass(CLASS_TYPE_RANGER, oPC))
            return TRUE;
    }
    if (iSettings & 0x00080000) //Ignore Barb
    {
        if (GetLevelByClass(CLASS_TYPE_BARBARIAN, oPC))
            return TRUE;
    }
    object oPlaceable = GetFirstObjectInShape(SHAPE_SPHERE, 6.0f, GetLocation(oPC), TRUE, OBJECT_TYPE_PLACEABLE);
    while (GetIsObjectValid(oPlaceable))
    {
        if (!(iSettings & 0x00001000) && GetTag(oPlaceable) == "dmfi_rest") //DMFI Placeables: by default, ON
            return TRUE;
        if ((iSettings & 0x00002000) && GetStringLowerCase(GetName(oPlaceable)) == "campfire") //Campfires
            return TRUE;
        if ((iSettings & 0x00004000) && (GetStringLowerCase(GetName(oPlaceable)) == "bed roll" || GetStringLowerCase(GetName(oPlaceable)) == "bedroll")) //Bed rolls
            return TRUE;
        if ((iSettings & 0x00008000) && GetStringLowerCase(GetName(oPlaceable)) == "bed") //beds
            return TRUE;
        if ((iSettings & 0x00010000) && GetStringLowerCase(GetName(oPlaceable)) == "tent") //tents
            return TRUE;
        oPlaceable = GetNextObjectInShape(SHAPE_SPHERE, 6.0f, GetLocation(oPC), TRUE, OBJECT_TYPE_PLACEABLE);
    }
    return FALSE;
}

// Updated to allow 6 hour breaks and to pass in a percentage if rest is interrupted
void SetNextRestTime(object oPC, int iSettings, float fPercentage = 1.0)
{
    if (fPercentage > 1.0 || fPercentage <= 0.0) {
        fPercentage = 1.0;
    }
    int iHours = (iSettings & 0x00000f00);
    int iTime = GetTimeHour() + GetCalendarDay() * 24 + GetCalendarMonth() * 24 * 28 + GetCalendarYear() * 24 * 28 * 12;

    switch(iHours)
    {
        default:
        case 0x00000100: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(1) * fPercentage)); break;
        case 0x00000200: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(2) * fPercentage)); break;
        case 0x00000300: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(4) * fPercentage)); break;
        case 0x00000400: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(6) * fPercentage)); break;
        case 0x00000500: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(8) * fPercentage)); break;
        case 0x00000600: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(12) * fPercentage)); break;
        case 0x00000700: SetLocalInt(oPC, "dmfi_r_nextrest", iTime + FloatToInt(IntToFloat(24) * fPercentage)); break;
    }
}


//This function determines whether or not you can rest.
int DMFI_CanIRest(object oPC, int iSettings)
{
    if (GetIsDM(oPC)) return TRUE;
    if (iSettings & 0x00000002) //No Rest Override
    {
        if (iSettings & 0x00000080)
            FloatyText("This is a No Rest area", oPC, iSettings);
        return FALSE;
    }
    if (!(iSettings & 0x00000001)) //Unlimited Rest Override
    {
        if (iSettings & 0x00000080)
            FloatyText("This is an Unlimited Rest area", oPC, iSettings);
        return TRUE;
    }
    if ((iSettings & 0x00000004) && (iSettings & 0x00000001)) //Time restriction
    {
        int iTime = GetTimeHour() + GetCalendarDay() * 24 + GetCalendarMonth() * 24 * 28 + GetCalendarYear() * 24 * 28 * 12;
        if (iTime < GetLocalInt(oPC, "dmfi_r_nextrest"))
        {
            FloatyText("You cannot rest at this time. You may rest again in " + IntToString(GetLocalInt(oPC, "dmfi_r_nextrest") - iTime) + " hours.", oPC, iSettings);
            return FALSE;
        }
    }
    if ((iSettings & 0x00000008) && (iSettings & 0x00000001)) //Placeable restriction
    {
        if (!GetIsNearRestingObject(oPC, iSettings))
        {
            FloatyText("You are not near a rest placeable", oPC, iSettings);
            return FALSE;
        }
    }
    if ((iSettings & 0x00000010) && (iSettings & 0x00000001)) //Armor restriction
    {
        if (GetIsWearingHeavyArmor(oPC, iSettings))
        {
            FloatyText("Your current armor is too heavy to rest", oPC, iSettings);
            return FALSE;
        }
    }
    return TRUE;
}

void main()
{
    object oPC = GetLastPCRested();
    object oArea = GetArea(oPC);
    int iSettings;
    int iModSettings = GetDMFIPersistentInt("dmfi", "dmfi_r_");
    int iAreaSettings = GetDMFIPersistentInt("dmfi", "dmfi_r_" + GetTag(oArea));
    if (iAreaSettings & 0x00000080)
    {
        iSettings = iAreaSettings;
    }
    else
    {
        iSettings = iModSettings;
    }
    SetLocalInt(oPC, "dmfi_r_settings", iSettings);

    if (GetLastRestEventType()==REST_EVENTTYPE_REST_STARTED)
    {
        SetLocalInt(oPC, "dmfi_norest", !(DMFI_CanIRest(oPC, iSettings)));
        SetLocalInt(oPC, "dmfi_r_hitpoints", GetCurrentHitPoints(oPC));
        if (GetIsDM(oPC) || (!(iSettings & 0x10000000) && !GetLocalInt(oPC, "dmfi_r_bypass")))
        { //If the Rest Conversation variable is set, then activate the rest conversation here.
            AssignCommand(oPC, ClearAllActions());
            SetLocalString(oPC, "dmfi_univ_conv", "rest");
            AssignCommand(oPC, ActionStartConversation(oPC, "dmfi_universal", TRUE));
            return;
        }
        if (GetLocalInt(oPC, "dmfi_norest")) //PC cannot rest
        {
            AssignCommand(oPC, ClearAllActions());
            DeleteLocalInt(oPC, "dmfi_r_bypass");
            return;
        }
        if ((iSettings & 0x00000004) && (iSettings & 0x00000001)) //Time restriction
            SetNextRestTime(oPC, iSettings);

        if (GetLocalInt(oPC, "dmfi_r_alternate") || ((iSettings & 0x00000040) && (iSettings & 0x00000001)))
        {
            AssignCommand(oPC, ClearAllActions());
            if ((iSettings & 0x00000040) && (iSettings & 0x00000001))
                FloatyText("You cannot regain your spells in this area",oPC, iSettings);
            DoPseudoRest(oPC, iSettings, ((iSettings & 0x00000040) && (iSettings & 0x00000001)));
            DeleteLocalInt(oPC, "dmfi_r_bypass");
            return;
        }
        else if (!(iSettings & 0x20000000))
        { //Rest VFX
            ApplyRestVFX(oPC, iSettings);
        }
        if ((iSettings & 0x00000020) && (iSettings & 0x00000001))
        { //Auto Party Drop
            FloatyText("You have been removed from the party to prevent rest canceling",oPC, iSettings);
            RemoveFromParty(oPC);
        }
    }
    else if (GetLastRestEventType()==REST_EVENTTYPE_REST_CANCELLED)
    {
        // Make sure that resting has been initialized and the start time has been set. Otherwise, the Cancelled Rest Event was fired by
        // the Resting conversation.
        if (GetLocalInt(oPC, "dmfi_r_init"))
        {
            int iTime = GetTimeSecond() + GetTimeMinute() * 60 + GetTimeHour() * 3600 + GetCalendarDay() * 24 * 3600 + GetCalendarMonth() *3600 * 24 * 28 + GetCalendarYear() * 24 * 28 * 12 * 3600;
            int nTimeRested = iTime - GetLocalInt(oPC, "dmfi_r_startseconds");
            int nFullTime = FloatToInt(GetRestDuration(oPC));
            float fPercentage = IntToFloat(nTimeRested) / IntToFloat(nFullTime);
            SetNextRestTime(oPC, iSettings, fPercentage);
            // SendMessageToPC(oPC, "Rest interrupted; resting for " + IntToString(nTimeRested) + " out of " + IntToString(nFullTime) + " seconds (" + FloatToString(fPercentage) + "%).");
            SetLocalInt(oPC, "dmfi_r_init", FALSE);
            if ((iSettings & 0x00000020) && GetCurrentHitPoints(oPC) > GetLocalInt(oPC, "dmfi_r_hitpoints") && iSettings & 0x00000001) //HP restriction
            {
                effect eDam = EffectDamage(GetMaxHitPoints(oPC) - GetLocalInt(oPC, "dmfi_r_hitpoints"));
                FloatyText("Your hitpoints have been reset",oPC, iSettings);
                AssignCommand(oPC, ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oPC));

            }
        }
        RemoveRestVFX(oPC);
    }
    else if (GetLastRestEventType()==REST_EVENTTYPE_REST_FINISHED)
    {
        if ((iSettings & 0x00000020) && (iSettings & 0x00000001)) //HP restriction
        {
            int iDam = GetMaxHitPoints(oPC) - GetLocalInt(oPC, "dmfi_r_hitpoints") - CalculateFinalHitPoints(oPC, iSettings);
            if (iDam > 0)
            {
                effect eDam = EffectDamage(iDam);
                FloatyText("You gain back limited HP from this rest",oPC, iSettings);
                AssignCommand(oPC, ApplyEffectToObject(DURATION_TYPE_INSTANT, eDam, oPC));
            }
        }
    }
    DeleteLocalInt(oPC, "dmfi_r_bypass");
}
