/**********************************/
/*          d1_cards_jcards
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 2nd March 2004
/**********************************/
/*  #include
/*  Constants for cards
/**********************************/

//Marker to indicate highest card ID number + 1
const int CARD_MAX_ID                       = 98;

//Generator cards
const int CARD_GENERATOR_GENERIC            = 5;

//Mythical cards
const int CARD_MYTHICAL_ARKNETH             = 89;
const int CARD_MYTHICAL_DEEKIN              = 91;
const int CARD_MYTHICAL_JYSIRAEL            = 90;
const int CARD_MYTHICAL_SAESHEN             = 92;

//Spell cards
const int CARD_SPELL_ANGELIC_CHOIR          = 75;
const int CARD_SPELL_ARMOUR                 = 19;
const int CARD_SPELL_ASSASSIN               = 12;
const int CARD_SPELL_BOOMERANG              = 78;
const int CARD_SPELL_COUNTERSPELL           = 80;
const int CARD_SPELL_DEATH_PACT             = 41;
const int CARD_SPELL_DISPEL_MAGIC           = 39;
const int CARD_SPELL_ELIXIR_OF_LIFE         = 22;
const int CARD_SPELL_ENERGY_DISRUPTION      = 76;
const int CARD_SPELL_EYE_OF_THE_BEHOLDER    = 77;
const int CARD_SPELL_FIREBALL               = 20;
const int CARD_SPELL_FIRE_SHIELD            = 71;
const int CARD_SPELL_FLUX                   = 42;
const int CARD_SPELL_HEALING_LIGHT          = 13;
const int CARD_SPELL_HIGHER_CALLING         = 96;
const int CARD_SPELL_HOLY_VENGEANCE         = 18;
const int CARD_SPELL_LIFE_DRAIN             = 14;
const int CARD_SPELL_LIGHTNING_BOLT         = 21;
const int CARD_SPELL_MIND_CONTROL           = 73;
const int CARD_SPELL_MIND_OVER_MATTER       = 79;
const int CARD_SPELL_PARALYZE               = 38;
const int CARD_SPELL_POTION_OF_HEROISM      = 72;
const int CARD_SPELL_POWER_STREAM           = 97;
const int CARD_SPELL_RESURRECT              = 57;
const int CARD_SPELL_SABOTAGE               = 25;
const int CARD_SPELL_SCORCHED_EARTH         = 56;
const int CARD_SPELL_SIMULACRUM             = 74;
const int CARD_SPELL_VORTEX                 = 55;
const int CARD_SPELL_WARP_REALITY           = 51;
const int CARD_SPELL_WRATH_OF_THE_HORDE     = 54;

//Stone cards
const int CARD_STONE_SOLAR                  = 49;

//Summon cards
const int CARD_SUMMON_ANGELIC_DEFENDER      = 70;
const int CARD_SUMMON_ANGELIC_HEALER        = 68;
const int CARD_SUMMON_ANGELIC_LIGHT         = 66;
const int CARD_SUMMON_ARCHANGEL             = 69;
const int CARD_SUMMON_ATLANTIAN             = 1;
const int CARD_SUMMON_AVENGING_ANGEL        = 67;
const int CARD_SUMMON_BEAR                  = 35;
const int CARD_SUMMON_BEHOLDER              = 64;
const int CARD_SUMMON_BONE_GOLEM            = 26;
const int CARD_SUMMON_BUGBEAR_BEZERKERS     = 4;
const int CARD_SUMMON_BULETTE               = 93;
const int CARD_SUMMON_CHAOS_WITCH           = 59;
const int CARD_SUMMON_COUGAR                = 36;
const int CARD_SUMMON_COW                   = 33;
const int CARD_SUMMON_DEMON_KNIGHT          = 81;
const int CARD_SUMMON_DRAGON                = 65;
const int CARD_SUMMON_DRUID                 = 30;
const int CARD_SUMMON_DWARVEN_DEFENDER      = 3;
const int CARD_SUMMON_ELDER_FIRE_ELEMENTAL  = 9;
const int CARD_SUMMON_FERAL_RAT             = 32;
const int CARD_SUMMON_FAIRY_DRAGON          = 60;
const int CARD_SUMMON_GIANT_SPIDER          = 6;
const int CARD_SUMMON_GOBLIN                = 2;
const int CARD_SUMMON_GOBLIN_CROSSBOW       = 31;
const int CARD_SUMMON_GOBLIN_SHAMAN         = 23;
const int CARD_SUMMON_GOBLIN_WARLORD        = 29;
const int CARD_SUMMON_GOBLIN_WITCHDOCTOR    = 61;
const int CARD_SUMMON_HILL_GIANT            = 8;
const int CARD_SUMMON_HOOK_HORROR           = 62;
const int CARD_SUMMON_INTELLECT_DEVOURER    = 58;
const int CARD_SUMMON_KOBOLD                = 85;
const int CARD_SUMMON_KOBOLD_CHIEF          = 84;
const int CARD_SUMMON_KOBOLD_ENGINEER       = 87;
const int CARD_SUMMON_KOBOLD_KAMIKAZE       = 86;
const int CARD_SUMMON_KOBOLD_POGOSTICK      = 88;
const int CARD_SUMMON_LICH                  = 27;
const int CARD_SUMMON_LOREMASTER            = 43;
const int CARD_SUMMON_MAIDEN_OF_PARADISE    = 44;
const int CARD_SUMMON_PAIN_GOLEM            = 45;
const int CARD_SUMMON_PHASE_SPIDER          = 82;
const int CARD_SUMMON_PIT_FIEND             = 46;
const int CARD_SUMMON_PLAGUE_BEARER         = 40;
const int CARD_SUMMON_RAT                   = 17;
const int CARD_SUMMON_RAT_KING              = 28;
const int CARD_SUMMON_REVENANT              = 83;
const int CARD_SUMMON_SEA_HAG               = 94;
const int CARD_SUMMON_SEWER_RAT             = 63;
const int CARD_SUMMON_SHADOW_ASSASSIN       = 47;
const int CARD_SUMMON_SKELETAL_ARCHER       = 24;
const int CARD_SUMMON_SKELETAL_WARRIOR      = 48;
const int CARD_SUMMON_SPIRIT_GUARDIAN       = 10;
const int CARD_SUMMON_STEEL_GUARDIAN        = 11;
const int CARD_SUMMON_TROGLODYTE            = 95;
const int CARD_SUMMON_UMBER_HULK            = 7;
const int CARD_SUMMON_VAMPIRE               = 16;
const int CARD_SUMMON_VAMPIRE_MASTER        = 50;
const int CARD_SUMMON_WHITE_STAG            = 37;
const int CARD_SUMMON_WHITE_WOLF            = 52;
const int CARD_SUMMON_WOLF                  = 34;
const int CARD_SUMMON_ZOMBIE                = 15;
const int CARD_SUMMON_ZOMBIE_LORD           = 53;
