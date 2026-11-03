"""Planet identity content for the Player City Revamp (slice 5g): the one place the names, descriptions and copied numbers live.

EVERY NAME AND DESCRIPTION BELOW IS A PROPOSAL for the owner's lore review, and so is every recipe level, bill and tier. Edit this
file, run `python tools/PlanetIdentity/generate.py`, then the steps in generate.py's docstring; nothing here is hand-copied elsewhere.

Each of the seven planets with areas open to cities, and the player stations, has an identity set:

* one signature dish and one signature drink, Agriculture recipes cooked at a Cookpot (Kitchen minigame) that only work inside a city
  on that planet (or on a station). A dish or drink takes the stats, level, slots and item shape of an existing dish or drink of the
  same tier (`copies`), so no number is new, and its bill is farm crops plus livestock meat.
* four decor pieces, Fabrication furniture. Each places an existing static placeable no other structure uses, and takes the
  category, level, enhancement slots and bill of the existing furniture recipe named in `copies`, so no number is new.

Tier. A planet's content level is the mean level of its regular spawns on the Design Bible's World NPCs tab, rounded up to the
recipe tier bands (1-10, 11-20, 21-30, 31-40, 41-50): Viscara 18 (tier 2), Mon Cala 28 (tier 3), Korriban 36 (tier 4), and Hutlar 44,
Tatooine 44, Dantooine 46 and Dathomir 47 (tier 5). A station is tier 5, because a Station Core is a level 50 recipe.
"""

# Structure ids: one block of 8 for each set, inside the City Revamp's 5130-5189 (5190-5199 belong to the city-exclusive store decor). Recipe ids: dish and drink pairs from 8100, decor from
# 8120, so a decor piece's RecipeType is its StructureType - 5130 + 8120. Both ranges are inside the City Revamp's 8100-8199.
STRUCTURE_START = 5130
STRUCTURE_BLOCK = 8
SIGNATURE_RECIPE_START = 8100
DECOR_RECIPE_START = 8120

# A crafted structure item is worth what every crafted furniture item is (structure_0162).
DECOR_COST = 100
DECOR_STORAGE = 1

SETS = [
    dict(
        key="visc", prefix="Viscara", planet="Viscara", tier=2,
        dish=dict(
            enum="ViscaraOrchardRoast", name="Veles Orchard Roast", copies="CookedMackerel",
            text="Cairnmog roasted with orchard apples and peas, the supper Veles is known for.",
            components=[("cairnmog_meat", 2), ("v_apple", 2), ("v_peas", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="ViscaraCiderFizz", name="Orchard Cider Fizz", copies="AppleAuLait",
            text="Pressed Viscaran apples with a squeeze of passion fruit, served cold and fizzing.",
            components=[("v_apple", 3), ("passion_fruit", 1), ("distilled_water", 2)]),
        decor=[
            dict(enum="ViscaraFountainStatue", name="Veles Fountain Statue", placeable="aswtor_021", copies="StatueGuardian",
                 text="A marble figure like the one in the fountain of Veles' main square, cut down to stand in a hall or garden."),
            dict(enum="ViscaraStreetLamp", name="Veles Street Lamp", placeable="_mdrn_pl_strtlmp", copies="LampPost",
                 text="A cast street lamp of the kind that lights the old Veles boulevards."),
            dict(enum="ViscaraOrchardPlanter", name="Orchard Planter", placeable="swlor_0762", copies="PotUrnGrecian",
                 text="A long planter for the herbs and dwarf fruit trees Viscaran farmers keep by their doors."),
            dict(enum="ViscaraFernCluster", name="Viscaran Fern Cluster", placeable="aswtor_185", copies="VaseTall",
                 text="A lush clump of the broad ferns that crowd the Viscaran wildlands."),
        ]),
    dict(
        key="mcal", prefix="MonCala", planet="MonCala", tier=3,
        dish=dict(
            enum="MonCalaKelpChowder", name="Aradile Kelp Chowder", copies="DeepwaterBroth",
            text="A thick chowder of aradile and reef kelp, ladled out in the cities of Mon Cala.",
            components=[("aradile_meat", 2), ("herb_c", 2), ("veggie_clump", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="MonCalaPineappleFizz", name="Tidepool Pineapple Fizz", copies="PineappleJuice",
            text="Strange pineapple and a pinch of sea-greens, sparkling like a tidepool at noon.",
            components=[("s_pineapple", 3), ("herb_c", 1), ("distilled_water", 2)]),
        decor=[
            dict(enum="MonCalaTideFountain", name="Tide Fountain", placeable="swlor_0808", copies="Fountain",
                 text="A sleek fountain whose water rises and curls like a Mon Calamari wave-song."),
            dict(enum="MonCalaGiantShell", name="Giant Reef Shell", placeable="zep_pn_shl_007", copies="PotClayUrn",
                 text="An enormous angel's wing shell, hauled up from the reefs of Mon Cala."),
            dict(enum="MonCalaKoltoTank", name="Reef Kolto Tank", placeable="swlor_0724", copies="CylinderCrossTop",
                 text="A kolto tank in the style of the reef clinics, humming with a pale glow."),
            dict(enum="MonCalaKelpPlanter", name="Kelp Garden Planter", placeable="swlor_1002", copies="PotBushTall",
                 text="A strange planter of the kind Mon Calamari gardeners use to keep kelp and sea-grass."),
        ]),
    dict(
        key="korr", prefix="Korriban", planet="Korriban", tier=4,
        dish=dict(
            enum="KorribanAcolyteRoast", name="Acolyte's Roast", copies="RoastPipira",
            text="K'lor'slug roasted with mushrooms and ginger, the hard food of the Sith academy.",
            components=[("klorslug_meat", 3), ("mushroom", 2), ("ginger", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="KorribanGingerFizz", name="Dark Ginger Fizz", copies="MelonAuLait",
            text="Melon and sharp ginger, a drink with a bite for long nights in the valley.",
            components=[("melon", 3), ("ginger", 1), ("distilled_water", 2)]),
        decor=[
            dict(enum="KorribanSithObelisk", name="Sith Obelisk", placeable="_mdrn_pl_obelisk", copies="RunePillar",
                 text="A black obelisk carved with Sith glyphs, copied from the monuments of the Valley of the Dark Lords."),
            dict(enum="KorribanSithStandard", name="Sith Standard", placeable="_mdrn_pl_bannr05", copies="BannerStanding",
                 text="A standing banner in Sith colors, hung from a dark iron frame."),
            dict(enum="KorribanSithPedestal", name="Sith Pedestal", placeable="swlor_0838", copies="AltarEvil",
                 text="A carved pedestal for relics and trophies of the dark side."),
            dict(enum="KorribanTombBrazier", name="Tomb Brazier", placeable="swlor_1054", copies="BrazierRound",
                 text="A heavy brazier that burns with a low red flame, like those that light the tombs."),
        ]),
    dict(
        key="hutl", prefix="Hutlar", planet="Hutlar", tier=5,
        dish=dict(
            enum="HutlarClanStew", name="Clan Hearth Stew", copies="ShiningStew",
            text="Thune and acorns slow-cooked in one pot, ladled out at a clan's hearth.",
            components=[("thune_meat", 2), ("h_acorn", 2), ("turnip", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="HutlarAcornFizz", name="Warrior's Acorn Fizz", copies="TomatoJuice",
            text="Roasted Hutlar acorns and tomato, a sharp, nutty drink for the end of a march.",
            components=[("h_acorn", 2), ("tomato", 2), ("distilled_water", 2)]),
        decor=[
            dict(enum="HutlarArmorStand", name="Mandalorian Armor Stand", placeable="nwn2armorsta001", copies="StatueSenator",
                 text="A stand for a full set of Mandalorian armor, kept polished between wars."),
            dict(enum="HutlarRifleRack", name="Clan Rifle Rack", placeable="_mdrn_pl_wpnrck3", copies="WeaponRackWallMounted",
                 text="A rack that holds a clan's rifles in a neat row."),
            dict(enum="HutlarWeaponsCrate", name="Clan Weapons Crate", placeable="swd3_wepcr01", copies="ChestLengthwise",
                 text="A battered crate stamped with a clan's mark, sized for a squad's spare weapons."),
            dict(enum="HutlarHearthBrazier", name="Clan Hearth Brazier", placeable="zep_brazier001", copies="LampOnPoles",
                 text="A standing brazier where a clan gathers to eat, argue and sing."),
        ]),
    dict(
        key="tato", prefix="Tatooine", planet="Tatooine", tier=5,
        dish=dict(
            enum="TatooineWompPotPie", name="Womp Farmer's Pot Pie", copies="WildPotPie",
            text="Womp rat, turnip and desert sage under a thick crust, the meal at the end of a day in the dunes.",
            components=[("wompratmeat", 3), ("turnip", 2), ("herb_t", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="TatooineSageMelonJuice", name="Dune Sage Melon Juice", copies="TomatoJuice",
            text="Cold melon juice with a leaf of desert sage, as welcome as shade.",
            components=[("melon", 3), ("herb_t", 1), ("distilled_water", 1)]),
        decor=[
            dict(enum="TatooineMoistureVaporator", name="Moisture Vaporator", placeable="swlor_0539", copies="PillarDishTower",
                 text="A moisture vaporator, the machine every Tatooine farm lives on."),
            dict(enum="TatooineWaterBarrels", name="Water Barrels", placeable="swlor_1379", copies="StorageTankCylinder",
                 text="A pair of barrels of reclaimed water, the most valuable thing on a farm."),
            dict(enum="TatooineTuskenRifle", name="Tusken Rifle Mount", placeable="swlor_0503", copies="WeaponRackWallMounted",
                 text="A Tusken raider's rifle, mounted on a bracket as a trophy."),
            dict(enum="TatooineDesertStall", name="Desert Food Stall", placeable="swlor_0811", copies="WorkStationMonitors",
                 text="A canvas-roofed stall for selling roasted meat and fruit in the shade."),
        ]),
    dict(
        key="dant", prefix="Dantooine", planet="Dantooine", tier=5,
        dish=dict(
            enum="DantooinePlainsPie", name="Plains Harvest Pie", copies="LongLingLung",
            text="Thune, turnips and mushrooms baked in a pie, the dish of the Dantooine farms around Khoonda.",
            components=[("thune_meat", 2), ("turnip", 2), ("mushroom", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="DantooineCreamAuLait", name="Khoonda Cream Au Lait", copies="TomatoAuLait",
            text="Fresh plains milk and sun-ripe tomato, whipped to a pale pink cream.",
            components=[("milk", 2), ("tomato", 2), ("distilled_water", 2)]),
        decor=[
            dict(enum="DantooineEnclaveRuin", name="Enclave Ruin Block", placeable="swlor_1066", copies="OvergrownPillar",
                 text="A weathered block of stone, salvaged from the ruined Jedi Enclave."),
            dict(enum="DantooineDairyVat", name="Khoonda Dairy Vat", placeable="zep_basket002", copies="Urn",
                 text="A wooden vat for curdling the milk of the plains herds."),
            dict(enum="DantooineCropRows", name="Plains Crop Rows", placeable="daf_sw339", copies="PotPlantAloa",
                 text="A bed of grain stalks, standing as they do in the fields around Khoonda."),
            dict(enum="DantooineGrainSacks", name="Grain Sacks", placeable="swd_bursac01", copies="FootlockerBlack",
                 text="A stack of burlap sacks full of threshed grain."),
        ]),
    dict(
        key="dath", prefix="Dathomir", planet="Dathomir", tier=5,
        dish=dict(
            enum="DathomirCauldronStew", name="Nightsister Cauldron Stew", copies="PopperBowl",
            text="Thune, oregano and mushrooms simmered in a black cauldron, as the clans cook it.",
            components=[("thune_meat", 2), ("herb_x", 2), ("tomato", 1), ("mushroom", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="DathomirOreganoAuLait", name="Coven Oregano Au Lait", copies="TomatoAuLait",
            text="A strange, herbal cream of oregano and tomato, poured at coven gatherings.",
            components=[("herb_x", 2), ("tomato", 2), ("distilled_water", 2)]),
        decor=[
            dict(enum="DathomirNightsisterCauldron", name="Nightsister Cauldron", placeable="zep_cauldron001", copies="PotClayUrn",
                 text="A black cauldron of the kind Nightsister covens brew in."),
            dict(enum="DathomirRancorSkull", name="Rancor Skull", placeable="_mdrn_pl_alnskll", copies="MonsterStatue",
                 text="The skull of a rancor, big enough to sit in."),
            dict(enum="DathomirBoneTree", name="Bone Tree", placeable="_mdrn_pl_bonetr2", copies="RunePillar",
                 text="A tree of pale bone, lashed together as a clan totem."),
            dict(enum="DathomirCovenBrazier", name="Coven Brazier", placeable="_mdrn_pl_brazier", copies="BrazierRound",
                 text="A squat iron brazier where a coven burns herbs and bones."),
        ]),
    dict(
        key="stat", prefix="Spacer", planet=None, tier=5,
        dish=dict(
            enum="SpacerHydroStew", name="Hydroponic Spacer Stew", copies="ZoniBroth",
            text="A filling stew of station-grown mushrooms and greens, the staple of spacers on long watches.",
            components=[("wompratmeat", 2), ("mushroom", 2), ("veggie_clump", 1), ("distilled_water", 1)]),
        drink=dict(
            enum="SpacerMelonFizz", name="Zero-G Melon Fizz", copies="TomatoJuice",
            text="Melon and pineapple with extra bubbles, the drink spacers swear tastes better without gravity.",
            components=[("melon", 2), ("s_pineapple", 1), ("distilled_water", 2)]),
        decor=[
            dict(enum="SpacerCryoTank", name="Cryo Tank", placeable="_mdrn_pl_cryotk1", copies="KoltoTankEmpty",
                 text="A cryogenic tank, empty and humming, a spacer's reminder of the long haul."),
            dict(enum="SpacerHydroponicTrough", name="Hydroponic Trough", placeable="_mdrn_pl_hydrop1", copies="StorageTankCylinder",
                 text="A trough of nutrient gel where station crews grow their greens."),
            dict(enum="SpacerCargoStack", name="Cargo Crate Stack", placeable="aswtor_102", copies="StorageCargoContainer",
                 text="A stack of cargo crates strapped down for transit."),
            dict(enum="SpacerCrewLocker", name="Crew Locker", placeable="aswtor_126", copies="LockerMetalTrapezoid",
                 text="A steel locker, standard issue for a station crew."),
        ]),
]


def structure_id(set_index, piece_index):
    return STRUCTURE_START + set_index * STRUCTURE_BLOCK + piece_index


def decor_recipe_id(set_index, piece_index):
    return structure_id(set_index, piece_index) - STRUCTURE_START + DECOR_RECIPE_START


def signature_recipe_ids(set_index):
    """(dish, drink) RecipeType values."""
    return SIGNATURE_RECIPE_START + set_index * 2, SIGNATURE_RECIPE_START + set_index * 2 + 1


def place_name(entry):
    """The place's name in player text."""
    return {"MonCala": "Mon Cala"}.get(entry["planet"], entry["planet"]) if entry["planet"] else "a player station"
