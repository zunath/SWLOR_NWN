//::///////////////////////////////////////////////
//:: DMFI - OnPlayerChat functions processor
//:: dmfi_plychat_exe
//:://////////////////////////////////////////////
/*
  Processor for the OnPlayerChat-triggered DMFI functions.
*/
//:://////////////////////////////////////////////
//:: Created By: The DMFI Team
//:: Created On:
//:://////////////////////////////////////////////
//:: 2007.12.12 Merle
//::    - revisions for NWN patch 1.69
//:: 2008.03.24 tsunami282
//::    - renamed from dmfi_voice_exe, updated to work with event hooking system
//:: 2008.06.23 Prince Demetri & Night Journey
//::    - added languages: Sylvan, Mulhorandi, Rashemi
//:: 2008.07.30 morderon
//::    - better emote processing, allow certain dot commands for PC's

#include "x2_inc_switches"
#include "x0_i0_stringlib"
#include "dmfi_string_inc"
#include "dmfi_plchlishk_i"
#include "dmfi_db_inc"

const int DMFI_LOG_CONVERSATION = TRUE; // turn on or off logging of conversation text

////////////////////////////////////////////////////////////////////////
int AppearType (string sCom)
{
//  2008.03.24 tsunami282 - pull descriptions from 2da first; allow numerics

    // is it numeric? If so just convert and return
    if (TestStringAgainstPattern("*n", sCom)) return StringToInt(sCom);
    if (sCom == "ARANEA")
        return APPEARANCE_TYPE_ARANEA;
    if (sCom == "ALLIP")
        return APPEARANCE_TYPE_ALLIP;
    if (sCom == "ARCH_TARGET")
        return APPEARANCE_TYPE_ARCH_TARGET;
    if (sCom == "ARIBETH")
        return APPEARANCE_TYPE_ARIBETH;
    if (sCom == "ASABI_CHIEFTAIN")
        return APPEARANCE_TYPE_ASABI_CHIEFTAIN;
    if (sCom == "ASABI_SHAMAN")
        return APPEARANCE_TYPE_ASABI_SHAMAN;
    if (sCom == "ASABI_WARRIOR")
        return APPEARANCE_TYPE_ASABI_WARRIOR;
    if (sCom == "BADGER")
        return APPEARANCE_TYPE_BADGER;
    if (sCom == "BADGER_DIRE")
        return APPEARANCE_TYPE_BADGER_DIRE;
    if (sCom == "BALOR")
        return APPEARANCE_TYPE_BALOR;
    if (sCom == "BARTENDER")
        return APPEARANCE_TYPE_BARTENDER;
    if (sCom == "BASILISK")
        return APPEARANCE_TYPE_BASILISK;
    if (sCom == "BAT")
        return APPEARANCE_TYPE_BAT;
    if (sCom == "BAT_HORROR")
        return APPEARANCE_TYPE_BAT_HORROR;
    if (sCom == "BEAR_BLACK")
        return APPEARANCE_TYPE_BEAR_BLACK;
    if (sCom == "BEAR_BROWN")
        return APPEARANCE_TYPE_BEAR_BROWN;
    if (sCom == "BEAR_DIRE")
        return APPEARANCE_TYPE_BEAR_DIRE;
    if (sCom == "BEAR_KODIAK")
        return APPEARANCE_TYPE_BEAR_KODIAK;
    if (sCom == "BEAR_POLAR")
        return APPEARANCE_TYPE_BEAR_POLAR;
    if (sCom == "BEETLE_FIRE")
        return APPEARANCE_TYPE_BEETLE_FIRE;
    if (sCom == "BEETLE_SLICER")
        return APPEARANCE_TYPE_BEETLE_SLICER;
    if (sCom == "BEETLE_STAG")
        return APPEARANCE_TYPE_BEETLE_STAG;
    if (sCom == "BEETLE_STINK")
        return APPEARANCE_TYPE_BEETLE_STINK;
    if (sCom == "BEGGER")
        return APPEARANCE_TYPE_BEGGER;
    if (sCom == "BLOOD_SAILER")
        return APPEARANCE_TYPE_BLOOD_SAILER;
    if (sCom == "BOAR")
        return APPEARANCE_TYPE_BOAR;
    if (sCom == "BOAR_DIRE")
        return APPEARANCE_TYPE_BOAR_DIRE;
    if (sCom == "BODAK")
        return APPEARANCE_TYPE_BODAK;
    if (sCom == "BUGBEAR_A")
        return APPEARANCE_TYPE_BUGBEAR_A;
    if (sCom == "BUGBEAR_B")
        return APPEARANCE_TYPE_BUGBEAR_B;
    if (sCom == "BUGBEAR_CHIEFTAIN_A")
        return APPEARANCE_TYPE_BUGBEAR_CHIEFTAIN_A;
    if (sCom == "BUGBEAR_CHIEFTAIN_B")
        return APPEARANCE_TYPE_BUGBEAR_CHIEFTAIN_B;
    if (sCom == "BUGBEAR_SHAMAN_A")
        return APPEARANCE_TYPE_BUGBEAR_SHAMAN_A;
    if (sCom == "BUGBEAR_SHAMAN_B")
        return APPEARANCE_TYPE_BUGBEAR_SHAMAN_B;
    if (sCom == "CAT_CAT_DIRE")
        return APPEARANCE_TYPE_CAT_CAT_DIRE;
    if (sCom == "CAT_COUGAR")
        return APPEARANCE_TYPE_CAT_COUGAR;
    if (sCom == "CAT_CRAG_CAT")
        return APPEARANCE_TYPE_CAT_CRAG_CAT;
    if (sCom == "CAT_JAGUAR")
        return APPEARANCE_TYPE_CAT_JAGUAR;
    if (sCom == "CAT_KRENSHAR")
        return APPEARANCE_TYPE_CAT_KRENSHAR;
    if (sCom == "CAT_LEOPARD")
        return APPEARANCE_TYPE_CAT_LEOPARD;
    if (sCom == "CAT_LION")
        return APPEARANCE_TYPE_CAT_LION;
    if (sCom == "CAT_MPANTHER")
        return APPEARANCE_TYPE_CAT_MPANTHER;
    if (sCom == "CAT_PANTHER")
        return APPEARANCE_TYPE_CAT_PANTHER;
    if (sCom == "CHICKEN")
        return APPEARANCE_TYPE_CHICKEN;
    if (sCom == "COCKATRICE")
        return APPEARANCE_TYPE_COCKATRICE;
    if (sCom == "COMBAT_DUMMY")
        return APPEARANCE_TYPE_COMBAT_DUMMY;
    if (sCom == "CONVICT")
        return APPEARANCE_TYPE_CONVICT;
    if (sCom == "COW")
        return APPEARANCE_TYPE_COW;
    if (sCom == "CULT_MEMBER")
        return APPEARANCE_TYPE_CULT_MEMBER;
    if (sCom == "DEER")
        return APPEARANCE_TYPE_DEER;
    if (sCom == "DEER_STAG")
        return APPEARANCE_TYPE_DEER_STAG;
    if (sCom == "DEVIL")
        return APPEARANCE_TYPE_DEVIL;
    if (sCom == "DOG")
        return APPEARANCE_TYPE_DOG;
    if (sCom == "DOG_BLINKDOG")
        return APPEARANCE_TYPE_DOG_BLINKDOG;
    if (sCom == "DOG_DIRE_WOLF")
        return APPEARANCE_TYPE_DOG_DIRE_WOLF;
    if (sCom == "DOG_FENHOUND")
        return APPEARANCE_TYPE_DOG_FENHOUND;
    if (sCom == "DOG_HELL_HOUND")
        return APPEARANCE_TYPE_DOG_HELL_HOUND;
    if (sCom == "DOG_SHADOW_MASTIF")
        return APPEARANCE_TYPE_DOG_SHADOW_MASTIF;
    if (sCom == "DOG_WINTER_WOLF")
        return APPEARANCE_TYPE_DOG_WINTER_WOLF;
    if (sCom == "DOG_WORG")
        return APPEARANCE_TYPE_DOG_WORG;
    if (sCom == "DOG_WOLF")
        return APPEARANCE_TYPE_DOG_WOLF;
    if (sCom == "DOOM_KNIGHT")
        return APPEARANCE_TYPE_DOOM_KNIGHT;
    if (sCom == "DRAGON_BLACK")
        return APPEARANCE_TYPE_DRAGON_BLACK;
    if (sCom == "DRAGON_BLUE")
        return APPEARANCE_TYPE_DRAGON_BLUE;
    if (sCom == "DRAGON_BRASS")
        return APPEARANCE_TYPE_DRAGON_BRASS;
    if (sCom == "DRAGON_BRONZE")
        return APPEARANCE_TYPE_DRAGON_BRONZE;
    if (sCom == "DRAGON_COPPER")
        return APPEARANCE_TYPE_DRAGON_COPPER;
    if (sCom == "DRAGON_GOLD")
        return APPEARANCE_TYPE_DRAGON_GOLD;
    if (sCom == "DRAGON_GREEN")
        return APPEARANCE_TYPE_DRAGON_GREEN;
    if (sCom == "DRAGON_RED")
        return APPEARANCE_TYPE_DRAGON_RED;
    if (sCom == "DRAGON_SILVER")
        return APPEARANCE_TYPE_DRAGON_SILVER;
    if (sCom == "DRAGON_WHITE")
        return APPEARANCE_TYPE_DRAGON_WHITE;
    if (sCom == "DROW_CLERIC")
        return APPEARANCE_TYPE_DROW_CLERIC;
    if (sCom == "DROW_FIGHTER")
        return APPEARANCE_TYPE_DROW_FIGHTER;
    if (sCom == "DRUEGAR_CLERIC")
        return APPEARANCE_TYPE_DRUEGAR_CLERIC;
    if (sCom == "DRUEGAR_FIGHTER")
        return APPEARANCE_TYPE_DRUEGAR_FIGHTER;
    if (sCom == "DRYAD")
        return APPEARANCE_TYPE_DRYAD;
    if (sCom == "DWARF")
        return APPEARANCE_TYPE_DWARF;
    if (sCom == "DWARF_NPC_FEMALE")
        return APPEARANCE_TYPE_DWARF_NPC_FEMALE;
    if (sCom == "DWARF_NPC_MALE")
        return APPEARANCE_TYPE_DWARF_NPC_MALE;
    if (sCom == "ELEMENTAL_AIR")
        return APPEARANCE_TYPE_ELEMENTAL_AIR;
    if (sCom == "ELEMENTAL_AIR_ELDER")
        return APPEARANCE_TYPE_ELEMENTAL_AIR_ELDER;
    if (sCom == "ELEMENTAL_EARTH")
        return APPEARANCE_TYPE_ELEMENTAL_EARTH;
    if (sCom == "ELEMENTAL_EARTH_ELDER")
        return APPEARANCE_TYPE_ELEMENTAL_EARTH_ELDER;
    if (sCom == "ELEMENTAL_FIRE")
        return APPEARANCE_TYPE_ELEMENTAL_FIRE;
    if (sCom == "ELEMENTAL_FIRE_ELDER")
        return APPEARANCE_TYPE_ELEMENTAL_FIRE_ELDER;
    if (sCom == "ELEMENTAL_WATER")
        return APPEARANCE_TYPE_ELEMENTAL_WATER;
    if (sCom == "ELEMENTAL_WATER_ELDER")
        return APPEARANCE_TYPE_ELEMENTAL_WATER_ELDER;
    if (sCom == "ELF")
        return APPEARANCE_TYPE_ELF;
    if (sCom == "ELF_NPC_FEMALE")
        return APPEARANCE_TYPE_ELF_NPC_FEMALE;
    if (sCom == "ELF_NPC_MALE_01")
        return APPEARANCE_TYPE_ELF_NPC_MALE_01;
    if (sCom == "ELF_NPC_MALE_02")
        return APPEARANCE_TYPE_ELF_NPC_MALE_02;
    if (sCom == "ETTERCAP")
        return APPEARANCE_TYPE_ETTERCAP;
    if (sCom == "ETTIN")
        return APPEARANCE_TYPE_ETTIN;
    if (sCom == "FAERIE_DRAGON")
        return APPEARANCE_TYPE_FAERIE_DRAGON;
    if (sCom == "FAIRY")
        return APPEARANCE_TYPE_FAIRY;
    if (sCom == "FALCON")
        return APPEARANCE_TYPE_FALCON;
    if (sCom == "FEMALE_01")
        return APPEARANCE_TYPE_FEMALE_01;
    if (sCom == "FEMALE_02")
        return APPEARANCE_TYPE_FEMALE_02;
    if (sCom == "FEMALE_03")
        return APPEARANCE_TYPE_FEMALE_03;
    if (sCom == "FEMALE_04")
        return APPEARANCE_TYPE_FEMALE_04;
    if (sCom == "FORMIAN_MYRMARCH")
        return APPEARANCE_TYPE_FORMIAN_MYRMARCH;
    if (sCom == "FORMIAN_QUEEN")
        return APPEARANCE_TYPE_FORMIAN_QUEEN;
    if (sCom == "FORMIAN_WARRIOR")
        return APPEARANCE_TYPE_FORMIAN_WARRIOR;
    if (sCom == "FORMIAN_WORKER")
        return APPEARANCE_TYPE_FORMIAN_WORKER;
    if (sCom == "GARGOYLE")
        return APPEARANCE_TYPE_GARGOYLE;
    if (sCom == "GHAST")
        return APPEARANCE_TYPE_GHAST;
    if (sCom == "GHOUL")
        return APPEARANCE_TYPE_GHOUL;
    if (sCom == "GHOUL_LORD")
        return APPEARANCE_TYPE_GHOUL_LORD;
    if (sCom == "GIANT_FIRE")
        return APPEARANCE_TYPE_GIANT_FIRE;
    if (sCom == "GIANT_FIRE_FEMALE")
        return APPEARANCE_TYPE_GIANT_FIRE_FEMALE;
    if (sCom == "GIANT_FROST")
        return APPEARANCE_TYPE_GIANT_FROST;
    if (sCom == "GIANT_FROST_FEMALE")
        return APPEARANCE_TYPE_GIANT_FROST_FEMALE;
    if (sCom == "GIANT_HILL")
        return APPEARANCE_TYPE_GIANT_HILL;
    if (sCom == "GIANT_MOUNTAIN")
        return APPEARANCE_TYPE_GIANT_MOUNTAIN;
    if (sCom == "GNOLL_WARRIOR")
        return APPEARANCE_TYPE_GNOLL_WARRIOR;
    if (sCom == "GNOLL_WIZ")
        return APPEARANCE_TYPE_GNOLL_WIZ;
    if (sCom == "GNOME")
        return APPEARANCE_TYPE_GNOME;
    if (sCom == "GNOME_NPC_FEMALE")
        return APPEARANCE_TYPE_GNOME_NPC_FEMALE;
    if (sCom == "GNOME_NPC_MALE")
        return APPEARANCE_TYPE_GNOME_NPC_MALE;
    if (sCom == "GOBLIN_A")
        return APPEARANCE_TYPE_GOBLIN_A;
    if (sCom == "GOBLIN_B")
        return APPEARANCE_TYPE_GOBLIN_B;
    if (sCom == "GOBLIN_CHIEF_A")
        return APPEARANCE_TYPE_GOBLIN_CHIEF_A;
    if (sCom == "GOBLIN_CHIEF_B")
        return APPEARANCE_TYPE_GOBLIN_CHIEF_B;
    if (sCom == "GOBLIN_SHAMAN_A")
        return APPEARANCE_TYPE_GOBLIN_SHAMAN_A;
    if (sCom == "GOBLIN_SHAMAN_B")
        return APPEARANCE_TYPE_GOBLIN_SHAMAN_B;
    if (sCom == "GOLEM_BONE")
        return APPEARANCE_TYPE_GOLEM_BONE;
    if (sCom == "GOLEM_CLAY")
        return APPEARANCE_TYPE_GOLEM_CLAY;
    if (sCom == "GOLEM_FLESH")
        return APPEARANCE_TYPE_GOLEM_FLESH;
    if (sCom == "GOLEM_IRON")
        return APPEARANCE_TYPE_GOLEM_IRON;
    if (sCom == "GOLEM_STONE")
        return APPEARANCE_TYPE_GOLEM_STONE;
    if (sCom == "GORGON")
        return APPEARANCE_TYPE_GORGON;
    if (sCom == "GREY_RENDER")
        return APPEARANCE_TYPE_GREY_RENDER;
    if (sCom == "GYNOSPHINX")
        return APPEARANCE_TYPE_GYNOSPHINX;
    if (sCom == "HALF_ELF")
        return APPEARANCE_TYPE_HALF_ELF;
    if (sCom == "HALF_ORC")
        return APPEARANCE_TYPE_HALF_ORC;
    if (sCom == "HALF_ORC_NPC_FEMALE")
        return APPEARANCE_TYPE_HALF_ORC_NPC_FEMALE;
    if (sCom == "HALF_ORC_NPC_MALE_01")
        return APPEARANCE_TYPE_HALF_ORC_NPC_MALE_01;
    if (sCom == "HALF_ORC_NPC_MALE_02")
        return APPEARANCE_TYPE_HALF_ORC_NPC_MALE_02;
    if (sCom == "HALFLING")
        return APPEARANCE_TYPE_HALFLING;
    if (sCom == "HALFLING_NPC_FEMALE")
        return APPEARANCE_TYPE_HALFLING_NPC_FEMALE;
    if (sCom == "HALFLING_NPC_MALE")
        return APPEARANCE_TYPE_HALFLING_NPC_MALE;
    if (sCom == "HELMED_HORROR")
        return APPEARANCE_TYPE_HELMED_HORROR;
    if (sCom == "HEURODIS_LICH")
        return APPEARANCE_TYPE_HEURODIS_LICH;
    if (sCom == "HOBGOBLIN_WARRIOR")
        return APPEARANCE_TYPE_HOBGOBLIN_WARRIOR;
    if (sCom == "HOOK_HORROR")
        return APPEARANCE_TYPE_HOOK_HORROR;
    if (sCom == "HOBGOBLIN_WIZARD")
        return APPEARANCE_TYPE_HOBGOBLIN_WIZARD;
    if (sCom == "HOUSE_GUARD")
        return APPEARANCE_TYPE_HOUSE_GUARD;
    if (sCom == "HUMAN")
        return APPEARANCE_TYPE_HUMAN;
    if (sCom == "HUMAN_NPC_FEMALE_01")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_01;
    if (sCom == "HUMAN_NPC_FEMALE_02")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_02;
    if (sCom == "HUMAN_NPC_FEMALE_03")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_03;
    if (sCom == "HUMAN_NPC_FEMALE_04")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_04;
    if (sCom == "HUMAN_NPC_FEMALE_05")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_05;
    if (sCom == "HUMAN_NPC_FEMALE_06")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_06;
    if (sCom == "HUMAN_NPC_FEMALE_07")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_07;
    if (sCom == "HUMAN_NPC_FEMALE_08")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_08;
    if (sCom == "HUMAN_NPC_FEMALE_09")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_09;
    if (sCom == "HUMAN_NPC_FEMALE_10")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_10;
    if (sCom == "HUMAN_NPC_FEMALE_11")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_11;
    if (sCom == "HUMAN_NPC_FEMALE_12")
        return APPEARANCE_TYPE_HUMAN_NPC_FEMALE_12;
    if (sCom == "HUMAN_NPC_MALE_01")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_01;
    if (sCom == "HUMAN_NPC_MALE_02")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_02;
    if (sCom == "HUMAN_NPC_MALE_03")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_03;
    if (sCom == "HUMAN_NPC_MALE_04")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_04;
    if (sCom == "HUMAN_NPC_MALE_05")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_05;
    if (sCom == "HUMAN_NPC_MALE_06")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_06;
    if (sCom == "HUMAN_NPC_MALE_07")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_07;
    if (sCom == "HUMAN_NPC_MALE_08")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_08;
    if (sCom == "HUMAN_NPC_MALE_09")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_09;
    if (sCom == "HUMAN_NPC_MALE_10")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_10;
    if (sCom == "HUMAN_NPC_MALE_11")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_11;
    if (sCom == "HUMAN_NPC_MALE_12")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_12;
    if (sCom == "HUMAN_NPC_MALE_13")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_13;
    if (sCom == "HUMAN_NPC_MALE_14")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_14;
    if (sCom == "HUMAN_NPC_MALE_15")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_15;
    if (sCom == "HUMAN_NPC_MALE_16")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_16;
    if (sCom == "HUMAN_NPC_MALE_17")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_17;
    if (sCom == "HUMAN_NPC_MALE_18")
        return APPEARANCE_TYPE_HUMAN_NPC_MALE_18;
    if (sCom == "IMP")
        return APPEARANCE_TYPE_IMP;
    if (sCom == "INN_KEEPER")
        return APPEARANCE_TYPE_INN_KEEPER;
    if (sCom == "INTELLECT_DEVOURER")
        return APPEARANCE_TYPE_INTELLECT_DEVOURER;
    if (sCom == "INVISIBLE_HUMAN_MALE")
        return APPEARANCE_TYPE_INVISIBLE_HUMAN_MALE;
    if (sCom == "INVISIBLE_STALKER")
        return APPEARANCE_TYPE_INVISIBLE_STALKER;
    if (sCom == "KID_FEMALE")
        return APPEARANCE_TYPE_KID_FEMALE;
    if (sCom == "KID_MALE")
        return APPEARANCE_TYPE_KID_MALE;
    if (sCom == "KOBOLD_A")
        return APPEARANCE_TYPE_KOBOLD_A;
    if (sCom == "KOBOLD_B")
        return APPEARANCE_TYPE_KOBOLD_B;
    if (sCom == "KOBOLD_CHIEF_A")
        return APPEARANCE_TYPE_KOBOLD_CHIEF_A;
    if (sCom == "KOBOLD_CHIEF_B")
        return APPEARANCE_TYPE_KOBOLD_CHIEF_B;
    if (sCom == "KOBOLD_SHAMAN_A")
        return APPEARANCE_TYPE_KOBOLD_SHAMAN_A;
    if (sCom == "KOBOLD_SHAMAN_B")
        return APPEARANCE_TYPE_KOBOLD_SHAMAN_B;
    if (sCom == "LANTERN_ARCHON")
        return APPEARANCE_TYPE_LANTERN_ARCHON;
    if (sCom == "LICH")
        return APPEARANCE_TYPE_LICH;
    if (sCom == "LIZARDFOLK_A")
        return APPEARANCE_TYPE_LIZARDFOLK_A;
    if (sCom == "LIZARDFOLK_B")
        return APPEARANCE_TYPE_LIZARDFOLK_B;
    if (sCom == "LIZARDFOLK_SHAMAN_A")
        return APPEARANCE_TYPE_LIZARDFOLK_SHAMAN_A;
    if (sCom == "LIZARDFOLK_SHAMAN_B")
        return APPEARANCE_TYPE_LIZARDFOLK_SHAMAN_B;
    if (sCom == "LIZARDFOLK_WARRIOR_A")
        return APPEARANCE_TYPE_LIZARDFOLK_WARRIOR_A;
    if (sCom == "LIZARDFOLK_WARRIOR_B")
        return APPEARANCE_TYPE_LIZARDFOLK_WARRIOR_B;
    if (sCom == "LUSKAN_GUARD")
        return APPEARANCE_TYPE_LUSKAN_GUARD;
    if (sCom == "MALE_01")
        return APPEARANCE_TYPE_MALE_01;
    if (sCom == "MALE_02")
        return APPEARANCE_TYPE_MALE_02;
    if (sCom == "MALE_03")
        return APPEARANCE_TYPE_MALE_03;
    if (sCom == "MALE_04")
        return APPEARANCE_TYPE_MALE_04;
    if (sCom == "MALE_05")
        return APPEARANCE_TYPE_MALE_05;
    if (sCom == "MANTICORE")
        return APPEARANCE_TYPE_MANTICORE;
    if (sCom == "MEDUSA")
        return APPEARANCE_TYPE_MEDUSA;
    if (sCom == "MEPHIT_AIR")
        return APPEARANCE_TYPE_MEPHIT_AIR;
    if (sCom == "MEPHIT_DUST")
        return APPEARANCE_TYPE_MEPHIT_DUST;
    if (sCom == "MEPHIT_EARTH")
        return APPEARANCE_TYPE_MEPHIT_EARTH;
    if (sCom == "MEPHIT_FIRE")
        return APPEARANCE_TYPE_MEPHIT_FIRE;
    if (sCom == "MEPHIT_ICE")
        return APPEARANCE_TYPE_MEPHIT_ICE;
    if (sCom == "MEPHIT_MAGMA")
        return APPEARANCE_TYPE_MEPHIT_MAGMA;
    if (sCom == "MEPHIT_OOZE")
        return APPEARANCE_TYPE_MEPHIT_OOZE;
    if (sCom == "MEPHIT_SALT")
        return APPEARANCE_TYPE_MEPHIT_SALT;
    if (sCom == "MEPHIT_STEAM")
        return APPEARANCE_TYPE_MEPHIT_STEAM;
    if (sCom == "MEPHIT_WATER")
        return APPEARANCE_TYPE_MEPHIT_WATER;
    if (sCom == "MINOGON")
        return APPEARANCE_TYPE_MINOGON;
    if (sCom == "MINOTAUR")
        return APPEARANCE_TYPE_MINOTAUR;
    if (sCom == "MINOTAUR_CHIEFTAIN")
        return APPEARANCE_TYPE_MINOTAUR_CHIEFTAIN;
    if (sCom == "MINOTAUR_SHAMAN")
        return APPEARANCE_TYPE_MINOTAUR_SHAMAN;
    if (sCom == "MOHRG")
        return APPEARANCE_TYPE_MOHRG;
    if (sCom == "MUMMY_COMMON")
        return APPEARANCE_TYPE_MUMMY_COMMON;
    if (sCom == "MUMMY_FIGHTER_2")
        return APPEARANCE_TYPE_MUMMY_FIGHTER_2;
    if (sCom == "MUMMY_GREATER")
        return APPEARANCE_TYPE_MUMMY_GREATER;
    if (sCom == "MUMMY_WARRIOR")
        return APPEARANCE_TYPE_MUMMY_WARRIOR;
    if (sCom == "NW_MILITIA_MEMBER")
        return APPEARANCE_TYPE_NW_MILITIA_MEMBER;
    if (sCom == "NWN_AARIN")
        return APPEARANCE_TYPE_NWN_AARIN;
    if (sCom == "NWN_ARIBETH_EVIL")
        return APPEARANCE_TYPE_NWN_ARIBETH_EVIL;
    if (sCom == "NWN_HAEDRALINE")
        return APPEARANCE_TYPE_NWN_HAEDRALINE;
    if (sCom == "NWN_MAUGRIM")
        return APPEARANCE_TYPE_NWN_MAUGRIM;
    if (sCom == "NWN_MORAG")
        return APPEARANCE_TYPE_NWN_MORAG;
    if (sCom == "NWN_NASHER")
        return APPEARANCE_TYPE_NWN_NASHER;
    if (sCom == "NWN_SEDOS")
        return APPEARANCE_TYPE_NWN_SEDOS;
    if (sCom == "NYMPH")
        return APPEARANCE_TYPE_NYMPH;
    if (sCom == "OGRE")
        return APPEARANCE_TYPE_OGRE;
    if (sCom == "OGRE_CHIEFTAIN")
        return APPEARANCE_TYPE_OGRE_CHIEFTAIN;
    if (sCom == "OGRE_CHIEFTAINB")
        return APPEARANCE_TYPE_OGRE_CHIEFTAINB;
    if (sCom == "OGRE_MAGE")
        return APPEARANCE_TYPE_OGRE_MAGE;
    if (sCom == "OGRE_MAGEB")
        return APPEARANCE_TYPE_OGRE_MAGEB;
    if (sCom == "OGREB")
        return APPEARANCE_TYPE_OGREB;
    if (sCom == "OLD_MAN")
        return APPEARANCE_TYPE_OLD_MAN;
    if (sCom == "OLD_WOMAN")
        return APPEARANCE_TYPE_OLD_WOMAN;
    if (sCom == "ORC_A")
        return APPEARANCE_TYPE_ORC_A;
    if (sCom == "ORC_B")
        return APPEARANCE_TYPE_ORC_B;
    if (sCom == "ORC_CHIEFTAIN_A")
        return APPEARANCE_TYPE_ORC_CHIEFTAIN_A;
    if (sCom == "ORC_CHIEFTAIN_B")
        return APPEARANCE_TYPE_ORC_CHIEFTAIN_B;
    if (sCom == "ORC_SHAMAN_A")
        return APPEARANCE_TYPE_ORC_SHAMAN_A;
    if (sCom == "ORC_SHAMAN_B")
        return APPEARANCE_TYPE_ORC_SHAMAN_B;
    if (sCom == "OX")
        return APPEARANCE_TYPE_OX;
    if (sCom == "PENGUIN")
        return APPEARANCE_TYPE_PENGUIN;
    if (sCom == "PLAGUE_VICTIM")
        return APPEARANCE_TYPE_PLAGUE_VICTIM;
    if (sCom == "PROSTITUTE_01")
        return APPEARANCE_TYPE_PROSTITUTE_01;
    if (sCom == "PROSTITUTE_02")
        return APPEARANCE_TYPE_PROSTITUTE_02;
    if (sCom == "PSEUDODRAGON")
        return APPEARANCE_TYPE_PSEUDODRAGON;
    if (sCom == "QUASIT")
        return APPEARANCE_TYPE_QUASIT;
    if (sCom == "RAKSHASA_BEAR_MALE")
        return APPEARANCE_TYPE_RAKSHASA_BEAR_MALE;
    if (sCom == "RAKSHASA_TIGER_FEMALE")
        return APPEARANCE_TYPE_RAKSHASA_TIGER_FEMALE;
    if (sCom == "RAKSHASA_TIGER_MALE")
        return APPEARANCE_TYPE_RAKSHASA_TIGER_MALE;
    if (sCom == "RAKSHASA_WOLF_MALE")
        return APPEARANCE_TYPE_RAKSHASA_WOLF_MALE;
    if (sCom == "RAT")
        return APPEARANCE_TYPE_RAT;
    if (sCom == "RAT_DIRE")
        return APPEARANCE_TYPE_RAT_DIRE;
    if (sCom == "RAVEN")
        return APPEARANCE_TYPE_RAVEN;
    if (sCom == "SHADOW")
        return APPEARANCE_TYPE_SHADOW;
    if (sCom == "SHADOW_FIEND")
        return APPEARANCE_TYPE_SHADOW_FIEND;
    if (sCom == "SHIELD_GUARDIAN")
        return APPEARANCE_TYPE_SHIELD_GUARDIAN;
    if (sCom == "SHOP_KEEPER")
        return APPEARANCE_TYPE_SHOP_KEEPER;
    if (sCom == "SKELETAL_DEVOURER")
        return APPEARANCE_TYPE_SKELETAL_DEVOURER;
    if (sCom == "SKELETON_CHIEFTAIN")
        return APPEARANCE_TYPE_SKELETON_CHIEFTAIN;
    if (sCom == "SKELETON_COMMON")
        return APPEARANCE_TYPE_SKELETON_COMMON;
    if (sCom == "SKELETON_MAGE")
        return APPEARANCE_TYPE_SKELETON_MAGE;
    if (sCom == "SKELETON_WARRIOR")
        return APPEARANCE_TYPE_SKELETON_WARRIOR;
    if (sCom == "SKELETON_PRIEST")
        return APPEARANCE_TYPE_SKELETON_PRIEST;
    if (sCom == "SKELETON_WARRIOR_1")
        return APPEARANCE_TYPE_SKELETON_WARRIOR_1;
    if (sCom == "SKELETON_WARRIOR_2")
        return APPEARANCE_TYPE_SKELETON_WARRIOR_2;
    if (sCom == "SPHINX")
        return APPEARANCE_TYPE_SPHINX;
    if (sCom == "SPIDER_WRAITH")
        return APPEARANCE_TYPE_SPIDER_WRAITH;
    if (sCom == "STINGER")
        return APPEARANCE_TYPE_STINGER;
    if (sCom == "STINGER_CHIEFTAIN")
        return APPEARANCE_TYPE_STINGER_CHIEFTAIN;
    if (sCom == "STINGER_MAGE")
        return APPEARANCE_TYPE_STINGER_MAGE;
    if (sCom == "STINGER_WARRIOR")
        return APPEARANCE_TYPE_STINGER_WARRIOR;
    if (sCom == "SUCCUBUS")
        return APPEARANCE_TYPE_SUCCUBUS;
    if (sCom == "TROLL")
        return APPEARANCE_TYPE_TROLL;
    if (sCom == "TROLL_CHIEFTAIN")
        return APPEARANCE_TYPE_TROLL_CHIEFTAIN;
    if (sCom == "TROLL_SHAMAN")
        return APPEARANCE_TYPE_TROLL_SHAMAN;
    if (sCom == "UMBERHULK")
        return APPEARANCE_TYPE_UMBERHULK;
    if (sCom == "UTHGARD_ELK_TRIBE")
        return APPEARANCE_TYPE_UTHGARD_ELK_TRIBE;
    if (sCom == "UTHGARD_TIGER_TRIBE")
        return APPEARANCE_TYPE_UTHGARD_TIGER_TRIBE;
    if (sCom == "VAMPIRE_FEMALE")
        return APPEARANCE_TYPE_VAMPIRE_FEMALE;
    if (sCom == "VAMPIRE_MALE")
        return APPEARANCE_TYPE_VAMPIRE_MALE;
    if (sCom == "VROCK")
        return APPEARANCE_TYPE_VROCK;
    if (sCom == "WAITRESS")
        return APPEARANCE_TYPE_WAITRESS;
    if (sCom == "WAR_DEVOURER")
        return APPEARANCE_TYPE_WAR_DEVOURER;
    if (sCom == "WERECAT")
        return APPEARANCE_TYPE_WERECAT;
    if (sCom == "WERERAT")
        return APPEARANCE_TYPE_WERERAT;
    if (sCom == "WEREWOLF")
        return APPEARANCE_TYPE_WEREWOLF;
    if (sCom == "WIGHT")
        return APPEARANCE_TYPE_WIGHT;
    if (sCom == "WILL_O_WISP")
        return APPEARANCE_TYPE_WILL_O_WISP;
    if (sCom == "WRAITH")
        return APPEARANCE_TYPE_WRAITH;
    if (sCom == "WYRMLING_BLACK")
        return APPEARANCE_TYPE_WYRMLING_BLACK;
    if (sCom == "WYRMLING_BLUE")
        return APPEARANCE_TYPE_WYRMLING_BLUE;
    if (sCom == "WYRMLING_BRASS")
        return APPEARANCE_TYPE_WYRMLING_BRASS;
    if (sCom == "WYRMLING_BRONZE")
        return APPEARANCE_TYPE_WYRMLING_BRONZE;
    if (sCom == "WYRMLING_COPPER")
        return APPEARANCE_TYPE_WYRMLING_COPPER;
    if (sCom == "WYRMLING_GOLD")
        return APPEARANCE_TYPE_WYRMLING_GOLD;
    if (sCom == "WYRMLING_GREEN")
        return APPEARANCE_TYPE_WYRMLING_GREEN;
    if (sCom == "WYRMLING_RED")
        return APPEARANCE_TYPE_WYRMLING_RED;
    if (sCom == "WYRMLING_SILVER")
        return APPEARANCE_TYPE_WYRMLING_SILVER;
    if (sCom == "WYRMLING_WHITE")
        return APPEARANCE_TYPE_WYRMLING_WHITE;
    if (sCom == "YUAN_TI")
        return APPEARANCE_TYPE_YUAN_TI;
    if (sCom == "YUAN_TI_CHIEFTEN")
        return APPEARANCE_TYPE_YUAN_TI_CHIEFTEN;
    if (sCom == "YUAN_TI_WIZARD")
        return APPEARANCE_TYPE_YUAN_TI_WIZARD;
    if (sCom == "ZOMBIE")
        return APPEARANCE_TYPE_ZOMBIE;
    if (sCom == "ZOMBIE_ROTTING")
        return APPEARANCE_TYPE_ZOMBIE_ROTTING;
    if (sCom == "ZOMBIE_TYRANT_FOG")
        return APPEARANCE_TYPE_ZOMBIE_TYRANT_FOG;
    if (sCom == "ZOMBIE_WARRIOR_1")
        return APPEARANCE_TYPE_ZOMBIE_WARRIOR_1;
    if (sCom == "ZOMBIE_WARRIOR_2")
        return APPEARANCE_TYPE_ZOMBIE_WARRIOR_2;

    // new 1.09 behavior - also check against the 2da
    string s2daval;
    int i = 0;
    while (1)
    {
        s2daval = Get2DAString("appearance", "NAME", i);
        if (s2daval == "") break; // end of file
        s2daval = Get2DAString("appearance", "LABEL", i);
        if (s2daval != "")
        {
            if (GetStringUpperCase(sCom) == GetStringUpperCase(s2daval)) return i;
        }
        i++;
    }
    return -1;
}

////////////////////////////////////////////////////////////////////////
void dmw_CleanUp(object oMySpeaker)
{
    int nCount;
    int nCache;
    //DeleteLocalObject(oMySpeaker, "dmfi_univ_target");
    DeleteLocalLocation(oMySpeaker, "dmfi_univ_location");
    DeleteLocalObject(oMySpeaker, "dmw_item");
    DeleteLocalString(oMySpeaker, "dmw_repamt");
    DeleteLocalString(oMySpeaker, "dmw_repargs");
    nCache = GetLocalInt(oMySpeaker, "dmw_playercache");
    for (nCount = 1; nCount <= nCache; nCount++)
    {
        DeleteLocalObject(oMySpeaker, "dmw_playercache" + IntToString(nCount));
    }
    DeleteLocalInt(oMySpeaker, "dmw_playercache");
    nCache = GetLocalInt(oMySpeaker, "dmw_itemcache");
    for (nCount = 1; nCount <= nCache; nCount++)
    {
        DeleteLocalObject(oMySpeaker, "dmw_itemcache" + IntToString(nCount));
    }
    DeleteLocalInt(oMySpeaker, "dmw_itemcache");
    for (nCount = 1; nCount <= 10; nCount++)
    {
        DeleteLocalString(oMySpeaker, "dmw_dialog" + IntToString(nCount));
        DeleteLocalString(oMySpeaker, "dmw_function" + IntToString(nCount));
        DeleteLocalString(oMySpeaker, "dmw_params" + IntToString(nCount));
    }
    DeleteLocalString(oMySpeaker, "dmw_playerfunc");
    DeleteLocalInt(oMySpeaker, "dmw_started");
}

////////////////////////////////////////////////////////////////////////
//Smoking Function by Jason Robinson
location GetLocationAboveAndInFrontOf(object oPC, float fDist, float fHeight)
{
    float fDistance = -fDist;
    object oTarget = (oPC);
    object oArea = GetArea(oTarget);
    vector vPosition = GetPosition(oTarget);
    vPosition.z += fHeight;
    float fOrientation = GetFacing(oTarget);
    vector vNewPos = AngleToVector(fOrientation);
    float vZ = vPosition.z;
    float vX = vPosition.x - fDistance * vNewPos.x;
    float vY = vPosition.y - fDistance * vNewPos.y;
    fOrientation = GetFacing(oTarget);
    vX = vPosition.x - fDistance * vNewPos.x;
    vY = vPosition.y - fDistance * vNewPos.y;
    vNewPos = AngleToVector(fOrientation);
    vZ = vPosition.z;
    vNewPos = Vector(vX, vY, vZ);
    return Location(oArea, vNewPos, fOrientation);
}

////////////////////////////////////////////////////////////////////////
//Smoking Function by Jason Robinson
void SmokePipe(object oActivator)
{
    string sEmote1 = "*puffs on a pipe*";
    string sEmote2 = "*inhales from a pipe*";
    string sEmote3 = "*pulls a mouthful of smoke from a pipe*";
    float fHeight = 1.7;
    float fDistance = 0.1;
    // Set height based on race and gender
    if (GetGender(oActivator) == GENDER_MALE)
    {
        switch (GetRacialType(oActivator))
        {
        case RACIAL_TYPE_HUMAN:
        case RACIAL_TYPE_HALFELF: fHeight = 1.7; fDistance = 0.12; break;
        case RACIAL_TYPE_ELF: fHeight = 1.55; fDistance = 0.08; break;
        case RACIAL_TYPE_GNOME:
        case RACIAL_TYPE_HALFLING: fHeight = 1.15; fDistance = 0.12; break;
        case RACIAL_TYPE_DWARF: fHeight = 1.2; fDistance = 0.12; break;
        case RACIAL_TYPE_HALFORC: fHeight = 1.9; fDistance = 0.2; break;
        }
    }
    else
    {
        // FEMALES
        switch (GetRacialType(oActivator))
        {
        case RACIAL_TYPE_HUMAN:
        case RACIAL_TYPE_HALFELF: fHeight = 1.6; fDistance = 0.12; break;
        case RACIAL_TYPE_ELF: fHeight = 1.45; fDistance = 0.12; break;
        case RACIAL_TYPE_GNOME:
        case RACIAL_TYPE_HALFLING: fHeight = 1.1; fDistance = 0.075; break;
        case RACIAL_TYPE_DWARF: fHeight = 1.2; fDistance = 0.1; break;
        case RACIAL_TYPE_HALFORC: fHeight = 1.8; fDistance = 0.13; break;
        }
    }
    location lAboveHead = GetLocationAboveAndInFrontOf(oActivator, fDistance, fHeight);
    // emotes
    switch (d3())
    {
    case 1: AssignCommand(oActivator, ActionSpeakString(sEmote1)); break;
    case 2: AssignCommand(oActivator, ActionSpeakString(sEmote2)); break;
    case 3: AssignCommand(oActivator, ActionSpeakString(sEmote3));break;
    }
    // glow red
    AssignCommand(oActivator, ActionDoCommand(ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_LIGHT_RED_5), oActivator, 0.15)));
    // wait a moment
    AssignCommand(oActivator, ActionWait(3.0));
    // puff of smoke above and in front of head
    AssignCommand(oActivator, ActionDoCommand(ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_FNF_SMOKE_PUFF), lAboveHead)));
    // if female, turn head to left
    if ((GetGender(oActivator) == GENDER_FEMALE) && (GetRacialType(oActivator) != RACIAL_TYPE_DWARF))
        AssignCommand(oActivator, ActionPlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_LEFT, 1.0, 5.0));
}

////////////////////////////////////////////////////////////////////////
void ParseEmote(string sEmote, object oPC)
{
    // allow builder to suppress
    if (GetLocalInt(GetModule(), "DMFI_SUPPRESS_EMOTES") != 0) return;

    // see if PC has muted their own emotes
    if (GetLocalInt(oPC, "hls_emotemute") != 0) return;

    DeleteLocalInt(oPC, "dmfi_univ_int");
    object oRightHand = GetItemInSlot(INVENTORY_SLOT_RIGHTHAND,oPC);
    object oLeftHand =  GetItemInSlot(INVENTORY_SLOT_LEFTHAND,oPC);

    if (GetStringLeft(sEmote, 1) == "*")
    {
        int iToggle;
        string sBuffer;
        sBuffer = GetStringRight(sEmote, GetStringLength(sEmote)-1);
        while (!iToggle && GetStringLength(sBuffer) > 1)
        {
            if (GetStringLeft(sBuffer,1) == "*")
                iToggle = abs(iToggle - 1);
            sBuffer = GetStringRight(sBuffer, GetStringLength(sBuffer)-1);
        }
        sEmote = GetStringLeft(sEmote, GetStringLength(sEmote)-GetStringLength(sBuffer));
    }

    int iSit;
    object oArea;
    object oChair;
    // morderon - rewrote from here to end for better text case handling
    string sLCEmote = GetStringLowerCase(sEmote);
    //*emote* rolls
    if ((FindSubString(sLCEmote, "strength") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 61);
    else if ((FindSubString(sLCEmote, "dexterity") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 62);
    else if ((FindSubString(sLCEmote, "constitution") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 63);
    else if ((FindSubString(sLCEmote, "intelligence") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 64);
    else if ((FindSubString(sLCEmote, "wisdom") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 65);
    else if ((FindSubString(sLCEmote, "charisma") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 66);
    else if ((FindSubString(sLCEmote, "fortitude") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 67);
    else if ((FindSubString(sLCEmote, "reflex") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 68);
    else if ((FindSubString(sLCEmote, "will") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 69);
    else if ((FindSubString(sLCEmote, "animal empathy") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 71);
    else if ((FindSubString(sLCEmote, "appraise") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 72);
    else if ((FindSubString(sLCEmote, "bluff") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 73);
    else if ((FindSubString(sLCEmote, "concentration") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 74);
    else if ((FindSubString(sLCEmote, "craft armor") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 75);
    else if ((FindSubString(sLCEmote, "craft trap") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 76);
    else if ((FindSubString(sLCEmote, "craft weapon") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 77);
    else if ((FindSubString(sLCEmote, "disable trap") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 78);
    else if ((FindSubString(sLCEmote, "discipline") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 79);
    else if ((FindSubString(sLCEmote, "heal") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 81);
    else if ((FindSubString(sLCEmote, "hide") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 82);
    else if ((FindSubString(sLCEmote, "intimidate") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 83);
    else if ((FindSubString(sLCEmote, "listen") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 84);
    else if ((FindSubString(sLCEmote, "lore") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 85);
    else if ((FindSubString(sLCEmote, "move silently") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 86);
    else if ((FindSubString(sLCEmote, "open lock") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 87);
    else if ((FindSubString(sLCEmote, "parry") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 88);
    else if ((FindSubString(sLCEmote, "perform") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 89);
    else if ((FindSubString(sLCEmote, "persuade") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 91);
    else if ((FindSubString(sLCEmote, "pick pocket") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 92);
    else if ((FindSubString(sLCEmote, "search") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 93);
    else if ((FindSubString(sLCEmote, "set trap") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 94);
    else if ((FindSubString(sLCEmote, "spellcraft") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 95);
    else if ((FindSubString(sLCEmote, "spot") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 96);
    else if ((FindSubString(sLCEmote, "taunt") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 97);
    else if ((FindSubString(sLCEmote, "tumble") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 98);
    else if ((FindSubString(sLCEmote, "use magic device") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 99);
    else if ((FindSubString(sLCEmote, "ride") != -1))
        SetLocalInt(oPC, "dmfi_univ_int", 90);
    if (GetLocalInt(oPC, "dmfi_univ_int"))
    {
        SetLocalString(oPC, "dmfi_univ_conv", "pc_dicebag");
        ExecuteScript("dmfi_execute", oPC);
        return;
    }

    //*emote*
    if (FindSubString(GetStringLowerCase(sEmote), "*bows") != -1 ||
        FindSubString(GetStringLowerCase(sEmote), " bows") != -1 ||
        FindSubString(GetStringLowerCase(sEmote), "curtsey") != -1)
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_BOW, 1.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "drink") != -1 ||
             FindSubString(GetStringLowerCase(sEmote), "sips") != -1)
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_DRINK, 1.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "drinks") != -1 &&
             FindSubString(GetStringLowerCase(sEmote), "sits") != -1)
    {
        AssignCommand(oPC, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, 99999.0f));
        DelayCommand(1.0f, AssignCommand(oPC, PlayAnimation( ANIMATION_FIREFORGET_DRINK, 1.0)));
        DelayCommand(3.0f, AssignCommand(oPC, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, 99999.0)));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "reads") != -1 &&
             FindSubString(GetStringLowerCase(sEmote), "sits") != -1)
    {
        AssignCommand(oPC, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, 99999.0f));
        DelayCommand(1.0f, AssignCommand(oPC, PlayAnimation( ANIMATION_FIREFORGET_READ, 1.0)));
        DelayCommand(3.0f, AssignCommand(oPC, PlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, 99999.0)));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "sit")!= -1)
    {
        AssignCommand(oPC, ActionPlayAnimation( ANIMATION_LOOPING_SIT_CROSS, 1.0, 99999.0f));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "greet")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "wave") != -1)
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_GREETING, 1.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "yawn")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "stretch") != -1 ||
             FindSubString(GetStringLowerCase(sEmote), "bored") != -1)
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_PAUSE_BORED, 1.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "scratch")!= -1)
    {
        AssignCommand(oPC,ActionUnequipItem(oRightHand));
        AssignCommand(oPC,ActionUnequipItem(oLeftHand));
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_PAUSE_SCRATCH_HEAD, 1.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "*reads")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), " reads")!= -1||
             FindSubString(GetStringLowerCase(sEmote), "read")!= -1)
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_READ, 1.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "salute")!= -1)
    {
        AssignCommand(oPC,ActionUnequipItem(oRightHand));
        AssignCommand(oPC,ActionUnequipItem(oLeftHand));
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_SALUTE, 1.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "steal")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "swipe") != -1)
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_STEAL, 1.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "taunt")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "mock") != -1)
    {
        PlayVoiceChat(VOICE_CHAT_TAUNT, oPC);
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_TAUNT, 1.0));
    }
    else if ((FindSubString(GetStringLowerCase(sEmote), "smokes") != -1)||
             (FindSubString(GetStringLowerCase(sEmote), "smoke") != -1))
    {
        SmokePipe(oPC);
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "cheer ")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "cheer*")!= -1)
    {
        PlayVoiceChat(VOICE_CHAT_CHEER, oPC);
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_VICTORY1, 1.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "hooray")!= -1)
    {
        PlayVoiceChat(VOICE_CHAT_CHEER, oPC);
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_VICTORY2, 1.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "celebrate")!= -1)
    {
        PlayVoiceChat(VOICE_CHAT_CHEER, oPC);
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_VICTORY3, 1.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "giggle")!= -1 && GetGender(oPC) == GENDER_FEMALE)
        AssignCommand(oPC, PlaySound("vs_fshaldrf_haha"));
    else if (FindSubString(GetStringLowerCase(sEmote), "flop")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "prone")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_DEAD_FRONT, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "bends")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "stoop")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_GET_LOW, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "fiddle")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_GET_MID, 1.0, 5.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "nod")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "agree")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_LISTEN, 1.0, 4.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "peers")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "scans")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "search")!= -1)
        AssignCommand(oPC,ActionPlayAnimation(ANIMATION_LOOPING_LOOK_FAR, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "*pray")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), " pray")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "meditate")!= -1)
    {
        AssignCommand(oPC,ActionUnequipItem(oRightHand));
        AssignCommand(oPC,ActionUnequipItem(oLeftHand));
        AssignCommand(oPC,ActionPlayAnimation(ANIMATION_LOOPING_MEDITATE, 1.0, 99999.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "drunk")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "woozy")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_PAUSE_DRUNK, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "tired")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "fatigue")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "exhausted")!= -1)
    {
        PlayVoiceChat(VOICE_CHAT_REST, oPC);
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_PAUSE_TIRED, 1.0, 3.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "fidget")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "shifts")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_PAUSE2, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "sits")!= -1 &&
             (FindSubString(GetStringLowerCase(sEmote), "floor")!= -1 ||
              FindSubString(GetStringLowerCase(sEmote), "ground")!= -1))
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_SIT_CROSS, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "demand")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "threaten")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_TALK_FORCEFUL, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "laugh")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "chuckle")!= -1)
    {
        PlayVoiceChat(VOICE_CHAT_LAUGH, oPC);
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_TALK_LAUGHING, 1.0, 2.0));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "begs")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "plead")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_TALK_PLEADING, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "worship")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_WORSHIP, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "snore")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "*naps")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), " naps")!= -1||
             FindSubString(GetStringLowerCase(sEmote), "nap")!= -1)
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_SLEEP), oPC);
    else if (FindSubString(GetStringLowerCase(sEmote), "*sings")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), " sings")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "hums")!= -1)
        ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_BARD_SONG), oPC, 6.0f);
    else if (FindSubString(GetStringLowerCase(sEmote), "whistles")!= -1)
        AssignCommand(oPC, PlaySound("as_pl_whistle2"));
    else if (FindSubString(GetStringLowerCase(sEmote), "talks")!= -1 ||
             FindSubString(GetStringLowerCase(sEmote), "chats")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_TALK_NORMAL, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "shakes head")!= -1)
    {
        AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_LEFT, 1.0, 0.25f));
        DelayCommand(0.15f, AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_RIGHT, 1.0, 0.25f)));
        DelayCommand(0.40f, AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_LEFT, 1.0, 0.25f)));
        DelayCommand(0.65f, AssignCommand(oPC, PlayAnimation(ANIMATION_FIREFORGET_HEAD_TURN_RIGHT, 1.0, 0.25f)));
    }
    else if (FindSubString(GetStringLowerCase(sEmote), "ducks")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_DUCK, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "dodge")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_FIREFORGET_DODGE_SIDE, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "cantrip")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_CONJURE1, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "spellcast")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_CONJURE2, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "fall")!= -1 &&
             FindSubString(GetStringLowerCase(sEmote), "back")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_DEAD_BACK, 1.0, 99999.0));
    else if (FindSubString(GetStringLowerCase(sEmote), "spasm")!= -1)
        AssignCommand(oPC, ActionPlayAnimation(ANIMATION_LOOPING_SPASM, 1.0, 99999.0));
}

////////////////////////////////////////////////////////////////////////
string ConvertCustom(string sLetter, int iRotate)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);

    //Functional groups for custom languages
    //Vowel Sounds: a, e, i, o, u
    //Hard Sounds: b, d, k, p, t
    //Sibilant Sounds: c, f, s, q, w
    //Soft Sounds: g, h, l, r, y
    //Hummed Sounds: j, m, n, v, z
    //Oddball out: x, the rarest letter in the alphabet

    string sTranslate = "aeiouAEIOUbdkptBDKPTcfsqwCFSQWghlryGHLRYjmnvzJMNVZxX";
    int iTrans = FindSubString(sTranslate, sLetter);
    if (iTrans == -1) return sLetter; //return any character that isn't on the cipher

    //Now here's the tricky part... recalculating the offsets according functional
    //letter group, to produce an huge variety of "new" languages.

    int iOffset = iRotate % 5;
    int iGroup = iTrans / 5;
    int iBonus = iTrans / 10;
    int iMultiplier = iRotate / 5;
    iOffset = iTrans + iOffset + (iMultiplier * iBonus);

    return GetSubString(sTranslate, iGroup * 5 + iOffset % 5, 1);
}//end ConvertCustom

////////////////////////////////////////////////////////////////////////
string ProcessCustom(string sPhrase, int iLanguage)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertCustom(GetStringLeft(sPhrase, 1), iLanguage);
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertDrow(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "il";
    case 26: return "Il";
    case 1: return "f";
    case 27: return "F";
    case 2: return "st";
    case 28: return "St";
    case 3: return "w";
    case 4: return "a";
    case 5: return "o";
    case 6: return "v";
    case 7: return "ir";
    case 33: return "Ir";
    case 8: return "e";
    case 9: return "vi";
    case 35: return "Vi";
    case 10: return "go";
    case 11: return "c";
    case 12: return "li";
    case 13: return "l";
    case 14: return "e";
    case 15: return "ty";
    case 41: return "Ty";
    case 16: return "r";
    case 17: return "m";
    case 18: return "la";
    case 44: return "La";
    case 19: return "an";
    case 45: return "An";
    case 20: return "y";
    case 21: return "el";
    case 47: return "El";
    case 22: return "ky";
    case 48: return "Ky";
    case 23: return "'";
    case 24: return "a";
    case 25: return "p'";
    case 29: return "W";
    case 30: return "A";
    case 31: return "O";
    case 32: return "V";
    case 34: return "E";
    case 36: return "Go";
    case 37: return "C";
    case 38: return "Li";
    case 39: return "L";
    case 40: return "E";
    case 42: return "R";
    case 43: return "M";
    case 46: return "Y";
    case 49: return "'";
    case 50: return "A";
    case 51: return "P'";

    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessDrow(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertDrow(GetStringLeft(sPhrase, 1));

        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}


////////////////////////////////////////////////////////////////////////
string ConvertLeetspeak(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "4";
    case 26: return "4";
    case 1: return "8";
    case 27: return "8";
    case 2: return "(";
    case 28: return "(";
    case 3: return "|)";
    case 29: return "|)";
    case 4: return "3";
    case 30: return "3";
    case 5: return "f";
    case 31: return "F";
    case 6: return "9";
    case 32: return "9";
    case 7: return "h";
    case 33: return "H";
    case 8: return "!";
    case 34: return "!";
    case 9: return "j";
    case 35: return "J";
    case 10: return "|<";
    case 36: return "|<";
    case 11: return "1";
    case 37: return "1";
    case 12: return "/\/\\";
    case 38: return "/\/\\";
    case 13: return "|\|";
    case 39: return "|\|";
    case 14: return "0";
    case 40: return "0";
    case 15: return "p";
    case 41: return "P";
    case 16: return "Q";
    case 42: return "Q";
    case 17: return "R";
    case 43: return "R";
    case 18: return "5";
    case 44: return "5";
    case 19: return "7";
    case 45: return "7";
    case 20: return "u";
    case 46: return "U";
    case 21: return "\/";
    case 47: return "\/";
    case 22: return "\/\/";
    case 48: return "\/\/";
    case 23: return "x";
    case 49: return "X";
    case 24: return "y";
    case 50: return "Y";
    case 25: return "2";
    case 51: return "2";
    default: return sLetter;
    }
    return "";
}//end ConvertLeetspeak

////////////////////////////////////////////////////////////////////////
string ProcessLeetspeak(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertLeetspeak(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertInfernal(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "o";
    case 1: return "c";
    case 2: return "r";
    case 3: return "j";
    case 4: return "a";
    case 5: return "v";
    case 6: return "k";
    case 7: return "r";
    case 8: return "y";
    case 9: return "z";
    case 10: return "g";
    case 11: return "m";
    case 12: return "z";
    case 13: return "r";
    case 14: return "y";
    case 15: return "k";
    case 16: return "r";
    case 17: return "n";
    case 18: return "k";
    case 19: return "d";
    case 20: return "'";
    case 21: return "r";
    case 22: return "'";
    case 23: return "k";
    case 24: return "i";
    case 25: return "g";
    case 26: return "O";
    case 27: return "C";
    case 28: return "R";
    case 29: return "J";
    case 30: return "A";
    case 31: return "V";
    case 32: return "K";
    case 33: return "R";
    case 34: return "Y";
    case 35: return "Z";
    case 36: return "G";
    case 37: return "M";
    case 38: return "Z";
    case 39: return "R";
    case 40: return "Y";
    case 41: return "K";
    case 42: return "R";
    case 43: return "N";
    case 44: return "K";
    case 45: return "D";
    case 46: return "'";
    case 47: return "R";
    case 48: return "'";
    case 49: return "K";
    case 50: return "I";
    case 51: return "G";
    default: return sLetter;
    }
    return "";
}//end ConvertInfernal

////////////////////////////////////////////////////////////////////////
string ProcessInfernal(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertInfernal(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertAbyssal(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 27: return "N";
    case 28: return "M";
    case 29: return "G";
    case 30: return "A";
    case 31: return "K";
    case 32: return "S";
    case 33: return "D";
    case 35: return "H";
    case 36: return "B";
    case 37: return "L";
    case 38: return "P";
    case 39: return "T";
    case 40: return "E";
    case 41: return "B";
    case 43: return "N";
    case 44: return "M";
    case 45: return "G";
    case 48: return "B";
    case 51: return "T";
    case 0: return "oo";
    case 26: return "OO";
    case 1: return "n";
    case 2: return "m";
    case 3: return "g";
    case 4: return "a";
    case 5: return "k";
    case 6: return "s";
    case 7: return "d";
    case 8: return "oo";
    case 34: return "OO";
    case 9: return "h";
    case 10: return "b";
    case 11: return "l";
    case 12: return "p";
    case 13: return "t";
    case 14: return "e";
    case 15: return "b";
    case 16: return "ch";
    case 42: return "Ch";
    case 17: return "n";
    case 18: return "m";
    case 19: return "g";
    case 20: return  "ae";
    case 46: return  "Ae";
    case 21: return  "ts";
    case 47: return  "Ts";
    case 22: return "b";
    case 23: return  "bb";
    case 49: return  "Bb";
    case 24: return  "ee";
    case 50: return  "Ee";
    case 25: return "t";
    default: return sLetter;
    }
    return "";
}//end ConvertAbyssal

////////////////////////////////////////////////////////////////////////
string ProcessAbyssal(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertAbyssal(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertCelestial(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "a";
    case 1: return "p";
    case 2: return "v";
    case 3: return "t";
    case 4: return "el";
    case 5: return "b";
    case 6: return "w";
    case 7: return "r";
    case 8: return "i";
    case 9: return "m";
    case 10: return "x";
    case 11: return "h";
    case 12: return "s";
    case 13: return "c";
    case 14: return "u";
    case 15: return "q";
    case 16: return "d";
    case 17: return "n";
    case 18: return "l";
    case 19: return "y";
    case 20: return "o";
    case 21: return "j";
    case 22: return "f";
    case 23: return "g";
    case 24: return "z";
    case 25: return "k";
    case 26: return "A";
    case 27: return "P";
    case 28: return "V";
    case 29: return "T";
    case 30: return "El";
    case 31: return "B";
    case 32: return "W";
    case 33: return "R";
    case 34: return "I";
    case 35: return "M";
    case 36: return "X";
    case 37: return "H";
    case 38: return "S";
    case 39: return "C";
    case 40: return "U";
    case 41: return "Q";
    case 42: return "D";
    case 43: return "N";
    case 44: return "L";
    case 45: return "Y";
    case 46: return "O";
    case 47: return "J";
    case 48: return "F";
    case 49: return "G";
    case 50: return "Z";
    case 51: return "K";
    default: return sLetter;
    }
    return "";
}//end ConvertCelestial

////////////////////////////////////////////////////////////////////////
string ProcessCelestial(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertCelestial(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertGoblin(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "u";
    case 1: return "p";
    case 2: return "";
    case 3: return "t";
    case 4: return "'";
    case 5: return "v";
    case 6: return "k";
    case 7: return "r";
    case 8: return "o";
    case 9: return "z";
    case 10: return "g";
    case 11: return "m";
    case 12: return "s";
    case 13: return "";
    case 14: return "u";
    case 15: return "b";
    case 16: return "";
    case 17: return "n";
    case 18: return "k";
    case 19: return "d";
    case 20: return "u";
    case 21: return "";
    case 22: return "'";
    case 23: return "";
    case 24: return "o";
    case 25: return "w";
    case 26: return "U";
    case 27: return "P";
    case 28: return "";
    case 29: return "T";
    case 30: return "'";
    case 31: return "V";
    case 32: return "K";
    case 33: return "R";
    case 34: return "O";
    case 35: return "Z";
    case 36: return "G";
    case 37: return "M";
    case 38: return "S";
    case 39: return "";
    case 40: return "U";
    case 41: return "B";
    case 42: return "";
    case 43: return "N";
    case 44: return "K";
    case 45: return "D";
    case 46: return "U";
    case 47: return "";
    case 48: return "'";
    case 49: return "";
    case 50: return "O";
    case 51: return "W";
    default: return sLetter;
    }
    return "";
}//end ConvertGoblin

////////////////////////////////////////////////////////////////////////
string ProcessGoblin(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertGoblin(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertDraconic(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "e";
    case 26: return "E";
    case 1: return "po";
    case 27: return "Po";
    case 2: return "st";
    case 28: return "St";
    case 3: return "ty";
    case 29: return "Ty";
    case 4: return "i";
    case 5: return "w";
    case 6: return "k";
    case 7: return "ni";
    case 33: return "Ni";
    case 8: return "un";
    case 34: return "Un";
    case 9: return "vi";
    case 35: return "Vi";
    case 10: return "go";
    case 36: return "Go";
    case 11: return "ch";
    case 37: return "Ch";
    case 12: return "li";
    case 38: return "Li";
    case 13: return "ra";
    case 39: return "Ra";
    case 14: return "y";
    case 15: return "ba";
    case 41: return "Ba";
    case 16: return "x";
    case 17: return "hu";
    case 43: return "Hu";
    case 18: return "my";
    case 44: return "My";
    case 19: return "dr";
    case 45: return "Dr";
    case 20: return "on";
    case 46: return "On";
    case 21: return "fi";
    case 47: return "Fi";
    case 22: return "zi";
    case 48: return "Zi";
    case 23: return "qu";
    case 49: return "Qu";
    case 24: return "an";
    case 50: return "An";
    case 25: return "ji";
    case 51: return "Ji";
    case 30: return "I";
    case 31: return "W";
    case 32: return "K";
    case 40: return "Y";
    case 42: return "X";
    default: return sLetter;
    }
    return "";
}//end ConvertDraconic

////////////////////////////////////////////////////////////////////////
string ProcessDraconic(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertDraconic(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertDwarf(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "az";
    case 26: return "Az";
    case 1: return "po";
    case 27: return "Po";
    case 2: return "zi";
    case 28: return "Zi";
    case 3: return "t";
    case 4: return "a";
    case 5: return "wa";
    case 31: return "Wa";
    case 6: return "k";
    case 7: return "'";
    case 8: return "a";
    case 9: return "dr";
    case 35: return "Dr";
    case 10: return "g";
    case 11: return "n";
    case 12: return "l";
    case 13: return "r";
    case 14: return "ur";
    case 40: return "Ur";
    case 15: return "rh";
    case 41: return "Rh";
    case 16: return "k";
    case 17: return "h";
    case 18: return "th";
    case 44: return "Th";
    case 19: return "k";
    case 20: return "'";
    case 21: return "g";
    case 22: return "zh";
    case 48: return "Zh";
    case 23: return "q";
    case 24: return "o";
    case 25: return "j";
    case 29: return "T";
    case 30: return "A";
    case 32: return "K";
    case 33: return "'";
    case 34: return "A";
    case 36: return "G";
    case 37: return "N";
    case 38: return "L";
    case 39: return "R";
    case 42: return "K";
    case 43: return "H";
    case 45: return "K";
    case 46: return "'";
    case 47: return "G";
    case 49: return "Q";
    case 50: return "O";
    case 51: return "J";
    default: return sLetter;
    } return "";
}//end ConvertDwarf

////////////////////////////////////////////////////////////////////////
string ProcessDwarf(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertDwarf(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertElven(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "il";
    case 26: return "Il";
    case 1: return "f";
    case 2: return "ny";
    case 28: return "Ny";
    case 3: return "w";
    case 4: return "a";
    case 5: return "o";
    case 6: return "v";
    case 7: return "ir";
    case 33: return "Ir";
    case 8: return "e";
    case 9: return "qu";
    case 35: return "Qu";
    case 10: return "n";
    case 11: return "c";
    case 12: return "s";
    case 13: return "l";
    case 14: return "e";
    case 15: return "ty";
    case 41: return "Ty";
    case 16: return "h";
    case 17: return "m";
    case 18: return "la";
    case 44: return "La";
    case 19: return "an";
    case 45: return "An";
    case 20: return "y";
    case 21: return "el";
    case 47: return "El";
    case 22: return "am";
    case 48: return "Am";
    case 23: return "'";
    case 24: return "a";
    case 25: return "j";

    case 27: return "F";
    case 29: return "W";
    case 30: return "A";
    case 31: return "O";
    case 32: return "V";
    case 34: return "E";
    case 36: return "N";
    case 37: return "C";
    case 38: return "S";
    case 39: return "L";
    case 40: return "E";
    case 42: return "H";
    case 43: return "M";
    case 46: return "Y";
    case 49: return "'";
    case 50: return "A";
    case 51: return "J";

    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessElven(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertElven(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertGnome(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
//cipher based on English -> Al Baed
    case 0: return "y";
    case 1: return "p";
    case 2: return "l";
    case 3: return "t";
    case 4: return "a";
    case 5: return "v";
    case 6: return "k";
    case 7: return "r";
    case 8: return "e";
    case 9: return "z";
    case 10: return "g";
    case 11: return "m";
    case 12: return "s";
    case 13: return "h";
    case 14: return "u";
    case 15: return "b";
    case 16: return "x";
    case 17: return "n";
    case 18: return "c";
    case 19: return "d";
    case 20: return "i";
    case 21: return "j";
    case 22: return "f";
    case 23: return "q";
    case 24: return "o";
    case 25: return "w";
    case 26: return "Y";
    case 27: return "P";
    case 28: return "L";
    case 29: return "T";
    case 30: return "A";
    case 31: return "V";
    case 32: return "K";
    case 33: return "R";
    case 34: return "E";
    case 35: return "Z";
    case 36: return "G";
    case 37: return "M";
    case 38: return "S";
    case 39: return "H";
    case 40: return "U";
    case 41: return "B";
    case 42: return "X";
    case 43: return "N";
    case 44: return "C";
    case 45: return "D";
    case 46: return "I";
    case 47: return "J";
    case 48: return "F";
    case 49: return "Q";
    case 50: return "O";
    case 51: return "W";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessGnome(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertGnome(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertHalfling(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
//cipher based on Al Baed -> English
    case 0: return "e";
    case 1: return "p";
    case 2: return "s";
    case 3: return "t";
    case 4: return "i";
    case 5: return "w";
    case 6: return "k";
    case 7: return "n";
    case 8: return "u";
    case 9: return "v";
    case 10: return "g";
    case 11: return "c";
    case 12: return "l";
    case 13: return "r";
    case 14: return "y";
    case 15: return "b";
    case 16: return "x";
    case 17: return "h";
    case 18: return "m";
    case 19: return "d";
    case 20: return "o";
    case 21: return "f";
    case 22: return "z";
    case 23: return "q";
    case 24: return "a";
    case 25: return "j";
    case 26: return "E";
    case 27: return "P";
    case 28: return "S";
    case 29: return "T";
    case 30: return "I";
    case 31: return "W";
    case 32: return "K";
    case 33: return "N";
    case 34: return "U";
    case 35: return "V";
    case 36: return "G";
    case 37: return "C";
    case 38: return "L";
    case 39: return "R";
    case 40: return "Y";
    case 41: return "B";
    case 42: return "X";
    case 43: return "H";
    case 44: return "M";
    case 45: return "D";
    case 46: return "O";
    case 47: return "F";
    case 48: return "Z";
    case 49: return "Q";
    case 50: return "A";
    case 51: return "J";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessHalfling(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertHalfling(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertOrc(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "ha";
    case 26: return "Ha";
    case 1: return "p";
    case 2: return "z";
    case 3: return "t";
    case 4: return "o";
    case 5: return "";
    case 6: return "k";
    case 7: return "r";
    case 8: return "a";
    case 9: return "m";
    case 10: return "g";
    case 11: return "h";
    case 12: return "r";
    case 13: return "k";
    case 14: return "u";
    case 15: return "b";
    case 16: return "k";
    case 17: return "h";
    case 18: return "g";
    case 19: return "n";
    case 20: return "";
    case 21: return "g";
    case 22: return "r";
    case 23: return "r";
    case 24: return "'";
    case 25: return "m";
    case 27: return "P";
    case 28: return "Z";
    case 29: return "T";
    case 30: return "O";
    case 31: return "";
    case 32: return "K";
    case 33: return "R";
    case 34: return "A";
    case 35: return "M";
    case 36: return "G";
    case 37: return "H";
    case 38: return "R";
    case 39: return "K";
    case 40: return "U";
    case 41: return "B";
    case 42: return "K";
    case 43: return "H";
    case 44: return "G";
    case 45: return "N";
    case 46: return "";
    case 47: return "G";
    case 48: return "R";
    case 49: return "R";
    case 50: return "'";
    case 51: return "M";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessOrc(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertOrc(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertAnimal(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "'";
    case 1: return "'";
    case 2: return "'";
    case 3: return "'";
    case 4: return "'";
    case 5: return "'";
    case 6: return "'";
    case 7: return "'";
    case 8: return "'";
    case 9: return "'";
    case 10: return "'";
    case 11: return "'";
    case 12: return "'";
    case 13: return "'";
    case 14: return "'";
    case 15: return "'";
    case 16: return "'";
    case 17: return "'";
    case 18: return "'";
    case 19: return "'";
    case 20: return "'";
    case 21: return "'";
    case 22: return "'";
    case 23: return "'";
    case 24: return "'";
    case 25: return "'";
    case 26: return "'";
    case 27: return "'";
    case 28: return "'";
    case 29: return "'";
    case 30: return "'";
    case 31: return "'";
    case 32: return "'";
    case 33: return "'";
    case 34: return "'";
    case 35: return "'";
    case 36: return "'";
    case 37: return "'";
    case 38: return "'";
    case 39: return "'";
    case 40: return "'";
    case 41: return "'";
    case 42: return "'";
    case 43: return "'";
    case 44: return "'";
    case 45: return "'";
    case 46: return "'";
    case 47: return "'";
    case 48: return "'";
    case 49: return "'";
    case 50: return "'";
    case 51: return "'";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessAnimal(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertAnimal(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ProcessCant(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);

    if (sLetter == "a" || sLetter == "A") return "*shields eyes*";
    if (sLetter == "b" || sLetter == "B") return "*blusters*";
    if (sLetter == "c" || sLetter == "C") return "*coughs*";
    if (sLetter == "d" || sLetter == "D") return "*furrows brow*";
    if (sLetter == "e" || sLetter == "E") return "*examines ground*";
    if (sLetter == "f" || sLetter == "F") return "*frowns*";
    if (sLetter == "g" || sLetter == "G") return "*glances up*";
    if (sLetter == "h" || sLetter == "H") return "*looks thoughtful*";
    if (sLetter == "i" || sLetter == "I") return "*looks bored*";
    if (sLetter == "j" || sLetter == "J") return "*rubs chin*";
    if (sLetter == "k" || sLetter == "K") return "*scratches ear*";
    if (sLetter == "l" || sLetter == "L") return "*looks around*";
    if (sLetter == "m" || sLetter == "M") return "*mmm hmm*";
    if (sLetter == "n" || sLetter == "N") return "*nods*";
    if (sLetter == "o" || sLetter == "O") return "*grins*";
    if (sLetter == "p" || sLetter == "P") return "*smiles*";
    if (sLetter == "q" || sLetter == "Q") return "*shivers*";
    if (sLetter == "r" || sLetter == "R") return "*rolls eyes*";
    if (sLetter == "s" || sLetter == "S") return "*scratches nose*";
    if (sLetter == "t" || sLetter == "T") return "*turns a bit*";
    if (sLetter == "u" || sLetter == "U") return "*glances idly*";
    if (sLetter == "v" || sLetter == "V") return "*runs hand through hair*";
    if (sLetter == "w" || sLetter == "W") return "*waves*";
    if (sLetter == "x" || sLetter == "X") return "*stretches*";
    if (sLetter == "y" || sLetter == "Y") return "*yawns*";
    if (sLetter == "z" || sLetter == "Z") return "*shrugs*";

    return "*nods*";
}

////////////////////////////////////////////////////////////////////////
string ConvertSylvan(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "i";
    case 1: return "ri";
    case 2: return "ba";
    case 3: return "ma";
    case 4: return "i";
    case 5: return "mo";
    case 6: return "yo";
    case 7: return "f";
    case 8: return "ya";
    case 9: return "ta";
    case 10: return "m";
    case 11: return "t";
    case 12: return "r";
    case 13: return "j";
    case 14: return "nu";
    case 15: return "wi";
    case 16: return "bo";
    case 17: return "w";
    case 18: return "ne";
    case 19: return "na";
    case 20: return "li";
    case 21: return "v";
    case 22: return "ni";
    case 23: return "ya";
    case 24: return "mi";
    case 25: return "og";
    case 26: return "I";
    case 27: return "Ri";
    case 28: return "Ba";
    case 29: return "Ma";
    case 30: return "I";
    case 31: return "Mo";
    case 32: return "Yo";
    case 33: return "F";
    case 34: return "Ya";
    case 35: return "Ta";
    case 36: return "M";
    case 37: return "T";
    case 38: return "R";
    case 39: return "J";
    case 40: return "Nu";
    case 41: return "Wi";
    case 42: return "Bo";
    case 43: return "W";
    case 44: return "Ne";
    case 45: return "Na";
    case 46: return "Li";
    case 47: return "V";
    case 48: return "Ni";
    case 49: return "Ya";
    case 50: return "Mi";
    case 51: return "Og";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessSylvan(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertSylvan(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertRashemi(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "a";
    case 1: return "s";
    case 2: return "n";
    case 3: return "y";
    case 4: return "ov";
    case 5: return "d";
    case 6: return "sk";
    case 7: return "fr";
    case 8: return "u";
    case 9: return "o";
    case 10: return "f";
    case 11: return "r";
    case 12: return "z";
    case 13: return "s";
    case 14: return "o";
    case 15: return "j";
    case 16: return "sk";
    case 17: return " ";
    case 18: return "or";
    case 19: return "ka";
    case 20: return "o";
    case 21: return "ka";
    case 22: return "ma";
    case 23: return "o";
    case 24: return "oj";
    case 25: return "y";
    case 26: return "A";
    case 27: return "S";
    case 28: return "N";
    case 29: return "Y";
    case 30: return "Ov";
    case 31: return "D";
    case 32: return "Sk";
    case 33: return "Fr";
    case 34: return "U";
    case 35: return "O";
    case 36: return "F";
    case 37: return "R";
    case 38: return "Z";
    case 39: return "S";
    case 40: return "O";
    case 41: return "J";
    case 42: return "Sk";
    case 43: return "M";
    case 44: return "Or";
    case 45: return "Ka";
    case 46: return "O";
    case 47: return "Ka";
    case 48: return "Ma";
    case 49: return "O";
    case 50: return "Oj";
    case 51: return "Y";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessRashemi(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertRashemi(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string ConvertMulhorandi(string sLetter)
{
    if (GetStringLength(sLetter) > 1)
        sLetter = GetStringLeft(sLetter, 1);
    string sTranslate = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    int iTrans = FindSubString(sTranslate, sLetter);

    switch (iTrans)
    {
    case 0: return "ri";
    case 1: return "dj";
    case 2: return "p";
    case 3: return "al";
    case 4: return "a";
    case 5: return "j";
    case 6: return "y";
    case 7: return "u";
    case 8: return "o";
    case 9: return "f";
    case 10: return "ch";
    case 11: return "d";
    case 12: return "t";
    case 13: return "m";
    case 14: return "eh";
    case 15: return "k";
    case 16: return "ng";
    case 17: return "sh";
    case 18: return "th";
    case 19: return "s";
    case 20: return "e";
    case 21: return "z";
    case 22: return "p";
    case 23: return "qu";
    case 24: return "o";
    case 25: return "z";
    case 26: return "Ri";
    case 27: return "Dj";
    case 28: return "P";
    case 29: return "Al";
    case 30: return "A";
    case 31: return "J";
    case 32: return "Y";
    case 33: return "U";
    case 34: return "O";
    case 35: return "F";
    case 36: return "Ch";
    case 37: return "D";
    case 38: return "T";
    case 39: return "M";
    case 40: return "Eh";
    case 41: return "K";
    case 42: return "Ng";
    case 43: return "Sh";
    case 44: return "Th";
    case 45: return "S";
    case 46: return "E";
    case 47: return "Z";
    case 48: return "P";
    case 49: return "Qu";
    case 50: return "O";
    case 51: return "Z";
    default: return sLetter;
    } return "";
}

////////////////////////////////////////////////////////////////////////
string ProcessMulhorandi(string sPhrase)
{
    string sOutput;
    int iToggle;
    while (GetStringLength(sPhrase) > 1)
    {
        if (GetStringLeft(sPhrase,1) == "*")
            iToggle = abs(iToggle - 1);
        if (iToggle)
            sOutput = sOutput + GetStringLeft(sPhrase,1);
        else
            sOutput = sOutput + ConvertMulhorandi(GetStringLeft(sPhrase, 1));
        sPhrase = GetStringRight(sPhrase, GetStringLength(sPhrase)-1);
    }
    return sOutput;
}

////////////////////////////////////////////////////////////////////////
string TranslateCommonToLanguage(int iLang, string sText)
{
    switch (iLang)
    {
    case 1: //Elven
        return ProcessElven(sText); break;
    case 2: //Gnome
        return ProcessGnome(sText); break;
    case 3: //Halfling
        return ProcessHalfling(sText); break;
    case 4: //Dwarf Note: Race 4 is normally Half Elf and Race 0 is normally Dwarf. This is changed.
        return ProcessDwarf(sText); break;
    case 5: //Orc
        return ProcessOrc(sText); break;
    case 6: //Goblin
        return ProcessGoblin(sText); break;
    case 7: //Draconic
        return ProcessDraconic(sText); break;
    case 8: //Animal
        return ProcessAnimal(sText); break;
    case 9: //Thieves Cant
        return ProcessCant(sText); break;
    case 10: //Celestial
        return ProcessCelestial(sText); break;
    case 11: //Abyssal
        return ProcessAbyssal(sText); break;
    case 12: //Infernal
        return ProcessInfernal(sText); break;
    case 13:
        return ProcessDrow(sText); break;
    case 14: // Sylvan
        return ProcessSylvan(sText); break;
    case 15: // Rashemi
        return ProcessRashemi(sText); break;
    case 16: // Mulhorandi
        return ProcessMulhorandi(sText); break;
    case 99: //1337
        return ProcessLeetspeak(sText); break;
    default: if (iLang > 100) return ProcessCustom(sText, iLang - 100);break;
    }
    return "";
}

////////////////////////////////////////////////////////////////////////
int GetDefaultRacialLanguage(object oPC, int iRename)
{
    switch (GetRacialType(oPC))
    {
    case RACIAL_TYPE_DWARF: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Dwarven");return 4; break;
    case RACIAL_TYPE_ELF:
    case RACIAL_TYPE_HALFELF: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Elven");return 1; break;
    case RACIAL_TYPE_GNOME: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Gnome");return 2; break;
    case RACIAL_TYPE_HALFLING: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Halfling");return 3; break;
    case RACIAL_TYPE_HUMANOID_ORC:
    case RACIAL_TYPE_HALFORC: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Orc");return 5; break;
    case RACIAL_TYPE_HUMANOID_GOBLINOID: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Goblin");return 6; break;
    case RACIAL_TYPE_HUMANOID_REPTILIAN:
    case RACIAL_TYPE_DRAGON: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Draconic");return 7; break;
    case RACIAL_TYPE_ANIMAL: if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Animal");return 8; break;
    default:
        if (GetLevelByClass(CLASS_TYPE_RANGER, oPC) || GetLevelByClass(CLASS_TYPE_DRUID, oPC))
        {
            if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Animal");
            return 8;
        }
        if (GetLevelByClass(CLASS_TYPE_ROGUE, oPC))
        {
            if (iRename) SetLocalString(oPC, "hls_MyLanguageName", "Thieves' Cant");
            return 9;
        }
        break;
    }
    return 0;
}

////////////////////////////////////////////////////////////////////////
int GetDefaultClassLanguage(object oPC)
{
    if (GetLevelByClass(CLASS_TYPE_RANGER, oPC) || GetLevelByClass(CLASS_TYPE_DRUID, oPC))
        return 8;
    if (GetLevelByClass(CLASS_TYPE_ROGUE, oPC))
        return 9;
    if ((GetSubRace(oPC)=="drow") ||(GetSubRace(oPC)=="DROW")||(GetSubRace(oPC)=="Drow"))
        return 13;
    if ((GetSubRace(oPC)=="fey") ||(GetSubRace(oPC)=="FEY")||(GetSubRace(oPC)=="Fey"))
        return 14;

    return 0;
}

////////////////////////////////////////////////////////////////////////
int GetIsAlphanumeric(string sCharacter)
{
    if (sCharacter == "a" ||
        sCharacter == "b" ||
        sCharacter == "c" ||
        sCharacter == "d" ||
        sCharacter == "e" ||
        sCharacter == "f" ||
        sCharacter == "g" ||
        sCharacter == "h" ||
        sCharacter == "i" ||
        sCharacter == "j" ||
        sCharacter == "k" ||
        sCharacter == "l" ||
        sCharacter == "m" ||
        sCharacter == "n" ||
        sCharacter == "o" ||
        sCharacter == "p" ||
        sCharacter == "q" ||
        sCharacter == "r" ||
        sCharacter == "s" ||
        sCharacter == "t" ||
        sCharacter == "u" ||
        sCharacter == "v" ||
        sCharacter == "w" ||
        sCharacter == "x" ||
        sCharacter == "y" ||
        sCharacter == "z" ||
        sCharacter == "A" ||
        sCharacter == "B" ||
        sCharacter == "C" ||
        sCharacter == "D" ||
        sCharacter == "E" ||
        sCharacter == "F" ||
        sCharacter == "G" ||
        sCharacter == "H" ||
        sCharacter == "I" ||
        sCharacter == "J" ||
        sCharacter == "K" ||
        sCharacter == "L" ||
        sCharacter == "M" ||
        sCharacter == "N" ||
        sCharacter == "O" ||
        sCharacter == "P" ||
        sCharacter == "Q" ||
        sCharacter == "R" ||
        sCharacter == "S" ||
        sCharacter == "T" ||
        sCharacter == "U" ||
        sCharacter == "V" ||
        sCharacter == "W" ||
        sCharacter == "X" ||
        sCharacter == "Y" ||
        sCharacter == "Z" ||
        sCharacter == "1" ||
        sCharacter == "2" ||
        sCharacter == "3" ||
        sCharacter == "4" ||
        sCharacter == "5" ||
        sCharacter == "6" ||
        sCharacter == "7" ||
        sCharacter == "8" ||
        sCharacter == "9" ||
        sCharacter == "0")
        return TRUE;

    return FALSE;
}

////////////////////////////////////////////////////////////////////////
void ParseCommand(object oTarget, object oCommander, string sComIn)
{
// :: 2008.07.31 morderon / tsunami282 - allow certain . commands for
// ::     PCs as well as DM's; allow shortcut targeting of henchies/pets

    int iOffset=0;
    if (GetIsDM(oTarget) && (oTarget != oCommander)) return; //DMs can only be affected by their own .commands

    int bValidTarget = GetIsObjectValid(oTarget);
    if (!bValidTarget)
    {
        DMFISendMessageToPC(oCommander, "No current command target - no commands will function.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
        return;
    }

    // break into command and args
    struct sStringTokenizer st = GetStringTokenizer(sComIn, " ");
    st = AdvanceToNextToken(st);
    string sCom = GetStringLowerCase(GetNextToken(st));
    string sArgs = LTrim(st.sRemaining);

    // ** commands usable by all pc's/dm's
    if (GetStringLeft(sCom, 4) == ".loc")
    {
        SetLocalInt(oCommander, "dmfi_dicebag", 2);
        SetCustomToken(20681, "Local");
        SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 2, oCommander);
        FloatingTextStringOnCreature("Broadcast Mode set to Local", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 4) == ".glo")
    {
        SetLocalInt(oCommander, "dmfi_dicebag", 1);
        SetCustomToken(20681, "Global");
        SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 1, oCommander);
        FloatingTextStringOnCreature("Broadcast Mode set to Global", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 4) == ".pri")
    {
        SetLocalInt(oCommander, "dmfi_dicebag", 0);
        SetCustomToken(20681, "Private");
        SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 0, oCommander);
        FloatingTextStringOnCreature("Broadcast Mode set to Private", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 3) == ".dm")
    {
        SetLocalInt(oCommander, "dmfi_dicebag", 3);
        SetCustomToken(20681, "DM Only");
        SetDMFIPersistentInt("dmfi", "dmfi_dicebag", 3, oCommander);
        FloatingTextStringOnCreature("Broadcast Mode set to DM Only", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 5) == ".aniy")
    {
        SetLocalInt(oCommander, "dmfi_dice_no_animate", 0);
        FloatingTextStringOnCreature("Rolls will show animation", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 5) == ".anin")
    {
        SetLocalInt(oCommander, "dmfi_dice_no_animate", 1);
        FloatingTextStringOnCreature("Rolls will NOT show animation", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 5) == ".emoy") // control emotes (based on Morderon code)
    {
        SetLocalInt(oCommander, "hls_emotemute", 0);
        FloatingTextStringOnCreature("*emote* commands are on", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 5) == ".emon") // control emotes (based on Morderon code)
    {
        SetLocalInt(oCommander, "hls_emotemute", 1);
        FloatingTextStringOnCreature("*emote* commands are off", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".lan") //sets the language of the target
    {
        // check target allowed
        if (!(GetIsDM(oCommander) || GetIsDMPossessed(oCommander) ||
            oTarget == oCommander || GetMaster(oTarget) == oCommander))
        {
            FloatingTextStringOnCreature("You cannot perform this command on a creature you do not control.", oCommander, FALSE);
            return;
        }

        string sArgsLC = GetStringLowerCase(sArgs);
        int iLang = 0;
        string sLang = "";
        if (FindSubString(sArgsLC, "elven") != -1 ||
            FindSubString(sArgsLC, "elf") != -1)
        {
            iLang = 1;
            sLang = "Elven";
        }
        else if (FindSubString(sArgsLC, "gnom") != -1)
        {
            iLang = 2;
            sLang = "Gnome";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "halfling") != -1)
        {
            iLang = 3;
            sLang = "Halfling";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "dwar") != -1)
        {
            iLang = 4;
            sLang = "Dwarven";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "orc") != -1)
        {
            iLang = 5;
            sLang = "Orc";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "goblin") != -1)
        {
            iLang = 6;
            sLang = "Goblin";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "draconic") != -1)
        {
            iLang = 7;
            sLang = "Draconic";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "animal") != -1)
        {
            iLang = 8;
            sLang = "Animal";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "cant") != -1)
        {
            iLang = 9;
            sLang = "Thieves' Cant";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "celestial") != -1)
        {
            iLang = 10;
            sLang = "Celestial";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "abyssal") != -1)
        {
            iLang = 11;
            sLang = "Abyssal";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "infernal") != -1)
        {
            iLang = 12;
            sLang = "Infernal";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "drow") != -1)
        {
            iLang = 13;
            sLang = "Drow";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "sylvan") != -1)
        {
            iLang = 14;
            sLang = "Sylvan";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "rashemi") != -1)
        {
            iLang = 15;
            sLang = "Rashemi";
        }
        else if (FindSubString(GetStringLowerCase(sCom), "mulhorandi") != -1)
        {
            iLang = 16;
            sLang = "Mulhorandi";
        }
        // see if target is allowed to speak that language
        if (!GetIsDM(oCommander) && !GetIsDMPossessed(oCommander)) // DM's can set any language on anyone
        {
            // commander is not DM, so see if target can speak desired language:
            // language must be default racial or class language, or target
            // must have a language widget for it
            if (!(GetIsObjectValid(GetItemPossessedBy(oTarget, "hlslang_"+IntToString(iLang))) ||
                GetDefaultRacialLanguage(oTarget, FALSE) == iLang ||
                GetDefaultClassLanguage(oTarget) == iLang))
            {
                iLang = 0;
            }
        }
        if (iLang > 0)
        {
            SetLocalInt(oTarget, "hls_MyLanguage", iLang);
            SetLocalString(oTarget, "hls_MyLanguageName", sLang);
            FloatingTextStringOnCreature("Language for "+GetName(oTarget)+" set to "+sLang, oCommander, FALSE);
        }
        else
        {
            FloatingTextStringOnCreature("Unable to set language - invalid target or language", oCommander, FALSE);
        }
        return;
    }

    // that's all the PC commands, bail out if not DM
    if (!GetIsDM(oCommander) && !GetIsDMPossessed(oCommander))
    {
        DMFISendMessageToPC(oCommander, "DMFI dot command nonexistent or restricted to DM's - aborting.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
        return;
    }

    if (GetStringLeft(sCom, 7) ==".appear")
    {
        string sNew = sArgs;
        DMFISendMessageToPC(oCommander, "Setting target appearance to: " + sNew, FALSE, DMFI_MESSAGE_COLOR_STATUS);
        int Appear = AppearType(sNew);

        if (Appear!=-1)
        {
            // SetCreatureAppearanceType(GetLocalObject(oCommander, "dmfi_univ_target"), Appear);
            SetCreatureAppearanceType(oTarget, Appear);
        }
        else
        {
            FloatingTextStringOnCreature("Invalid Appearance Type", oCommander);
        }


        dmw_CleanUp(oCommander);
        return;
    }


    if (GetStringLeft(sCom, 5) == ".stre")
        iOffset=  11;
    else if (GetStringLeft(sCom, 5) == ".dext")
        iOffset = 12;
    else if (GetStringLeft(sCom, 5) == ".cons")
        iOffset = 13;
    else if (GetStringLeft(sCom, 5) == ".inte")
        iOffset = 14;
    else if (GetStringLeft(sCom, 5) == ".wisd")
        iOffset = 15;
    else if (GetStringLeft(sCom, 5) == ".char")
        iOffset = 16;
    else if (GetStringLeft(sCom, 5) == ".fort")
        iOffset = 17;
    else if (GetStringLeft(sCom, 5) == ".refl")
        iOffset = 18;
    else if (GetStringLeft(sCom, 5) == ".anim")
        iOffset = 21;
    else if (GetStringLeft(sCom, 5) == ".appr")
        iOffset = 22;
    else if (GetStringLeft(sCom, 5) == ".bluf")
        iOffset =  23;
    else if (GetStringLeft(sCom, 5) == ".conc")
        iOffset = 24;
    else if (GetStringLeft(sCom, 9) == ".craft ar")
        iOffset =  25;
    else if (GetStringLeft(sCom, 9) == ".craft tr")
        iOffset =  26;
    else if (GetStringLeft(sCom, 9) == ".craft we")
        iOffset =  27;
    else if (GetStringLeft(sCom, 5) == ".disa")
        iOffset =  28;
    else if (GetStringLeft(sCom, 5) == ".disc")
        iOffset =  29;
    else if (GetStringLeft(sCom, 5) == ".heal")
        iOffset =  31;
    else if (GetStringLeft(sCom, 5) == ".hide")
        iOffset =  32;
    else if (GetStringLeft(sCom, 5) == ".inti")
        iOffset =  33;
    else if (GetStringLeft(sCom, 5) == ".list")
        iOffset =  34;
    else if (GetStringLeft(sCom, 5) == ".lore")
        iOffset =  35;
    else if (GetStringLeft(sCom, 5) == ".move")
        iOffset =  36;
    else if (GetStringLeft(sCom, 5) == ".open")
        iOffset =   37;
    else if (GetStringLeft(sCom, 5) == ".parr")
        iOffset =  38;
    else if (GetStringLeft(sCom, 5) == ".perf")
        iOffset =  39;
    else if (GetStringLeft(sCom, 5) == ".pers")
        iOffset =  41;
    else if (GetStringLeft(sCom, 5) == ".pick")
        iOffset =  42;
    else if (GetStringLeft(sCom, 5) == ".sear")
        iOffset =  43;
    else if (GetStringLeft(sCom, 6) == ".set t")
        iOffset =  44;
    else if (GetStringLeft(sCom, 5) == ".spel")
        iOffset =  45;
    else if (GetStringLeft(sCom, 5) == ".spot")
        iOffset =  46;
    else if (GetStringLeft(sCom, 5) == ".taun")
        iOffset =   47;
    else if (GetStringLeft(sCom, 5) == ".tumb")
        iOffset =  48;
    else if (GetStringLeft(sCom, 4) == ".use")
        iOffset =   49;

    if (iOffset!=0)
    {
        if (FindSubString(sCom, "all") != -1 || FindSubString(sArgs, "all") != -1)
            SetLocalInt(oCommander, "dmfi_univ_int", iOffset+40);
        else
            SetLocalInt(oCommander, "dmfi_univ_int", iOffset);

        SetLocalString(oCommander, "dmfi_univ_conv", "dicebag");
        if (GetIsObjectValid(oTarget))
        {
            if (oTarget != GetLocalObject(oCommander, "dmfi_univ_target"))
            {
                SetLocalObject(oCommander, "dmfi_univ_target", oTarget);
                FloatingTextStringOnCreature("DMFI Target set to "+GetName(oTarget), oCommander);
            }
            ExecuteScript("dmfi_execute", oCommander);
        }
        else
        {
            DMFISendMessageToPC(oCommander, "No valid DMFI target!", FALSE, DMFI_MESSAGE_COLOR_ALERT);
        }

        dmw_CleanUp(oCommander);
        return;
    }


    if (GetStringLeft(sCom, 4) == ".set")
    {
        // sCom = GetStringRight(sCom, GetStringLength(sCom) - 4);
        while (sArgs != "")
        {
            if (GetStringLeft(sArgs, 1) == " " ||
                GetStringLeft(sArgs, 1) == "[" ||
                GetStringLeft(sArgs, 1) == "." ||
                GetStringLeft(sArgs, 1) == ":" ||
                GetStringLeft(sArgs, 1) == ";" ||
                GetStringLeft(sArgs, 1) == "*" ||
                GetIsAlphanumeric(GetStringLeft(sArgs, 1)))
                sArgs = GetStringRight(sArgs, GetStringLength(sArgs) - 1);
            else
            {
                SetLocalObject(GetModule(), "hls_NPCControl" + GetStringLeft(sArgs, 1), oTarget);
                FloatingTextStringOnCreature("The Control character for " + GetName(oTarget) + " is " + GetStringLeft(sArgs, 1), oCommander, FALSE);
                return;
            }
        }
        FloatingTextStringOnCreature("Your Control Character is not valid. Perhaps you are using a reserved character.", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".ani")
    {
        int iArg = StringToInt(sArgs);
        AssignCommand(oTarget, ClearAllActions(TRUE));
        AssignCommand(oTarget, ActionPlayAnimation(iArg, 1.0, 99999.0f));
        return;
    }
    else if (GetStringLowerCase(GetStringLeft(sCom, 4)) == ".buf")
    {
        string sArgsLC = GetStringLowerCase(sArgs);
        if (FindSubString(sArgsLC, "low") !=-1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectACIncrease(3, AC_NATURAL_BONUS), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROT_BARKSKIN), oTarget, 3600.0f);
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_RESISTANCE, oTarget, METAMAGIC_ANY, TRUE, 5, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_GHOSTLY_VISAGE, oTarget, METAMAGIC_ANY, TRUE, 5, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_CLARITY,  oTarget,METAMAGIC_ANY, TRUE, 5, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            FloatingTextStringOnCreature("Low Buff applied: " + GetName(oTarget), oCommander);   return;
        }
        else if (FindSubString(sArgsLC, "mid") !=-1)
        {
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_LESSER_SPELL_MANTLE, oTarget, METAMAGIC_ANY, TRUE, 10, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_STONESKIN, oTarget, METAMAGIC_ANY, TRUE, 10, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_ELEMENTAL_SHIELD,  oTarget,METAMAGIC_ANY, TRUE, 10, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            FloatingTextStringOnCreature("Mid Buff applied: " + GetName(oTarget), oCommander);  return;
        }
        else if (FindSubString(sArgsLC, "high") !=-1)
        {
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_SPELL_MANTLE, oTarget, METAMAGIC_ANY, TRUE, 15, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_STONESKIN, oTarget, METAMAGIC_ANY, TRUE,15, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_SHADOW_SHIELD,  oTarget,METAMAGIC_ANY, TRUE, 15, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            FloatingTextStringOnCreature("High Buff applied: " + GetName(oTarget), oCommander);  return;
        }
        else if (FindSubString(sArgsLC, "epic") !=-1)
        {
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_GREATER_SPELL_MANTLE, oTarget, METAMAGIC_ANY, TRUE, 20, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_SPELL_RESISTANCE, oTarget, METAMAGIC_ANY, TRUE, 20, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_SHADOW_SHIELD,  oTarget,METAMAGIC_ANY, TRUE, 20, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            AssignCommand(oTarget, ActionCastSpellAtObject(SPELL_CLARITY,  oTarget,METAMAGIC_ANY, TRUE, 20, PROJECTILE_PATH_TYPE_DEFAULT, TRUE));
            FloatingTextStringOnCreature("Epic Buff applied: " + GetName(oTarget), oCommander);  return;
        }
        else if (FindSubString(sArgsLC, "barkskin") != -1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectACIncrease(3, AC_NATURAL_BONUS), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROT_BARKSKIN), oTarget, 3600.0f);  return;
        }
        else if (FindSubString(sArgsLC, "elements") != -1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectDamageResistance(DAMAGE_TYPE_COLD, 20, 40), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectDamageResistance(DAMAGE_TYPE_FIRE, 20, 40), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectDamageResistance(DAMAGE_TYPE_ACID, 20, 40), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectDamageResistance(DAMAGE_TYPE_SONIC, 20, 40), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectDamageResistance(DAMAGE_TYPE_ELECTRICAL, 20, 40), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROTECTION_ELEMENTS), oTarget, 3600.0f);  return;
        }
        else if (FindSubString(sArgsLC, "haste") != -1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectHaste(), oTarget, 3600.0f);  return;
        }
        else if (FindSubString(sArgsLC, "immortal") != -1) // tsunami282 added
        {
            SetImmortal(oTarget, TRUE);
            FloatingTextStringOnCreature("The target is set to Immortal (cannot die).", oCommander, FALSE);  return;
        }
        else if (FindSubString(sArgsLC, "mortal") != -1) // tsunami282 added
        {
            SetImmortal(oTarget, TRUE);
            FloatingTextStringOnCreature("The target is set to Mortal (can die).", oCommander, FALSE);  return;
        }
        else if (FindSubString(sArgsLC, "invis") != -1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectInvisibility(INVISIBILITY_TYPE_NORMAL), oTarget, 3600.0f);   return;
        }
        else if (FindSubString(sArgsLC, "unplot") != -1)
        {
            SetPlotFlag(oTarget, FALSE);
            FloatingTextStringOnCreature("The target is set to non-Plot.", oCommander, FALSE); return;
        }
        else if (FindSubString(sArgsLC, "plot") != -1)
        {
            SetPlotFlag(oTarget, TRUE);
            FloatingTextStringOnCreature("The target is set to Plot.", oCommander, FALSE);  return;
        }
        else if (FindSubString(sArgsLC, "stoneskin") != -1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectDamageReduction(10, DAMAGE_POWER_PLUS_THREE, 100), oTarget, 3600.0f);
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectVisualEffect(VFX_DUR_PROT_GREATER_STONESKIN), oTarget, 3600.0f); return;
        }
        else if (FindSubString(sArgsLC, "trues") != -1)
        {
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, EffectTrueSeeing(), oTarget, 3600.0f); return;
        }
    }
    else if (GetStringLeft(sCom, 4) == ".dam")
    {
        int iArg = StringToInt(sArgs);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDamage(iArg, DAMAGE_TYPE_MAGICAL, DAMAGE_POWER_NORMAL), oTarget);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_COM_BLOOD_LRG_RED), oTarget);
        FloatingTextStringOnCreature(GetName(oTarget) + " has taken " + IntToString(iArg) + " damage.", oCommander, FALSE);
        return;
    }
    // 2008.05.29 tsunami282 - set description
    else if (GetStringLeft(sCom, 5) == ".desc")
    {
        // object oTgt = GetLocalObject(oCommander, "dmfi_univ_target");
        if (GetIsObjectValid(oTarget))
        {
            if (sArgs == ".") // single dot means reset to base description
            {
                SetDescription(oTarget);
            }
            else // assign new description
            {
                SetDescription(oTarget, sArgs);
            }
            FloatingTextStringOnCreature("Target's description set to " + GetDescription(oTarget), oCommander, FALSE);
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target - command not processed.", oCommander, FALSE);
        }
    }
    else if (GetStringLeft(sCom, 5) == ".dism")
    {
        DestroyObject(oTarget);
        FloatingTextStringOnCreature(GetName(oTarget) + " dismissed", oCommander, FALSE); return;
    }
    else if (GetStringLeft(sCom, 4) == ".inv")
    {
        OpenInventory(oTarget, oCommander);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".dmt")
    {
        SetLocalInt(GetModule(), "dmfi_DMToolLock", abs(GetLocalInt(GetModule(), "dmfi_DMToolLock") -1)); return;
    }
    // else if (GetStringLowerCase(GetStringLeft(sCom, 4)) == ".dms")
    // {
    //     SetDMFIPersistentInt("dmfi", "dmfi_DMSpy", abs(GetDMFIPersistentInt("dmfi", "dmfi_DMSpy", oCommander) -1), oCommander); return;
    // }
    else if (GetStringLeft(sCom, 4) == ".fac")
    {
        string sArgsLC = GetStringLowerCase(sArgs);
        if (FindSubString(sArgsLC, "hostile") != -1)
        {
            ChangeToStandardFaction(oTarget, STANDARD_FACTION_HOSTILE);
            FloatingTextStringOnCreature("Faction set to hostile", oCommander, FALSE);
        }
        else if (FindSubString(sArgsLC, "commoner") != -1)
        {
            ChangeToStandardFaction(oTarget, STANDARD_FACTION_COMMONER);
            FloatingTextStringOnCreature("Faction set to commoner", oCommander, FALSE);
        }
        else if (FindSubString(sArgsLC, "defender") != -1)
        {
            ChangeToStandardFaction(oTarget, STANDARD_FACTION_DEFENDER);
            FloatingTextStringOnCreature("Faction set to defender", oCommander, FALSE);
        }
        else if (FindSubString(sArgsLC, "merchant") != -1)
        {
            ChangeToStandardFaction(oTarget, STANDARD_FACTION_MERCHANT);
            FloatingTextStringOnCreature("Faction set to merchant", oCommander, FALSE);
        }
        else
        {
            DMFISendMessageToPC(oCommander, "Invalid faction name - command aborted.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
            return;
        }

        // toggle blindness on the target, to cause a re-perception
        if (GetIsImmune(oTarget, IMMUNITY_TYPE_BLINDNESS))
        {
            DMFISendMessageToPC(oCommander, "Targeted creature is blind immune - no attack will occur until new perception event is fired", FALSE, DMFI_MESSAGE_COLOR_ALERT);
        }
        else
        {
            effect eInvis =EffectBlindness();
            ApplyEffectToObject(DURATION_TYPE_TEMPORARY, eInvis, oTarget, 6.1);
            DMFISendMessageToPC(oCommander, "Faction Adjusted - will take effect in 6 seconds", FALSE, DMFI_MESSAGE_COLOR_STATUS);
        }
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".fle")
    {
        AssignCommand(oTarget, ClearAllActions(TRUE));
        AssignCommand(oTarget, ActionMoveAwayFromObject(oCommander, TRUE));
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".fly")
    {
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectDisappear(), oTarget);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".fol")
    {
        int iArg = StringToInt(sArgs);
        FloatingTextStringOnCreature(GetName(oTarget) + " will follow you for "+IntToString(iArg)+" seconds.", oCommander, FALSE);
        AssignCommand(oTarget, ClearAllActions(TRUE));
        AssignCommand(oTarget, ActionForceMoveToObject(oCommander, TRUE, 2.0f, IntToFloat(iArg)));
        DelayCommand(IntToFloat(iArg), FloatingTextStringOnCreature(GetName(oTarget) + " has stopped following you.", oCommander, FALSE));
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".fre")
    {
        FloatingTextStringOnCreature(GetName(oTarget) + " frozen", oCommander, FALSE);
        SetCommandable(TRUE, oTarget);
        AssignCommand(oTarget, ClearAllActions(TRUE));
        DelayCommand(0.5f, SetCommandable(FALSE, oTarget));
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".get")
    {
        while (sArgs != "")
        {
            if (GetStringLeft(sArgs, 1) == " " ||
                GetStringLeft(sArgs, 1) == "[" ||
                GetStringLeft(sArgs, 1) == "." ||
                GetStringLeft(sArgs, 1) == ":" ||
                GetStringLeft(sArgs, 1) == ";" ||
                GetStringLeft(sArgs, 1) == "*" ||
                GetIsAlphanumeric(GetStringLeft(sArgs, 1)))
                sArgs = GetStringRight(sArgs, GetStringLength(sArgs) - 1);
            else
            {
                object oJump = GetLocalObject(GetModule(), "hls_NPCControl" + GetStringLeft(sArgs, 1));
                if (GetIsObjectValid(oJump))
                {
                    AssignCommand(oJump, ClearAllActions());
                    AssignCommand(oJump, ActionJumpToLocation(GetLocation(oCommander)));
                }
                else
                {
                    FloatingTextStringOnCreature("Your Control Character is not valid. Perhaps you are using a reserved character.", oCommander, FALSE);
                }
                return;
            }
        }
        FloatingTextStringOnCreature("Your Control Character is not valid. Perhaps you are using a reserved character.", oCommander, FALSE);
        return;

    }
    else if (GetStringLeft(sCom, 4) == ".got")
    {
        while (sArgs != "")
        {
            if (GetStringLeft(sArgs, 1) == " " ||
                GetStringLeft(sArgs, 1) == "[" ||
                GetStringLeft(sArgs, 1) == "." ||
                GetStringLeft(sArgs, 1) == ":" ||
                GetStringLeft(sArgs, 1) == ";" ||
                GetStringLeft(sArgs, 1) == "*" ||
                GetIsAlphanumeric(GetStringLeft(sArgs, 1)))
                sArgs = GetStringRight(sArgs, GetStringLength(sArgs) - 1);
            else
            {
                object oJump = GetLocalObject(GetModule(), "hls_NPCControl" + GetStringLeft(sArgs, 1));
                if (GetIsObjectValid(oJump))
                {
                    AssignCommand(oCommander, ClearAllActions());
                    AssignCommand(oCommander, ActionJumpToLocation(GetLocation(oJump)));
                }
                else
                {
                    FloatingTextStringOnCreature("Your Control Character is not valid. Perhaps you are using a reserved character.", oCommander, FALSE);
                }
                return;
            }
        }
        FloatingTextStringOnCreature("Your Control Character is not valid. Perhaps you are using a reserved character.", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".hea")
    {
        int iArg = StringToInt(sArgs);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectHeal(iArg), oTarget);
        ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(VFX_IMP_HEALING_M), oTarget);
        FloatingTextStringOnCreature(GetName(oTarget) + " has healed " + IntToString(iArg) + " HP.", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".ite")
    {
        object oCreate = CreateItemOnObject(sArgs, oTarget, 1);
        if (GetIsObjectValid(oCreate)) FloatingTextStringOnCreature("Item " + GetName(oCreate) + " created.", oCommander, FALSE);
        return;
    }
    // 2008.05.29 tsunami282 - set name
    else if (GetStringLeft(sCom, 5) == ".name")
    {
        // object oTgt = GetLocalObject(oCommander, "dmfi_univ_target");
        if (GetIsObjectValid(oTarget))
        {
            if (sArgs == ".") // single dot means reset to base name
            {
                SetName(oTarget);
            }
            else // assign new name
            {
                SetName(oTarget, sArgs);
            }
            FloatingTextStringOnCreature("Target's name set to " + GetName(oTarget), oCommander, FALSE);
        }
        else
        {
            FloatingTextStringOnCreature("Invalid target - command not processed.", oCommander, FALSE);
        }
    }
    else if (GetStringLeft(sCom, 4) == ".mut")
    {
        FloatingTextStringOnCreature(GetName(oTarget) + " muted", oCommander, FALSE);
        SetLocalInt(oTarget, "dmfi_Mute", 1);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".npc")
    {
        object oCreate = CreateObject(OBJECT_TYPE_CREATURE, sArgs, GetLocation(oTarget));
        if (GetIsObjectValid(oCreate))
            FloatingTextStringOnCreature("NPC " + GetName(oCreate) + " created.", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".pla")
    {
        object oCreate = CreateObject(OBJECT_TYPE_PLACEABLE, sArgs, GetLocation(oTarget));
        if (GetIsObjectValid(oCreate))
            FloatingTextStringOnCreature("Placeable " + GetName(oCreate) + " created.", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".rem")
    {
        effect eRemove = GetFirstEffect(oTarget);
        while (GetIsEffectValid(eRemove))
        {
            RemoveEffect(oTarget, eRemove);
            eRemove = GetNextEffect(oTarget);
        }
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".say")
    {
        int iArg = StringToInt(sArgs);
        if (GetDMFIPersistentString("dmfi", "hls206" + IntToString(iArg)) != "")
        {
            AssignCommand(oTarget, SpeakString(GetDMFIPersistentString("dmfi", "hls206" + IntToString(iArg))));
        }
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".tar")
    {
        object oGet = GetFirstObjectInArea(GetArea(oCommander));
        while (GetIsObjectValid(oGet))
        {
            if (FindSubString(GetName(oGet), sArgs) != -1)
            {
                // SetLocalObject(oCommander, "dmfi_VoiceTarget", oGet);
                SetLocalObject(oCommander, "dmfi_univ_target", oGet);
                FloatingTextStringOnCreature("You have targeted " + GetName(oGet) + " with the DMFI Targeting Widget", oCommander, FALSE);
                return;
            }
            oGet = GetNextObjectInArea(GetArea(oCommander));
        }
        FloatingTextStringOnCreature("Target not found.", oCommander, FALSE);
        return;
    }
    else if (GetStringLeft(sCom, 4) == ".unf")
    {
        FloatingTextStringOnCreature(GetName(oTarget) + " unfrozen", oCommander, FALSE);
        SetCommandable(TRUE, oTarget); return;
    }
    else if (GetStringLeft(sCom, 4) == ".unm")
    {
        FloatingTextStringOnCreature(GetName(oTarget) + " un-muted", oCommander, FALSE);
        DeleteLocalInt(oTarget, "dmfi_Mute"); return;
    }
    else if (GetStringLeft(sCom, 4) == ".vfx")
    {
        int iArg = StringToInt(sArgs);
        if (GetTag(oTarget) == "dmfi_voice")
            ApplyEffectAtLocation(DURATION_TYPE_INSTANT, EffectVisualEffect(iArg), GetLocation(oTarget), 10.0f);
        else
            ApplyEffectToObject(DURATION_TYPE_INSTANT, EffectVisualEffect(iArg), oTarget, 10.0f);
        return;
    }
    else if (GetStringLeft(sCom, 5) == ".vtar")
    {
        object oGet = GetFirstObjectInArea(GetArea(oCommander));
        while (GetIsObjectValid(oGet))
        {
            if (FindSubString(GetName(oGet), sArgs) != -1)
            {
                SetLocalObject(oCommander, "dmfi_VoiceTarget", oGet);
                FloatingTextStringOnCreature("You have targeted " + GetName(oGet) + " with the Voice Widget", oCommander, FALSE);
                return;
            }
            oGet = GetNextObjectInArea(GetArea(oCommander));
        }
        FloatingTextStringOnCreature("Target not found.", oCommander, FALSE);
        return;
    }
}

////////////////////////////////////////////////////////////////////////
void subTranslateToLanguage(string sSaid, object oShouter, int nVolume,
                            object oMaster, int iTranslate, string sLanguageName,
                            object oEavesdrop)
{
    string sVolume = "said";
    if (nVolume == TALKVOLUME_WHISPER) sVolume = "whispered";
    else if (nVolume == TALKVOLUME_SHOUT) sVolume = "shouted";
    else if (nVolume == TALKVOLUME_PARTY) sVolume = "said to the party";
    else if (nVolume == TALKVOLUME_SILENT_SHOUT) sVolume = "said to the DM's";

    //Translate and Send or do Lore check
    if (oEavesdrop == oMaster ||
        GetIsObjectValid(GetItemPossessedBy(oEavesdrop, "hlslang_" + IntToString(iTranslate))) ||
        GetIsObjectValid(GetItemPossessedBy(oEavesdrop, "babelfish")) ||
        iTranslate == GetDefaultRacialLanguage(oEavesdrop, 0) ||
        iTranslate == GetDefaultClassLanguage(oEavesdrop) ||
        GetIsDM(oEavesdrop) ||
        GetIsDMPossessed(oEavesdrop))
    {
        DelayCommand(0.1, DMFISendMessageToPC(oEavesdrop, GetName(oShouter) + " " + sVolume + " in " + sLanguageName + ": " + sSaid, FALSE, DMFI_MESSAGE_COLOR_TRANSLATION));
    }
    else
    {
        if (iTranslate != 9)
        {
            string sKnownLanguage;
            if (d20() + GetSkillRank(SKILL_LORE, oEavesdrop) > 20) sKnownLanguage = sLanguageName;
            else sKnownLanguage = "a language you do not recognize";
            DelayCommand(0.1, DMFISendMessageToPC(oEavesdrop, GetName(oShouter)+" "+sVolume+" something in "+sKnownLanguage+".", FALSE, DMFI_MESSAGE_COLOR_TRANSLATION));
        }
    }
}

////////////////////////////////////////////////////////////////////////
string TranslateToLanguage(string sSaid, object oShouter, int nVolume, object oMaster)
{
// arguments
//  (return) = translated text
//  sSaid = string to translate
//  oShouter = object that spoke sSaid
//  iVolume = TALKVOLUME setting of speaker
//  oMaster = master of oShouter (if oShouter has no master, oMaster should equal oShouter)

    //Gets the current language that the character is speaking
    int iTranslate = GetLocalInt(oShouter, "hls_MyLanguage");
    if (!iTranslate) iTranslate = GetDefaultRacialLanguage(oShouter, 1);
    if (!iTranslate)
    {
        DMFISendMessageToPC(oMaster, "Translator Error: your message was dropped.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
        return "";
    }

    //Defines language name
    string sLanguageName = GetLocalString(oShouter, "hls_MyLanguageName");

    sSaid = GetStringRight(sSaid, GetStringLength(sSaid)-1); // toss the leading translate flag '['
    //Thieves' Cant character limit of 25
    if (iTranslate == 9 && GetStringLength(sSaid) > 25)
        sSaid = GetStringLeft(sSaid, 25);
    string sSpeak = TranslateCommonToLanguage(iTranslate, sSaid);
    // lop off trailing ']'
    if (GetStringRight(sSaid, 1) == "]")
        sSaid = GetStringLeft(sSaid, GetStringLength(sSaid)-1);
    // AssignCommand(oShouter, SpeakString(sSpeak)); // no need reissue translated speech, handled in player chat hook

    // send speech to everyone who should be able to hear
    float fDistance = 20.0f;
    if (nVolume == TALKVOLUME_WHISPER)
    {
        fDistance = 1.0f;
    }
    string sVolume = "said";
    if (nVolume == TALKVOLUME_WHISPER) sVolume = "whispered";
    else if (nVolume == TALKVOLUME_SHOUT) sVolume = "shouted";
    else if (nVolume == TALKVOLUME_PARTY) sVolume = "said to the party";
    else if (nVolume == TALKVOLUME_SILENT_SHOUT) sVolume = "said to the DM's";
    string sKnownLanguage;

    // send translated message to PC's in range who understand it
    object oEavesdrop = GetFirstObjectInShape(SHAPE_SPHERE, fDistance, GetLocation(oShouter), FALSE, OBJECT_TYPE_CREATURE);
    while (GetIsObjectValid(oEavesdrop))
    {
        if (GetIsPC(oEavesdrop) || GetIsDM(oEavesdrop) || GetIsDMPossessed(oEavesdrop) || GetIsPossessedFamiliar(oEavesdrop))
        {
            subTranslateToLanguage(sSaid, oShouter, nVolume, oMaster, iTranslate, sLanguageName, oEavesdrop);
        }
        oEavesdrop = GetNextObjectInShape(SHAPE_SPHERE, fDistance, GetLocation(oShouter), FALSE, OBJECT_TYPE_CREATURE);
    }

    // send translated message to DM's in range
    oEavesdrop = GetFirstPC();
    while (GetIsObjectValid(oEavesdrop))
    {
        if (GetIsDM(oEavesdrop))
        {
            if (GetArea(oShouter) == GetArea(oEavesdrop) &&
                GetDistanceBetweenLocations(GetLocation(oShouter), GetLocation(oEavesdrop)) <= fDistance)
            {
                subTranslateToLanguage(sSaid, oShouter, nVolume, oMaster, iTranslate, sLanguageName, oEavesdrop);
            }
        }
        oEavesdrop = GetNextPC();
    }
    return sSpeak;
}

////////////////////////////////////////////////////////////////////////
int RelayTextToEavesdropper(object oShouter, int nVolume, string sSaid)
{
// arguments
//  (return) - flag to continue processing text: X2_EXECUTE_SCRIPT_CONTINUE or
//             X2_EXECUTE_SCRIPT_END
//  oShouter - object that spoke
//  nVolume - channel (TALKVOLUME) text was spoken on
//  sSaid - text that was spoken

    int bScriptEnd = X2_EXECUTE_SCRIPT_CONTINUE;

    // sanity checks
    if (GetIsObjectValid(oShouter))
    {
        int iHookToDelete = 0;
        int iHookType = 0;
        int channels = 0;
        int rangemode = 0;
        string siHook = "";
        object oMod = GetModule();
        int iHook = 1;
        while (1)
        {
            siHook = IntToString(iHook);
            iHookType = GetLocalInt(oMod, sHookTypeVarname+siHook);
            if (iHookType == 0) break; // end of list

            // check channel
            channels = GetLocalInt(oMod, sHookChannelsVarname+siHook);
            if (((1 << nVolume) & channels) != 0)
            {
                string sVol = (nVolume == TALKVOLUME_WHISPER ? "whispers" : "says");
                object oOwner = GetLocalObject(oMod, sHookOwnerVarname+siHook);
                if (GetIsObjectValid(oOwner))
                {
                    // it's a channel for us to listen on, process
                    int bcast = GetLocalInt(oMod, sHookBcastDMsVarname+siHook);
                    // for type 1, see if speaker is the one we want (pc or party)
                    // for type 2, see if speaker says his stuff within ("earshot" / area / module) of listener's location
                    if (iHookType == 1) // listen to what a PC hears
                    {
                        object oListener;
                        location locShouter, locListener;
                        object oTargeted = GetLocalObject(oMod, sHookCreatureVarname+siHook);
                        if (GetIsObjectValid(oTargeted))
                        {
                            rangemode = GetLocalInt(oMod, sHookRangeModeVarname+siHook);
                            if (rangemode) oListener = GetFirstFactionMember(oTargeted, FALSE); // everyone in party are our listeners
                            else oListener = oTargeted; // only selected PC is our listener
                            while (GetIsObjectValid(oListener))
                            {
                                // check speaker:
                                // check within earshot
                                int bInRange = FALSE;
                                locShouter = GetLocation(oShouter);
                                locListener = GetLocation(oListener);
                                if (oShouter == oListener)
                                {
                                    bInRange = TRUE; // the target can always hear himself
                                }
                                else if (GetAreaFromLocation(locShouter) == GetAreaFromLocation(locListener))
                                {
                                    float dist = GetDistanceBetweenLocations(locListener, locShouter);
                                    if ((nVolume == TALKVOLUME_WHISPER && dist <= WHISPER_DISTANCE) ||
                                        (nVolume != TALKVOLUME_WHISPER && dist <= TALK_DISTANCE))
                                    {
                                        bInRange = TRUE;
                                    }
                                }
                                if (bInRange)
                                {
                                    // relay what's said to the hook owner
                                    string sMesg = "("+GetName(GetArea(oShouter))+") "+GetName(oShouter)+" "+sVol+": "+sSaid;
                                    // if (bcast) SendMessageToAllDMs(sMesg);
                                    // else SendMessageToPC(oOwner, sMesg);
                                    DMFISendMessageToPC(oOwner, sMesg, bcast, DMFI_MESSAGE_COLOR_EAVESDROP);
                                }
                                if (rangemode == 0) break; // only check the target creature for rangemode 0
                                if (bInRange) break; // once any party member hears shouter, we're done
                                oListener = GetNextFactionMember(oTargeted, FALSE);
                            }
                        }
                        else
                        {
                            // bad desired speaker, remove hook
                            iHookToDelete = iHook;
                        }
                    }
                    else if (iHookType == 2) // listen at location
                    {
                        location locShouter, locListener;
                        object oListener = GetLocalObject(oMod, sHookCreatureVarname+siHook);
                        if (oListener != OBJECT_INVALID)
                        {
                            locListener = GetLocation(oListener);
                        }
                        else
                        {
                            locListener = GetLocalLocation(oMod, sHookLocationVarname+siHook);
                        }
                        locShouter = GetLocation(oShouter);
                        rangemode = GetLocalInt(oMod, sHookRangeModeVarname+siHook);
                        int bInRange = FALSE;
                        if (rangemode == 0)
                        {
                            // check within earshot
                            if (GetAreaFromLocation(locShouter) == GetAreaFromLocation(locListener))
                            {
                                float dist = GetDistanceBetweenLocations(locListener, locShouter);
                                if ((nVolume == TALKVOLUME_WHISPER && dist <= WHISPER_DISTANCE) ||
                                    (nVolume != TALKVOLUME_WHISPER && dist <= TALK_DISTANCE))
                                {
                                    bInRange = TRUE;
                                }
                            }
                        }
                        else if (rangemode == 1)
                        {
                            // check within area
                            if (GetAreaFromLocation(locShouter) == GetAreaFromLocation(locListener)) bInRange = TRUE;
                        }
                        else
                        {
                            // module-wide
                            bInRange = TRUE;
                        }
                        if (bInRange)
                        {
                            // relay what's said to the hook owner
                            string sMesg = "("+GetName(GetArea(oShouter))+") "+GetName(oShouter)+" "+sVol+": "+sSaid;
                            // if (bcast) SendMessageToAllDMs(sMesg);
                            // else SendMessageToPC(oOwner, sMesg);
                            DMFISendMessageToPC(oOwner, sMesg, bcast, DMFI_MESSAGE_COLOR_EAVESDROP);
                        }
                    }
                    else
                    {
                        WriteTimestampedLogEntry("ERROR: DMFI OnPlayerChat handler: invalid iHookType; removing hook.");
                        iHookToDelete = iHook;
                    }
                }
                else
                {
                    // bad owner, delete hook
                    iHookToDelete = iHook;
                }
            }

            iHook++;
        }

        // remove a bad hook: note we can only remove one bad hook this way, have to rely on subsequent calls to remove any others
        if (iHookToDelete > 0)
        {
            RemoveListenerHook(iHookToDelete);
        }
    }

    return bScriptEnd;
}

////////////////////////////////////////////////////////////////////////
void main()
{
    int bScriptEnd = X2_EXECUTE_SCRIPT_CONTINUE;
    int nVolume = GetPCChatVolume();
    object oShouter = GetPCChatSpeaker();
    string sSaid = GetPCChatMessage();

// SpawnScriptDebugger();
// DMFISendMessageToPC(oShouter, IntToString(nVolume)+">> "+sSaid, FALSE, "737");

    // pass on any heard text to registered listeners
    // since listeners are set by DM's, pass the raw unprocessed command text to them
    bScriptEnd = RelayTextToEavesdropper(oShouter, nVolume, sSaid);

    if (bScriptEnd == X2_EXECUTE_SCRIPT_CONTINUE)
    {
        // see if we're supposed to listen on this channel
        if ((nVolume == TALKVOLUME_TALK && DMFI_LISTEN_ON_CHANNEL_TALK) ||
            (nVolume == TALKVOLUME_SILENT_SHOUT && DMFI_LISTEN_ON_CHANNEL_DM) ||
            (nVolume == TALKVOLUME_WHISPER && DMFI_LISTEN_ON_CHANNEL_WHISPER) ||
            (nVolume == TALKVOLUME_PARTY && DMFI_LISTEN_ON_CHANNEL_PARTY) ||
            (nVolume == TALKVOLUME_SHOUT && DMFI_LISTEN_ON_CHANNEL_SHOUT))
        {
            // yes we are
            // now see if we have a command to parse
            // special chars:
            //     [ = speak in alternate language
            //     * = perform emote
            //     : = throw voice to last designated target
            //     ; = throw voice to master / animal companion / familiar / henchman / summon
            //     , = throw voice summon / henchman / familiar / animal companion / master
            //     . = command to execute

            int bChangedText = 0;
            object oTarget = OBJECT_INVALID;
            int iTargetType = 0;

            // eat leading whitespace
            while (GetStringLeft(sSaid, 1) == " ")
            {
                sSaid = GetStringRight(sSaid, GetStringLength(sSaid)-1);
            }

            string sLeadChar = GetStringLeft(sSaid, 1);
            string s2ndChar = GetStringRight(GetStringLeft(sSaid, 2), 1);

            // check for target selection
            if (s2ndChar != sLeadChar) // doubled leadins should be ignored
            {
                if (sLeadChar == ":")
                {
                    if (GetIsDM(oShouter) || GetIsDMPossessed(oShouter))
                    {
                        // last creature targeted with DMFI Voice Widget
                        iTargetType = 1;
                        oTarget = GetLocalObject(oShouter, "dmfi_VoiceTarget");
                    }
                    else
                    {
                        // non-DM's can't target others
                        iTargetType = -1;
                        oTarget = OBJECT_INVALID;
                    }
                }
                else if (sLeadChar == ";")
                {
                    // master / animal companion / familiar / henchman / summon
                    iTargetType = 2;
                    oTarget = GetMaster(oShouter);
                    if (!GetIsObjectValid(oTarget))
                    {
                        oTarget = GetAssociate(ASSOCIATE_TYPE_ANIMALCOMPANION, oShouter);
                        if (!GetIsObjectValid(oTarget))
                        {
                            oTarget = GetAssociate(ASSOCIATE_TYPE_FAMILIAR, oShouter);
                            if (!GetIsObjectValid(oTarget))
                            {
                                oTarget = GetAssociate(ASSOCIATE_TYPE_HENCHMAN, oShouter);
                                if (!GetIsObjectValid(oTarget))
                                {
                                    oTarget = GetAssociate(ASSOCIATE_TYPE_SUMMONED, oShouter);
                                }
                            }
                        }
                    }
                }
                else if (sLeadChar == ",")
                {
                    // summon / henchman / familiar / animal companion / master
                    iTargetType = 3;
                    oTarget = GetAssociate(ASSOCIATE_TYPE_SUMMONED, oShouter);
                    if (!GetIsObjectValid(oTarget))
                    {
                        oTarget = GetAssociate(ASSOCIATE_TYPE_HENCHMAN, oShouter);
                        if (!GetIsObjectValid(oTarget))
                        {
                            oTarget = GetAssociate(ASSOCIATE_TYPE_FAMILIAR, oShouter);
                            if (!GetIsObjectValid(oTarget))
                            {
                                oTarget = GetAssociate(ASSOCIATE_TYPE_ANIMALCOMPANION, oShouter);
                                if (!GetIsObjectValid(oTarget))
                                {
                                    oTarget = GetMaster(oShouter);
                                }
                            }
                        }
                    }
                }

                if (iTargetType != 0)
                {
                    // eat the targeting character and any whitespace following it
                    sSaid = GetStringRight(sSaid, GetStringLength(sSaid)-1);
                    while (GetStringLeft(sSaid, 1) == " ")
                    {
                        sSaid = GetStringRight(sSaid, GetStringLength(sSaid)-1);
                    }
                    sLeadChar = GetStringLeft(sSaid, 1);
                }

                // now parse special command char (.command, *emote, [lang)
                if (sLeadChar == ".")
                {
                    bChangedText = 1;
                    if (oTarget == OBJECT_INVALID)
                    {
                        // 2008.05.29 tsunami282 - no target set, so dot command uses DMFI targeting wand
                        oTarget = GetLocalObject(oShouter, "dmfi_univ_target");
                    }

                    if (GetIsObjectValid(oTarget))
                    {
                        ParseCommand(oTarget, oShouter, sSaid);
                        sSaid = "";
                    }
                    else
                    {
                        // target invalid
                        bChangedText = 1;
                        DMFISendMessageToPC(oShouter, "Invalid command target - not processed.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
                        sSaid = "";
                    }
                }
                else if (sLeadChar == "*")
                {
                    bChangedText = 1;
                    if (oTarget == OBJECT_INVALID) oTarget = oShouter; // untargeted emotes apply to self
                    if (GetIsObjectValid(oTarget))
                    {
                        ParseEmote(sSaid, oTarget);
                    }
                    else
                    {
                        // target invalid
                        bChangedText = 1;
                        DMFISendMessageToPC(oShouter, "Invalid emote target - not processed.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
                        sSaid = "";
                    }
                }
                else if (sLeadChar == "[")
                {
                    bChangedText = 1;
                    if (oTarget == OBJECT_INVALID) oTarget = oShouter; // untargeted languages spoken by self
                    if (GetIsObjectValid(oTarget))
                    {
                        sSaid = TranslateToLanguage(sSaid, oTarget, nVolume, oShouter);
                    }
                    else
                    {
                        // target invalid
                        bChangedText = 1;
                        DMFISendMessageToPC(oShouter, "Invalid language target - not processed.", FALSE, DMFI_MESSAGE_COLOR_ALERT);
                        sSaid = "";
                    }
                }
            }

            if (iTargetType != 0)
            {
                // throw the message
                if (sSaid != "")
                {
                    bChangedText = 1;
                    AssignCommand(oTarget, SpeakString(sSaid, nVolume));
                    if (DMFI_LOG_CONVERSATION)
                    {
                        PrintString("<Conv>"+GetName(GetArea(oTarget))+ " " + GetName(oTarget) + ": " + sSaid + " </Conv>");
                    }
                    sSaid = "";
                }
            }
            else
            {
                // log what was said
                if (DMFI_LOG_CONVERSATION && (sSaid != ""))
                {
                    PrintString("<Conv>"+GetName(GetArea(oShouter))+ " " + GetName(oShouter) + ": " + sSaid + " </Conv>");
                }
            }

            if (bChangedText)
            {
                SetPCChatMessage(sSaid);
                bScriptEnd = X2_EXECUTE_SCRIPT_END;
            }
        }
    }

    SetExecutedScriptReturnValue(bScriptEnd);
}

