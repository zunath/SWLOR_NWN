#include "zep_inc_scrptdlg"

const int ITEM_PROPERTY_USE_LIMITATION_GENDER = 150;

//Baseitem: New Weapon Types
const int BASE_ITEM_TRIDENT_1H = 300;
const int BASE_ITEM_HEAVYPICK = 301;
const int BASE_ITEM_LIGHTPICK = 302;
const int BASE_ITEM_SAI = 303;
const int BASE_ITEM_NUNCHAKU = 304;
const int BASE_ITEM_FALCHION1 = 305;
const int BASE_ITEM_SAP = 308;
const int BASE_ITEM_DAGGERASSASSIN = 309;
const int BASE_ITEM_KATAR = 310;
const int BASE_ITEM_LIGHTMACE2 = 312;
const int BASE_ITEM_KUKRI2 = 313;
const int BASE_ITEM_FALCHION2 = 316;
const int BASE_ITEM_HEAVYMACE = 317;
const int BASE_ITEM_MAUL = 318;
const int BASE_ITEM_MERCURIALLONGSWORD = 319;
const int BASE_ITEM_MERCURIALGREATSWORD = 320;
const int BASE_ITEM_DOUBLESCIMITAR = 321;
const int BASE_ITEM_GOAD = 322;
const int BASE_ITEM_WINDFIREWHEEL = 323;


int ColorInit(string sLightConst);

int ColorInit(string sLightConst)
{
    int nLight = VFX_DUR_LIGHT;//if sLight remains uninitialized,
    //this causes no light effect to be placed rather than the humming,
    //blue/white flashing effect that occurs when the VFX constant is set to 0
    string sLeft = GetStringLeft(sLightConst, 2);
    string sRight = GetStringRight(sLightConst, 2);
    //this converts the name of the light constant in the string CEP_L_LIGHTCONST
    //to an integer which can be used by the engine.
    if(sLeft == "BL"){
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_BLUE_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_BLUE_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_BLUE_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_BLUE_20;
    }
    if(sLeft == "GR"){//note: grey lighting is actually green, go figure
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_GREY_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_GREY_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_GREY_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_GREY_20;
    }
    if(sLeft == "OR"){
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_ORANGE_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_ORANGE_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_ORANGE_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_ORANGE_20;
    }
    if(sLeft == "PU"){
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_PURPLE_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_PURPLE_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_PURPLE_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_PURPLE_20;
    }
    if(sLeft == "RE"){
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_RED_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_RED_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_RED_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_RED_20;
    }
    if(sLeft == "WH"){
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_WHITE_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_WHITE_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_WHITE_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_WHITE_20;
    }
    if(sLeft == "YE"){
        if(sRight == "_5") nLight = VFX_DUR_LIGHT_YELLOW_5;
        if(sRight == "10") nLight = VFX_DUR_LIGHT_YELLOW_10;
        if(sRight == "15") nLight = VFX_DUR_LIGHT_YELLOW_15;
        if(sRight == "20") nLight = VFX_DUR_LIGHT_YELLOW_20;
    }
    return nLight;
}

//Function will check if oPC is the correct gender
//to equip oItem.  If not, it will force the player
//to unequip the item and inform them of the reason.
void ZEPGenderRestrict(object oItem, object oPC);

void ZEPGenderRestrict(object oItem, object oPC)
{
// ---------------------------------------------------
    // CEP Gender restriction property script
    // Searches the item just equipped for a gender
    // restriction property.  If it finds it, it will
    // check the PC's gender against the appropriate value
    // and de-equip the item if they are found not to
    // match.
    // ---------------------------------------------------
    //First we check if this has the item property: Use Limitation: Gender.
    //If so, we enter the if statment and check PC gender
    //vs. the item's limitation.  Else we continue out of the
    //function.
    if (GetItemHasItemProperty(oItem,ITEM_PROPERTY_USE_LIMITATION_GENDER))
        {
        itemproperty ipGenderProperty=GetFirstItemProperty(oItem);
        //We're not sure if the above property is the one
        //we want, so we'll check it vs. the Gender property,
        //and, if it's not it, loop through until we find it.
        while ((GetIsItemPropertyValid(ipGenderProperty))&&(GetItemPropertyType(ipGenderProperty)!=ITEM_PROPERTY_USE_LIMITATION_GENDER))
            {
            ipGenderProperty=GetNextItemProperty(oItem);
            }
        //If, after all that, the property is invalid for
        //some reason, we return.  Else we now have a property
        //with the data of the Gender restriction of teh PC's
        //item.
        if (!GetIsItemPropertyValid(ipGenderProperty)) return;
        //Next line is kind of long and wonky looking, but bear
        //with me as I'm doing this to cut out variables.
        //We're comparing the item property parameter value (gender
        //type) vs. the PCs.  Theoretically they use the same
        //scale...
        //If they are not the same, we de-euip the item
        if (GetItemPropertySubType(ipGenderProperty)!=GetGender(oPC))
            {
            //Not equal, so take it off!
            AssignCommand(oPC,ActionUnequipItem(oItem));
            //Tell PC why.
            string sMessageToPC= GetStringByStrRef(nZEPGenderRestTXT,GENDER_MALE);
            SendMessageToPC(oPC,sMessageToPC);
            }
        }
}

