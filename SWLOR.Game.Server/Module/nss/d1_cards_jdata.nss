/**********************************/
/*          d1_cards_jdata
/*
/*  Created By: Robert Straughan
/**********************************/
/*  Created For: Adam Miller
/*  Created On: 18th February 2004
/**********************************/
/*  #include
/*  Card database.  See the file
/*  d1_cards_jdec for explanation
/*  of card entries.
/**********************************/
/*  YOU DO NOT NEED TO ALTER ANY
/*  OF THIS TO ADD NEW CARDS
/**********************************/

struct sCard GetCardInfo (int nCard)
{
    struct sCard sReturn;

    if (nCard < 0 || nCard > CARD_MAX_ID)
        return sReturn;

    sReturn.nCard = nCard;

    switch (nCard)
    {
        case CARD_GENERATOR_GENERIC:
            sReturn.sName = "Magic Generator";
            sReturn.sDesc = "Magic generators are key to your success in this game.";
            sReturn.sGame = "In order to generate magic to cast your spells, these must be put into play." +
                            "  You may only put one in play each cycle, so you won't be able to immediately empty your hand once play starts." +
                            "  Also, the magic that they generate slowly recharges, so cast your spells carefully.";
            sReturn.nType = CARD_TYPE_GENERATOR;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            break;

        case CARD_MYTHICAL_ARKNETH:
            sReturn.sName = "Ark'Neth";
            sReturn.sDesc = "Champion of the Blood Wars, a striding figure of destruction, Ark'Neth is capable of laying waste to all those around him.";
            sReturn.sGame = "Ark'Neth can be sacrificed to inflict status effects upon all enemies within 10m." +
                            "  The status inflicted is determined by the creature's distance in alignment from Chaotic Evil." +
                            "  Does not die when triggered." +
                            "  Only one Ark'Neth may be in play at one time.";
            sReturn.nType = CARD_TYPE_MYTHICAL;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_DEMON;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_ULTRA_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 5;
            sReturn.nDefend = 5;
            sReturn.nSacrifice = TRUE;
            sReturn.nCombat = TRUE;
            break;

        case CARD_MYTHICAL_DEEKIN:
            sReturn.sName = "Deekin";
            sReturn.sDesc = "";
            sReturn.sGame = "Can trigger his sacrifice effect to cause all standard kobolds on the playing field to become Kobold Pogosticks.  This effect does not kill Deekin.";
            sReturn.nType = CARD_TYPE_MYTHICAL;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_KOBOLD;
            sReturn.nDeck = CARD_DECK_TYPE_KOBOLDS;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_ULTRA_RARE;
            sReturn.nMagic = 2;
            sReturn.nAttack = 1;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_MYTHICAL_JYSIRAEL:
            sReturn.sName = "Jysirael";
            sReturn.sDesc = "An angel of light, a warrior for hope and all that is good, Jysirael stands as a beacon of light against the darkness of evil.  Only one Jysirael can be in play at any time.";
            sReturn.sGame = "Can trigger a sacrifice effect to fully heal all friendly summons within a 10m radius.  Using this ability does not kill Jysirael, but does reduce her hit points by half.";
            sReturn.nType = CARD_TYPE_MYTHICAL;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANGEL;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_ULTRA_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 5;
            sReturn.nDefend = 5;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_MYTHICAL_SAESHEN:
            sReturn.sName = "Saeshen";
            sReturn.sDesc = "Saeshen is the queen of the jungle, the ruler of the faeries, and a magical trickster of legendary status.";
            sReturn.sGame = "During the upkeep phase, automatically brings the topmost fairy dragon in the discard pile back into play.";
            sReturn.nType = CARD_TYPE_MYTHICAL;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_FEY;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_ULTRA_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 0;
            sReturn.nDefend = 1;
            break;

        case CARD_SPELL_ANGELIC_CHOIR:
            sReturn.sName = "Angelic Choir";
            sReturn.sDesc = "";
            sReturn.sGame = "Enchants all angels with an attack and defence boosting effect, that increases in effect when more angels are on the field.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT_MULTI;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_ARMOUR:
            sReturn.sName = "Armour";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell is first cast on your avatar, then it finds the toughest of your creatures and boosts their defense considerably.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 10;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_ASSASSIN:
            sReturn.sName = "Assassin";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell will cause the strongest of your enemy's creatures to be instantly slain.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 30;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 3;
            break;

        case CARD_SPELL_BOOMERANG:
            sReturn.sName = "Boomerang";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell will cause the strongest of your enemy's creatures to be instantly returned to their hand.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_COUNTERSPELL:
            sReturn.sName = "Counterspell";
            sReturn.sDesc = "";
            sReturn.sGame = "Automatically counters the next spell.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_CONTINGENCY;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 3;
            break;

        case CARD_SPELL_DEATH_PACT:
            sReturn.sName = "Death Pact";
            sReturn.sDesc = "";
            sReturn.sGame = "When cast, up to five corpses are cleared from the battlefield, their essence in life converted into healing for the caster." +
                            "  If five corpses are on the field, they will all be consumed regardless of the player's health.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_MULTI;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            break;

        case CARD_SPELL_DISPEL_MAGIC:
            sReturn.sName = "Dispel Magic";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell removes all enchantments from all creatures in the game, as well as any global card-spells in play.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 20;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 3;
            break;

        case CARD_SPELL_ELIXIR_OF_LIFE:
            sReturn.sName = "Elixir of Life";
            sReturn.sDesc = "";
            sReturn.sGame = "When cast, all available magic energy flows into the spell, healing your avatar in proportion to the number of powered generators you possess.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 5;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nBoost = TRUE;
            break;

        case CARD_SPELL_ENERGY_DISRUPTION:
            sReturn.sName = "Energy Disruption";
            sReturn.sDesc = "";
            sReturn.sGame = "When cast, each turn, both players will be drained of an amount of power equal to the maximum hand size minus the current number of cards in hand.  Additional castings will add one to this value.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_EYE_OF_THE_BEHOLDER:
            sReturn.sName = "Eye of the Beholder";
            sReturn.sDesc = "";
            sReturn.sGame = "All beholders that the caster controls will use special eye ray attacks." +
                            "  These will target individual opponents, starting with the highest attack creature the enemy controls." +
                            "  If the enemy has less creatures than you have beholders, they will target the avatar.";
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_MULTI;
            sReturn.nCost = 40;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_FIREBALL:
            sReturn.sName = "Fireball";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell launches a fireball at your enemy's avatar, damaging all nearby enemy creatures.  " +
                            "This spell automatically uses up all available magic you possess.  With each available generator, the power of the blast increases.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_MULTI;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 5;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nBoost = TRUE;
            break;

        case CARD_SPELL_FIRE_SHIELD:
            sReturn.sName = "Fire Shield";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell creates a ring of fire around the target that deals 1d4 + 1 fire damage to any who strike the recipient." +
                            "  Targets start with the avatar, and then move on to the highest defence creature you own.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 3;
            break;

        case CARD_SPELL_FLUX:
            sReturn.sName = "Flux";
            sReturn.sDesc = "";
            sReturn.sGame = "This terrible spell was once thought lost forever.  Once cast, only a Dispel Magic can remove it." +
                            "  Each turn, damage is dealt to each players' avatars and creatures equal to the total number of power generators in play on both sides." +
                            "  Should more than one Flux be cast, even more damage will be dealt.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 125;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 4;
            break;

        case CARD_SPELL_HEALING_LIGHT:
            sReturn.sName = "Healing Light";
            sReturn.sDesc = "";
            sReturn.sGame = "Casting this spell will heal your avatar if it is injured.  If it isn't hurt, it will heal the most damaged creature instead.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_MULTI;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 20;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_HIGHER_CALLING:
            sReturn.sName = "Higher Calling";
            sReturn.sDesc = "";
            sReturn.sGame = "The Higher Calling spell lays an enchantment across the battlefield, affecting all powerful creatures with a siren's call that slows them in battle.  The lesser beasts are unaffected and Dispel Magic removes the enchantment.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 3;
            break;

        case CARD_SPELL_HOLY_VENGEANCE:
            sReturn.sName = "Holy Vengeance";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell finds the strongest of your creatures and boosts their attack considerably.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 2;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_LIFE_DRAIN:
            sReturn.sName = "Life Drain";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell drains a small amount of life from the living creatures in play and gives their energy to your avatar." +
                            "  The more magic generators available, the more creatures can be affected.  Note that it doesn't work on undead creatures.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_MULTI;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_LIGHTNING_BOLT:
            sReturn.sName = "Lightning Bolt";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell fires a single lightning bolt at your opponent.  If any enemy creatures are on the battlefield, they are targeted first." +
                            "  If there are none, the blast goes directly at your opponent's avatar.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 3;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_MIND_CONTROL:
            sReturn.sName = "Mind Control";
            sReturn.sDesc = "";
            sReturn.sGame = "When cast, the highest attack rating creature of the opponent switches sides." +
                            "  The cost of this spell is the cost of the target creature." +
                            "  If not enough power is available for the highest attack rating creature, finds one with a lower cost.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nBoost = TRUE;
            break;

        case CARD_SPELL_MIND_OVER_MATTER:
            sReturn.sName = "Mind Over Matter";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell returns one card from the discard pile to the casters hand.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_PARALYZE:
            sReturn.sName = "Paralyse";
            sReturn.sDesc = "";
            sReturn.sGame = "When cast, the most powerful of your opponent's creatures will become paralyzed.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_PENALTY;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 40;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_POTION_OF_HEROISM:
            sReturn.sName = "Potion Of Heroism";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell grants the recipient a +5 attack bonus, +50 temporary hit points, and a haste effect for two game rounds.  Targets highest attack creatures first.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_BOOSTER;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_POWER_STREAM:
            sReturn.sName = "Power Stream";
            sReturn.sDesc = "When this enchantment is in place, all players draw an additional card.";
            sReturn.sGame = "The effects of this enchantment are cumulative.  For example, if cast twice, two extra cards are drawn.  A Dispel Magic spell will remove all Power Stream effects in play.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_RATS;
            sReturn.nCost = 50;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_RESURRECT:
            sReturn.sName = "Resurrect";
            sReturn.sDesc = "";
            sReturn.sGame = "This spell raises the most powerful of your dead creatures to fight in battle once again.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_ANGELS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_UNDEAD +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 40;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 2;
            break;

        case CARD_SPELL_SABOTAGE:
            sReturn.sName = "Sabotage";
            sReturn.sDesc = "";
            sReturn.sGame = "Casting this spell results in the destruction of one of your opponent's magic generators.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 3;
            break;

        case CARD_SPELL_SCORCHED_EARTH:
            sReturn.sName = "Scorched Earth";
            sReturn.sDesc = "";
            sReturn.sGame = "Casting this spell kills all creatures and removes all corpses from play.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 4;
            break;

        case CARD_SPELL_SIMULACRUM:
            sReturn.sName = "Simulacrum";
            sReturn.sDesc = "";
            sReturn.sGame = "Creates a clone of the highest attack creature the opponent has in play for you.  Dispel Magic will destroy the clone.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            break;

        case CARD_SPELL_VORTEX:
            sReturn.sName = "Vortex";
            sReturn.sDesc = "This whirlwind disrupts magic across a wide area.";
            sReturn.sGame = "While Vortex is in play, only half of both players' available magic generators will be restored of power at the beginning of each turn." +
                            "  Additional Vortex effects will increase the reduction by increasing the size of division." +
                            "  Ie. the first Vortex will 1/2 the available power, the second will reduce it to 1/3, then 1/4, 1/5, etc.  A Dispel Magic spell will remove all Vortex effects in play.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_ENCHANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_RATS;
            sReturn.nCost = 250;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            break;

        case CARD_SPELL_WARP_REALITY:
            sReturn.sName = "Warp Reality";
            sReturn.sDesc = "";
            sReturn.sGame = "When cast, ownership of all creatures is swapped.  Additionally, all of the caster's magic generators are destroyed.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_INSTANT_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 1;
            break;

        case CARD_SPELL_WRATH_OF_THE_HORDE:
            sReturn.sName = "Wrath of the Horde";
            sReturn.sDesc = "";
            sReturn.sGame = "Every creature the caster controls gets a boost to attack and defense equal to the number of creatures the caster controls." +
                            "  Needless to say, for players with large numbers of creatures, this spell is quite powerful.";
            sReturn.nType = CARD_TYPE_SPELL;
            sReturn.nSubType = CARD_SUBTYPE_SPELL_BOOSTER_GLOBAL;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS +
                            CARD_DECK_TYPE_KOBOLDS +
                            CARD_DECK_TYPE_RATS +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 250;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 4;
            break;

        case CARD_STONE_SOLAR:
            sReturn.sName = "Solar Stone";
            sReturn.sDesc = "";
            sReturn.sGame = "Each turn, a solar stone will destroy a random card from the opponent's undrawn cards." +
                            "  Also, two points of magic are required to maintain the stone." +
                            "  Should it not be available, the solar stone will be destroyed.";
            sReturn.nType = CARD_TYPE_STONE;
            sReturn.nSubType = CARD_SUBTYPE_STONE_DECK;
            sReturn.nDeck = CARD_DECK_TYPE_SPELLS;
            sReturn.nCost = 300;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            break;

        case CARD_SUMMON_ANGELIC_DEFENDER:
            sReturn.sName = "Angelic Defender";
            sReturn.sDesc = "";
            sReturn.sGame = "Angelic defenders boost the defense of nearby angels.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANGEL;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 80;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 3;
            sReturn.nDefend = 4;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_ANGELIC_HEALER:
            sReturn.sName = "Angelic Healer";
            sReturn.sDesc = "";
            sReturn.sGame = "Though comparatively weak, these angels will heal your avatar a small amount each turn.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANGEL;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 5;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 0;
            sReturn.nDefend = 2;
            break;

        case CARD_SUMMON_ANGELIC_LIGHT:
            sReturn.sName = "Angelic Light";
            sReturn.sDesc = "";
            sReturn.sGame = "These tiny lights are the spirits of good souls who have given their lives to the cause of righteousness.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANGEL;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_ARCHANGEL:
            sReturn.sName = "Archangel";
            sReturn.sDesc = "";
            sReturn.sGame = "This valliant angelic warrior rallies other angels to her cause, boosting their attack abilities considerably.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANGEL;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_ATLANTIAN:
            sReturn.sName = "Atlantian";
            sReturn.sDesc = "";
            sReturn.sGame = "Atlantians are spawned from the depths of the sea.  When cast, any free magic generators are used to boost the creatures abilities up to a maximum of five.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ELEMENTAL_WATER;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 60;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            sReturn.nBoost = TRUE;
            break;

        case CARD_SUMMON_AVENGING_ANGEL:
            sReturn.sName = "Avenging Angel";
            sReturn.sDesc = "These divine beings are stalwart defenders of the faith.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANGEL;
            sReturn.nDeck = CARD_DECK_TYPE_ANGELS;
            sReturn.nCost = 20;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 3;
            sReturn.nAttack = 3;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_BEAR:
            sReturn.sName = "Bear";
            sReturn.sDesc = "These forest creatures are fierce protectors of their dens and best avoided.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANIMAL;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 5;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 3;
            sReturn.nAttack = 2;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_BEHOLDER:
            sReturn.sName = "Beholder";
            sReturn.sDesc = "Beholders strike fear into the hearts of their opponents, attacking with their massive jaws and stunning gaze.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_BEHOLDER;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 80;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 4;
            sReturn.nAttack = 4;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_BONE_GOLEM:
            sReturn.sName = "Bone Golem";
            sReturn.sDesc = "Bone Golems are massive undead constructs, deadly and rugged.";
            sReturn.sGame = "Their presence also grants the lower skeletons a bonus in combat if they are nearby.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOLEM;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_BULETTE:
            sReturn.sName = "Bulette";
            sReturn.sDesc = "The bulette is ponderous and well protected, an excellent creature to slow down the opposition.  Their bite is particularly effective against animals due to a unique venom.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_BEAST;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 4;
            sReturn.nAttack = 1;
            sReturn.nDefend = 6;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_BUGBEAR_BEZERKERS:
            sReturn.sName = "Bugbear Bezerkers";
            sReturn.sDesc = "These fierce warriors care little about their own wounds, merely plunging into the fight.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_BUGBEAR;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 25;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_CHAOS_WITCH:
            sReturn.sName = "Chaos Witch";
            sReturn.sDesc = "Chaos witches care little for what sides they fight on.";
            sReturn.sGame = "Should one damage an opponent, they immediately switch sides.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_WITCH;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 1;
            sReturn.nAttack = 3;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_COUGAR:
            sReturn.sName = "Cougar";
            sReturn.sDesc = "Cougars have been known to attack even the largest of creatures.";
            sReturn.sGame = "The cougar can be sacrificed to deal 10 points of damage to all enemy creatures.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANIMAL;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 80;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 4;
            sReturn.nDefend = 3;
            sReturn.nSacrifice = TRUE;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_COW:
            sReturn.sName = "Cow";
            sReturn.sDesc = "Most consider cows laughable beasts, but they do have a mighty kick when enraged.";
            sReturn.sGame = "The cow can be sacrificed to heal 1 point of damage to your avatar for every card you have in your hand.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANIMAL;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nSacrifice = TRUE;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_DEMON_KNIGHT:
            sReturn.sName = "Demon Knight";
            sReturn.sDesc = "";
            sReturn.sGame = "Demon Knight can be sacrificed to inflict status effects upon all enemies within 10m.  The status inflicted is determined by the creature's distance in alignment from Chaotic Evil.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_DEMON;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 5;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_DRAGON:
            sReturn.sName = "Dragon";
            sReturn.sDesc = "These immense creatures are incredibly tough and deadly.  Their immense size comes with a price, however.";
            sReturn.sGame = "When summoned, they will feed upon whatever morsels are handy.  50 hit points worth of creatures will be devoured.  If none are available, the dragon will take a bit directly out of the player's avatar.  It is wise to feed a waking dragon.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_DRAGON;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 7;
            sReturn.nDefend = 7;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_DRUID:
            sReturn.sName = "Druid";
            sReturn.sDesc = "";
            sReturn.sGame = "When the druid comes into play, all other animals except for rats gain bonuses to their attack and defense.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_HUMAN;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 2;
            sReturn.nDefend = 4;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_DWARVEN_DEFENDER:
            sReturn.sName = "Dwarven Defender";
            sReturn.sDesc = "These proud warriors stand their ground firmly.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_DWARF;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 15;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 1;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_ELDER_FIRE_ELEMENTAL:
            sReturn.sName = "Elder Fire Elemental";
            sReturn.sDesc = "The Elder Fire Elemental is one of the most feared creatures in all the planes.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ELEMENTAL_FIRE;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 65;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 5;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_FAIRY_DRAGON:
            sReturn.sName = "Fairy Dragon";
            sReturn.sDesc = "These fickle creatures of the woods are known tricksters and best avoided.";
            sReturn.sGame = "While in play, drains a point of magic from both players' available magic generators." +
                            "If either player has no magic generators, then the Fairy Dragon will switch sides to attack the player with no generators." +
                            "If both players have no magic generators, then Fairy Dragon will self-sacrifice, and have its sacrifice effect played on both players." +
                            "\n\nSacrifice Fairy Dragon to automatically play a randomly selected card from your hand, with a magic cost of up to 2." +
                            "Any pumpable casting cost cards played in this manner will consume all magic available as though they had been cast normally, though their initial cost of one magic will be free.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_FEY;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 150;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_FERAL_RAT:
            sReturn.sName = "Feral Rat";
            sReturn.sDesc = "Feral rats are tougher and more vicious than their weaker kin.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_RAT;
            sReturn.nDeck = CARD_DECK_TYPE_RATS;
            sReturn.nCost = 30;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 3;
            sReturn.nAttack = 2;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_GIANT_SPIDER:
            sReturn.sName = "Giant Spider";
            sReturn.sDesc = "These enormous spiders have a poisonous bite and a rugged carapace.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_SPIDER;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS;
            sReturn.nCost = 5;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 3;
            sReturn.nAttack = 2;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_GOBLIN:
            sReturn.sName = "Goblin";
            sReturn.sDesc = "Goblins are the grunts of any army, plentiful and eager to fight.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOBLIN;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_GOBLIN_CROSSBOW:
            sReturn.sName = "Goblin Crossbow";
            sReturn.sDesc = "Goblin crossbows are smaller than their human varieties, though just as deadly.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOBLIN;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 2;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_GOBLIN_SHAMAN:
            sReturn.sName = "Goblin Shaman";
            sReturn.sDesc = "Goblin shaman are the long-range casters of the goblin horde.  Though weak, their spells can cause problems for advancing armies.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOBLIN;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 10;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_GOBLIN_WARLORD:
            sReturn.sName = "Goblin Warlord";
            sReturn.sDesc = "";
            sReturn.sGame = "When the goblin warlord comes into play, all other goblins gain bonuses to their attack and defense.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOBLIN;
            sReturn.nDeck = CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_GOBLIN_WITCHDOCTOR:
            sReturn.sName = "Goblin Witchdoctor";
            sReturn.sDesc = "Often scoffed at by those more knowlegable in the arts, the goblin witchdoctor has skills unknown in more learned circles.";
            sReturn.sGame = "Owner of the Witchdoctor may sacrifice the highest defense value creature he currently controls to the Witchdoctor to heal his avatar of a number of hit points equal to the maximum hit points of the sacrificed creature." +
                            "  This ability may not take the avatar above their maximum hit point total." +
                            "  This ability is controlled by selecting the Witchdoctor from the sacrifice placeable." +
                            "  Doing so does not destroy the Witchdoctor, but deals an amount of damage to the Witchdoctor equal to a half of the amount of healing provided.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOBLIN;
            sReturn.nDeck = CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 150;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 2;
            sReturn.nDefend = 2;
            sReturn.nSacrifice = TRUE;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_HILL_GIANT:
            sReturn.sName = "Hill Giant";
            sReturn.sDesc = "Hill Giants wield massive clubs and can soak up a huge amount of damage.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GIANT;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_GOBLINS;
            sReturn.nCost = 40;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 4;
            sReturn.nAttack = 4;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_HOOK_HORROR:
            sReturn.sName = "Hook Horror";
            sReturn.sDesc = "This freakish creature seeks to kill for pleasure whenever possible.";
            sReturn.sGame = "Owner of the Hook Horror may sacrifice the highest attack value creature he currently controls to the Hook Horror to deal an amount of damage equal to half the maximum hit points of the sacrificed creature to the opponent's avatar, and to the Hook Horror." +
                            "  This ability is controlled by selecting the Hook Horror from the sacrifice placeable." +
                            "  Doing so does not destroy the Hook Horror.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_DEMON;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 150;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 3;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_INTELLECT_DEVOURER:
            sReturn.sName = "Intellect Devourer";
            sReturn.sDesc = "These bizarre creatures feed off the thoughts of the living.";
            sReturn.sGame = "Sacrifice Intellect Devourer to remove four random cards from the opponents undrawn cards, and two random cards from your own undrawn cards.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ABERRATION;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 3;
            sReturn.nAttack = 1;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_KOBOLD:
            sReturn.sName = "Kobold";
            sReturn.sDesc = "Lowly grunts.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_KOBOLD;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_KOBOLDS;
            sReturn.nCost = 2;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_KOBOLD_CHIEF:
            sReturn.sName = "Kobold Chief";
            sReturn.sDesc = "";
            sReturn.sGame = "The Kobold Chief has an aura that grants any kobolds around him the effects of the Potion of Heroism spell.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_KOBOLD;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_KOBOLDS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 3;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_KOBOLD_ENGINEER:
            sReturn.sName = "Kobold Engineer";
            sReturn.sDesc = "";
            sReturn.sGame = "Lays down mines in periods of one game turn.  Carries four mines in total.  Mines will detonate five seconds after a non-kobold steps into their trigger zone.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_KOBOLD;
            sReturn.nDeck = CARD_DECK_TYPE_KOBOLDS;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 3;
            sReturn.nAttack = 0;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_KOBOLD_KAMIKAZE:
            sReturn.sName = "Kobold Kamikaze";
            sReturn.sDesc = "";
            sReturn.sGame = "Can be sacrificed to create a 3 point fireball effect.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_KOBOLD;
            sReturn.nDeck = CARD_DECK_TYPE_KOBOLDS;
            sReturn.nCost = 30;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 1;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_KOBOLD_POGOSTICK:
            sReturn.sName = "Kobold Pogostick";
            sReturn.sDesc = "";
            sReturn.sGame = "One game round after being brought into play, if still alive, will jump up into the air and land on either the enemy avatar, or the highest attack rating creature." +
                            "  Impact will kill the Kobold, but will deal a fixed amount of damage to the target.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_KOBOLD;
            sReturn.nDeck = CARD_DECK_TYPE_KOBOLDS;
            sReturn.nCost = 30;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 2;
            sReturn.nAttack = 0;
            sReturn.nDefend = 1;
            break;

        case CARD_SUMMON_LICH:
            sReturn.sName = "Lich";
            sReturn.sDesc = "When a necromancer takes the final step and transforms into a lich, they gain great powers over the undead.";
            sReturn.sGame = "When a lich enters play, all other undead gain bonuses to their attack and defense.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_LICH;
            sReturn.nDeck = CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 120;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_LOREMASTER:
            sReturn.sName = "Loremaster";
            sReturn.sDesc = "";
            sReturn.sGame = "Loremasters are useless in battle, but have the great power of allowing an additional draw per turn.  The down side is that they require the use of two magic generators to maintain their existence.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_HUMAN;
            sReturn.nCost = 140;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 0;
            sReturn.nDefend = 2;
            break;

        case CARD_SUMMON_MAIDEN_OF_PARADISE:
            sReturn.sName = "Maiden of Paradise";
            sReturn.sDesc = "";
            sReturn.sGame = "Useless in combat, a Maiden of Paradise can be sacrificed to produce three additional points of magic.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_HUMAN;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 200;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 2;
            sReturn.nAttack = 0;
            sReturn.nDefend = 1;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_PAIN_GOLEM:
            sReturn.sName = "Pain Golem";
            sReturn.sDesc = "";
            sReturn.sGame = "Pain golems have the unusual ability to cause damage directly to the player's avatar.  For every card over four they hold, your opponent will take two points of damage.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GOLEM;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 180;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 4;
            sReturn.nDefend = 4;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_PHASE_SPIDER:
            sReturn.sName = "Phase Spider";
            sReturn.sDesc = "";
            sReturn.sGame = "Phase Spider phases out of the game every other turn.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_SPIDER;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 3;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_PIT_FIEND:
            sReturn.sName = "Pit Fiend";
            sReturn.sDesc = "Pit Fiends are the most deadly creature possible, but the caster pays a heavy price bringing it into battle.";
            sReturn.sGame = "Every turn, they destroy a player's magic generator.  If there are none to destroy, it will switch sides and the opponent must pay the cost.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_DEMON;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 7;
            sReturn.nDefend = 7;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_PLAGUE_BEARER:
            sReturn.sName = "Plague Bearer";
            sReturn.sDesc = "";
            sReturn.sGame = "This abomination of a rat carries such a virulent plague that all non-rat creatures in the game will constantly take damage from this deforming disease.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_RAT;
            sReturn.nDeck = CARD_DECK_TYPE_RATS;
            sReturn.nCost = 35;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 4;
            sReturn.nAttack = 2;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_RAT:
            sReturn.sName = "Rat";
            sReturn.sDesc = "Though rats are easily dispatched, they have a nasty bite that can cause disease.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_RAT;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_RATS;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_RAT_KING:
            sReturn.sName = "Rat King";
            sReturn.sDesc = "";
            sReturn.sGame = "When the rat king comes into play, all other rats gain bonuses to their attack and defense.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_RAT;
            sReturn.nDeck = CARD_DECK_TYPE_RATS;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_REVENANT:
            sReturn.sName = "Revenant";
            sReturn.sDesc = "";
            sReturn.sGame = "Sacrifice revenant to kill the highest attack rating non-undead creature owned by the enemy." +
                            "  During upkeep cycle, if alive, drains life from the avatar." +
                            "  During upkeep, if in discard pile with one other creature card above it, automatically returns to play." +
                            "  Doing so destroys the creature card from the discard pile." +
                            "  If discarded from players hand, Revenant is removed from the game.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_UNDEAD;
            sReturn.nDeck = CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 150;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 5;
            sReturn.nDefend = 0;
            sReturn.nCombat = TRUE;
            sReturn.nHandDiscard = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_SEA_HAG:
            sReturn.sName = "Sea Hag";
            sReturn.sDesc = "Sea Hags are tricksters who dabble in dark magics, making them particularly effective against vermin of all kinds.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ABERRATION;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 30;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_SEWER_RAT:
            sReturn.sName = "Sewer Rat";
            sReturn.sDesc = "Sewer rats are one step above typical rats, a bit more fierce but vermin all the same.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_RAT;
            sReturn.nDeck = CARD_DECK_TYPE_RATS;
            sReturn.nCost = 3;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_SHADOW_ASSASSIN:
            sReturn.sName = "Shadow Assassin";
            sReturn.sDesc = "";
            sReturn.sGame = "The shadow assassin can be sacrificed to destroy the enemy's strongest creature.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_SHADE;
            sReturn.nDeck = CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 90;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 3;
            sReturn.nAttack = 4;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_SKELETAL_ARCHER:
            sReturn.sName = "Skeletal Archer";
            sReturn.sDesc = "Skeletal archers are frail, but quite accurate with their bows.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_SKELETON;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 3;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_SKELETAL_WARRIOR:
            sReturn.sName = "Skeletal Warrior";
            sReturn.sDesc = "Skeletal warriors are the grunts of any undead army.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_SKELETON;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_SPIRIT_GUARDIAN:
            sReturn.sName = "Spirit Guardian";
            sReturn.sDesc = "Spirit Guardians are employed to confuse or fend off the enemy, as they are insubstantial and difficult to hit, though doing no damage.";
            sReturn.sGame = "Spirit Guardians can be sacrificed to heal 30 points of damage to your avatar.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GUARDIAN;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 30;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 0;
            sReturn.nDefend = 4;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_STEEL_GUARDIAN:
            sReturn.sName = "Steel Guardian";
            sReturn.sDesc = "Steel Guardians are incredibly tough, soaking up huge amounts of damage, though clumsy in combat.";
            sReturn.sGame = "Steel Guardians radiate an aura that increases the defense of any nearby creatures, including your avatar.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_GUARDIAN;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 1;
            sReturn.nDefend = 5;
            break;

        case CARD_SUMMON_TROGLODYTE:
            sReturn.sName = "Troglodyte";
            sReturn.sDesc = "Troglodytes are cruel warriors, preferring to attack enemies in large groups.  They have a particular hatred for goblins and are especially effective against them.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ABERRATION;
            sReturn.nDeck = CARD_DECK_TYPE_FAST_CREATURES;
            sReturn.nCost = 20;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_UMBER_HULK:
            sReturn.sName = "Umber Hulk";
            sReturn.sDesc = "These subterranean beasts have a powerful shell and nasty bite, not to mention a stunning gaze.";
            sReturn.sGame = "The Umber Hulk can be sacrificed to briefly paralyze all opponents.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ABERRATION;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 150;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 2;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            sReturn.nSacrifice = TRUE;
            break;

        case CARD_SUMMON_VAMPIRE:
            sReturn.sName = "Vampire";
            sReturn.sDesc = "Vampires are known for their vicious bite and toughness.";
            sReturn.sGame = "Also, should a vampire succeed in killing their opponent, they will be greatly strengthened.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_VAMPIRE;
            sReturn.nDeck = CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 120;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 2;
            sReturn.nDefend = 2;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_VAMPIRE_MASTER:
            sReturn.sName = "Vampire Master";
            sReturn.sDesc = "";
            sReturn.sGame = "If a vampire master kills a creature, a vampire will be raised to join the fight.  There is a cost to this card, however.  Every turn, they drain a small measure of life from the avatar in order to feed his great hunger.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_VAMPIRE;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 250;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 5;
            sReturn.nDefend = 4;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_WHITE_STAG:
            sReturn.sName = "White Stag";
            sReturn.sDesc = "This mythical beast is the defender of the forests, powerful in spiritual magics.";
            sReturn.sGame = "";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ANIMAL;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_BIG_CREATURES;
            sReturn.nCost = 300;
            sReturn.nRarity = CARD_RARITY_VERY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 5;
            sReturn.nDefend = 4;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_WHITE_WOLF:
            sReturn.sName = "White Wolf";
            sReturn.sDesc = "";
            sReturn.sGame = "The white wolf enhances nearby wolves, making them quick and agile in combat.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_WOLF;
            sReturn.nDeck = CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 4;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_WOLF:
            sReturn.sName = "Wolf";
            sReturn.sDesc = "";
            sReturn.sGame = "These predators often hunt in packs, providing an attack bonus to others of their kind.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_WOLF;
            sReturn.nDeck = CARD_DECK_TYPE_ANIMALS +
                            CARD_DECK_TYPE_FAST_CREATURES +
                            CARD_DECK_TYPE_WOLVES;
            sReturn.nCost = 75;
            sReturn.nRarity = CARD_RARITY_UNCOMMON;
            sReturn.nMagic = 2;
            sReturn.nAttack = 2;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_ZOMBIE:
            sReturn.sName = "Zombie";
            sReturn.sDesc = "";
            sReturn.sGame = "Zombies, while frail and weak, have the uncanny ability to repair all damage to themselves after consuming the flesh of those they have killed.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ZOMBIE;
            sReturn.nDeck = CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 1;
            sReturn.nRarity = CARD_RARITY_COMMON;
            sReturn.nMagic = 1;
            sReturn.nAttack = 1;
            sReturn.nDefend = 1;
            sReturn.nCombat = TRUE;
            break;

        case CARD_SUMMON_ZOMBIE_LORD:
            sReturn.sName = "Zombie Lord";
            sReturn.sDesc = "";
            sReturn.sGame = "Zombie lords enhance the attack and defense of nearby zombies.  Furthermore, should they kill an enemy, they gain additional attack, defense, and hit points.";
            sReturn.nType = CARD_TYPE_SUMMON;
            sReturn.nSubType = CARD_SUBTYPE_SUMMON_ZOMBIE;
            sReturn.nDeck = CARD_DECK_TYPE_BIG_CREATURES +
                            CARD_DECK_TYPE_UNDEAD;
            sReturn.nCost = 100;
            sReturn.nRarity = CARD_RARITY_RARE;
            sReturn.nMagic = 5;
            sReturn.nAttack = 3;
            sReturn.nDefend = 3;
            sReturn.nCombat = TRUE;
            break;

        default:    sReturn = GetCustomCardInfo (nCard);    break;
    }

    return sReturn;
}
