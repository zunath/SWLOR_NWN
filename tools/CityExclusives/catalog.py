"""City-exclusive store items for the Player City Revamp (slice 4e): the one place the names, descriptions, copied stats and prices live.

The owner approved every name, description, price, weekly limit and unlock level below on 2026-11-03. Edit this file, run
`python tools/CityExclusives/generate.py`, and keep `CityStoreCatalog.cs` (which holds the unlock levels and weekly limits the server uses)
in step; `CityExclusiveItemTests` fails when the two differ.

Each city store sells a few items found nowhere else, limited by a weekly allowance per city and stock theme (CityStockAllowance).

* Food and drinks are copies of an existing dish or drink of the same tier (`copies`): same base item, model, icon and food bonus, so
  no stat is new. Only the name, description and price differ. A house drink copies the "au lait" of its tier, and the Cantina level it
  unlocks at is the tier it copies (level 1 = tier 1, 3 = tier 3, 5 = tier 5).
* Decor is a structure item (the shape of structure_5022, the space decor items) that places an existing placeable no other structure
  uses. No new art. Sold only by city stores, and placeable in houses and cities like any furniture.

Prices. Existing city-store comparables: a Cantina shot (cosmetic, no stats) is 249, the Decor stall's trinkets are 151, crafted
furniture is worth 100, a Mk III mining laser 400 and a Basic Bag 486. So food and drink cost 250 per tier (a tier 1 drink costs a shot),
small decor 250 and the larger, ornate pieces 400. The city's Sales tax is added on top at the store.
"""

# Structure ids: inside the City Revamp's 5190-5199 (this slice's sub-range).
STRUCTURE_START = 5190

FOOD_PRICE_PER_TIER = 250
DECOR_SMALL = 250
DECOR_LARGE = 400
DECOR_STORAGE = 1

# store is a CityStoreKind member name. level is the store level (city level for City Hall, building level for the others, 1 for a stall)
# from which the item is stocked. limit is how many the store sells a week.
FOODS = [
    dict(resref="house_citrus", name="House Citrus Cream", copies="o_aulait", tier=1, store="Cantina", level=1, limit=12,
         text="The house cream of a city cantina: orange pressed into fresh cream and served cold. Sold only at a city's Cantina."),
    dict(resref="house_sunrise", name="House Sunrise Cream", copies="pine_aulait", tier=3, store="Cantina", level=3, limit=8,
         text="Pineapple and herbs whipped into cream, a favorite of the early crowd. Sold only at a city's Cantina."),
    dict(resref="house_reserve", name="House Reserve Cream", copies="tomato_aulait", tier=5, store="Cantina", level=5, limit=5,
         text="A tomato and herb cream the bartender saves for regulars. Sold only at a city's Cantina."),
    dict(resref="stall_pie", name="Stallkeeper's Pot Pie", copies="cairn_potpie", tier=2, store="StallGeneralGoods", level=1, limit=10,
         text="A hand pie baked for the market stalls, a hot meal for the road. Sold only at a city's general goods stalls."),
]

DECOR = [
    dict(enum="CityBannerBlue", name="City Banner, Blue", placeable="swlor_0117", price=DECOR_SMALL, store="CityHall", level=1, limit=6,
         text="A blue civic banner to hang in a hall or a front room. Sold only at a city's City Hall."),
    dict(enum="CityBannerGold", name="City Banner, Gold", placeable="swlor_0118", price=DECOR_SMALL, store="CityHall", level=1, limit=6,
         text="A gold civic banner to hang in a hall or a front room. Sold only at a city's City Hall."),
    dict(enum="CivicWallCrest", name="Civic Wall Crest", placeable="swlor_0880", price=DECOR_LARGE, store="CityHall", level=2, limit=4,
         text="A decorative wall hanging that bears a city's crest, for a hall or a study. Sold only at a city's City Hall."),
    dict(enum="ArchiveCrest", name="Archive Crest", placeable="swlor_0121", price=DECOR_LARGE, store="CityHall", level=2, limit=4,
         text="A banner that bears the crest of a city archive, for a study or a library. Sold only at a city's City Hall."),
    dict(enum="ClinicDisplayPanel", name="Clinic Display Panel", placeable="swlor_0643", price=DECOR_SMALL, store="MedicalCenter", level=1, limit=4,
         text="A medical display panel of the kind a clinic keeps beside its beds. It is decoration only. Sold only at a city's Medical Center."),
    dict(enum="ClinicBactaTank", name="Clinic Bacta Tank", placeable="swlor_1417", price=DECOR_LARGE, store="MedicalCenter", level=3, limit=3,
         text="A bacta tank for a clinic or a lab. It is decoration only and heals nobody. Sold only at a city's Medical Center."),
    dict(enum="SpaceportSignpost", name="Spaceport Signpost", placeable="_mdrn_pl_signp14", price=DECOR_SMALL, store="Starport", level=1, limit=4,
         text="A signpost that points the way to the spaceport. Sold only at a city's Starport."),
    dict(enum="SpaceportWallSign", name="Spaceport Wall Sign", placeable="swlor_1336", price=DECOR_SMALL, store="Starport", level=3, limit=4,
         text="A wall sign that marks the spaceport. Sold only at a city's Starport."),
    dict(enum="PavilionBanner", name="Pavilion Banner", placeable="swlor_0124", price=DECOR_SMALL, store="StallDecor", level=1, limit=4,
         text="A banner of the kind that hangs over a festival pavilion. Sold only at a city's decor stalls."),
    dict(enum="GuildBanner", name="Artisans' Guild Banner", placeable="swlor_0120", price=DECOR_SMALL, store="StallCraftingSupplies", level=1, limit=4,
         text="A guild banner for a workshop wall. Sold only at a city's crafting supplies stalls."),
]


def structure_id(index):
    return STRUCTURE_START + index


def food_price(food):
    return FOOD_PRICE_PER_TIER * food["tier"]
