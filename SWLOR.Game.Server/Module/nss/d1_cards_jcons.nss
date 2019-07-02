/**********************************/
/*          d1_cards_jcons
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Constants
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

//Game and managing areas
object MODULE = GetModule();

//Card game AI difficulty levels
const int CARD_AI_DIFFICULTY_TRAINING       = 1;
const int CARD_AI_DIFFICULTY_EASY           = 2;
const int CARD_AI_DIFFICULTY_NORMAL         = 3;
const int CARD_AI_DIFFICULTY_HARD           = 4;

//Card weight values for rating of card priority
const int CARD_AI_WEIGHT_LOSING_CARD        = -100;
const int CARD_AI_WEIGHT_HIGH_LOSS          = -25;
const int CARD_AI_WEIGHT_MEDIUM_LOSS        = -10;
const int CARD_AI_WEIGHT_WORTHY_LOSS        = -5;
const int CARD_AI_WEIGHT_LOW_LOSS           = -2;
const int CARD_AI_WEIGHT_NOT_RECOMMENDED    = -1;
const int CARD_AI_WEIGHT_NEGLIGIBLE         = 1;
const int CARD_AI_WEIGHT_LOW_IMPACT         = 2;
const int CARD_AI_WEIGHT_WORTHY             = 5;
const int CARD_AI_WEIGHT_MEDIUM_IMPACT      = 10;
const int CARD_AI_WEIGHT_HIGH_IMPACT        = 25;
const int CARD_AI_WEIGHT_WINNING_CARD       = 100;

//Types of scan for creatures; used for spell targetting
const int CARD_CREATURE_SCAN_HIGHEST_ATTACK         = 1;
const int CARD_CREATURE_SCAN_HIGHEST_ATTACK_DEAD    = 2;
const int CARD_CREATURE_SCAN_HIGHEST_ATTACK_LIVING  = 3;
const int CARD_CREATURE_SCAN_HIGHEST_ATTACK_UNDEAD  = 4;
const int CARD_CREATURE_SCAN_HIGHEST_DEFEND         = 5;
const int CARD_CREATURE_SCAN_HIGHEST_DEFEND_DEAD    = 6;
const int CARD_CREATURE_SCAN_HIGHEST_DEFEND_LIVING  = 7;
const int CARD_CREATURE_SCAN_HIGHEST_DEFEND_UNDEAD  = 8;
const int CARD_CREATURE_SCAN_HIGHEST_LIFE           = 9;
const int CARD_CREATURE_SCAN_HIGHEST_LIFE_DEAD      = 10;
const int CARD_CREATURE_SCAN_HIGHEST_LIFE_LIVING    = 11;
const int CARD_CREATURE_SCAN_HIGHEST_LIFE_UNDEAD    = 12;
const int CARD_CREATURE_SCAN_LOWEST_ATTACK          = 13;
const int CARD_CREATURE_SCAN_LOWEST_ATTACK_DEAD     = 14;
const int CARD_CREATURE_SCAN_LOWEST_ATTACK_LIVING   = 15;
const int CARD_CREATURE_SCAN_LOWEST_ATTACK_UNDEAD   = 16;
const int CARD_CREATURE_SCAN_LOWEST_DEFEND          = 17;
const int CARD_CREATURE_SCAN_LOWEST_DEFEND_DEAD     = 18;
const int CARD_CREATURE_SCAN_LOWEST_DEFEND_LIVING   = 19;
const int CARD_CREATURE_SCAN_LOWEST_DEFEND_UNDEAD   = 20;
const int CARD_CREATURE_SCAN_LOWEST_LIFE            = 21;
const int CARD_CREATURE_SCAN_LOWEST_LIFE_DEAD       = 22;
const int CARD_CREATURE_SCAN_LOWEST_LIFE_LIVING     = 23;
const int CARD_CREATURE_SCAN_LOWEST_LIFE_UNDEAD     = 24;

//Card game NPC deck types
const int CARD_DECK_TYPE_ANGELS             = 0x00000001;
const int CARD_DECK_TYPE_ANIMALS            = 0x00000002;
const int CARD_DECK_TYPE_BIG_CREATURES      = 0x00000004;
const int CARD_DECK_TYPE_FAST_CREATURES     = 0x00000008;
const int CARD_DECK_TYPE_GOBLINS            = 0x00000010;
const int CARD_DECK_TYPE_KOBOLDS            = 0x00000020;
const int CARD_DECK_TYPE_RANDOM             = 0x00000040;
const int CARD_DECK_TYPE_RATS               = 0x00000080;
const int CARD_DECK_TYPE_SPELLS             = 0x00000100;
const int CARD_DECK_TYPE_UNDEAD             = 0x00000200;
const int CARD_DECK_TYPE_WOLVES             = 0x00000400;

//Default values for normal game.
const int CARD_GAME_DEFAULT_GRACE_PERIOD    = 1;
const int CARD_GAME_DEFAULT_MAX_CARDS_TYPE  = 4;
const int CARD_GAME_DEFAULT_MAX_DECK_SIZE   = 40;
const int CARD_GAME_DEFAULT_MAX_GENERATORS  = 100;
const int CARD_GAME_DEFAULT_MAX_HAND_SIZE   = 7;

//Possible ends to the game
const int CARD_GAME_END_CHEAT_ATTACK        = 1;
const int CARD_GAME_END_CHEAT_SPELL         = 2;
const int CARD_GAME_END_CONCEDE             = 3;
const int CARD_GAME_END_DRAW                = 4;
const int CARD_GAME_END_RESULT_DRAW         = 5;
const int CARD_GAME_END_RESULT_WIN          = 6;
const int CARD_GAME_END_RESULT_LOSE         = 7;

//Music change types for each game event
const int CARD_GAME_MUSIC_END_MP            = 1;
const int CARD_GAME_MUSIC_END_SP_LOSE       = 2;
const int CARD_GAME_MUSIC_END_SP_WIN        = 3;
const int CARD_GAME_MUSIC_START             = 4;

//Player rating values
const int CARD_PLAYER_RATING_APPRENTICE     = 5;
const int CARD_PLAYER_RATING_NOVICE         = 15;
const int CARD_PLAYER_RATING_PLAYER         = 25;
const int CARD_PLAYER_RATING_EXPERT         = 35;
const int CARD_PLAYER_RATING_MASTER         = 45;
const int CARD_PLAYER_RATING_COMPETITOR     = 100;
const int CARD_PLAYER_RATING_GRANDMASTER    = 200;
const int CARD_PLAYER_RATING_ELITE          = 500;

//Player rating calculation values
const int CARD_PLAYER_RATING_APPRENTICE_X   = 1;
const int CARD_PLAYER_RATING_NOVICE_X       = 2;
const int CARD_PLAYER_RATING_PLAYER_X       = 3;
const int CARD_PLAYER_RATING_EXPERT_X       = 4;
const int CARD_PLAYER_RATING_MASTER_X       = 5;

//In a purchase, the number of lands gained will be a minimum of 1 land per
//every # cards.  For all other cards, their rarity chance of showing up is
//*-in-% chance, where % is the CARD_PURCHASE_RARITY_* value, and the * is a
//value of the total number of cards sold so far divided by #.  When a purchase
//is made, the total number of cards sold decreases by * x # (each rarity has
//its own total)
const int CARD_PURCHASE_LANDS               = 5;
const int CARD_PURCHASE_ULTRA_RARES         = 10000;
const int CARD_PURCHASE_VERY_RARES          = 50;
const int CARD_PURCHASE_RARES               = 20;
const int CARD_PURCHASE_UNCOMMONS           = 8;

//This is the 1-in-# chance of a card being available for purchase.  There is a fixed 1-in-(2x#)
const int CARD_PURCHASE_RARITY_ULTRA_RARES  = 30000;
const int CARD_PURCHASE_RARITY_VERY_RARES   = 25;
const int CARD_PURCHASE_RARITY_RARES        = 10;
const int CARD_PURCHASE_RARITY_UNCOMMONS    = 4;

//If the number of cards being purchased is equal to this or higher, then a new
//deck object will be created and placed on the purchaser
const int CARD_PURCHASE_DECK                = 25;

//Used to determine the number of cards generated in a purchase
const int CARD_RARITY_COMMON                = 1;
const int CARD_RARITY_UNCOMMON              = 2;
const int CARD_RARITY_RARE                  = 3;
const int CARD_RARITY_VERY_RARE             = 4;
const int CARD_RARITY_ULTRA_RARE            = 5;

//Variant rules
const int CARD_RULE_DECK_20                 = 0x00000001;
const int CARD_RULE_DECK_30                 = 0x00000002;
const int CARD_RULE_DECK_40                 = 0x00000004;
const int CARD_RULE_DECK_50                 = 0x00000008;
const int CARD_RULE_DECK_60                 = 0x00000010;
const int CARD_RULE_DRAW_3                  = 0x00000020;
const int CARD_RULE_DRAW_5                  = 0x00000040;
const int CARD_RULE_DRAW_7                  = 0x00000080;
const int CARD_RULE_DRAW_9                  = 0x00000100;
const int CARD_RULE_LAST_DRAW_CONTINUE      = 0x00000200;
const int CARD_RULE_LAST_DRAW_DEATH         = 0x00000400;
const int CARD_RULE_LIMIT_1                 = 0x00000800;
const int CARD_RULE_LIMIT_2                 = 0x00001000;
const int CARD_RULE_LIMIT_3                 = 0x00002000;
const int CARD_RULE_LIMIT_4                 = 0x00004000;
const int CARD_RULE_LIMIT_5                 = 0x00008000;
const int CARD_RULE_LIMIT_8                 = 0x00010000;
const int CARD_RULE_LIMIT_10                = 0x00020000;
const int CARD_RULE_NORMAL                  = 0x00040000;
const int CARD_RULE_RESTRICT_2              = 0x00080000;
const int CARD_RULE_RESTRICT_3              = 0x00100000;
const int CARD_RULE_RESTRICT_4              = 0x00200000;
const int CARD_RULE_RESTRICT_X              = 0x00400000;
const int CARD_RULE_TRADE_ALL               = 0x00800000;
const int CARD_RULE_TRADE_ONE               = 0x01000000;

//Card scan types
const int CARD_SCAN_CARD_ID                 = 1;
const int CARD_SCAN_CARD_SUBTYPE            = 2;
const int CARD_SCAN_CREATURE_SCAN           = 3;
const int CARD_SCAN_HAS_EFFECT              = 4;
const int CARD_SCAN_IS_CLONE                = 5;
const int CARD_SCAN_IS_MYTHICAL             = 6;
const int CARD_SCAN_NO_EFFECT               = 7;

//Card sources
const int CARD_SOURCE_ALL_CARDS             = 1;
const int CARD_SOURCE_ANTE_PLAYER_1         = 2;
const int CARD_SOURCE_ANTE_PLAYER_2         = 3;
const int CARD_SOURCE_COLLECTION            = 4;
const int CARD_SOURCE_DECK                  = 5;
const int CARD_SOURCE_DISCARD_PLAYER_1      = 6;
const int CARD_SOURCE_DISCARD_PLAYER_2      = 7;
const int CARD_SOURCE_GAME_PLAYER_1         = 8;
const int CARD_SOURCE_GAME_PLAYER_2         = 9;

//Card subtypes
const int CARD_SUBTYPE_SPELL_CONTINGENCY            = 1;
const int CARD_SUBTYPE_SPELL_BOOSTER                = 2;
const int CARD_SUBTYPE_SPELL_BOOSTER_GLOBAL         = 3;
const int CARD_SUBTYPE_SPELL_BOOSTER_MULTI          = 4;
const int CARD_SUBTYPE_SPELL_ENCHANT                = 5;
const int CARD_SUBTYPE_SPELL_ENCHANT_GLOBAL         = 6;
const int CARD_SUBTYPE_SPELL_ENCHANT_MULTI          = 7;
const int CARD_SUBTYPE_SPELL_INSTANT                = 8;
const int CARD_SUBTYPE_SPELL_INSTANT_GLOBAL         = 9;
const int CARD_SUBTYPE_SPELL_INSTANT_MULTI          = 10;
const int CARD_SUBTYPE_SPELL_PENALTY                = 11;
const int CARD_SUBTYPE_SPELL_PENALTY_GLOBAL         = 12;
const int CARD_SUBTYPE_SPELL_PENALTY_MULTI          = 13;
const int CARD_SUBTYPE_STONE_DECK                   = 14;
const int CARD_SUBTYPE_SUMMON_ABERRATION            = 15;
const int CARD_SUBTYPE_SUMMON_ANGEL                 = 16;
const int CARD_SUBTYPE_SUMMON_ANIMAL                = 17;
const int CARD_SUBTYPE_SUMMON_BEAST                 = 18;
const int CARD_SUBTYPE_SUMMON_BEHOLDER              = 19;
const int CARD_SUBTYPE_SUMMON_BUGBEAR               = 20;
const int CARD_SUBTYPE_SUMMON_DEMON                 = 21;
const int CARD_SUBTYPE_SUMMON_DRAGON                = 22;
const int CARD_SUBTYPE_SUMMON_DWARF                 = 23;
const int CARD_SUBTYPE_SUMMON_ELEMENTAL_AIR         = 24;
const int CARD_SUBTYPE_SUMMON_ELEMENTAL_EARTH       = 25;
const int CARD_SUBTYPE_SUMMON_ELEMENTAL_FIRE        = 26;
const int CARD_SUBTYPE_SUMMON_ELEMENTAL_WATER       = 27;
const int CARD_SUBTYPE_SUMMON_FEY                   = 28;
const int CARD_SUBTYPE_SUMMON_GIANT                 = 29;
const int CARD_SUBTYPE_SUMMON_GOBLIN                = 30;
const int CARD_SUBTYPE_SUMMON_GOLEM                 = 31;
const int CARD_SUBTYPE_SUMMON_GUARDIAN              = 32;
const int CARD_SUBTYPE_SUMMON_HUMAN                 = 33;
const int CARD_SUBTYPE_SUMMON_KOBOLD                = 34;
const int CARD_SUBTYPE_SUMMON_LICH                  = 35;
const int CARD_SUBTYPE_SUMMON_RAT                   = 36;
const int CARD_SUBTYPE_SUMMON_SKELETON              = 37;
const int CARD_SUBTYPE_SUMMON_SHADE                 = 38;
const int CARD_SUBTYPE_SUMMON_SPIDER                = 39;
const int CARD_SUBTYPE_SUMMON_UNDEAD                = 40;
const int CARD_SUBTYPE_SUMMON_VAMPIRE               = 41;
const int CARD_SUBTYPE_SUMMON_WITCH                 = 42;
const int CARD_SUBTYPE_SUMMON_WOLF                  = 43;
const int CARD_SUBTYPE_SUMMON_ZOMBIE                = 44;

//Card types
const int CARD_TYPE_GENERATOR               = 1;
const int CARD_TYPE_MYTHICAL                = 2;
const int CARD_TYPE_SPELL                   = 3;
const int CARD_TYPE_STONE                   = 4;
const int CARD_TYPE_SUMMON                  = 5;

//Percentage amounts of CYCLE_TIME that the AI will play its cards in
const float CARD_AI_PLAY_GENERATOR          = 0.02;
const float CARD_AI_PLAY_CARD_1             = 0.3;
const float CARD_AI_PLAY_CARD_2             = 0.6;
const float CARD_AI_PLAY_CARD_3             = 0.9;

//Size of play field; should be based on the area construction
const float CARD_PLAY_DISTANCE              = 9.0f;

//Used for card layouts
const float CARD_SIZE_X                     = 2.0f;
const float CARD_SIZE_Y                     = 3.0f;

//Number of seconds between upkeep cycles
const float CYCLE_TIME                      = 12.0f;
