//DMFI Persistence wrapper functions
// modified version for Knat's NBDE support

//:://////////////////////////////////////////////
//:: Created By: The DMFI Team
//:: Created On:
//:://////////////////////////////////////////////
//:: 2008.07.10 tsunami282 - implemented alternate database support, initially
//::                         for Knat's NBDE


#include "nbde_inc"

void FlushDMFIPersistentData(string sDBName)
{
    NBDE_SetCampaignInt(sDBName, "DMFI_DB_DIRTY", FALSE);
    NBDE_FlushCampaignDatabase(sDBName);
}

int IsDMFIPersistentDataDirty(string sDBName)
{
    return NBDE_GetCampaignInt(sDBName, "DMFI_DB_DIRTY");
}

//Int functions
int GetDMFIPersistentInt(string sDBName, string sDBSetting, object oPlayer = OBJECT_INVALID)
{
    int iReturn = NBDE_GetCampaignInt(sDBName, sDBSetting, oPlayer);
    return iReturn;
}

void SetDMFIPersistentInt(string sDBName, string sDBSetting, int iDBValue, object oPlayer = OBJECT_INVALID)
{
    NBDE_SetCampaignInt(sDBName, sDBSetting, iDBValue, oPlayer);
    NBDE_SetCampaignInt(sDBName, "DMFI_DB_DIRTY", TRUE);
}

//Float functions
float GetDMFIPersistentFloat(string sDBName, string sDBSetting, object oPlayer = OBJECT_INVALID)
{
    float fReturn = NBDE_GetCampaignFloat(sDBName, sDBSetting, oPlayer);
    return fReturn;
}

void SetDMFIPersistentFloat(string sDBName, string sDBSetting, float fDBValue, object oPlayer = OBJECT_INVALID)
{
    NBDE_SetCampaignFloat(sDBName, sDBSetting, fDBValue, oPlayer);
    NBDE_SetCampaignInt(sDBName, "DMFI_DB_DIRTY", TRUE);
}

//String functions
string GetDMFIPersistentString(string sDBName, string sDBSetting, object oPlayer = OBJECT_INVALID)
{
    string sReturn = NBDE_GetCampaignString(sDBName, sDBSetting, oPlayer);
    return sReturn;
}

void SetDMFIPersistentString(string sDBName, string sDBSetting, string sDBValue, object oPlayer = OBJECT_INVALID)
{
    NBDE_SetCampaignString(sDBName, sDBSetting, sDBValue, oPlayer);
    NBDE_SetCampaignInt(sDBName, "DMFI_DB_DIRTY", TRUE);
}

