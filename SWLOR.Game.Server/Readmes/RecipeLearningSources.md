# Recipe learning sources

Training Sabers and Training Saberstaffs use the existing DM-issued books `recipe_trnsabers` and `recipe_trnsabstf`. Each book teaches its five original tiers plus Field, Veteran, Prime, and Ascendant. The Jedi character restriction remains in place.

The existing stored-item and login item migrations also append missing tiers to previously issued books. Extra recipes added by DMs are preserved, and retrying the migration does not duplicate recipe IDs.

The older DM-issued books for Keebada's Binggona, Basilisk War Droid, Aurek Strikefighter, Sith Fighter, and Arkanian Dragon Armor intentionally have no loot, merchant, or quest source.

Droid resistance blueprints drop from the following existing named rare spawns. Each uses weight 1 and quantity 1 in that creature's rare loot table, matching the existing rare rewards. These sources are separate from the capstone quest bosses and wardens.

Both item value fields (`Cost` and `AddCost`) follow the existing droid enhancement blueprint tiers: 10,000 for rank I and 30,000 for rank II.

| Blueprint | Resref | Rare NPC | Loot table |
|---|---|---|---|
| Fire I | `recipe_dr_fir1` | Ascendant Flameweaver | `KORFORGE_FLAME_RARES` |
| Poison I | `recipe_dr_psn1` | Chem Slinger Ovo | `DANMED_CHEM_RARES` |
| Electrical I | `recipe_dr_elec1` | Overwatch Prime | `CZERKA_OVERWATCH_RARES` |
| Ice I | `recipe_dr_ice1` | Thermal Lancer Qel | `HUTQION_THERMAL_RARES` |
| Mind I | `recipe_dr_mnd1` | Hexcaller | `FIGHTCLUB_HEXCALLER_RARES` |
| Mobility I | `recipe_dr_mob1` | Quickdraw Sella | `FIGHTCLUB_QUICKDRAW_RARES` |
| Trauma I | `recipe_dr_tra1` | Ironjaw | `FIGHTCLUB_IRONJAW_RARES` |
| Disruption I | `recipe_dr_dis1` | Demolisher ZR-9 | `CZ220_DEMOLISHER_RARES` |
| Fire II | `recipe_dr_fir2` | Forgewright Malak-Kin | `KORFORGE_FORGE_RARES` |
| Poison II | `recipe_dr_psn2` | Rhydel Alpha-Matriarch | `DATHTARN_RHYDEL_RARES` |
| Electrical II | `recipe_dr_elec2` | Vrix-7, Pulse Butcher | `VISCARA_VRIX7_RARES` |
| Ice II | `recipe_dr_ice2` | Barrier Overseer | `HUTQION_BARRIER_RARES` |
| Mind II | `recipe_dr_mnd2` | Eclipse Shade | `KORCRYPT_ECLIPSE_RARES` |
| Mobility II | `recipe_dr_mob2` | Cyclone Adept | `DANENCLAVE_CYCLONE_RARES` |
| Trauma II | `recipe_dr_tra2` | Grotto Alpha | `DATHGROTTO_ALPHA_RARES` |
| Disruption II | `recipe_dr_dis2` | Dead Hand Zeph | `ANCHRANGE_DEADHAND_RARES` |

The module must be repacked when deploying these blueprint changes.
