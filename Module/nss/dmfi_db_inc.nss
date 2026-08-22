//DMFI Persistence wrapper functions
//This include file contains the wrapper functions for the
//persistent settings of the DMFI Wand and Widget package
//Advanced users can adapt this to the database system that
//they want to use for NWN.

//:://////////////////////////////////////////////
//:: Created By: The DMFI Team
//:: Created On:
//:://////////////////////////////////////////////

//Listen Pattern ** variable
//Change this to 0 to make the DMFI W&W more compatible with Jasperre's AI
const int LISTEN_PATTERN = 20600;

const int DMFI_DB_TYPE_BIOWARE      = 1;
const int DMFI_DB_TYPE_RESERVED_3   = 3;
const int DMFI_DB_TYPE_RESERVED_4   = 4;
const int DMFI_DB_TYPE_RESERVED_5   = 5;
const int DMFI_DB_TYPE_RESERVED_6   = 6;
const int DMFI_DB_TYPE_RESERVED_7   = 7;
const int DMFI_DB_TYPE_RESERVED_8   = 8;
const int DMFI_DB_TYPE_RESERVED_9   = 9;
const int DMFI_DB_TYPE_RESERVED_10  = 10;

// DMFI uses the standard Bioware database.
#include "dmfi_db_biow_inc"

