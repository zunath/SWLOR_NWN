//void main(){}

//::///////////////////////////////////////////////
//:: Natural Bioware Database Extension v1.0
//:: nbde_inc
//:: Copyright (c) 2001 Bioware Corp.
//:://////////////////////////////////////////////
/*

*/
//:://////////////////////////////////////////////
//:: Created By: Knat
//:: Created On: 8/2004
//:://////////////////////////////////////////////
/*

Natural Bioware Database Extension v1.0
"Andale, Andale! EEEE-ha....!"

-----------------------------------------------------------------------------
--- What is NBDE ?
-----------------------------------------------------------------------------

NBDE is basically a wrapper for the standard bioware database system,
eliminating most of its restrictions. But even more important, it significantly
boosts both reading and writing speed.

It will make your db scripts more secure and always keeps your database files
in the best possible shape. It furthermore reduces the amount of overhead in
your database and keeps it as slim as possible.

there is no need to periodically use a pack utility, which further
reduces administrative tasks...

and this all gets achieved with the use of this simple script. I recommend any
scripter to check this out if he plans to use biowares onboard database
functionality. It should also be very easy to convert already existing scripts.

-----------------------------------------------------------------------------
--- Installation
-----------------------------------------------------------------------------

Simply import nbde.erf and you are done.

It includes the following stuff:

Scripts:

 name: nbde_inc
   main include script...

Areas:

  name: _NBDE
   special area. this is a 2x2 microset area holding the database vault container.
   you can delete this area and move the container to another place if you want.

Items:

  Custom > Special > Custom 4
   name: Database
     special database item with the resref "nbde_database"
     don't touch this item...

-----------------------------------------------------------------------------
--- Eliminated Restrictions ?
-----------------------------------------------------------------------------

Biowares database system mimics the interface of Local Variables.
Instead of SetLocalInt() you use SetCampaignInt(), GetLocalInt() turns into
GetCampaignInt(). This makes it very easy to use, even for novice scripters.
But the normal bioware database does not consequently implement this approach.
Several stumbling blocks, slight differences to normal Set-/GetLocal functions,
may lead to severe functional problems and hard to track bugs if you try to
achieve a bit more complex goals...

>>> 32-Char sVarName Limitation:
--------------------------------

the sVarName parameter in the original functions only accepts strings with a
maximum length of 32 chars. it will simply cut the string if it exceeds this
limit...

example:

SetCampaignInt("MYDB", "PREFIX" + GetTag(oArea), 100);

The second parameter is the sVarName one, with the 32 char limit. The above
statement is a bit risky, because GetTag(oArea) could return a string with a
maximum length of 32. "PREFIX" has a length of 6 chars, so any area with a
tag of length >26 could lead to unintended sVarNames.

Same for this example:

SetCampaignInt("MYDB", GetName(oPC) + GetPCPlayerName(oPC), 100);

same problem. GetName() alone may return a string with a length >32, which
could again lead to a problematic sVarName.

NBDE completely eliminates the 32-char sVarName limitation and enables the
scripter to use the full scope of dynamically concatenated sVarNames,
without the need of hashing systems or other workarounds, which generally
consume a bit of extra cpu time...

>>> UNIQUE sVarName Limitation:
-------------------------------

the sVarName parameter in the original functions MUST be unique across the
entire database, regardless of the variable type.

example:

SetCampaignInt("MYDB", "TEST", 10);
SetCampaignString("MYDB", "TEST", "ABCD");

the second line will OVERWRITE the former integer variable "TEST" with a string.
This means a GetCampaignInt("MYDB", "TEST") returns 0

using NBDE eliminates this limitation.
It works now similar to LocalVariables (the intended goal)

NBDE conversion of the above example:

NBDE_SetCampaignInt("MYDB", "TEST", 10);
NBDE_SetCampaignString("MYDB", "TEST", "ABCD");

second line will not overwrite the integer variable

NBDE_GetCampaignInt("MYDB", "TEST") returns the correct 10
NBDE_GetCampaignString("MYDB", "TEST") returns "ABCD"

The original function set contains only one delete command, called
DeleteCampaignVariable(), because of the unique nature of sVarNames.

NBDE contains one delete command for each variable type, to account for the
possible non uniqueness:

NBDE_DeleteCampaignInt()
NBDE_DeleteCampaignFloat()
NBDE_DeleteCampaignString()
NBDE_DeleteCampaignVector()
NBDE_DeleteCampaignLocation()

this again now works similar to the LocalVariables interface, which also
gives you a delete command for each variable type:

aka DeleteLoaclInt(), DeleteLocalFloat(), DeleteLocalString(),
    DeleteLocalVector(), DeleteLocalLocation()

>>> Broken Locations:
---------------------

the original SetCampaignLocation/GetCampaignLocation functions are not very
reliable, because they are using the areas object-id for reference, which
is a runtime generated ID. stored locations in the database can get invalid
if you change the area layout in the toolset (e.g. deleting old areas, etc.)

nbde location functions are 100% reliable, as long as you use unique TAGs for
your areas. I repeat, you need to use UNIQUE TAGS for your areas...

-----------------------------------------------------------------------------
--- No need to pack the database
-----------------------------------------------------------------------------

NWN's database files grow very large, very fast, because deleted entries get
only "flagged" as deleted. but they still reside in the dabase file physically.

to stop this evergrowing database, you usually call an external "pack"
utility which reorganizes the database files (deletion of flagged entries,
index re-ordering, etc.)

unfortunately, the only working pack utility is the one you find in the
/utils directory, called DataPack.exe . But some people reported problems
on large database files... (i never had problems with this tool, though)

the good news is, you don't need to touch this utility ever, while using
this extension. NBDE will automatically keep all your database files as
compact/small as possible.

no external maintenance needed...

NBDE_Delete commands immediately shrink your database in size (physically
deleted records) after a flushing command (read more about that in a minute).

attention:
there is a known problem in the linux version:

The DestroyCampaignDatabase command doesnt always work in linux. i think
this relates to the different file systems used.

you should be ok using the following rules for your database
names (sCampaignName parameter):

  - max length 16 chars
  - only use alphanumeric chars and underscore
  - NO space


-----------------------------------------------------------------------------
--- Usage
-----------------------------------------------------------------------------

first, include nbde_inc to all scripts using this extension:

#include "nbde_inc"

You basically use it the same way you would use the original
database. just add the NBDE_ prefix infront of the function.

original example:

int n = GetCampaignInt("MYDB", "MYVAR");

nbde conversion:

#include "nbde_inc"

int n = NBDE_GetCampaignInt("MYDB", "MYVAR");

Important differences:

Database Flushing:
------------------

writing to the database will not issue a physical write directly.
You need to "Flush" a database in order to physically write the contents of
a complete database to your HD. this sounds slow, but its not, because of
the large overhead of standard SetCampaign calls...
Writing out a single integer via SetCampaignInt takes roughly
100ms (0.1 seconds), writing out an object with 1000 integers via
SetCampaignObject takes roughly 150ms. that's the whole
magic behind the system. it basically just consolidates your writes..

original example:

SetCampaignInt("MYDB", "MYVAR1", 10);
SetCampaignInt("MYDB", "MYVAR2", 20);
SetCampaignInt("MYDB", "MYVAR3", 30);
SetCampaignInt("MYDB", "MYVAR4", 40);
SetCampaignInt("MYDB", "MYVAR5", 50);

nbde conversion:

NBDE_SetCampaignInt("MYDB", "MYVAR1", 10);
NBDE_SetCampaignInt("MYDB", "MYVAR2", 20);
NBDE_SetCampaignInt("MYDB", "MYVAR3", 30);
NBDE_SetCampaignInt("MYDB", "MYVAR4", 40);
NBDE_SetCampaignInt("MYDB", "MYVAR5", 50);
NBDE_FlushCampaignDatabase("MYDB");

the original example takes roughly half a second (500ms),
the converted example only 100ms.

you can gain a lot of speed and do things impossible with the original
database using the right flushing scheme. you can flush critical data asap
but you can get away flushing not so critical stuff only once every few
minutes, or during onClientLeave, or once an Area is out of players, and so on...

keep in mind: you can loose data if the server crashes before
you flushed your database.

delete function:
----------------

the original version only got one delete function, DeleteCampaignVariable.
That's because of the unique nature of sVarNames...
NBDE eliminates this restriction and therefore exposes one delete
function for each data-type.

original example:

DeleteCampaignVariable("MYDB", "MYVAR");

you need to know the datatype of "MYVAR" in order to correctly convert this
line to NBDE. lets assume it's an integer...

nbde conversion:

NBDE_DeleteCampaignInt("MYDB", "MYVAR");


Unloading a Database:
---------------------

nbde databases are kept in memory. NBDE_UnloadCampaignDatabase() unloads
the database with the name sCampaignName from memory.

useful to unload databases you don't need often. unloading/reloading is quite
fast, so don't hesitate to use this regulary...


*/

// database item name, used as sVarName parameter in Store-/RetrieveCampaignObject
const string NBDE_DATABASE_ITEM_VARNAME = "+++_DATABASE_ITEM_+++";

// database item resref, needed for auto-creation
const string NBDE_DATABASE_ITEM_RESREF = "nbde_database";

// database index prefix
// used to index a database via Get/SetLocalObject
const string NBDE_INDEX_PREFIX = "NBDE_DATABASE_";

// database vault tag
// this vault is usually a container
const string NBDE_VAULT_TAG = "NBDE_VAULT";

// prefixes used to store locations/vectors as strings
// this should eliminate collisions with normal strings
const string NBDE_LOC_PREFIX = "¥Æ¥";
const string NBDE_VEC_PREFIX = "ø£ø";

// This stores an int out to the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Vastly improved writing speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
void NBDE_SetCampaignInt(string sCampaignName, string sVarname, int nInt, object oPlayer = OBJECT_INVALID);

// This stores a float out to the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Vastly improved writing speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
void NBDE_SetCampaignFloat(string sCampaignName, string sVarname, float flFloat, object oPlayer = OBJECT_INVALID);

// This stores a string out to the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Vastly improved writing speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
void NBDE_SetCampaignString(string sCampaignName, string sVarname, string sString, object oPlayer = OBJECT_INVALID);

// This stores a location out to the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Vastly improved writing speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
//
// Original function is not very reliable, because it is using the areas object-id, which is
// a runtime generated ID. Stored locations may turn invalid in case you change the area layout in the toolset.
// (e.g. deleting old areas)
//
// This function is 100% reliable, as long as you use unique TAGs for your areas
void NBDE_SetCampaignLocation(string sCampaignName, string sVarname, location locLocation, object oPlayer = OBJECT_SELF);

// This stores a vector out to the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Vastly improved writing speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
void NBDE_SetCampaignVector(string sCampaignName, string sVarname, vector vVector, object oPlayer = OBJECT_SELF);

// This will read an int from the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Improved reading speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
int NBDE_GetCampaignInt(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will read a float from the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Improved reading speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
float NBDE_GetCampaignFloat(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will read a string from the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Improved reading speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
string NBDE_GetCampaignString(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will read a location from the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Improved reading speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
//
// Original function is not very reliable, because it is using the areas object-id, which is
// a runtime generated ID. Stored locations may turn invalid in case you change the area layout in the toolset.
// (e.g. deleting old areas)
//
// This function is 100% reliable, as long as you use unique TAGs for your areas
location NBDE_GetCampaignLocation(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will read a vector from the specified campaign database
// The database name IS case sensitive and it must be the same for both set and get functions.
// If you want a variable to pertain to a specific player in the game, provide a player object.
//
// Improvements to original bioware function:
// Improved reading speed...
// There is no limit on the length of sVarname (original function is limited to 32 chars)
// sVarname must NOT be unique. you can use the same sVarname with a different data-type
vector NBDE_GetCampaignVector(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// this will remove an integer from the specified campaign database
//
// Improvements to original bioware function:
// This will physically delete the variable from the database, not only flagging it
// Database will shrink in size
// No need to pack your database ever
void NBDE_DeleteCampaignInt(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// this will remove a float from the specified campaign database
//
// Improvements to original bioware function:
// This will physically delete the variable from the database, not only flagging it
// Database will shrink in size
// No need to pack your database ever
void NBDE_DeleteCampaignFloat(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will remove a string from the specified campaign database
//
// Improvements to original bioware function:
// This will physically delete the variable from the database, not only flagging it
// Database will shrink in size
// No need to pack your database ever
void NBDE_DeleteCampaignString(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will remove a location from the specified campaign database
//
// Improvements to original bioware function:
// This will physically delete the variable from the database, not only flagging it
// Database will shrink in size
// No need to pack your database ever
void NBDE_DeleteCampaignLocation(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will remove a vector from the specified campaign database
//
// Improvements to original bioware function:
// This will physically delete the variable from the database, not only flagging it
// Database will shrink in size
// No need to pack your database ever
void NBDE_DeleteCampaignVector(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID);

// This will flush a database to disk with a SINGLE StoreCampaignObject() call
//
// Don't use this function in a rapid manner.
// Delay each call to this function by at least 1 second (using delaycommand)
// in order to eliminate possible spikes...
void NBDE_FlushCampaignDatabase(string sCampaignName);

// NBDE databases are kept in memory. this commands unloads
// the database with the name sCampaignName from memory.
//
// Useful to unload databases you don't need often.
// Unloading/reloading is quite fast, so don't hesitate to use
// this regulary...
//
// Reloading happens automatically, btw...
void NBDE_UnloadCampaignDatabase(string sCampaignName);


// --------------------------- IMPLEMENTATION ----------------------------
/* ----------------------------------------------------------------------- */

// everything not in here gets considered an illegal character
// - mixed up for additional security
const string HASH_INDEX = "#i!j$k%l{&M/n(o)p=q?r^¤Xs`Tu'v]AwBxCyDzE1F2-G3t;4I}5Y:J6_K7+Z[Lm9N\ l0kOjPhQ,gRfSeHdU8cVbWa.";

const int HASH_PRIME = 3021377;

// simple hash
// returns -1 if string contains illegal character
int NBDE_Hash(string sData)
{
  int nLen = GetStringLength(sData);
  int i, nHash, nChar;
  for(i=0;i<nLen;i++)
  {
     nChar = FindSubString(HASH_INDEX, GetSubString(sData,i,1));
     if(nChar == -1) return -1;
     nHash = ((nHash<<5) ^ (nHash>>27)) ^ nChar;
  }
  return nHash % HASH_PRIME;
}

// serialize location to padded string
string NBDE_LocationToString(location lLoc)
{
  // serialization garbage... more or less "redo if it screws" code
  string sLoc = IntToString(FloatToInt(GetPositionFromLocation(lLoc).x*100));
  sLoc = (GetStringLength(sLoc) < 5) ? sLoc + GetStringLeft("     ",5 - GetStringLength(sLoc)) : GetStringLeft(sLoc,5);
  sLoc += IntToString(FloatToInt(GetPositionFromLocation(lLoc).y*100));
  sLoc = (GetStringLength(sLoc) < 10) ? sLoc + GetStringLeft("     ",10 - GetStringLength(sLoc)) : GetStringLeft(sLoc,10);
  sLoc += IntToString(FloatToInt(GetPositionFromLocation(lLoc).z*100));
  sLoc = (GetStringLength(sLoc) < 15) ? sLoc + GetStringLeft("     ",15 - GetStringLength(sLoc)) : GetStringLeft(sLoc,15);
  sLoc += IntToString(FloatToInt(GetFacingFromLocation(lLoc)*100));
  sLoc = (GetStringLength(sLoc) < 20) ? sLoc + GetStringLeft("     ",20 - GetStringLength(sLoc)) : GetStringLeft(sLoc,20);
  sLoc += GetTag(GetAreaFromLocation(lLoc));
  sLoc = (GetStringLength(sLoc) < 52) ? sLoc + GetStringLeft("                                ",52 - GetStringLength(sLoc)) : GetStringLeft(sLoc,52);
  return sLoc;
}

// de-serialize string to location
location NBDE_StringToLocation(string sLoc)
{
  // fast de-serialize code using padded strings
  vector vVec;
  // build vector
  vVec.x = StringToFloat(GetStringLeft(sLoc,5)) / 100;
  vVec.y = StringToFloat(GetSubString(sLoc,5,5)) / 100;
  vVec.z = StringToFloat(GetSubString(sLoc,10,5)) / 100;;
  int nPad = FindSubString(GetSubString(sLoc, 20,32)," ");
  // build & return location
  return Location(GetObjectByTag((nPad != -1) ? GetSubString(sLoc, 20,nPad) : GetSubString(sLoc, 20,32)), vVec, StringToFloat(GetSubString(sLoc,15,5)) / 100);
}

// serialize vector to padded string
string NBDE_VectorToString(vector vVec)
{
  // serialization garbage... more or less "redo if it screws" code
  string sVec = IntToString(FloatToInt(vVec.x*100));
  sVec = (GetStringLength(sVec) < 5) ? sVec + GetStringLeft("     ",5 - GetStringLength(sVec)) : GetStringLeft(sVec,5);
  sVec += IntToString(FloatToInt(vVec.y*100));
  sVec = (GetStringLength(sVec) < 10) ? sVec + GetStringLeft("     ",10 - GetStringLength(sVec)) : GetStringLeft(sVec,10);
  sVec += IntToString(FloatToInt(vVec.z*100));
  sVec = (GetStringLength(sVec) < 15) ? sVec + GetStringLeft("     ",15 - GetStringLength(sVec)) : GetStringLeft(sVec,15);
  return sVec;
}

vector NBDE_StringToVector(string sVec)
{
  // fast de-serialize code using padded strings
  vector vVec;
  vVec.x = StringToFloat(GetStringLeft(sVec,5)) / 100;
  vVec.y = StringToFloat(GetSubString(sVec,5,5)) / 100;
  vVec.z = StringToFloat(GetSubString(sVec,10,5)) / 100;
  return vVec;
}

// returns player key with hopefully safe delimiter
string NBDE_GetPlayerKey(object oPC)
{
  return GetName(oPC)+"¤"+GetPCPlayerName(oPC);
}

// returns database object for the specified campaign database
//
// - auto-creates database object in case it doesn't exist
// - builds index for fast access
//
// you usually don't need to use this function directly...
object NBDE_GetCampaignDatabaseObject(string sCampaignName)
{
  // get database item
  object oDatabase = GetLocalObject(GetObjectByTag(NBDE_VAULT_TAG), NBDE_INDEX_PREFIX + sCampaignName);
  // retrieve/create database if not indexed already
  if(!GetIsObjectValid(oDatabase))
  {
    // get database vault object
    // this container holds all database objects/items
    object oVault = GetObjectByTag(NBDE_VAULT_TAG);
    // check for valid vault
    if(!GetIsObjectValid(oVault))
    {
      WriteTimestampedLogEntry("NBDE> Error: unable to locate '"+NBDE_VAULT_TAG+"' vault container object");
      return OBJECT_INVALID;
    }
    // one time load
    oDatabase = RetrieveCampaignObject(sCampaignName, NBDE_DATABASE_ITEM_VARNAME, GetLocation(oVault), oVault);
    // not found ? create it
    if(!GetIsObjectValid(oDatabase)) oDatabase = CreateItemOnObject(NBDE_DATABASE_ITEM_RESREF, oVault);
    // check for valid database object
    if(!GetIsObjectValid(oDatabase))
    {
      WriteTimestampedLogEntry("NBDE> Error: unable to create '"+sCampaignName+"' database object");
      return OBJECT_INVALID;
    }
    // index item for fast access
    SetLocalObject(oVault, NBDE_INDEX_PREFIX + sCampaignName, oDatabase);
  }
  return oDatabase;
}

// this will flush (aka write to disk) the specified campaign database in one big swoop
//
// don't use this function in a rapid manner.
// delay each subsequent call to this function by at least 1 second (using delaycommand)
// this way you completely eliminate cpu-spikes, no matter how many database
// you flush.
void NBDE_FlushCampaignDatabase(string sCampaignName)
{
  // get database vault, it holds all database items
  object oVault = GetObjectByTag(NBDE_VAULT_TAG);
  if(GetIsObjectValid(oVault))
  {
    // get database item
    object oDatabase = GetLocalObject(oVault, NBDE_INDEX_PREFIX + sCampaignName);
    // store the whole database via one single StoreCampaignObject call
    // all variables on the item get stored with the item
    if(GetIsObjectValid(oDatabase))
    {
      // delete database on each flush to keep it compact and clean
      DestroyCampaignDatabase(sCampaignName);
      // store database
      StoreCampaignObject(sCampaignName, NBDE_DATABASE_ITEM_VARNAME , oDatabase);
    }
    // database not loaded, no need to flush...
  }
  else // vault container missing
    WriteTimestampedLogEntry("NBDE> Error: unable to locate '"+NBDE_VAULT_TAG+"' vault container object");
}

void NBDE_UnloadCampaignDatabase(string sCampaignName)
{
  // get database vault, it holds all database items
  object oVault = GetObjectByTag(NBDE_VAULT_TAG);
  if(GetIsObjectValid(oVault))
  {
    // get database item
    object oDatabase = GetLocalObject(oVault, NBDE_INDEX_PREFIX + sCampaignName);
    if(GetIsObjectValid(oDatabase))
    {
      // delete index
      DeleteLocalObject(oVault, NBDE_INDEX_PREFIX + sCampaignName);
      // delete database object
      DestroyObject(oDatabase);
    }
    // database not loaded, do nothing
  }
  else // vault container missing
    WriteTimestampedLogEntry("NBDE> Error: unable to locate '"+NBDE_VAULT_TAG+"' vault container object");
}

void NBDE_SetCampaignInt(string sCampaignName, string sVarname, int nInt, object oPlayer = OBJECT_INVALID)
{
  SetLocalInt(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname, nInt );
}

void NBDE_SetCampaignFloat(string sCampaignName, string sVarname, float fFloat, object oPlayer = OBJECT_INVALID)
{
  SetLocalFloat(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname, fFloat);
}

void NBDE_SetCampaignString(string sCampaignName, string sVarname, string sString, object oPlayer = OBJECT_INVALID)
{
  SetLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname, sString);
}

void NBDE_SetCampaignLocation(string sCampaignName, string sVarname, location locLocation, object oPlayer = OBJECT_SELF)
{
  SetLocalString( NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   NBDE_LOC_PREFIX + ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname ,
   NBDE_LocationToString(locLocation) );
}

void NBDE_SetCampaignVector(string sCampaignName, string sVarname, vector vVector, object oPlayer = OBJECT_SELF)
{
  SetLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   NBDE_VEC_PREFIX + ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname ,
   NBDE_VectorToString(vVector) );
}

int NBDE_GetCampaignInt(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  return GetLocalInt(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname );
}

float NBDE_GetCampaignFloat(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  return GetLocalFloat(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname );
}

string NBDE_GetCampaignString(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  return GetLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname );
}

location NBDE_GetCampaignLocation(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  return NBDE_StringToLocation( GetLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   NBDE_LOC_PREFIX + ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname) );
}

vector NBDE_GetCampaignVector(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  return NBDE_StringToVector( GetLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName),
   NBDE_VEC_PREFIX + ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname) );
}

void NBDE_DeleteCampaignInt(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  DeleteLocalInt(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname);
}

void NBDE_DeleteCampaignFloat(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  DeleteLocalFloat(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname);
}

void NBDE_DeleteCampaignString(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  DeleteLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname);
}

void NBDE_DeleteCampaignLocation(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  DeleteLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   NBDE_LOC_PREFIX + ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname);
}

void NBDE_DeleteCampaignVector(string sCampaignName, string sVarname, object oPlayer = OBJECT_INVALID)
{
  DeleteLocalString(NBDE_GetCampaignDatabaseObject(sCampaignName) ,
   NBDE_VEC_PREFIX + ((GetIsObjectValid(oPlayer)) ? NBDE_GetPlayerKey(oPlayer) : "") + sVarname);
}


