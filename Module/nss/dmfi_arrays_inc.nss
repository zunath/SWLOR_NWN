//::///////////////////////////////////////////////
//:: DMFI - array functions include
//:: dmfi_arrays_inc
//:://////////////////////////////////////////////
/*
  Functions to use object-attached local variables as arrays.
*/
//:://////////////////////////////////////////////
//:: Created By: Noel
//:: Created On: November 17, 2001
//:://////////////////////////////////////////////
//:: 2007.12.24 tsunami282 - yanked most of these routines from Bioware's
//::                         nw_o0_itemmaker, then expanded for bounds management.

int GetLocalArrayLowerBound(object oidObject, string sVarName);
int GetLocalArrayUpperBound(object oidObject, string sVarName);
void SetLocalArrayLowerBound(object oidObject, string sVarName, int nMin);
void SetLocalArrayUpperBound(object oidObject, string sVarName, int nMax);

////////////////////////////////////////////////////////////////////////
int GetLocalArrayInitialized(object oidObject, string sVarName)
{
    string sFullVarName = sVarName + "_INIT";
    return GetLocalInt(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void InitializeLocalArray(object oidObject, string sVarName)
{
    int i, iBegin, iEnd;
    string sFullVarName;

    if (GetLocalArrayInitialized(oidObject, sVarName))
    {
        // wipe current contents
        iBegin = GetLocalArrayLowerBound(oidObject, sVarName);
        iEnd = GetLocalArrayUpperBound(oidObject, sVarName);
        for (i = iEnd; i >= iBegin; i--)
        {
            sFullVarName = sVarName + IntToString(i);
            DeleteLocalInt(oidObject, sFullVarName);
            DeleteLocalFloat(oidObject, sFullVarName);
            DeleteLocalString(oidObject, sFullVarName);
            DeleteLocalObject(oidObject, sFullVarName);
            DeleteLocalLocation(oidObject, sFullVarName);
        }
    }

    SetLocalArrayLowerBound(oidObject, sVarName, 0);
    SetLocalArrayUpperBound(oidObject, sVarName, -1);
    sFullVarName = sVarName + "_INIT";
    SetLocalInt(oidObject, sFullVarName, TRUE);
}

////////////////////////////////////////////////////////////////////////
int GetLocalArrayLowerBound(object oidObject, string sVarName)
{
    string sFullVarName = sVarName + "_MIN";
    return GetLocalInt(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
int GetLocalArrayUpperBound(object oidObject, string sVarName)
{
    string sFullVarName = sVarName + "_MAX";
    return GetLocalInt(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayLowerBound(object oidObject, string sVarName, int nMin)
{
    string sFullVarName = sVarName + "_MIN";
    SetLocalInt(oidObject, sFullVarName, nMin);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayUpperBound(object oidObject, string sVarName, int nMax)
{
    string sFullVarName = sVarName + "_MAX";
    SetLocalInt(oidObject, sFullVarName, nMax);
}

////////////////////////////////////////////////////////////////////////
int GetLocalArrayInt(object oidObject, string sVarName, int nVarNum)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    return GetLocalInt(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayInt(object oidObject, string sVarName, int nVarNum, int nValue)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    SetLocalInt(oidObject, sFullVarName, nValue);
    // update bounds
    if (nVarNum < GetLocalArrayLowerBound(oidObject, sVarName))
        SetLocalArrayLowerBound(oidObject, sVarName, nVarNum);
    if (nVarNum > GetLocalArrayUpperBound(oidObject, sVarName))
        SetLocalArrayUpperBound(oidObject, sVarName, nVarNum);
}

////////////////////////////////////////////////////////////////////////
float GetLocalArrayFloat(object oidObject, string sVarName, int nVarNum)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    return GetLocalFloat(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayFloat(object oidObject, string sVarName, int nVarNum, float fValue)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    SetLocalFloat(oidObject, sFullVarName, fValue);
    // update bounds
    if (nVarNum < GetLocalArrayLowerBound(oidObject, sVarName))
        SetLocalArrayLowerBound(oidObject, sVarName, nVarNum);
    if (nVarNum > GetLocalArrayUpperBound(oidObject, sVarName))
        SetLocalArrayUpperBound(oidObject, sVarName, nVarNum);
}

////////////////////////////////////////////////////////////////////////
string GetLocalArrayString(object oidObject, string sVarName, int nVarNum)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    return GetLocalString(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayString(object oidObject, string sVarName, int nVarNum, string nValue)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    SetLocalString(oidObject, sFullVarName, nValue);
    // update bounds
    if (nVarNum < GetLocalArrayLowerBound(oidObject, sVarName))
        SetLocalArrayLowerBound(oidObject, sVarName, nVarNum);
    if (nVarNum > GetLocalArrayUpperBound(oidObject, sVarName))
        SetLocalArrayUpperBound(oidObject, sVarName, nVarNum);
}

////////////////////////////////////////////////////////////////////////
object GetLocalArrayObject(object oidObject, string sVarName, int nVarNum)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    return GetLocalObject(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayObject(object oidObject, string sVarName, int nVarNum, object oidValue)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    SetLocalObject(oidObject, sFullVarName, oidValue);
    // update bounds
    if (nVarNum < GetLocalArrayLowerBound(oidObject, sVarName))
        SetLocalArrayLowerBound(oidObject, sVarName, nVarNum);
    if (nVarNum > GetLocalArrayUpperBound(oidObject, sVarName))
        SetLocalArrayUpperBound(oidObject, sVarName, nVarNum);
}

////////////////////////////////////////////////////////////////////////
location GetLocalArrayLocation(object oidObject, string sVarName, int nVarNum)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    return GetLocalLocation(oidObject, sFullVarName);
}

////////////////////////////////////////////////////////////////////////
void SetLocalArrayLocation(object oidObject, string sVarName, int nVarNum, location locValue)
{
    string sFullVarName = sVarName + IntToString(nVarNum) ;
    SetLocalLocation(oidObject, sFullVarName, locValue);
    // update bounds
    if (nVarNum < GetLocalArrayLowerBound(oidObject, sVarName))
        SetLocalArrayLowerBound(oidObject, sVarName, nVarNum);
    if (nVarNum > GetLocalArrayUpperBound(oidObject, sVarName))
        SetLocalArrayUpperBound(oidObject, sVarName, nVarNum);
}

